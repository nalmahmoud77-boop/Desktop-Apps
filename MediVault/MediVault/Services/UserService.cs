using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediVault.Data;
using MediVault.Models;
using Microsoft.EntityFrameworkCore;

namespace MediVault.Services;

public static class UserService
{
    public static async Task<List<User>> GetAllAsync()
    {
        await using var ctx = new MediVaultDbContext();
        return await ctx.Users
            .AsNoTracking()
            .OrderBy(u => u.Username)
            .ToListAsync();
    }

    public static async Task<User> AddAsync(User user, string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("A password is required for a new user.");

        await using var ctx = new MediVaultDbContext();
        if (await ctx.Users.AnyAsync(u => u.Username == user.Username))
            throw new InvalidOperationException($"Username '{user.Username}' is already taken.");

        user.PasswordHash = AuthService.HashPassword(password);
        user.CreatedAt = DateTime.UtcNow;
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        await AuditService.LogAsync(AuditAction.Create, "User", user.Id.ToString(),
            $"Created user {user.Username} ({user.Role}).");

        return user;
    }

    /// <param name="newPassword">When non-empty, replaces the stored password.</param>
    public static async Task<User> UpdateAsync(User user, string? newPassword)
    {
        await using var ctx = new MediVaultDbContext();
        var existing = await ctx.Users.FirstOrDefaultAsync(u => u.Id == user.Id)
            ?? throw new InvalidOperationException("User not found.");

        if (existing.Username != user.Username &&
            await ctx.Users.AnyAsync(u => u.Username == user.Username && u.Id != user.Id))
            throw new InvalidOperationException($"Username '{user.Username}' is already taken.");

        // Don't let an admin demote or deactivate the last remaining active admin.
        bool wasActiveAdmin = existing.Role == UserRole.Admin && existing.IsActive;
        bool staysActiveAdmin = user.Role == UserRole.Admin && user.IsActive;
        if (wasActiveAdmin && !staysActiveAdmin)
        {
            var otherActiveAdmins = await ctx.Users.CountAsync(u =>
                u.Role == UserRole.Admin && u.IsActive && u.Id != existing.Id);
            if (otherActiveAdmins == 0)
                throw new InvalidOperationException("There must be at least one active administrator.");
        }

        var changes = new List<string>();
        if (existing.Username != user.Username) changes.Add($"Username: '{existing.Username}' → '{user.Username}'");
        if (existing.FullName != user.FullName) changes.Add($"FullName: '{existing.FullName}' → '{user.FullName}'");
        if (existing.Role != user.Role) changes.Add($"Role: {existing.Role} → {user.Role}");
        if (existing.IsActive != user.IsActive) changes.Add($"IsActive: {existing.IsActive} → {user.IsActive}");

        existing.Username = user.Username;
        existing.FullName = user.FullName;
        existing.Role = user.Role;
        existing.IsActive = user.IsActive;

        if (!string.IsNullOrWhiteSpace(newPassword))
        {
            existing.PasswordHash = AuthService.HashPassword(newPassword);
            changes.Add("Password reset");
        }

        await ctx.SaveChangesAsync();

        if (changes.Count > 0)
        {
            await AuditService.LogAsync(AuditAction.Update, "User", existing.Id.ToString(),
                $"Updated user {existing.Username}: {string.Join("; ", changes)}");
        }

        return existing;
    }

    public static async Task DeleteAsync(int id)
    {
        await using var ctx = new MediVaultDbContext();
        var user = await ctx.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return;

        if (user.Role == UserRole.Admin)
        {
            var otherActiveAdmins = await ctx.Users.CountAsync(u =>
                u.Role == UserRole.Admin && u.IsActive && u.Id != id);
            if (otherActiveAdmins == 0)
                throw new InvalidOperationException("You can't delete the last administrator.");
        }

        ctx.Users.Remove(user);
        await ctx.SaveChangesAsync();

        await AuditService.LogAsync(AuditAction.Delete, "User", id.ToString(),
            $"Deleted user {user.Username}.");
    }
}
