namespace StoreApp.Licensing
{
    public class LicenseInfo
    {
        public LicenseTier Tier { get; set; }
        public string Key { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime LastSeenUtc { get; set; }

        public bool IsLifetime => Tier == LicenseTier.Lifetime || ExpiresAt == null;

        public string PlanName => Tier switch
        {
            LicenseTier.Monthly => "Monthly",
            LicenseTier.Yearly => "Yearly",
            LicenseTier.Lifetime => "Lifetime",
            _ => "Unknown"
        };

        public bool IsValid(DateTime nowUtc) => IsLifetime || nowUtc <= ExpiresAt!.Value;

        public int? DaysRemaining(DateTime nowUtc)
        {
            if (IsLifetime) return null;
            var span = ExpiresAt!.Value - nowUtc;
            return span.TotalSeconds <= 0 ? 0 : (int)Math.Ceiling(span.TotalDays);
        }

        public static LicenseInfo Issue(LicenseTier tier, string key, DateTime nowUtc) => new()
        {
            Tier = tier,
            Key = LicenseKey.Normalize(key),
            IssuedAt = nowUtc,
            ExpiresAt = tier switch
            {
                LicenseTier.Monthly => nowUtc.AddDays(30),
                LicenseTier.Yearly => nowUtc.AddYears(1),
                LicenseTier.Lifetime => null,
                _ => throw new ArgumentOutOfRangeException(nameof(tier))
            },
            LastSeenUtc = nowUtc
        };
    }
}
