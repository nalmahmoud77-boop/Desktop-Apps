using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediVault.Data;
using MediVault.Models;
using Microsoft.EntityFrameworkCore;

namespace MediVault.Services;

public static class PatientService
{
    public static async Task<List<Patient>> GetAllAsync(string? search = null)
    {
        await using var ctx = new MediVaultDbContext();
        IQueryable<Patient> q = ctx.Patients.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            q = q.Where(p =>
                p.FirstName.ToLower().Contains(s) ||
                p.LastName.ToLower().Contains(s) ||
                p.MedicalId.ToLower().Contains(s) ||
                p.Phone.ToLower().Contains(s));
        }
        return await q.OrderBy(p => p.LastName).ThenBy(p => p.FirstName).ToListAsync();
    }

    public static async Task<Patient?> GetByIdAsync(int id)
    {
        await using var ctx = new MediVaultDbContext();
        return await ctx.Patients
            .Include(p => p.PatientMedications).ThenInclude(pm => pm.Medication)
            .Include(p => p.PatientConditions).ThenInclude(pc => pc.Condition)
            .Include(p => p.Appointments).ThenInclude(a => a.Doctor)
            .Include(p => p.Prescriptions).ThenInclude(rx => rx.Doctor)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public static async Task<Patient> AddAsync(Patient patient)
    {
        await using var ctx = new MediVaultDbContext();
        patient.CreatedAt = DateTime.UtcNow;
        patient.UpdatedAt = DateTime.UtcNow;
        ctx.Patients.Add(patient);
        await ctx.SaveChangesAsync();

        await AuditService.LogAsync(AuditAction.Create, "Patient", patient.Id.ToString(),
            $"Created patient {patient.FullName} ({patient.MedicalId}).");

        return patient;
    }

    public static async Task<Patient> UpdateAsync(Patient patient)
    {
        await using var ctx = new MediVaultDbContext();
        var existing = await ctx.Patients.FirstOrDefaultAsync(p => p.Id == patient.Id)
            ?? throw new InvalidOperationException("Patient not found.");

        var changes = new List<string>();
        if (existing.FirstName != patient.FirstName) changes.Add($"FirstName: '{existing.FirstName}' → '{patient.FirstName}'");
        if (existing.LastName != patient.LastName) changes.Add($"LastName: '{existing.LastName}' → '{patient.LastName}'");
        if (existing.Phone != patient.Phone) changes.Add($"Phone: '{existing.Phone}' → '{patient.Phone}'");
        if (existing.Email != patient.Email) changes.Add($"Email: '{existing.Email}' → '{patient.Email}'");
        if (existing.Address != patient.Address) changes.Add($"Address changed");
        if (existing.MedicalHistory != patient.MedicalHistory) changes.Add($"MedicalHistory changed");
        if (existing.Allergies != patient.Allergies) changes.Add($"Allergies changed");
        if (existing.BloodGroup != patient.BloodGroup) changes.Add($"BloodGroup: '{existing.BloodGroup}' → '{patient.BloodGroup}'");
        if (existing.Gender != patient.Gender) changes.Add($"Gender: {existing.Gender} → {patient.Gender}");
        if (existing.DateOfBirth != patient.DateOfBirth) changes.Add($"DOB: {existing.DateOfBirth:yyyy-MM-dd} → {patient.DateOfBirth:yyyy-MM-dd}");

        existing.MedicalId = patient.MedicalId;
        existing.FirstName = patient.FirstName;
        existing.LastName = patient.LastName;
        existing.DateOfBirth = patient.DateOfBirth;
        existing.Gender = patient.Gender;
        existing.Phone = patient.Phone;
        existing.Email = patient.Email;
        existing.Address = patient.Address;
        existing.BloodGroup = patient.BloodGroup;
        existing.MedicalHistory = patient.MedicalHistory;
        existing.Allergies = patient.Allergies;
        existing.UpdatedAt = DateTime.UtcNow;

        await ctx.SaveChangesAsync();

        if (changes.Count > 0)
        {
            await AuditService.LogAsync(AuditAction.Update, "Patient", existing.Id.ToString(),
                $"Updated patient {existing.FullName}: {string.Join("; ", changes)}");
        }

        return existing;
    }

    public static async Task DeleteAsync(int id)
    {
        await using var ctx = new MediVaultDbContext();
        var patient = await ctx.Patients.FirstOrDefaultAsync(p => p.Id == id);
        if (patient == null) return;
        ctx.Patients.Remove(patient);
        await ctx.SaveChangesAsync();
        await AuditService.LogAsync(AuditAction.Delete, "Patient", id.ToString(),
            $"Deleted patient {patient.FullName} ({patient.MedicalId}).");
    }

    public static async Task<List<Medication>> GetAllMedicationsAsync()
    {
        await using var ctx = new MediVaultDbContext();
        return await ctx.Medications.AsNoTracking().OrderBy(m => m.Name).ToListAsync();
    }

    public static async Task<List<Condition>> GetAllConditionsAsync()
    {
        await using var ctx = new MediVaultDbContext();
        return await ctx.Conditions.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
    }

    public static async Task AssignMedicationAsync(int patientId, int medicationId, string dosage, string frequency, DateTime startDate, DateTime? endDate, string? notes)
    {
        await using var ctx = new MediVaultDbContext();
        var existing = await ctx.PatientMedications.FirstOrDefaultAsync(pm =>
            pm.PatientId == patientId && pm.MedicationId == medicationId && pm.StartDate == startDate);
        if (existing != null) return;

        ctx.PatientMedications.Add(new PatientMedication
        {
            PatientId = patientId,
            MedicationId = medicationId,
            Dosage = dosage,
            Frequency = frequency,
            StartDate = startDate,
            EndDate = endDate,
            Notes = notes
        });
        await ctx.SaveChangesAsync();

        await AuditService.LogAsync(AuditAction.Update, "Patient", patientId.ToString(),
            $"Assigned medication #{medicationId} to patient #{patientId}.");
    }

    public static async Task RemoveMedicationAsync(int patientId, int medicationId, DateTime startDate)
    {
        await using var ctx = new MediVaultDbContext();
        var pm = await ctx.PatientMedications.FirstOrDefaultAsync(x =>
            x.PatientId == patientId && x.MedicationId == medicationId && x.StartDate == startDate);
        if (pm == null) return;
        ctx.PatientMedications.Remove(pm);
        await ctx.SaveChangesAsync();
        await AuditService.LogAsync(AuditAction.Update, "Patient", patientId.ToString(),
            $"Removed medication #{medicationId} from patient #{patientId}.");
    }

    public static async Task AssignConditionAsync(int patientId, int conditionId, DateTime diagnosedOn, ConditionSeverity severity, string? notes)
    {
        await using var ctx = new MediVaultDbContext();
        var existing = await ctx.PatientConditions.FirstOrDefaultAsync(pc =>
            pc.PatientId == patientId && pc.ConditionId == conditionId && pc.DiagnosedOn == diagnosedOn);
        if (existing != null) return;

        ctx.PatientConditions.Add(new PatientCondition
        {
            PatientId = patientId,
            ConditionId = conditionId,
            DiagnosedOn = diagnosedOn,
            Severity = severity,
            Notes = notes
        });
        await ctx.SaveChangesAsync();

        await AuditService.LogAsync(AuditAction.Update, "Patient", patientId.ToString(),
            $"Assigned condition #{conditionId} to patient #{patientId}.");
    }

    public static async Task RemoveConditionAsync(int patientId, int conditionId, DateTime diagnosedOn)
    {
        await using var ctx = new MediVaultDbContext();
        var pc = await ctx.PatientConditions.FirstOrDefaultAsync(x =>
            x.PatientId == patientId && x.ConditionId == conditionId && x.DiagnosedOn == diagnosedOn);
        if (pc == null) return;
        ctx.PatientConditions.Remove(pc);
        await ctx.SaveChangesAsync();
        await AuditService.LogAsync(AuditAction.Update, "Patient", patientId.ToString(),
            $"Removed condition #{conditionId} from patient #{patientId}.");
    }

    public static async Task<int> CountAsync()
    {
        await using var ctx = new MediVaultDbContext();
        return await ctx.Patients.CountAsync();
    }
}
