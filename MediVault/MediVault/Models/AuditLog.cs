using System;
using System.ComponentModel.DataAnnotations;

namespace MediVault.Models;

public enum AuditAction
{
    Create,
    Update,
    Delete,
    Login,
    Logout
}

public class AuditLog
{
    public int Id { get; set; }

    [Required, MaxLength(80)]
    public string Username { get; set; } = string.Empty;

    public AuditAction Action { get; set; }

    [Required, MaxLength(80)]
    public string Entity { get; set; } = string.Empty;

    [MaxLength(60)]
    public string? EntityId { get; set; }

    [MaxLength(2000)]
    public string? Details { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.Now;
}
