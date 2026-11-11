using System.ComponentModel.DataAnnotations;

namespace HabitTracker.Web.Models.Entities;

/// <summary>Definition of a badge that users can unlock.</summary>
public class Achievement
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    [Display(Name = "Achievement name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(400)]
    public string? Description { get; set; }

    public AchievementCriteria Criteria { get; set; } = AchievementCriteria.TotalCompletions;

    [Range(1, 100000)]
    [Display(Name = "Threshold")]
    public int PointsRequired { get; set; } = 10;

    [StringLength(60)]
    public string Icon { get; set; } = "bi-award";

    [StringLength(20)]
    public string Color { get; set; } = "#F59E0B";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
}
