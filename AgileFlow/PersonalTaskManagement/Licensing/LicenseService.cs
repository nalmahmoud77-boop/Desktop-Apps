using System;
using System.IO;
using Microsoft.Win32;

namespace PersonalTaskManagement.Licensing
{
    /// <summary>
    /// Loads/stores the activated key and evaluates it against this machine and the
    /// tamper-guarded clock. This is the single entry point the app uses.
    /// </summary>
    public sealed class LicenseService
    {
        private const string RegSubKey = @"Software\AgileFlow";
        private const string RegValueName = "Lic";

        private static readonly string KeyFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AgileFlow", "license.key");

        private readonly TamperGuard _guard;

        public LicenseService(TamperGuard guard) => _guard = guard;

        /// <summary>Evaluate the currently stored license (if any).</summary>
        public LicenseEvaluation Evaluate()
        {
            string? key = LoadStoredKey();
            if (string.IsNullOrWhiteSpace(key))
                return new LicenseEvaluation { State = LicenseState.NotActivated, Message = "No license activated yet." };

            return EvaluateKey(key);
        }

        /// <summary>Validate an arbitrary key string without storing it.</summary>
        public LicenseEvaluation EvaluateKey(string key)
        {
            if (!LicenseKey.TryParse(key, out LicenseDetails? details, out LicenseState parseState))
            {
                string msg = parseState == LicenseState.InvalidSignature
                    ? "This license key failed verification (it may be forged or corrupted)."
                    : "This does not look like a valid license key.";
                return new LicenseEvaluation { State = parseState, Message = msg };
            }

            if (details!.IsMachineBound &&
                !string.Equals(details.MachineId, MachineIdentity.Id, StringComparison.OrdinalIgnoreCase))
            {
                return new LicenseEvaluation
                {
                    State = LicenseState.WrongMachine,
                    Details = details,
                    Message = "This license was issued for a different computer."
                };
            }

            DateTime now = _guard.EffectiveUtcNow;

            if (details.Tier != LicenseTier.Full &&
                details.ExpiresUtc.HasValue && now > details.ExpiresUtc.Value)
            {
                return new LicenseEvaluation
                {
                    State = LicenseState.Expired,
                    Details = details,
                    ClockTampered = _guard.ClockRolledBack,
                    Message = $"Your {details.TierName} license expired on {details.ExpiresUtc:yyyy-MM-dd}."
                };
            }

            string okMsg = details.Tier == LicenseTier.Full
                ? "Full lifetime license active."
                : $"{details.TierName} license active until {details.ExpiresUtc:yyyy-MM-dd}.";

            return new LicenseEvaluation
            {
                State = LicenseState.Valid,
                Details = details,
                ClockTampered = _guard.ClockRolledBack,
                Message = okMsg
            };
        }

        /// <summary>Validate a key and, if usable, persist it as the active license.</summary>
        public LicenseEvaluation Activate(string key)
        {
            string trimmed = key?.Trim() ?? string.Empty;
            LicenseEvaluation eval = EvaluateKey(trimmed);
            if (eval.State == LicenseState.Valid)
                StoreKey(trimmed);
            return eval;
        }

        public void Deactivate()
        {
            try { if (File.Exists(KeyFilePath)) File.Delete(KeyFilePath); } catch { }
            try
            {
                using var k = Registry.CurrentUser.OpenSubKey(RegSubKey, writable: true);
                k?.DeleteValue(RegValueName, throwOnMissingValue: false);
            }
            catch { }
        }

        // ----------------- storage (registry + file, redundant) -----------------

        public string? LoadStoredKey()
        {
            string? fromFile = null;
            try { if (File.Exists(KeyFilePath)) fromFile = File.ReadAllText(KeyFilePath); } catch { }
            if (!string.IsNullOrWhiteSpace(fromFile)) return fromFile;

            try
            {
                using var k = Registry.CurrentUser.OpenSubKey(RegSubKey);
                return k?.GetValue(RegValueName) as string;
            }
            catch { return null; }
        }

        private void StoreKey(string key)
        {
            try
            {
                var dir = Path.GetDirectoryName(KeyFilePath)!;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(KeyFilePath, key);
            }
            catch { }

            try
            {
                using var k = Registry.CurrentUser.CreateSubKey(RegSubKey);
                k?.SetValue(RegValueName, key, RegistryValueKind.String);
            }
            catch { }
        }
    }
}
