using System.Windows;
using System.Windows.Input;
using StoreApp.Licensing;
using StoreApp.Services;

namespace StoreApp.ViewModels
{
    public class LicenseInfoViewModel : BaseViewModel
    {
        private readonly ILicenseService _license;
        private bool _showFullKey;
        private string _infoMessage = string.Empty;

        public LicenseInfoViewModel(ILicenseService license)
        {
            _license = license;
            _license.StatusChanged += OnLicenseChanged;

            CopyKeyCommand = new RelayCommand(_ => CopyKey(), _ => HasLicense);
            ToggleKeyVisibilityCommand = new RelayCommand(_ => ShowFullKey = !ShowFullKey, _ => HasLicense);
            DeactivateCommand = new RelayCommand(_ => Deactivate(), _ => HasLicense);
        }

        public LicenseInfo? Current => _license.Current;
        public bool HasLicense => _license.Current != null;
        public bool HasNoLicense => _license.Current == null;

        public string PlanName => _license.Current?.PlanName ?? "Not activated";
        public string StatusText => _license.StatusText;
        public string IssuedAt => _license.Current?.IssuedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "—";
        public string ExpiresAt => _license.Current == null
            ? "—"
            : _license.Current.IsLifetime ? "Never (Lifetime)" : _license.Current.ExpiresAt!.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        public string DaysRemainingText
        {
            get
            {
                if (_license.Current == null) return "—";
                if (_license.Current.IsLifetime) return "Unlimited";
                var d = _license.DaysRemaining ?? 0;
                return $"{d} day{(d == 1 ? "" : "s")}";
            }
        }

        public string DisplayedKey
        {
            get
            {
                var key = _license.Current?.Key;
                if (string.IsNullOrEmpty(key)) return "—";
                if (_showFullKey) return key;
                return MaskKey(key);
            }
        }

        public bool ShowFullKey
        {
            get => _showFullKey;
            set
            {
                if (SetProperty(ref _showFullKey, value))
                {
                    OnPropertyChanged(nameof(DisplayedKey));
                    OnPropertyChanged(nameof(ToggleKeyText));
                }
            }
        }

        public string ToggleKeyText => _showFullKey ? "Hide" : "Show";

        public string InfoMessage
        {
            get => _infoMessage;
            set => SetProperty(ref _infoMessage, value);
        }

        public ICommand CopyKeyCommand { get; }
        public ICommand ToggleKeyVisibilityCommand { get; }
        public ICommand DeactivateCommand { get; }

        public event Action? CloseRequested;

        public void RequestClose() => CloseRequested?.Invoke();

        private void CopyKey()
        {
            var key = _license.Current?.Key;
            if (string.IsNullOrEmpty(key)) return;
            try
            {
                Clipboard.SetText(key);
                InfoMessage = "License key copied to clipboard.";
            }
            catch
            {
                InfoMessage = "Could not access clipboard.";
            }
        }

        private void Deactivate()
        {
            var confirm = MessageBox.Show(
                "Deactivate the current license? You will need to re-activate to keep using StoreApp.",
                "Confirm deactivation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            _license.Deactivate();
            InfoMessage = "License deactivated.";
            RequestClose();
        }

        private void OnLicenseChanged()
        {
            OnPropertyChanged(nameof(Current));
            OnPropertyChanged(nameof(HasLicense));
            OnPropertyChanged(nameof(HasNoLicense));
            OnPropertyChanged(nameof(PlanName));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(IssuedAt));
            OnPropertyChanged(nameof(ExpiresAt));
            OnPropertyChanged(nameof(DaysRemainingText));
            OnPropertyChanged(nameof(DisplayedKey));
        }

        private static string MaskKey(string key)
        {
            var parts = key.Split('-');
            if (parts.Length < 2) return new string('•', Math.Min(key.Length, 16));
            for (int i = 1; i < parts.Length - 1; i++)
                parts[i] = new string('•', parts[i].Length);
            return string.Join('-', parts);
        }
    }
}
