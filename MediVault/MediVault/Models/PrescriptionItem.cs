using System.ComponentModel.DataAnnotations;

namespace MediVault.Models;

public class PrescriptionItem
{
    public int Id { get; set; }

    public int PrescriptionId { get; set; }
    public Prescription Prescription { get; set; } = null!;

    public int MedicationId { get; set; }
    public Medication Medication { get; set; } = null!;

    [MaxLength(80)]
    public string Dosage { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Frequency { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Duration { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Instructions { get; set; }

    public int Quantity { get; set; } = 1;
}
