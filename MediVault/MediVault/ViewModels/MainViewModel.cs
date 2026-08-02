using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MediVault.Licensing;
using MediVault.Models;
using MediVault.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MediVault.ViewModels;

public enum NavSection
{
    Dashboard,
    Patients,
    Appointments,
    Prescriptions,
    AuditLog,
    Permissions
}

public class MainViewModel : BaseViewModel, IDisposable
{
    private readonly ILicenseService _licenseService;

    private NavSection _current = NavSection.Dashboard;
    private BaseViewModel? _activeView;
    private LicenseSnapshot? _license;

    public DashboardViewModel Dashboard { get; } = new();
    public PatientsViewModel Patients { get; } = new();
    public AppointmentsViewModel Appointments { get; } = new();
    public PrescriptionsViewModel Prescriptions { get; } = new();
    public AuditLogViewModel AuditLog { get; } = new();
    public PermissionsViewModel Permissions { get; } = new();

    public NavSection Current
    {
        get => _current;
        set
        {
            if (SetField(ref _current, value))
            {
                OnPropertyChanged(nameof(IsDashboard));
                OnPropertyChanged(nameof(IsPatients));
                OnPropertyChanged(nameof(IsAppointments));
                OnPropertyChanged(nameof(IsPrescriptions));
                OnPropertyChanged(nameof(IsAuditLog));
                OnPropertyChanged(nameof(IsPermissions));
                OnPropertyChanged(nameof(SectionTitle));
                _ = NavigateAsync(value);
            }
        }
    }

    public bool IsDashboard => Current == NavSection.Dashboard;
    public bool IsPatients => Current == NavSection.Patients;
    public bool IsAppointments => Current == NavSection.Appointments;
    public bool IsPrescriptions => Current == NavSection.Prescriptions;
    public bool IsAuditLog => Current == NavSection.AuditLog;
    public bool IsPermissions => Current == NavSection.Permissions;

    // Per-role nav gating. Dashboard is always available so there is a landing page.
    public bool CanViewPatients => PermissionService.CurrentUserCan(AppPermission.ViewPatients);
    public bool CanViewAppointments => PermissionService.CurrentUserCan(AppPermission.ViewAppointments);
    public bool CanViewPrescriptions => PermissionService.CurrentUserCan(AppPermission.ViewPrescriptions);
    public bool CanViewAuditLog => PermissionService.CurrentUserCan(AppPermission.ViewAuditLog);
    public bool CanManageUsers => PermissionService.CurrentUserCan(AppPermission.ManageUsers);

    public string SectionTitle => Current switch
    {
        NavSection.Dashboard => "Dashboard",
        NavSection.Patients => "Patients",
        NavSection.Appointments => "Appointments",
        NavSection.Prescriptions => "Prescriptions",
        NavSection.AuditLog => "Audit Log",
        NavSection.Permissions => "Permissions",
        _ => string.Empty
    };

    public BaseViewModel? ActiveView
    {
        get => _activeView;
        private set => SetField(ref _activeView, value);
    }

    public string CurrentUserName => AuthService.CurrentUser?.FullName ?? "Guest";
    public string CurrentUserRole => AuthService.CurrentUser?.Role.ToString() ?? string.Empty;
    public string UserInitials
    {
        get
        {
            var n = AuthService.CurrentUser?.FullName ?? "G";
            var parts = n.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return parts[0][..1].ToUpper();
            return $"{parts[0][0]}{parts[^1][0]}".ToUpper();
        }
    }

    public LicenseSnapshot? License
    {
        get => _license;
        private set
        {
            if (SetField(ref _license, value))
            {
                OnPropertyChanged(nameof(LicenseTierLabel));
                OnPropertyChanged(nameof(LicenseDaysLabel));
                OnPropertyChanged(nameof(LicenseIsExpiringSoon));
                OnPropertyChanged(nameof(LicenseIssuedTo));
                OnPropertyChanged(nameof(LicenseIssuedAtLocal));
                OnPropertyChanged(nameof(LicenseExpiresAtLocal));
                OnPropertyChanged(nameof(LicenseStatusLabel));
                OnPropertyChanged(nameof(HasLicense));
            }
        }
    }

