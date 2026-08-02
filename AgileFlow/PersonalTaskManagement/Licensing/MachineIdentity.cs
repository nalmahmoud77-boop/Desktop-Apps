using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace PersonalTaskManagement.Licensing
{
    /// <summary>
    /// Derives a stable per-machine fingerprint from the Windows MachineGuid.
    /// MUST stay in sync with the LicenseKeyTool's GetMachineId().
    /// </summary>
    public static class MachineIdentity
    {
        private static readonly string _raw = ReadRawFingerprint();

        /// <summary>16 hex chars used for binding a license to this machine.</summary>
        public static string Id { get; } = ComputeId(_raw);

        /// <summary>32-byte key derived from the machine, used to HMAC tamper state.</summary>
        public static byte[] SecretKey { get; } = ComputeSecretKey(_raw);

        private static string ReadRawFingerprint()
        {
            try
            {
                using var k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
                if (k?.GetValue("MachineGuid") is string guid && !string.IsNullOrWhiteSpace(guid))
                    return guid;
            }
            catch
            {
                // fall through to hostname
            }
            return Environment.MachineName;
        }

        private static string ComputeId(string raw)
        {
            using var sha = SHA256.Create();
            byte[] h = sha.ComputeHash(Encoding.UTF8.GetBytes("AGF|" + raw));
            var sb = new StringBuilder(16);
            for (int i = 0; i < 8; i++) sb.Append(h[i].ToString("X2"));
            return sb.ToString();
        }

        private static byte[] ComputeSecretKey(string raw)
        {
            using var sha = SHA256.Create();
            return sha.ComputeHash(Encoding.UTF8.GetBytes("AgileFlow.tamper.v1|" + raw));
        }
    }
}
