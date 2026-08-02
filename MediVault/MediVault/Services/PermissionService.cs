using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediVault.Data;
using MediVault.Models;
using Microsoft.EntityFrameworkCore;

namespace MediVault.Services;

/// <summary>
/// Owns the role × permission matrix. The matrix is cached in memory so access
/// checks are synchronous; call <see cref="RefreshAsync"/> at startup and after
/// any edit to keep the cache current.
/// </summary>
public static class PermissionService
{
    private static readonly Dictionary<(UserRole Role, AppPermission Permission), bool> _cache = new();

    public static async Task RefreshAsync()
    {
        await using var ctx = new MediVaultDbContext();
        var rows = await ctx.RolePermissions.AsNoTracking().ToListAsync();
        _cache.Clear();
        foreach (var row in rows)
            _cache[(row.Role, row.Permission)] = row.Allowed;
    }

    /// <summary>Whether the given role may use a permission. Admin always can.</summary>
    public static bool Has(UserRole role, AppPermission permission)
    {
        if (role == UserRole.Admin) return true;
        return _cache.TryGetValue((role, permission), out var allowed) && allowed;
    }

    /// <summary>Whether the currently signed-in user may use a permission.</summary>
    public static bool CurrentUserCan(AppPermission permission)
    {
        var role = AuthService.CurrentUser?.Role;
        return role.HasValue && Has(role.Value, permission);
    }

    public static async Task<List<RolePermission>> GetAllAsync()
    {
        await using var ctx = new MediVaultDbContext();
        return await ctx.RolePermissions.AsNoTracking().ToListAsync();
    }

    /// <summary>Upserts the supplied matrix entries, audits, and refreshes the cache.</summary>
    public static async Task SaveAsync(IEnumerable<RolePermission> entries)
    {
        await using var ctx = new MediVaultDbContext();
        foreach (var entry in entries)
        {
            var existing = await ctx.RolePermissions
                .FirstOrDefaultAsync(rp => rp.Role == entry.Role && rp.Permission == entry.Permission);
            if (existing == null)
                ctx.RolePermissions.Add(new RolePermission { Role = entry.Role, Permission = entry.Permission, Allowed = entry.Allowed });
            else
                existing.Allowed = entry.Allowed;
        }
        await ctx.SaveChangesAsync();

        await AuditService.LogAsync(AuditAction.Update, "Permissions", null, "Updated the role permission matrix.");
        await RefreshAsync();
    }
}
