using System.ComponentModel.DataAnnotations;

namespace HabitTracker.Web.Models.Entities;

/// <summary>Classification used to group habits (Health, Education, Fitness, Productivity...).</summary>
public class Category
{
    public int Id { get; set; }

    [Required, StringLength(80)]
    [Display(Name = "Category name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Description { get; set; }

    [StringLength(20)]
    public string Color { get; set; } = "#1D4ED8";

    [StringLength(60)]
    public string Icon { get; set; } = "bi-tag";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Habit> Habits { get; set; } = new List<Habit>();
}
