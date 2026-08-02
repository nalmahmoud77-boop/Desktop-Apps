using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediVault.Data;
using MediVault.Models;
using Microsoft.EntityFrameworkCore;

namespace MediVault.Services;

public static class AppointmentService
{
    public static async Task<List<Appointment>> GetForRangeAsync(DateTime start, DateTime end)
    {
        await using var ctx = new MediVaultDbContext();
        return await ctx.Appointments
            .AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Where(a => a.StartTime >= start && a.StartTime < end)
            .OrderBy(a => a.StartTime)
            .ToListAsync();
    }

    public static async Task<List<Appointment>> GetUpcomingAsync(int take = 50)
    {
        await using var ctx = new MediVaultDbContext();
        var now = DateTime.Now;
        return await ctx.Appointments
            .AsNoTracking()
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Where(a => a.StartTime >= now && a.Status == AppointmentStatus.Scheduled)
            .OrderBy(a => a.StartTime)
            .Take(take)
            .ToListAsync();
    }

    public static async Task<List<Doctor>> GetActiveDoctorsAsync()
    {
        await using var ctx = new MediVaultDbContext();
        return await ctx.Doctors.AsNoTracking().Where(d => d.IsActive).OrderBy(d => d.FullName).ToListAsync();
    }

    public static async Task<bool> HasConflictAsync(int doctorId, DateTime start, DateTime end, int? ignoreAppointmentId = null)
    {
        await using var ctx = new MediVaultDbContext();
        return await ctx.Appointments.AnyAsync(a =>
            a.DoctorId == doctorId &&
            a.Status != AppointmentStatus.Cancelled &&
            (ignoreAppointmentId == null || a.Id != ignoreAppointmentId) &&
            a.StartTime < end && start < a.EndTime);
    }

    public static async Task<Appointment> CreateAsync(Appointment appointment)
    {
        if (appointment.EndTime <= appointment.StartTime)
            throw new InvalidOperationException("End time must be after start time.");

        if (await HasConflictAsync(appointment.DoctorId, appointment.StartTime, appointment.EndTime))
            throw new InvalidOperationException("This doctor already has an appointment in that time slot.");

        await using var ctx = new MediVaultDbContext();
        appointment.CreatedAt = DateTime.UtcNow;
        ctx.Appointments.Add(appointment);
        await ctx.SaveChangesAsync();

        await AuditService.LogAsync(AuditAction.Create, "Appointment", appointment.Id.ToString(),
            $"Booked appointment for patient #{appointment.PatientId} with doctor #{appointment.DoctorId} on {appointment.StartTime:yyyy-MM-dd HH:mm}.");

        return appointment;
    }

    public static async Task<Appointment> UpdateAsync(Appointment appointment)
    {
        if (appointment.EndTime <= appointment.StartTime)
            throw new InvalidOperationException("End time must be after start time.");

        if (await HasConflictAsync(appointment.DoctorId, appointment.StartTime, appointment.EndTime, appointment.Id))
            throw new InvalidOperationException("This doctor already has an appointment in that time slot.");

        await using var ctx = new MediVaultDbContext();
        var existing = await ctx.Appointments.FirstOrDefaultAsync(a => a.Id == appointment.Id)
            ?? throw new InvalidOperationException("Appointment not found.");

        existing.PatientId = appointment.PatientId;
        existing.DoctorId = appointment.DoctorId;
        existing.StartTime = appointment.StartTime;
        existing.EndTime = appointment.EndTime;
        existing.Reason = appointment.Reason;
        existing.Notes = appointment.Notes;
        existing.Status = appointment.Status;

        await ctx.SaveChangesAsync();

        await AuditService.LogAsync(AuditAction.Update, "Appointment", existing.Id.ToString(),
            $"Updated appointment #{existing.Id}.");

        return existing;
    }

    public static async Task DeleteAsync(int id)
    {
        await using var ctx = new MediVaultDbContext();
        var appt = await ctx.Appointments.FirstOrDefaultAsync(a => a.Id == id);
        if (appt == null) return;
        ctx.Appointments.Remove(appt);
        await ctx.SaveChangesAsync();
        await AuditService.LogAsync(AuditAction.Delete, "Appointment", id.ToString(),
            $"Deleted appointment #{id}.");
    }

    public static async Task<int> CountTodayAsync()
    {
        await using var ctx = new MediVaultDbContext();
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        return await ctx.Appointments.CountAsync(a => a.StartTime >= today && a.StartTime < tomorrow);
    }
}
