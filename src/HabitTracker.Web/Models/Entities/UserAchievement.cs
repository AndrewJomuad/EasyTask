namespace HabitTracker.Web.Models.Entities;

/// <summary>Join record: an achievement earned by a user. Unique per (UserId, AchievementId).</summary>
public class UserAchievement
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public int AchievementId { get; set; }
    public Achievement? Achievement { get; set; }

    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;
}
