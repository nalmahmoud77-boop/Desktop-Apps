using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediVault.Licensing;

public sealed class LicenseService : ILicenseService
{
    private static readonly TimeSpan ClockBackTolerance = TimeSpan.FromHours(24);

    private static readonly byte[] AesKeyPepper =
    {
        0x91, 0x4E, 0xC0, 0x37, 0x6D, 0xA2, 0x5B, 0xE8,
        0x12, 0xF9, 0x84, 0x4D, 0x21, 0x76, 0xCB, 0x05
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IMachineFingerprintProvider _fingerprint;
    private readonly string _primaryPath;
    private readonly string _secondaryPath;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public LicenseSnapshot? Current { get; private set; }
    public event EventHandler<LicenseSnapshot?>? StatusChanged;

    public LicenseService(IMachineFingerprintProvider fingerprint)
    {
        _fingerprint = fingerprint;
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _primaryPath = Path.Combine(local, "MediVault", "license", "primary.dat");
        _secondaryPath = Path.Combine(roaming, "MediVault", "license", "secondary.dat");
    }

    public async Task<LicenseSnapshot?> RefreshAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var (state, recoveredFromSecondary) = await ReadStateAsync(ct).ConfigureAwait(false);
            if (state == null)
                return SetCurrent(null);

            var snapshot = ComputeSnapshot(state);

            if (snapshot.Status == LicenseStatus.Active)
            {
                var now = DateTime.UtcNow;
                if (now > state.LastSeenAtUtc)
                {
                    state = state with { LastSeenAtUtc = now };
                    await WriteStateAsync(state, ct).ConfigureAwait(false);
                }
            }
            else if (recoveredFromSecondary)
            {
                await WriteStateAsync(state, ct).ConfigureAwait(false);
            }

            return SetCurrent(snapshot);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ActivationResult> ActivateAsync(string key, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return ActivationResult.Empty;

        if (!LicenseKey.TryParse(key, out var info, out var parseError) || info == null)
        {
            return parseError switch
            {
                LicenseKeyError.Empty => ActivationResult.Empty,
                LicenseKeyError.BadSignature => ActivationResult.BadSignature,
                LicenseKeyError.UnknownVersion => ActivationResult.UnknownVersion,
                LicenseKeyError.UnknownTier => ActivationResult.UnknownTier,
                _ => ActivationResult.Malformed
            };
        }

        var now = DateTime.UtcNow;
        var expires = info.ComputeExpiryUtc(now);
        if (expires.HasValue && expires.Value <= now)
            return ActivationResult.AlreadyExpired;

        var state = new LicenseStatePayload
        {
            Info = info,
            Key = key.Trim(),
            ActivatedAtUtc = now,
            LastSeenAtUtc = now
        };

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            try
            {
                await WriteStateAsync(state, ct).ConfigureAwait(false);
            }
            catch
            {
                return ActivationResult.StorageFailed;
            }

            SetCurrent(ComputeSnapshot(state));
            return ActivationResult.Ok;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task DeactivateAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            TryDeleteFile(_primaryPath);
            TryDeleteFile(_secondaryPath);
            SetCurrent(null);
        }
        finally
        {
            _gate.Release();
        }
        await Task.CompletedTask;
    }

    private LicenseSnapshot? SetCurrent(LicenseSnapshot? snapshot)
    {
        var changed = !Equals(Current, snapshot);
        Current = snapshot;
        if (changed) StatusChanged?.Invoke(this, snapshot);
        return snapshot;
    }

    private LicenseSnapshot ComputeSnapshot(LicenseStatePayload state)
    {
        var now = DateTime.UtcNow;
        var expires = state.Info.ComputeExpiryUtc(state.ActivatedAtUtc);

        LicenseStatus status;
        if (now + ClockBackTolerance < state.LastSeenAtUtc)
            status = LicenseStatus.Tampered;
        else if (expires.HasValue && expires.Value <= now)
            status = LicenseStatus.Expired;
        else
            status = LicenseStatus.Active;

        return new LicenseSnapshot(state.Info, state.Key, state.ActivatedAtUtc, expires, status);
    }

    private async Task<(LicenseStatePayload? state, bool recoveredFromSecondary)> ReadStateAsync(CancellationToken ct)
    {
        var key = DeriveAesKey();

        var primary = await TryReadFileAsync(_primaryPath, key, ct).ConfigureAwait(false);
        var secondary = await TryReadFileAsync(_secondaryPath, key, ct).ConfigureAwait(false);

        if (primary != null && secondary != null)
            return (primary.LastSeenAtUtc >= secondary.LastSeenAtUtc ? primary : secondary, false);

        if (primary != null) return (primary, false);
        if (secondary != null) return (secondary, true);
        return (null, false);
    }

    private async Task WriteStateAsync(LicenseStatePayload state, CancellationToken ct)
    {
        var key = DeriveAesKey();
        var blob = Encrypt(state, key);
        await WriteFileAtomicAsync(_primaryPath, blob, ct).ConfigureAwait(false);
        await WriteFileAtomicAsync(_secondaryPath, blob, ct).ConfigureAwait(false);
    }

    private byte[] DeriveAesKey()
    {
        var fp = Encoding.UTF8.GetBytes(_fingerprint.GetFingerprint());
        var buf = new byte[fp.Length + AesKeyPepper.Length];
        Buffer.BlockCopy(fp, 0, buf, 0, fp.Length);
        Buffer.BlockCopy(AesKeyPepper, 0, buf, fp.Length, AesKeyPepper.Length);
        return SHA256.HashData(buf);
    }

    private static async Task<LicenseStatePayload?> TryReadFileAsync(string path, byte[] key, CancellationToken ct)
    {
        try
        {
            if (!File.Exists(path)) return null;
            var blob = await File.ReadAllBytesAsync(path, ct).ConfigureAwait(false);
            return Decrypt(blob, key);
        }
        catch
        {
            return null;
        }
    }

    private static byte[] Encrypt(LicenseStatePayload state, byte[] key)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(state, JsonOptions);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var ciphertext = new byte[json.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(key, 16);
        aes.Encrypt(nonce, json, ciphertext, tag);

        var blob = new byte[12 + 16 + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, blob, 0, 12);
        Buffer.BlockCopy(tag, 0, blob, 12, 16);
        Buffer.BlockCopy(ciphertext, 0, blob, 28, ciphertext.Length);
        return blob;
    }

    private static LicenseStatePayload? Decrypt(byte[] blob, byte[] key)
    {
        if (blob.Length < 28) return null;
        var nonce = blob.AsSpan(0, 12);
        var tag = blob.AsSpan(12, 16);
        var ciphertext = blob.AsSpan(28);
        var plaintext = new byte[ciphertext.Length];

        try
        {
            using var aes = new AesGcm(key, 16);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
        }
        catch
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<LicenseStatePayload>(plaintext, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static async Task WriteFileAtomicAsync(string path, byte[] data, CancellationToken ct)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var tmp = path + ".tmp";
        await File.WriteAllBytesAsync(tmp, data, ct).ConfigureAwait(false);
        try
        {
            File.Move(tmp, path, overwrite: true);
        }
        catch
        {
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }
    }

    private static void TryDeleteFile(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* best effort */ }
    }

    private sealed record LicenseStatePayload
    {
        public LicenseInfo Info { get; init; } = default!;
        public string Key { get; init; } = string.Empty;
        public DateTime ActivatedAtUtc { get; init; }
        public DateTime LastSeenAtUtc { get; init; }
    }
}
