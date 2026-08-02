using MediVault.Licensing;

namespace MediVault.Licensing;

public enum LicenseStatus
{
    None,
    Active,
    Expired,
    Tampered
}

public sealed record LicenseSnapshot(
    LicenseInfo Info,
    string Key,
    DateTime ActivatedAtUtc,
    DateTime? ExpiresAtUtc,
    LicenseStatus Status)
{
    public int? DaysRemaining
    {
        get
        {
            if (ExpiresAtUtc is not { } e) return null;
            var now = DateTime.UtcNow;
            if (e <= now) return 0;
            return (int)Math.Ceiling((e - now).TotalDays);
        }
    }

    public bool IsLifetime => ExpiresAtUtc == null;
}

public enum ActivationResult
{
    Ok,
    Empty,
    Malformed,
    BadSignature,
    UnknownVersion,
    UnknownTier,
    AlreadyExpired,
    StorageFailed
}

public static class ActivationResultExtensions
{
    public static string Message(this ActivationResult r) => r switch
    {
        ActivationResult.Ok => "Activated.",
        ActivationResult.Empty => "Please enter a license key.",
        ActivationResult.Malformed => "License key format is not recognized.",
        ActivationResult.BadSignature => "License key is invalid or has been tampered with.",
        ActivationResult.UnknownVersion => "License key uses an unsupported format.",
        ActivationResult.UnknownTier => "License key has an unknown tier.",
        ActivationResult.AlreadyExpired => "This license has already expired.",
        ActivationResult.StorageFailed => "Could not save license to disk. Please try running as administrator.",
        _ => string.Empty
    };
}
