using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MediVault.Models;

public class Condition
{
    public int Id { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? IcdCode { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public ICollection<PatientCondition> PatientConditions { get; set; } = new List<PatientCondition>();
}
