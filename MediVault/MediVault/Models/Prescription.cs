using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MediVault.Models;

public class Prescription
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string Code { get; set; } = string.Empty; // e.g., RX-2026-00001

    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public int DoctorId { get; set; }
    public Doctor Doctor { get; set; } = null!;

    public DateTime IssuedOn { get; set; } = DateTime.Now;

    [MaxLength(500)]
    public string? Diagnosis { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public ICollection<PrescriptionItem> Items { get; set; } = new List<PrescriptionItem>();
}
