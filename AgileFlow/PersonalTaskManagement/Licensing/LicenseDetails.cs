using System;

namespace PersonalTaskManagement.Licensing
{
    /// <summary>The verified contents of a license key.</summary>
    public sealed class LicenseDetails
    {
        public string Id { get; init; } = string.Empty;
        public LicenseTier Tier { get; init; }
        public DateTime IssuedUtc { get; init; }

        /// <summary>Null for a perpetual (Full) license.</summary>
        public DateTime? ExpiresUtc { get; init; }

        /// <summary>Machine fingerprint the key is bound to, or "ANY".</summary>
        public string MachineId { get; init; } = "ANY";

        public bool IsMachineBound => !string.Equals(MachineId, "ANY", StringComparison.OrdinalIgnoreCase);

        public string TierName => Tier switch
        {
            LicenseTier.Monthly => "Monthly",
            LicenseTier.Yearly => "Yearly",
            LicenseTier.Full => "Full (Lifetime)",
            _ => Tier.ToString()
        };
    }

    /// <summary>Result of evaluating a license against the current machine and clock.</summary>
    public sealed class LicenseEvaluation
    {
        public LicenseState State { get; init; }
        public LicenseDetails? Details { get; init; }
        public string Message { get; init; } = string.Empty;

        /// <summary>True when the system clock appears to have been rolled back.</summary>
        public bool ClockTampered { get; init; }

        public bool IsUsable => State == LicenseState.Valid;

        public string Summary
        {
            get
            {
                if (Details == null) return "Unlicensed";
                return Details.Tier switch
                {
                    LicenseTier.Full => "Full license",
                    _ when Details.ExpiresUtc.HasValue =>
                        $"{Details.TierName} · {(State == LicenseState.Expired ? "expired" : "valid until")} {Details.ExpiresUtc:yyyy-MM-dd}",
                    _ => Details.TierName
                };
            }
        }
    }
}
