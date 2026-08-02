using System;
using System.ComponentModel.DataAnnotations;

namespace MediVault.Models;

public class PatientMedication
{
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public int MedicationId { get; set; }
    public Medication Medication { get; set; } = null!;

    [MaxLength(80)]
    public string? Dosage { get; set; }

    [MaxLength(80)]
    public string? Frequency { get; set; }

    public DateTime StartDate { get; set; } = DateTime.Today;

    public DateTime? EndDate { get; set; }

    [MaxLength(300)]
    public string? Notes { get; set; }
}
