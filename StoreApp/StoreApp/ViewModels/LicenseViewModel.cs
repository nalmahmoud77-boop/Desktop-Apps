using System.Windows.Input;
using StoreApp.Licensing;
using StoreApp.Services;

namespace StoreApp.ViewModels
{
    public class LicenseViewModel : BaseViewModel
    {
        private readonly ILicenseService _license;

        private string _licenseKey = string.Empty;
        private string _errorMessage = string.Empty;
        private string _infoMessage = string.Empty;
        private bool _isBusy;

        public LicenseViewModel(ILicenseService license)
        {
            _license = license;
            ActivateKeyCommand = new RelayCommand(_ => ActivateKey(), _ => !IsBusy && !string.IsNullOrWhiteSpace(LicenseKey));
        }

        public string LicenseKey { get => _licenseKey; set => SetProperty(ref _licenseKey, value); }
        public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }
        public string InfoMessage { get => _infoMessage; set => SetProperty(ref _infoMessage, value); }
        public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }

        public string CurrentStatus => _license.StatusText;

        public ICommand ActivateKeyCommand { get; }

        public event Action<LicenseInfo>? Activated;

        private void ActivateKey()
        {
            ErrorMessage = string.Empty;
            InfoMessage = string.Empty;
            IsBusy = true;
            try
            {
                var info = _license.Activate(LicenseKey);
                InfoMessage = $"{info.PlanName} license activated.";
                Activated?.Invoke(info);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(CurrentStatus));
            }
        }
    }
}
