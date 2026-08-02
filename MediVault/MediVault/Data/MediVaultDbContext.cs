using MediVault.Models;
using Microsoft.EntityFrameworkCore;

namespace MediVault.Data;

public class MediVaultDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Medication> Medications => Set<Medication>();
    public DbSet<Condition> Conditions => Set<Condition>();
    public DbSet<PatientMedication> PatientMedications => Set<PatientMedication>();
    public DbSet<PatientCondition> PatientConditions => Set<PatientCondition>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public MediVaultDbContext() { }
    public MediVaultDbContext(DbContextOptions<MediVaultDbContext> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var dbPath = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                "MediVault",
                "medivault.db");
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dbPath)!);
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // User
        modelBuilder.Entity<User>(b =>
        {
            b.HasIndex(u => u.Username).IsUnique();
        });

        // Patient
        modelBuilder.Entity<Patient>(b =>
        {
            b.HasIndex(p => p.MedicalId).IsUnique();
            b.Ignore(p => p.FullName);
            b.Ignore(p => p.Age);
        });

        // Doctor
        modelBuilder.Entity<Doctor>(b =>
        {
            b.HasIndex(d => d.LicenseNumber);
        });

        // Appointment - One-to-Many: Patient -> Appointments, Doctor -> Appointments
        modelBuilder.Entity<Appointment>(b =>
        {
            b.HasOne(a => a.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(a => a.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasIndex(a => new { a.DoctorId, a.StartTime });
            b.Property(a => a.Status).HasConversion<string>();
        });

        // Many-to-Many: Patient <-> Medication via PatientMedication (with payload)
        modelBuilder.Entity<PatientMedication>(b =>
        {
            b.HasKey(pm => new { pm.PatientId, pm.MedicationId, pm.StartDate });

            b.HasOne(pm => pm.Patient)
                .WithMany(p => p.PatientMedications)
                .HasForeignKey(pm => pm.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(pm => pm.Medication)
                .WithMany(m => m.PatientMedications)
                .HasForeignKey(pm => pm.MedicationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Many-to-Many: Patient <-> Condition via PatientCondition (with payload)
        modelBuilder.Entity<PatientCondition>(b =>
        {
            b.HasKey(pc => new { pc.PatientId, pc.ConditionId, pc.DiagnosedOn });

            b.HasOne(pc => pc.Patient)
                .WithMany(p => p.PatientConditions)
                .HasForeignKey(pc => pc.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(pc => pc.Condition)
                .WithMany(c => c.PatientConditions)
                .HasForeignKey(pc => pc.ConditionId)
                .OnDelete(DeleteBehavior.Restrict);

            b.Property(pc => pc.Severity).HasConversion<string>();
        });

        // Prescription
        modelBuilder.Entity<Prescription>(b =>
        {
            b.HasIndex(p => p.Code).IsUnique();

            b.HasOne(p => p.Patient)
                .WithMany(p => p.Prescriptions)
                .HasForeignKey(p => p.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(p => p.Doctor)
                .WithMany(d => d.Prescriptions)
                .HasForeignKey(p => p.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // PrescriptionItem
        modelBuilder.Entity<PrescriptionItem>(b =>
        {
            b.HasOne(pi => pi.Prescription)
                .WithMany(p => p.Items)
                .HasForeignKey(pi => pi.PrescriptionId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(pi => pi.Medication)
                .WithMany(m => m.PrescriptionItems)
                .HasForeignKey(pi => pi.MedicationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // AuditLog
        modelBuilder.Entity<AuditLog>(b =>
        {
            b.HasIndex(a => a.Timestamp);
            b.Property(a => a.Action).HasConversion<string>();
        });

        // RolePermission - one row per (Role, Permission)
        modelBuilder.Entity<RolePermission>(b =>
        {
            b.HasIndex(rp => new { rp.Role, rp.Permission }).IsUnique();
            b.Property(rp => rp.Role).HasConversion<string>();
            b.Property(rp => rp.Permission).HasConversion<string>();
        });

        modelBuilder.Entity<User>().Property(u => u.Role).HasConversion<string>();
        modelBuilder.Entity<Patient>().Property(p => p.Gender).HasConversion<string>();
    }
}
