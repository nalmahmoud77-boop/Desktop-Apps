using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace MediVault.Licensing;

public enum LicenseKeyError
{
    None,
    Empty,
    Malformed,
    BadSignature,
    UnknownVersion,
    UnknownTier
}

public static class LicenseKey
{
    private const byte FormatVersion = 0x01;
    private const int SignatureLengthBytes = 16; // truncated HMAC-SHA256
    private const int ChunkSize = 5;

    public static string Generate(LicenseTier tier, string issuedTo)
    {
        var info = new LicenseInfo(
            KeyId: Guid.NewGuid(),
            Tier: tier,
            IssuedTo: issuedTo ?? string.Empty,
            IssuedAtUtc: DateTime.UtcNow);
        return Format(info);
    }

    public static string Format(LicenseInfo info)
    {
        var payload = SerializePayload(info);
        var sig = ComputeSignature(payload);
        var raw = $"{ToBase32(payload)}.{ToBase32(sig)}";
        return Chunk(raw, ChunkSize);
    }

    public static bool TryParse(string? key, out LicenseInfo? info, out LicenseKeyError error)
    {
        info = null;
        if (string.IsNullOrWhiteSpace(key))
        {
            error = LicenseKeyError.Empty;
            return false;
        }

        var clean = StripFormatting(key);
        var dot = clean.IndexOf('.');
        if (dot <= 0 || dot == clean.Length - 1)
        {
            error = LicenseKeyError.Malformed;
            return false;
        }

        byte[] payload;
        byte[] sig;
        try
        {
            payload = FromBase32(clean[..dot]);
            sig = FromBase32(clean[(dot + 1)..]);
        }
        catch
        {
            error = LicenseKeyError.Malformed;
            return false;
        }

        if (sig.Length != SignatureLengthBytes)
        {
            error = LicenseKeyError.Malformed;
            return false;
        }

        var expected = ComputeSignature(payload);
        if (!CryptographicOperations.FixedTimeEquals(expected, sig))
        {
            error = LicenseKeyError.BadSignature;
            return false;
        }

        if (!TryDeserializePayload(payload, out info, out error))
            return false;

        error = LicenseKeyError.None;
        return true;
    }

    private static byte[] SerializePayload(LicenseInfo info)
    {
        var nameBytes = Encoding.UTF8.GetBytes(info.IssuedTo ?? string.Empty);
        if (nameBytes.Length > 255)
            nameBytes = nameBytes.AsSpan(0, 255).ToArray();

        var size = 1 + 1 + 16 + 8 + 1 + nameBytes.Length;
        var buf = new byte[size];
        var span = buf.AsSpan();

        span[0] = FormatVersion;
        span[1] = (byte)info.Tier;
        info.KeyId.TryWriteBytes(span.Slice(2, 16));
        BinaryPrimitives.WriteInt64BigEndian(
            span.Slice(18, 8),
            new DateTimeOffset(DateTime.SpecifyKind(info.IssuedAtUtc, DateTimeKind.Utc)).ToUnixTimeSeconds());
        span[26] = (byte)nameBytes.Length;
        nameBytes.CopyTo(span[27..]);

        return buf;
    }

    private static bool TryDeserializePayload(byte[] payload, out LicenseInfo? info, out LicenseKeyError error)
    {
        info = null;
        if (payload.Length < 27)
        {
            error = LicenseKeyError.Malformed;
            return false;
        }

        if (payload[0] != FormatVersion)
        {
            error = LicenseKeyError.UnknownVersion;
            return false;
        }

        var tierByte = payload[1];
        if (tierByte is not ((byte)LicenseTier.Monthly or (byte)LicenseTier.Yearly or (byte)LicenseTier.Lifetime))
        {
            error = LicenseKeyError.UnknownTier;
            return false;
        }

        var tier = (LicenseTier)tierByte;
        var keyId = new Guid(payload.AsSpan(2, 16));
        var issuedAt = DateTimeOffset.FromUnixTimeSeconds(
            BinaryPrimitives.ReadInt64BigEndian(payload.AsSpan(18, 8))).UtcDateTime;

        var nameLen = payload[26];
        if (payload.Length != 27 + nameLen)
        {
            error = LicenseKeyError.Malformed;
            return false;
        }
        var name = Encoding.UTF8.GetString(payload.AsSpan(27, nameLen));

        info = new LicenseInfo(keyId, tier, name, issuedAt);
        error = LicenseKeyError.None;
        return true;
    }

    private static byte[] ComputeSignature(byte[] payload)
    {
        Span<byte> hash = stackalloc byte[32];
        HMACSHA256.HashData(EmbeddedSecret.HmacKey, payload, hash);
        return hash[..SignatureLengthBytes].ToArray();
    }

    public static string Chunk(string s, int size)
    {
        if (size <= 0 || s.Length <= size) return s;
        var sb = new StringBuilder(s.Length + s.Length / size);
        for (var i = 0; i < s.Length; i++)
        {
            if (i > 0 && i % size == 0) sb.Append('-');
            sb.Append(s[i]);
        }
        return sb.ToString();
    }

    private static string StripFormatting(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var c in s)
            if (c != '-' && !char.IsWhiteSpace(c)) sb.Append(c);
        return sb.ToString();
    }

    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    private static string ToBase32(byte[] bytes)
    {
        if (bytes.Length == 0) return string.Empty;
        var sb = new StringBuilder((bytes.Length * 8 + 4) / 5);
        int buffer = 0, bitsLeft = 0;
        foreach (var b in bytes)
        {
            buffer = (buffer << 8) | b;
            bitsLeft += 8;
            while (bitsLeft >= 5)
            {
                bitsLeft -= 5;
                sb.Append(Base32Alphabet[(buffer >> bitsLeft) & 0x1F]);
            }
        }
        if (bitsLeft > 0)
            sb.Append(Base32Alphabet[(buffer << (5 - bitsLeft)) & 0x1F]);
        return sb.ToString();
    }

    private static byte[] FromBase32(string s)
    {
        if (string.IsNullOrEmpty(s)) return Array.Empty<byte>();
        var upper = s.ToUpperInvariant();
        var result = new List<byte>((upper.Length * 5) / 8);
        int buffer = 0, bitsLeft = 0;
        foreach (var c in upper)
        {
            var idx = Base32Alphabet.IndexOf(c);
            if (idx < 0) throw new FormatException("Invalid Base32 character.");
            buffer = (buffer << 5) | idx;
            bitsLeft += 5;
            if (bitsLeft >= 8)
            {
                bitsLeft -= 8;
                result.Add((byte)((buffer >> bitsLeft) & 0xFF));
            }
        }
        return result.ToArray();
    }
}
