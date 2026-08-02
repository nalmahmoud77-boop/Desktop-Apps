using System;
using System.Security.Cryptography;
using System.Text;

namespace PersonalTaskManagement.Licensing
{
    /// <summary>
    /// Parses and cryptographically verifies AgileFlow license keys.
    ///
    /// The app embeds only the PUBLIC key, so keys cannot be forged without the
    /// vendor's private signing key (held by the LicenseKeyTool).
    ///
    /// FORMAT (v1) — must stay in sync with tools/LicenseKeyTool/Program.cs:
    ///     payload   = "AGF1|{id}|{TIER}|{issuedUnix}|{expiresUnix}|{machineId}"
    ///     key       = base64url(payloadUtf8) + "." + base64url(signature)
    ///     signature = ECDSA P-256 SignData(payload, SHA-256)  [IEEE P1363]
    /// </summary>
    public static class LicenseKey
    {
        // ECDSA P-256 public key (SubjectPublicKeyInfo, base64). Verification only.
        public const string PublicKeyB64 =
            "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE5YrlYPUj+Zzg6RBnQvxPQbImjSdgEdNQNZN7qQEDQFkck6OfU7sUVbFBppLcFovA4vMvaW7WKcNkAk+fSbRKrg==";

        public static bool TryParse(string? rawKey, out LicenseDetails? details, out LicenseState state)
        {
            details = null;
            state = LicenseState.Malformed;
            if (string.IsNullOrWhiteSpace(rawKey)) return false;

            // Tolerate whitespace/line breaks introduced by copy-paste.
            string key = new string(rawKey.Where(c => !char.IsWhiteSpace(c)).ToArray());

            int dot = key.IndexOf('.');
            if (dot <= 0 || dot >= key.Length - 1) return false;

            byte[] payloadBytes, sig;
            try
            {
                payloadBytes = FromB64Url(key.Substring(0, dot));
                sig = FromB64Url(key.Substring(dot + 1));
            }
            catch
            {
                return false;
            }

            // Verify the signature before trusting any field.
            if (!VerifySignature(payloadBytes, sig))
            {
                state = LicenseState.InvalidSignature;
                return false;
            }

            string payload = Encoding.UTF8.GetString(payloadBytes);
            string[] p = payload.Split('|');
            if (p.Length != 6 || p[0] != "AGF1") return false;

            LicenseTier tier = p[2] switch
            {
                "MONTHLY" => LicenseTier.Monthly,
                "YEARLY" => LicenseTier.Yearly,
                "FULL" => LicenseTier.Full,
                _ => (LicenseTier)(-1)
            };
            if ((int)tier < 0) return false;

            if (!long.TryParse(p[3], out long issued)) return false;
            if (!long.TryParse(p[4], out long expires)) return false;

            details = new LicenseDetails
            {
                Id = p[1],
                Tier = tier,
                IssuedUtc = DateTimeOffset.FromUnixTimeSeconds(issued).UtcDateTime,
                ExpiresUtc = expires == 0 ? null : DateTimeOffset.FromUnixTimeSeconds(expires).UtcDateTime,
                MachineId = p[5]
            };
            state = LicenseState.Valid;
            return true;
        }

        private static bool VerifySignature(byte[] payload, byte[] signature)
        {
            try
            {
                using var ecdsa = ECDsa.Create();
                ecdsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(PublicKeyB64), out _);
                return ecdsa.VerifyData(payload, signature, HashAlgorithmName.SHA256);
            }
            catch
            {
                return false;
            }
        }

        private static byte[] FromB64Url(string s)
        {
            string b64 = s.Replace('-', '+').Replace('_', '/');
            switch (b64.Length % 4)
            {
                case 2: b64 += "=="; break;
                case 3: b64 += "="; break;
            }
            return Convert.FromBase64String(b64);
        }
    }
}
