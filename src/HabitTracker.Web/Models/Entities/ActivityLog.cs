using System.ComponentModel.DataAnnotations;

namespace HabitTracker.Web.Models.Entities;

/// <summary>Audit trail of security-relevant and administrative actions.</summary>
public class ActivityLog
{
    public int Id { get; set; }

    public int? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    [Required, StringLength(120)]
    public string Action { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Details { get; set; }

    [StringLength(60)]
    public string? IpAddress { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
