using System.Security.Cryptography;
using System.Text.Json;

namespace StoreApp.LicenseIssuer
{
    public class LicensePayload
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;
        public string Plan { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public static class LicenseToken
    {
        private const string Header = "STORE1";

        public static string Encode(LicensePayload payload, ECDsa privateKey)
        {
            var json = JsonSerializer.SerializeToUtf8Bytes(payload);
            var sig = privateKey.SignData(
                json,
                HashAlgorithmName.SHA256,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
            return $"{Header}.{Base64Url(json)}.{Base64Url(sig)}";
        }

        private static string Base64Url(byte[] data) =>
            Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
