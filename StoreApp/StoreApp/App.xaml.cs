using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StoreApp.Data;
using StoreApp.Repositories;
using StoreApp.Services;
using StoreApp.ViewModels;
using StoreApp.Views;

namespace StoreApp
{
    public partial class App : Application
    {
        public IServiceProvider Services { get; private set; } = null!;
        private ILicenseService? _license;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Services = BuildServices();

            using (var scope = Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
                StoreDbContext.SeedData(db);
            }

            _license = Services.GetRequiredService<ILicenseService>();
            _license.StatusChanged += OnLicenseStatusChanged;

            if (!EnsureLicensed()) { Shutdown(); return; }
            ShowLogin();
        }

        public void Restart()
        {
            if (_license != null) _license.StatusChanged -= OnLicenseStatusChanged;
            foreach (Window w in Windows.OfType<Window>().ToList()) w.Close();
            Services = BuildServices();
            using (var scope = Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<StoreDbContext>();
                StoreDbContext.SeedData(db);
            }
            _license = Services.GetRequiredService<ILicenseService>();
            _license.StatusChanged += OnLicenseStatusChanged;
            if (!EnsureLicensed()) { Shutdown(); return; }
            ShowLogin();
        }

        private bool EnsureLicensed()
        {
            if (_license!.IsValid) return true;

            var window = Services.GetRequiredService<LicenseWindow>();
            var result = window.ShowDialog();
            return result == true && _license.IsValid;
        }

        private void ShowLogin()
        {
            var login = Services.GetRequiredService<LoginWindow>();
            login.Show();
        }

        private void OnLicenseStatusChanged()
        {
            // If the license becomes invalid (deactivated, expired, clock-tampered)
            // mid-session, kick the app back to the activation gate.
            if (_license == null || _license.IsValid) return;

            Dispatcher.BeginInvoke(new Action(Restart));
        }

        private static IServiceProvider BuildServices()
        {
            var services = new ServiceCollection();

            services.AddDbContext<StoreDbContext>(opt =>
                opt.UseInMemoryDatabase("StoreAppDemo"), ServiceLifetime.Singleton);

            services.AddSingleton<CurrentSession>();
            services.AddSingleton<ICartService, CartService>();
            services.AddSingleton<ILicenseService, LicenseService>();

            services.AddSingleton<IUserRepository, UserRepository>();
            services.AddSingleton<IProductRepository, ProductRepository>();
            services.AddSingleton<IOrderRepository, OrderRepository>();
            services.AddSingleton<ICustomerRepository, CustomerRepository>();

            services.AddSingleton<IAuthService, AuthService>();
            services.AddSingleton<IProductService, ProductService>();
            services.AddSingleton<IOrderService, OrderService>();
            services.AddSingleton<ICustomerService, CustomerService>();
            services.AddSingleton<IDashboardService, DashboardService>();

            services.AddTransient<LoginViewModel>();
            services.AddTransient<LicenseViewModel>();
            services.AddTransient<LicenseInfoViewModel>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<ShopViewModel>();
            services.AddTransient<ProductsViewModel>();
            services.AddTransient<OrdersViewModel>();
            services.AddTransient<CustomersViewModel>();
            services.AddTransient<CartViewModel>();

            services.AddTransient<LoginWindow>();
            services.AddTransient<LicenseWindow>();
            services.AddTransient<LicenseInfoWindow>();
            services.AddTransient<MainWindow>();

            return services.BuildServiceProvider();
        }
    }
}
