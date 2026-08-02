using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;
using StoreApp.Licensing;

namespace StoreApp.Services
{
    public class LicenseService : ILicenseService
    {
        // Tolerance window for clock-back detection. We allow normal clock drift /
        // timezone changes but reject any leap backwards beyond this from the
        // last-seen timestamp.
        private static readonly TimeSpan ClockBackTolerance = TimeSpan.FromHours(1);

        private static readonly string StoreDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StoreApp");
        private static readonly string PrimaryPath = Path.Combine(StoreDir, "license.dat");
        private static readonly string BackupPath = Path.Combine(StoreDir, "license.bak");

        // Pepper mixed with the machine fingerprint to derive the AES-GCM key.
        // Different from the HMAC secret in StoreApp.Licensing on purpose.
        private static readonly byte[] Pepper =
        {
            0x4E, 0xD1, 0x0A, 0x77, 0x9C, 0x36, 0xB2, 0x58,
            0xF1, 0x6E, 0x83, 0x21, 0xCB, 0x47, 0x90, 0x5D,
            0x12, 0xA8, 0xE7, 0x3F, 0x66, 0x84, 0x0B, 0xD9,
            0x7C, 0x52, 0xAF, 0x18, 0x35, 0xE9, 0x60, 0x4B
        };

        private readonly byte[] _aesKey;
        private LicenseInfo? _current;
        private bool _clockTampered;

        public LicenseService()
        {
            _aesKey = DeriveAesKey();
            _current = LoadFromAnyStore();
            CheckClock(DateTime.UtcNow);
            if (_current != null && !_clockTampered) TouchLastSeen();
        }

        public LicenseInfo? Current => _current;

        public bool IsValid => !_clockTampered && _current != null && _current.IsValid(DateTime.UtcNow);

        public int? DaysRemaining => _current?.DaysRemaining(DateTime.UtcNow);

        public string StatusText
        {
            get
            {
                if (_clockTampered) return "Suspended — system clock changed";
                if (_current == null) return "Not activated";
                if (!_current.IsValid(DateTime.UtcNow)) return $"{_current.PlanName} — Expired";
                if (_current.IsLifetime) return "Lifetime — Active";
                var d = DaysRemaining ?? 0;
                return $"{_current.PlanName} — {d} day{(d == 1 ? "" : "s")} left";
            }
        }

        public event Action? StatusChanged;

        public LicenseInfo Activate(string key)
        {
            if (!LicenseKey.TryParse(key, out var tier))
                throw new InvalidOperationException("Invalid license key.");

            var info = LicenseInfo.Issue(tier, key, DateTime.UtcNow);
            Persist(info);
            _current = info;
            _clockTampered = false;
            StatusChanged?.Invoke();
            return info;
        }

        public void Deactivate()
        {
            TryDelete(PrimaryPath);
            TryDelete(BackupPath);
            _current = null;
            _clockTampered = false;
            StatusChanged?.Invoke();
        }

        public void Refresh()
        {
            var reloaded = LoadFromAnyStore();
            _current = reloaded;
            _clockTampered = false;
            CheckClock(DateTime.UtcNow);
            if (_current != null && !_clockTampered) TouchLastSeen();
            StatusChanged?.Invoke();
        }

        private void CheckClock(DateTime nowUtc)
        {
            if (_current == null) return;
            if (nowUtc + ClockBackTolerance < _current.LastSeenUtc) _clockTampered = true;
            if (nowUtc + ClockBackTolerance < _current.IssuedAt) _clockTampered = true;
        }

        private void TouchLastSeen()
        {
            if (_current == null) return;
            _current.LastSeenUtc = DateTime.UtcNow;
            try { Persist(_current); } catch { }
        }

        private LicenseInfo? LoadFromAnyStore()
        {
            var primary = TryLoad(PrimaryPath);
            var backup = TryLoad(BackupPath);
            var winner = primary ?? backup;
            if (winner == null) return null;

            // Heal whichever copy is missing or out-of-sync.
            try
            {
                if (primary == null || !RecordsMatch(primary, winner)) WriteEnvelope(PrimaryPath, winner);
                if (backup == null || !RecordsMatch(backup, winner)) WriteEnvelope(BackupPath, winner);
            }
            catch { }

            return winner;
        }

        private LicenseInfo? TryLoad(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                var raw = File.ReadAllBytes(path);
                return Decrypt(raw);
            }
            catch
            {
                return null;
            }
        }

        private void Persist(LicenseInfo info)
        {
            Directory.CreateDirectory(StoreDir);
            WriteEnvelope(PrimaryPath, info);
            WriteEnvelope(BackupPath, info);
        }

        private void WriteEnvelope(string path, LicenseInfo info)
        {
            var bytes = Encrypt(info);
            var tmp = path + ".tmp";
            File.WriteAllBytes(tmp, bytes);
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }

        private byte[] Encrypt(LicenseInfo info)
        {
            var json = JsonSerializer.SerializeToUtf8Bytes(info);
            var nonce = RandomNumberGenerator.GetBytes(12);
            var cipher = new byte[json.Length];
            var tag = new byte[16];
            using var aes = new AesGcm(_aesKey, tag.Length);
            aes.Encrypt(nonce, json, cipher, tag);

            var output = new byte[nonce.Length + tag.Length + cipher.Length];
            Buffer.BlockCopy(nonce, 0, output, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, output, nonce.Length, tag.Length);
            Buffer.BlockCopy(cipher, 0, output, nonce.Length + tag.Length, cipher.Length);
            return output;
        }

        private LicenseInfo? Decrypt(byte[] envelope)
        {
            if (envelope.Length < 12 + 16 + 1) return null;
            var nonce = envelope.AsSpan(0, 12);
            var tag = envelope.AsSpan(12, 16);
            var cipher = envelope.AsSpan(28);
            var plain = new byte[cipher.Length];
            using var aes = new AesGcm(_aesKey, tag.Length);
            aes.Decrypt(nonce, cipher, tag, plain);
            return JsonSerializer.Deserialize<LicenseInfo>(plain);
        }

        private static bool RecordsMatch(LicenseInfo a, LicenseInfo b) =>
            a.Key == b.Key && a.Tier == b.Tier &&
            a.IssuedAt == b.IssuedAt && a.ExpiresAt == b.ExpiresAt &&
            a.LastSeenUtc == b.LastSeenUtc;

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }

        private static byte[] DeriveAesKey()
        {
            var fingerprint = MachineFingerprint();
            var combined = new byte[fingerprint.Length + Pepper.Length];
            Buffer.BlockCopy(fingerprint, 0, combined, 0, fingerprint.Length);
            Buffer.BlockCopy(Pepper, 0, combined, fingerprint.Length, Pepper.Length);
            return SHA256.HashData(combined);
        }

        private static byte[] MachineFingerprint()
        {
            var sb = new StringBuilder();
            sb.Append(ReadMachineGuid());
            sb.Append('|');
            sb.Append(Environment.MachineName);
            sb.Append('|');
            sb.Append(Environment.OSVersion.Platform);
            sb.Append('|');
            sb.Append(Environment.ProcessorCount);
            return SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        }

        private static string ReadMachineGuid()
        {
            try
            {
                using var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                    .OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
                return key?.GetValue("MachineGuid") as string ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
