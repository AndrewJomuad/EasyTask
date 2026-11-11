using System.ComponentModel.DataAnnotations;

namespace HabitTracker.Web.Models.Entities;

/// <summary>Append-only points ledger. The authoritative audit record for rewards.</summary>
public class RewardTransaction
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public int? LogId { get; set; }
    public HabitLog? Log { get; set; }

    public int PointsChange { get; set; }

    [Required, StringLength(200)]
    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
