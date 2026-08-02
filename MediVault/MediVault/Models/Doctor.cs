using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MediVault.Models;

public class Doctor
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string Specialty { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(120)]
    public string? Email { get; set; }

    [MaxLength(40)]
    public string? LicenseNumber { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
}
