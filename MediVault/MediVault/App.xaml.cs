using System;
using System.Threading.Tasks;
using System.Windows;
using MediVault.Data;
using MediVault.Licensing;
using MediVault.ViewModels;
using MediVault.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MediVault;

public partial class App : Application
{
    public static IHost Host { get; private set; } = null!;
    public static IServiceProvider Services => Host.Services;

    private bool _routingToActivation;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Host = Microsoft.Extensions.Hosting.Host
            .CreateDefaultBuilder()
            .ConfigureServices(ConfigureServices)
            .Build();

        try
        {
            await DbInitializer.InitializeAsync();
            await MediVault.Services.PermissionService.RefreshAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to initialize database:\n{ex.Message}", "MediVault", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
            return;
        }

        var licenseService = Services.GetRequiredService<ILicenseService>();
        licenseService.StatusChanged += OnLicenseStatusChanged;
        await licenseService.RefreshAsync();

        if (IsLicenseValid(licenseService.Current))
            ShowLogin();
        else
            await ShowActivationAsync(licenseService);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IMachineFingerprintProvider, MachineFingerprintProvider>();
        services.AddSingleton<ILicenseService, LicenseService>();

        services.AddTransient<LoginViewModel>();
        services.AddTransient<LicenseActivationViewModel>();
        services.AddTransient<MainViewModel>();

        services.AddTransient<LoginWindow>();
        services.AddTransient<LicenseActivationWindow>();
        services.AddTransient<MainWindow>();
    }

    private Task ShowActivationAsync(ILicenseService licenseService)
    {
        var window = Services.GetRequiredService<LicenseActivationWindow>();
        MainWindow = window;
        window.ShowDialog();

        if (IsLicenseValid(licenseService.Current))
            ShowLogin();
        else
            Shutdown();

        return Task.CompletedTask;
    }

    private void ShowLogin()
    {
        foreach (Window w in Windows)
        {
            if (w is Views.MainWindow or Views.LoginWindow) w.Close();
        }
        var login = Services.GetRequiredService<LoginWindow>();
        MainWindow = login;
        login.Show();
    }

    private async void OnLicenseStatusChanged(object? sender, LicenseSnapshot? snapshot)
    {
        if (IsLicenseValid(snapshot)) return;
        if (_routingToActivation) return;

        await Dispatcher.InvokeAsync(async () =>
        {
            if (_routingToActivation) return;
            _routingToActivation = true;
            try
            {
                foreach (Window w in Windows)
                    if (w is Views.MainWindow or Views.LoginWindow) w.Close();

                var licenseService = Services.GetRequiredService<ILicenseService>();
                await ShowActivationAsync(licenseService);
            }
            finally
            {
                _routingToActivation = false;
            }
        });
    }

    private static bool IsLicenseValid(LicenseSnapshot? snapshot) =>
        snapshot is { Status: LicenseStatus.Active };

    protected override void OnExit(ExitEventArgs e)
    {
        Host?.Dispose();
        base.OnExit(e);
    }
}
