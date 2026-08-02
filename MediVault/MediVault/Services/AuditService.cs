using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediVault.Data;
using MediVault.Models;
using Microsoft.EntityFrameworkCore;

namespace MediVault.Services;

public static class AuditService
{
    public static async Task LogAsync(AuditAction action, string entity, string? entityId, string? details)
    {
        await using var ctx = new MediVaultDbContext();
        ctx.AuditLogs.Add(new AuditLog
        {
            Username = AuthService.CurrentUser?.Username ?? "system",
            Action = action,
            Entity = entity,
            EntityId = entityId,
            Details = details,
            Timestamp = DateTime.Now
        });
        await ctx.SaveChangesAsync();
    }

    public static async Task<List<AuditLog>> GetRecentAsync(int take = 500)
    {
        await using var ctx = new MediVaultDbContext();
        return await ctx.AuditLogs
            .AsNoTracking()
            .OrderByDescending(a => a.Timestamp)
            .Take(take)
            .ToListAsync();
    }
}
