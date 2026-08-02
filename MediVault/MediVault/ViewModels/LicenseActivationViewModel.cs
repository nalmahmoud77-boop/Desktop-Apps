using System;
using System.Threading.Tasks;
using System.Windows.Input;
using MediVault.Licensing;

namespace MediVault.ViewModels;

public class LicenseActivationViewModel : BaseViewModel
{
    private readonly ILicenseService _licenseService;

    private string _key = string.Empty;
    private string? _error;
    private string? _success;
    private bool _isBusy;
    private string _machineId = string.Empty;

    public string Key
    {
        get => _key;
        set => SetField(ref _key, value);
    }

    public string? Error
    {
        get => _error;
        set => SetField(ref _error, value);
    }

    public string? Success
    {
        get => _success;
        set => SetField(ref _success, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetField(ref _isBusy, value);
    }

    public string MachineId
    {
        get => _machineId;
        set => SetField(ref _machineId, value);
    }

    public ICommand ActivateCommand { get; }

    public event Action? ActivationSucceeded;

    public LicenseActivationViewModel(
        ILicenseService licenseService,
        IMachineFingerprintProvider fingerprint)
    {
        _licenseService = licenseService;
        var fp = fingerprint.GetFingerprint();
        MachineId = fp.Length > 16 ? fp[..16] + "…" : fp;
        ActivateCommand = new AsyncRelayCommand(ActivateAsync, () => !IsBusy);
    }

    private async Task ActivateAsync()
    {
        Error = null;
        Success = null;
        IsBusy = true;
        try
        {
            var result = await _licenseService.ActivateAsync(Key);
            if (result != ActivationResult.Ok)
            {
                Error = result.Message();
                return;
            }

            var snapshot = _licenseService.Current!;
            var expiry = snapshot.IsLifetime
                ? "lifetime license"
                : $"valid until {snapshot.ExpiresAtUtc!.Value.ToLocalTime():yyyy-MM-dd}";
            Success = $"Activated — {snapshot.Info.Tier.DisplayName()} ({expiry}).";
            await Task.Delay(700);
            ActivationSucceeded?.Invoke();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
