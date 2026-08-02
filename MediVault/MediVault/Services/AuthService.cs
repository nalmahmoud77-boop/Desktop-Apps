using System;
using System.Threading.Tasks;
using MediVault.Data;
using MediVault.Models;
using Microsoft.EntityFrameworkCore;

namespace MediVault.Services;

public static class AuthService
{
    public static User? CurrentUser { get; private set; }

    public static async Task<User?> LoginAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return null;

        await using var ctx = new MediVaultDbContext();
        var user = await ctx.Users.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
        if (user == null) return null;

        bool ok;
        try
        {
            ok = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        }
        catch
        {
            ok = false;
        }
        if (!ok) return null;

        CurrentUser = user;

        ctx.AuditLogs.Add(new AuditLog
        {
            Username = user.Username,
            Action = AuditAction.Login,
            Entity = "User",
            EntityId = user.Id.ToString(),
            Details = $"User {user.Username} signed in.",
            Timestamp = DateTime.Now
        });
        await ctx.SaveChangesAsync();

        return user;
    }

    public static async Task LogoutAsync()
    {
        if (CurrentUser == null) return;

        await using var ctx = new MediVaultDbContext();
        ctx.AuditLogs.Add(new AuditLog
        {
            Username = CurrentUser.Username,
            Action = AuditAction.Logout,
            Entity = "User",
            EntityId = CurrentUser.Id.ToString(),
            Details = $"User {CurrentUser.Username} signed out.",
            Timestamp = DateTime.Now
        });
        await ctx.SaveChangesAsync();
        CurrentUser = null;
    }

    public static string HashPassword(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);
}
