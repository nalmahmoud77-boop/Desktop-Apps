using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MediVault.Models;

public class Medication
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? GenericName { get; set; }

    [MaxLength(80)]
    public string? Form { get; set; } // tablet, capsule, syrup, etc.

    [MaxLength(40)]
    public string? Strength { get; set; } // 500mg

    [MaxLength(500)]
    public string? Description { get; set; }

    public ICollection<PatientMedication> PatientMedications { get; set; } = new List<PatientMedication>();
    public ICollection<PrescriptionItem> PrescriptionItems { get; set; } = new List<PrescriptionItem>();
}
