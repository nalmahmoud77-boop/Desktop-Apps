using System;
using System.ComponentModel.DataAnnotations;

namespace MediVault.Models;

public enum ConditionSeverity
{
    Mild,
    Moderate,
    Severe,
    Critical
}

public class PatientCondition
{
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public int ConditionId { get; set; }
    public Condition Condition { get; set; } = null!;

    public DateTime DiagnosedOn { get; set; } = DateTime.Today;

    public ConditionSeverity Severity { get; set; } = ConditionSeverity.Mild;

    public bool IsActive { get; set; } = true;

    [MaxLength(500)]
    public string? Notes { get; set; }
}
