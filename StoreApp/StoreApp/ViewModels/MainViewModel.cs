using System.Windows;
using System.Windows.Input;
using StoreApp.Services;
using StoreApp.Views;

namespace StoreApp.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private const int ExpiryWarningDays = 7;

        private readonly IServiceProvider _sp;
        private readonly CurrentSession _session;
        private readonly ICartService _cart;
        private readonly ILicenseService _license;
        private BaseViewModel? _currentView;
        private string _currentTitle = "Dashboard";

        public MainViewModel(IServiceProvider sp, CurrentSession session, ICartService cart, ILicenseService license)
        {
            _sp = sp;
            _session = session;
            _cart = cart;
            _license = license;

            _cart.Changed += () =>
            {
                OnPropertyChanged(nameof(CartItemsCount));
                OnPropertyChanged(nameof(HasCartItems));
            };

            _license.StatusChanged += () =>
            {
                OnPropertyChanged(nameof(LicenseStatus));
                OnPropertyChanged(nameof(LicensePlan));
                OnPropertyChanged(nameof(IsLifetimeLicense));
                OnPropertyChanged(nameof(IsLicenseExpiringSoon));
            };

            ShowDashboardCommand = new RelayCommand(_ => Show<DashboardViewModel>("Dashboard"));
            ShowShopCommand = new RelayCommand(_ => Show<ShopViewModel>("Shop"));
            ShowProductsCommand = new RelayCommand(_ => Show<ProductsViewModel>("Products"));
            ShowOrdersCommand = new RelayCommand(_ => Show<OrdersViewModel>("Orders"));
            ShowCustomersCommand = new RelayCommand(_ => Show<CustomersViewModel>("Customers"));
            ShowCartCommand = new RelayCommand(_ => Show<CartViewModel>("Shopping Cart"));
            ShowLicenseCommand = new RelayCommand(_ => ShowLicenseInfo());
            LogoutCommand = new RelayCommand(_ => DoLogout());

            Show<DashboardViewModel>("Dashboard");
        }

        public BaseViewModel? CurrentView { get => _currentView; set => SetProperty(ref _currentView, value); }
        public string CurrentTitle { get => _currentTitle; set => SetProperty(ref _currentTitle, value); }

        public string UserName => _session.User?.FullName ?? "Guest";
        public string UserRole => _session.User?.Role.ToString() ?? string.Empty;
        public string UserInitials
        {
            get
            {
                var name = _session.User?.FullName ?? "?";
                var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) return "?";
                if (parts.Length == 1) return parts[0].Substring(0, 1).ToUpper();
                return ($"{parts[0][0]}{parts[1][0]}").ToUpper();
            }
        }

        public int CartItemsCount => _cart.TotalItems;
        public bool HasCartItems => _cart.TotalItems > 0;

        public string LicenseStatus => _license.StatusText;
        public string LicensePlan => _license.Current?.PlanName ?? "Inactive";
        public bool IsLifetimeLicense => _license.Current?.IsLifetime ?? false;
        public bool IsLicenseExpiringSoon =>
            !IsLifetimeLicense && _license.DaysRemaining is int d && d <= ExpiryWarningDays;

        public ICommand ShowDashboardCommand { get; }
        public ICommand ShowShopCommand { get; }
        public ICommand ShowProductsCommand { get; }
        public ICommand ShowOrdersCommand { get; }
        public ICommand ShowCustomersCommand { get; }
        public ICommand ShowCartCommand { get; }
        public ICommand ShowLicenseCommand { get; }
        public ICommand LogoutCommand { get; }

        public event Action? LoggedOut;

        private void Show<T>(string title) where T : BaseViewModel
        {
            var vm = (T)_sp.GetService(typeof(T))!;
            if (vm is DashboardViewModel d) d.Refresh();
            CurrentView = vm;
            CurrentTitle = title;
        }

        private void ShowLicenseInfo()
        {
            var window = (LicenseInfoWindow)_sp.GetService(typeof(LicenseInfoWindow))!;
            window.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
            window.ShowDialog();
        }

        private void DoLogout()
        {
            if (MessageBox.Show("Sign out?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            LoggedOut?.Invoke();
        }
    }
}