    public bool HasLicense => License != null;
    public string LicenseTierLabel => License?.Info.Tier.DisplayName() ?? "—";
    public string LicenseDaysLabel
    {
        get
        {
            if (License == null) return "Unlicensed";
            if (License.IsLifetime) return "Lifetime";
            var days = License.DaysRemaining ?? 0;
            return days <= 0 ? "Expired" : $"{days} day{(days == 1 ? "" : "s")} left";
        }
    }
    public bool LicenseIsExpiringSoon =>
        License is { IsLifetime: false, DaysRemaining: <= 7 };

    public string LicenseIssuedTo => License?.Info.IssuedTo ?? "—";
    public string LicenseIssuedAtLocal =>
        License == null ? "—" : License.Info.IssuedAtUtc.ToLocalTime().ToString("yyyy-MM-dd");
    public string LicenseExpiresAtLocal =>
        License == null ? "—" : License.IsLifetime ? "Never" : License.ExpiresAtUtc!.Value.ToLocalTime().ToString("yyyy-MM-dd");
    public string LicenseStatusLabel => License?.Status.ToString() ?? "None";

    public ICommand NavigateCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand DeactivateLicenseCommand { get; }

    public MainViewModel(ILicenseService licenseService)
    {
        _licenseService = licenseService;
        License = _licenseService.Current;
        _licenseService.StatusChanged += OnLicenseStatusChanged;

        NavigateCommand = new RelayCommand(p =>
        {
            if (p is NavSection section) Current = section;
            else if (p is string s && Enum.TryParse<NavSection>(s, out var parsed)) Current = parsed;
        });
        LogoutCommand = new AsyncRelayCommand(LogoutAsync);
        DeactivateLicenseCommand = new AsyncRelayCommand(DeactivateLicenseAsync);

        ActiveView = Dashboard;
        _ = Dashboard.LoadAsync();
    }

    private void OnLicenseStatusChanged(object? sender, LicenseSnapshot? snapshot)
    {
        Application.Current?.Dispatcher.Invoke(() => License = snapshot);
    }

    private async Task NavigateAsync(NavSection section)
    {
        ActiveView = section switch
        {
            NavSection.Dashboard => Dashboard,
            NavSection.Patients => Patients,
            NavSection.Appointments => Appointments,
            NavSection.Prescriptions => Prescriptions,
            NavSection.AuditLog => AuditLog,
            NavSection.Permissions => Permissions,
            _ => Dashboard
        };

        switch (section)
        {
            case NavSection.Dashboard: await Dashboard.LoadAsync(); break;
            case NavSection.Patients: await Patients.LoadAsync(); break;
            case NavSection.Appointments: await Appointments.LoadAsync(); break;
            case NavSection.Prescriptions: await Prescriptions.LoadAsync(); break;
            case NavSection.AuditLog: await AuditLog.LoadAsync(); break;
            case NavSection.Permissions: await Permissions.LoadAsync(); break;
        }
    }

    private async Task LogoutAsync()
    {
        await AuthService.LogoutAsync();
        var login = App.Services.GetRequiredService<Views.LoginWindow>();
        Application.Current.MainWindow = login;
        login.Show();
        foreach (Window w in Application.Current.Windows)
        {
            if (w is Views.MainWindow main) main.Close();
        }
    }

    private async Task DeactivateLicenseAsync()
    {
        var confirm = MessageBox.Show(
            "Deactivate this license? You'll need to re-enter a license key to keep using MediVault.",
            "Deactivate license",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.OK) return;

        await _licenseService.DeactivateAsync();
    }

    public void Dispose()
    {
        _licenseService.StatusChanged -= OnLicenseStatusChanged;
    }
}
