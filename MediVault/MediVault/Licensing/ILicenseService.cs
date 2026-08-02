namespace MediVault.Licensing;

public interface ILicenseService
{
    LicenseSnapshot? Current { get; }

    event EventHandler<LicenseSnapshot?>? StatusChanged;

    Task<ActivationResult> ActivateAsync(string key, CancellationToken ct = default);
    Task DeactivateAsync(CancellationToken ct = default);
    Task<LicenseSnapshot?> RefreshAsync(CancellationToken ct = default);
}
