using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MediVault.Models;

public enum Gender
{
    Male,
    Female,
    Other
}

public class Patient
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string MedicalId { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(80)]
    public string LastName { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; }

    public Gender Gender { get; set; }

    [MaxLength(30)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? Email { get; set; }

    [MaxLength(250)]
    public string? Address { get; set; }

    [MaxLength(10)]
    public string? BloodGroup { get; set; }

    [MaxLength(2000)]
    public string? MedicalHistory { get; set; }

    [MaxLength(2000)]
    public string? Allergies { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<PatientMedication> PatientMedications { get; set; } = new List<PatientMedication>();
    public ICollection<PatientCondition> PatientConditions { get; set; } = new List<PatientCondition>();
    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();

    public string FullName => $"{FirstName} {LastName}";

    public int Age
    {
        get
        {
            var today = DateTime.Today;
            var age = today.Year - DateOfBirth.Year;
            if (DateOfBirth.Date > today.AddYears(-age)) age--;
            return age;
        }
    }
}
