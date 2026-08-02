namespace MediVault.Licensing;

public sealed record LicenseInfo(
    Guid KeyId,
    LicenseTier Tier,
    string IssuedTo,
    DateTime IssuedAtUtc)
{
    public DateTime? ComputeExpiryUtc(DateTime activatedAtUtc) =>
        Tier.Duration() is { } d ? activatedAtUtc + d : null;
}
