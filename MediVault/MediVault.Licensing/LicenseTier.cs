namespace MediVault.Licensing;

public enum LicenseTier : byte
{
    Monthly = 1,
    Yearly = 2,
    Lifetime = 3
}

public static class LicenseTierExtensions
{
    public static string DisplayName(this LicenseTier tier) => tier switch
    {
        LicenseTier.Monthly => "Monthly",
        LicenseTier.Yearly => "Yearly",
        LicenseTier.Lifetime => "Lifetime",
        _ => tier.ToString()
    };

    public static TimeSpan? Duration(this LicenseTier tier) => tier switch
    {
        LicenseTier.Monthly => TimeSpan.FromDays(30),
        LicenseTier.Yearly => TimeSpan.FromDays(365),
        LicenseTier.Lifetime => null,
        _ => TimeSpan.Zero
    };
}
