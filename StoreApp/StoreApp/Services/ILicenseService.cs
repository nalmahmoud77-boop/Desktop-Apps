using StoreApp.Licensing;

namespace StoreApp.Services
{
    public interface ILicenseService
    {
        LicenseInfo? Current { get; }
        bool IsValid { get; }
        int? DaysRemaining { get; }
        string StatusText { get; }

        LicenseInfo Activate(string key);
        void Deactivate();
        void Refresh();

        event Action? StatusChanged;
    }
}
