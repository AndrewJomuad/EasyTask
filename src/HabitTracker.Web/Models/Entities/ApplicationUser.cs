using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace HabitTracker.Web.Models.Entities;

/// <summary>
/// Application account. Extends ASP.NET Core Identity so that password hashing,
/// lockout, security stamps and role membership are handled by the framework.
/// </summary>
public class ApplicationUser : IdentityUser<int>
{
    [Required, StringLength(120)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Bio { get; set; }

    [StringLength(200)]
    public string? TimeZoneId { get; set; } = "Asia/Singapore";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    /// <summary>Denormalised running total, always reconciled from the reward ledger.</summary>
    public int TotalPoints { get; set; }

    public ICollection<Habit> Habits { get; set; } = new List<Habit>();
    public ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
    public ICollection<RewardTransaction> RewardTransactions { get; set; } = new List<RewardTransaction>();
    public ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
}
