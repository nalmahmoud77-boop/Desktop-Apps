using System.Security.Cryptography;

namespace StoreApp.Licensing
{
    public static class LicenseKey
    {
        public const string Prefix = "STORE";

        // Key shape: STORE-XX-XXXXXXXX-XXXXXXXX
        //   parts[0] = "STORE"
        //   parts[1] = tier code (MM | YY | LT)
        //   parts[2] = 8-char random
        //   parts[3] = 8-char HMAC-SHA256 checksum of the prefix
        private const int RandomChunkLength = 8;
        private const int ChecksumLength = 8;
        private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        public static string Generate(LicenseTier tier)
        {
            var code = TierCode(tier);
            var random = RandomChunk(RandomChunkLength);
            var prefix = $"{Prefix}-{code}-{random}";
            return $"{prefix}-{Checksum(prefix)}";
        }

        public static bool TryParse(string? key, out LicenseTier tier)
        {
            tier = default;
            if (string.IsNullOrWhiteSpace(key)) return false;

            var clean = key.Trim().ToUpperInvariant();
            var parts = clean.Split('-');
            if (parts.Length != 4) return false;
            if (parts[0] != Prefix) return false;
            if (parts[2].Length != RandomChunkLength) return false;
            if (parts[3].Length != ChecksumLength) return false;

            tier = parts[1] switch
            {
                "MM" => LicenseTier.Monthly,
                "YY" => LicenseTier.Yearly,
                "LT" => LicenseTier.Lifetime,
                _ => (LicenseTier)0
            };
            if ((int)tier == 0) return false;

            var expected = Checksum($"{parts[0]}-{parts[1]}-{parts[2]}");
            return CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.ASCII.GetBytes(expected),
                System.Text.Encoding.ASCII.GetBytes(parts[3]));
        }

        public static string Normalize(string key) => (key ?? string.Empty).Trim().ToUpperInvariant();

        private static string TierCode(LicenseTier tier) => tier switch
        {
            LicenseTier.Monthly => "MM",
            LicenseTier.Yearly => "YY",
            LicenseTier.Lifetime => "LT",
            _ => throw new ArgumentOutOfRangeException(nameof(tier))
        };

        private static string Checksum(string prefix)
        {
            var hash = LicenseSecret.Hmac(prefix);
            return Convert.ToHexString(hash).Substring(0, ChecksumLength);
        }

        private static string RandomChunk(int length)
        {
            var bytes = RandomNumberGenerator.GetBytes(length);
            var chars = new char[length];
            for (int i = 0; i < length; i++) chars[i] = Alphabet[bytes[i] % Alphabet.Length];
            return new string(chars);
        }
    }
}
