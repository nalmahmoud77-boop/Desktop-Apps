using System;
using System.Linq;
using System.Threading.Tasks;
using BCrypt.Net;
using MediVault.Models;
using Microsoft.EntityFrameworkCore;

namespace MediVault.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync()
    {
        using var ctx = new MediVaultDbContext();
        await ctx.Database.EnsureCreatedAsync();

        if (!await ctx.Users.AnyAsync())
        {
            ctx.Users.Add(new User
            {
                Username = "admin",
                FullName = "System Administrator",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123", workFactor: 11),
                Role = UserRole.Admin
            });
            ctx.Users.Add(new User
            {
                Username = "doctor",
                FullName = "Dr. Sarah Mitchell",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("doctor123", workFactor: 11),
                Role = UserRole.Doctor
            });
            await ctx.SaveChangesAsync();
        }

        // EnsureCreated doesn't add tables to a database that already exists, so
        // create RolePermissions on demand for databases provisioned before this
        // feature shipped. No-op for a freshly created database.
        await ctx.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "RolePermissions" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_RolePermissions" PRIMARY KEY AUTOINCREMENT,
                "Role" TEXT NOT NULL,
                "Permission" TEXT NOT NULL,
                "Allowed" INTEGER NOT NULL
            );
            """);
        await ctx.Database.ExecuteSqlRawAsync(
            """
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_RolePermissions_Role_Permission"
                ON "RolePermissions" ("Role", "Permission");
            """);

        if (!await ctx.RolePermissions.AnyAsync())
        {
            // Doctor and Receptionist defaults. Admin is always granted everything
            // in code, but we seed its rows too so the matrix displays consistently.
            var doctorAllowed = new[]
            {
                AppPermission.ViewDashboard, AppPermission.ViewPatients,
                AppPermission.ViewAppointments, AppPermission.ViewPrescriptions
            };
            var receptionistAllowed = new[]
            {
                AppPermission.ViewDashboard, AppPermission.ViewPatients,
                AppPermission.ViewAppointments
            };

            foreach (AppPermission permission in Enum.GetValues(typeof(AppPermission)))
            {
                ctx.RolePermissions.Add(new RolePermission { Role = UserRole.Admin, Permission = permission, Allowed = true });
                ctx.RolePermissions.Add(new RolePermission { Role = UserRole.Doctor, Permission = permission, Allowed = doctorAllowed.Contains(permission) });
                ctx.RolePermissions.Add(new RolePermission { Role = UserRole.Receptionist, Permission = permission, Allowed = receptionistAllowed.Contains(permission) });
            }
            await ctx.SaveChangesAsync();
        }

        if (!await ctx.Doctors.AnyAsync())
        {
            ctx.Doctors.AddRange(
                new Doctor { FullName = "Dr. Sarah Mitchell", Specialty = "General Practice", LicenseNumber = "MD-1001", Phone = "+1-555-0101", Email = "s.mitchell@medivault.io" },
                new Doctor { FullName = "Dr. Jonathan Reed", Specialty = "Cardiology", LicenseNumber = "MD-1002", Phone = "+1-555-0102", Email = "j.reed@medivault.io" },
                new Doctor { FullName = "Dr. Amelia Carter", Specialty = "Pediatrics", LicenseNumber = "MD-1003", Phone = "+1-555-0103", Email = "a.carter@medivault.io" },
                new Doctor { FullName = "Dr. Marcus Bennett", Specialty = "Dermatology", LicenseNumber = "MD-1004", Phone = "+1-555-0104", Email = "m.bennett@medivault.io" },
                new Doctor { FullName = "Dr. Priya Sharma", Specialty = "Neurology", LicenseNumber = "MD-1005", Phone = "+1-555-0105", Email = "p.sharma@medivault.io" }
            );
            await ctx.SaveChangesAsync();
        }

        if (!await ctx.Medications.AnyAsync())
        {
            ctx.Medications.AddRange(
                new Medication { Name = "Amoxicillin", GenericName = "Amoxicillin", Form = "Capsule", Strength = "500mg", Description = "Antibiotic" },
                new Medication { Name = "Lisinopril", GenericName = "Lisinopril", Form = "Tablet", Strength = "10mg", Description = "ACE inhibitor for hypertension" },
                new Medication { Name = "Metformin", GenericName = "Metformin HCl", Form = "Tablet", Strength = "500mg", Description = "Diabetes type 2" },
                new Medication { Name = "Atorvastatin", GenericName = "Atorvastatin", Form = "Tablet", Strength = "20mg", Description = "Cholesterol lowering" },
                new Medication { Name = "Ibuprofen", GenericName = "Ibuprofen", Form = "Tablet", Strength = "400mg", Description = "NSAID pain reliever" },
                new Medication { Name = "Paracetamol", GenericName = "Acetaminophen", Form = "Tablet", Strength = "500mg", Description = "Pain reliever and fever reducer" },
                new Medication { Name = "Omeprazole", GenericName = "Omeprazole", Form = "Capsule", Strength = "20mg", Description = "Proton pump inhibitor" },
                new Medication { Name = "Salbutamol", GenericName = "Albuterol", Form = "Inhaler", Strength = "100mcg", Description = "Bronchodilator" }
            );
            await ctx.SaveChangesAsync();
        }

        if (!await ctx.Conditions.AnyAsync())
        {
            ctx.Conditions.AddRange(
                new Condition { Name = "Hypertension", IcdCode = "I10", Description = "High blood pressure" },
                new Condition { Name = "Type 2 Diabetes", IcdCode = "E11", Description = "Diabetes mellitus type 2" },
                new Condition { Name = "Asthma", IcdCode = "J45", Description = "Chronic respiratory condition" },
                new Condition { Name = "Migraine", IcdCode = "G43", Description = "Recurrent headache disorder" },
                new Condition { Name = "Hyperlipidemia", IcdCode = "E78", Description = "Elevated cholesterol" },
                new Condition { Name = "Anxiety Disorder", IcdCode = "F41", Description = "Generalized anxiety" },
                new Condition { Name = "Allergic Rhinitis", IcdCode = "J30", Description = "Hay fever" }
            );
            await ctx.SaveChangesAsync();
        }

        if (!await ctx.Patients.AnyAsync())
        {
            var patients = new[]
            {
                new Patient { MedicalId = "MED-100001", FirstName = "Emily", LastName = "Johnson", DateOfBirth = new DateTime(1985, 3, 14), Gender = Gender.Female, Phone = "+1-555-2001", Email = "emily.j@example.com", Address = "120 Oak Street, Springfield", BloodGroup = "A+", MedicalHistory = "No significant history.", Allergies = "Penicillin" },
                new Patient { MedicalId = "MED-100002", FirstName = "Michael", LastName = "Chen", DateOfBirth = new DateTime(1972, 11, 22), Gender = Gender.Male, Phone = "+1-555-2002", Email = "m.chen@example.com", Address = "45 Pine Avenue, Riverside", BloodGroup = "O+", MedicalHistory = "Hypertension since 2015.", Allergies = "None" },
                new Patient { MedicalId = "MED-100003", FirstName = "Olivia", LastName = "Martinez", DateOfBirth = new DateTime(1990, 7, 8), Gender = Gender.Female, Phone = "+1-555-2003", Email = "o.martinez@example.com", Address = "78 Maple Lane, Brookfield", BloodGroup = "B+", MedicalHistory = "Asthma since childhood.", Allergies = "Pollen, Dust" },
                new Patient { MedicalId = "MED-100004", FirstName = "James", LastName = "Wilson", DateOfBirth = new DateTime(1965, 5, 30), Gender = Gender.Male, Phone = "+1-555-2004", Email = "j.wilson@example.com", Address = "230 Elm Road, Fairview", BloodGroup = "AB+", MedicalHistory = "Type 2 diabetes since 2010.", Allergies = "Sulfa drugs" },
                new Patient { MedicalId = "MED-100005", FirstName = "Sophia", LastName = "Brown", DateOfBirth = new DateTime(1998, 9, 12), Gender = Gender.Female, Phone = "+1-555-2005", Email = "s.brown@example.com", Address = "56 Cedar Court, Hillside", BloodGroup = "A-", MedicalHistory = "Migraines.", Allergies = "Latex" }
            };
            ctx.Patients.AddRange(patients);
            await ctx.SaveChangesAsync();
        }

        if (!await ctx.Appointments.AnyAsync())
        {
            var firstPatient = await ctx.Patients.OrderBy(p => p.Id).FirstAsync();
            var secondPatient = await ctx.Patients.OrderBy(p => p.Id).Skip(1).FirstAsync();
            var firstDoctor = await ctx.Doctors.OrderBy(d => d.Id).FirstAsync();
            var secondDoctor = await ctx.Doctors.OrderBy(d => d.Id).Skip(1).FirstAsync();

            var today = DateTime.Today;
            ctx.Appointments.AddRange(
                new Appointment { PatientId = firstPatient.Id, DoctorId = firstDoctor.Id, StartTime = today.AddHours(10), EndTime = today.AddHours(10).AddMinutes(30), Reason = "General check-up", Status = AppointmentStatus.Scheduled },
                new Appointment { PatientId = secondPatient.Id, DoctorId = secondDoctor.Id, StartTime = today.AddDays(1).AddHours(14), EndTime = today.AddDays(1).AddHours(14).AddMinutes(45), Reason = "Cardiology follow-up", Status = AppointmentStatus.Scheduled }
            );
            await ctx.SaveChangesAsync();
        }
    }
}
