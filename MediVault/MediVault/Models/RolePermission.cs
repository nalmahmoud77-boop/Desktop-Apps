using System.ComponentModel.DataAnnotations;

namespace MediVault.Models;

/// <summary>
/// A gateable capability in the app. The first five map one-to-one to the
/// sidebar navigation sections; <see cref="ManageUsers"/> gates the
/// Permissions section itself.
/// </summary>
public enum AppPermission
{
    ViewDashboard,
    ViewPatients,
    ViewAppointments,
    ViewPrescriptions,
    ViewAuditLog,
    ManageUsers
}

/// <summary>
/// One cell of the role × permission matrix. Presence + <see cref="Allowed"/>
/// decides whether a role may use a permission. Admin is always granted
/// everything in code, regardless of the stored rows.
/// </summary>
public class RolePermission
{
    public int Id { get; set; }

    public UserRole Role { get; set; }

    public AppPermission Permission { get; set; }

    public bool Allowed { get; set; }
}

public static class AppPermissionExtensions
{
    public static string DisplayName(this AppPermission permission) => permission switch
    {
        AppPermission.ViewDashboard => "View Dashboard",
        AppPermission.ViewPatients => "View Patients",
        AppPermission.ViewAppointments => "View Appointments",
        AppPermission.ViewPrescriptions => "View Prescriptions",
        AppPermission.ViewAuditLog => "View Audit Log",
        AppPermission.ManageUsers => "Manage Users & Permissions",
        _ => permission.ToString()
    };

    public static string Description(this AppPermission permission) => permission switch
    {
        AppPermission.ViewDashboard => "Open the dashboard overview.",
        AppPermission.ViewPatients => "Open patient records and edit demographics.",
        AppPermission.ViewAppointments => "Open and manage the appointment calendar.",
        AppPermission.ViewPrescriptions => "Open and manage prescriptions.",
        AppPermission.ViewAuditLog => "Read the system-wide audit trail.",
        AppPermission.ManageUsers => "Manage user accounts and this permission matrix.",
        _ => string.Empty
    };
}
