using System;
using System.ComponentModel.DataAnnotations;

namespace MediVault.Models;

public enum AppointmentStatus
{
    Scheduled,
    Completed,
    Cancelled,
    NoShow
}

public class Appointment
{
    public int Id { get; set; }

    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    [MaxLength(200)]
    public string? Reason { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
