using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediVault.Data;
using MediVault.Models;
using Microsoft.EntityFrameworkCore;

namespace MediVault.Services;

public static class PrescriptionService
{
    public static async Task<List<Prescription>> GetAllAsync()
    {
        await using var ctx = new MediVaultDbContext();
        return await ctx.Prescriptions
            .AsNoTracking()
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
            .Include(p => p.Items).ThenInclude(i => i.Medication)
            .OrderByDescending(p => p.IssuedOn)
            .ToListAsync();
    }

    public static async Task<Prescription?> GetByIdAsync(int id)
    {
        await using var ctx = new MediVaultDbContext();
        return await ctx.Prescriptions
            .AsNoTracking()
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
            .Include(p => p.Items).ThenInclude(i => i.Medication)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public static async Task<Prescription> CreateAsync(Prescription prescription)
    {
        await using var ctx = new MediVaultDbContext();
        if (string.IsNullOrWhiteSpace(prescription.Code))
        {
            var year = DateTime.Now.Year;
            var lastSeq = await ctx.Prescriptions
                .Where(p => p.Code.StartsWith($"RX-{year}-"))
                .CountAsync();
            prescription.Code = $"RX-{year}-{(lastSeq + 1):D5}";
        }
        prescription.IssuedOn = DateTime.Now;
        ctx.Prescriptions.Add(prescription);
        await ctx.SaveChangesAsync();

        await AuditService.LogAsync(AuditAction.Create, "Prescription", prescription.Id.ToString(),
            $"Issued prescription {prescription.Code} for patient #{prescription.PatientId}.");

        return prescription;
    }

    public static async Task DeleteAsync(int id)
    {
        await using var ctx = new MediVaultDbContext();
        var rx = await ctx.Prescriptions.FirstOrDefaultAsync(p => p.Id == id);
        if (rx == null) return;
        ctx.Prescriptions.Remove(rx);
        await ctx.SaveChangesAsync();
        await AuditService.LogAsync(AuditAction.Delete, "Prescription", id.ToString(),
            $"Deleted prescription {rx.Code}.");
    }
}
