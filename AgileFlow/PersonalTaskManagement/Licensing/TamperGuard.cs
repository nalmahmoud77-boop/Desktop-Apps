using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace PersonalTaskManagement.Licensing
{
    /// <summary>
    /// Defeats "just move the system clock back" attacks on time-limited licenses.
    ///
    /// It keeps a monotonic high-water timestamp — the latest time the app has ever
    /// seen — in two independent places (registry + a hidden file), each stamped with
    /// an HMAC derived from the machine. The "effective" clock never goes backwards:
    ///     effectiveNow = max(systemClock, highWaterMark)
    /// so once a term has elapsed it stays elapsed even if the user rewinds the clock,
    /// and a rollback cannot extend a subscription. Both stores are cross-checked and
    /// the greatest trusted value wins, so tampering with one store is not enough.
    /// </summary>
    public sealed class TamperGuard
    {
        private const string RegSubKey = @"Software\AgileFlow";
        private const string RegValueName = "Sync";
        private const long RollbackToleranceSeconds = 24 * 60 * 60; // 1 day of slack

        private static readonly string StateFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AgileFlow", ".sync");

        public DateTime EffectiveUtcNow { get; private set; } = DateTime.UtcNow;

        /// <summary>True if the system clock is meaningfully behind the high-water mark.</summary>
        public bool ClockRolledBack { get; private set; }

        /// <summary>Read the trusted clock, detect rollback, and advance the high-water mark.</summary>
        public void Initialize()
        {
            long stored = Math.Max(ReadRegistry(), ReadFile());
            long system = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            ClockRolledBack = stored > 0 && system < stored - RollbackToleranceSeconds;

            long effective = Math.Max(system, stored);
            EffectiveUtcNow = DateTimeOffset.FromUnixTimeSeconds(effective).UtcDateTime;

            // Never let the mark regress.
            Advance(effective);
        }

        /// <summary>Push the high-water mark forward to at least the effective clock.</summary>
        public void Advance(long? unixSeconds = null)
        {
            long value = unixSeconds ?? Math.Max(
                DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                new DateTimeOffset(EffectiveUtcNow, TimeSpan.Zero).ToUnixTimeSeconds());

            long current = Math.Max(ReadRegistry(), ReadFile());
            if (value < current) value = current;

            WriteRegistry(value);
            WriteFile(value);
        }

        // ----------------- storage: registry -----------------

        private long ReadRegistry()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RegSubKey);
                return Parse(key?.GetValue(RegValueName) as string);
            }
            catch { return 0; }
        }

        private void WriteRegistry(long value)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(RegSubKey);
                key?.SetValue(RegValueName, Stamp(value), RegistryValueKind.String);
            }
            catch { /* best effort */ }
        }

        // ----------------- storage: hidden file -----------------

        private long ReadFile()
        {
            try
            {
                if (!File.Exists(StateFilePath)) return 0;
                return Parse(File.ReadAllText(StateFilePath));
            }
            catch { return 0; }
        }

        private void WriteFile(long value)
        {
            try
            {
                var dir = Path.GetDirectoryName(StateFilePath)!;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(StateFilePath, Stamp(value));
                File.SetAttributes(StateFilePath, FileAttributes.Hidden);
            }
            catch { /* best effort */ }
        }

        // ----------------- HMAC-stamped records -----------------

        // record = "{unix}.{hmacHex}"  where hmac = HMACSHA256(machineKey, unix)
        private static string Stamp(long value)
        {
            using var hmac = new HMACSHA256(MachineIdentity.SecretKey);
            byte[] mac = hmac.ComputeHash(Encoding.UTF8.GetBytes(value.ToString()));
            return value + "." + Convert.ToHexString(mac);
        }

        private static long Parse(string? record)
        {
            if (string.IsNullOrWhiteSpace(record)) return 0;
            int dot = record.IndexOf('.');
            if (dot <= 0) return 0;

            string valuePart = record.Substring(0, dot);
            string macPart = record.Substring(dot + 1);
            if (!long.TryParse(valuePart, out long value) || value < 0) return 0;

            using var hmac = new HMACSHA256(MachineIdentity.SecretKey);
            byte[] expected = hmac.ComputeHash(Encoding.UTF8.GetBytes(valuePart));
            byte[] actual;
            try { actual = Convert.FromHexString(macPart); }
            catch { return 0; }

            // Reject records that were edited or copied from another machine.
            return CryptographicOperations.FixedTimeEquals(expected, actual) ? value : 0;
        }
    }
}
