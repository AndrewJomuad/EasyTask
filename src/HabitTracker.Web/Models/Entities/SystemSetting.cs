using System.ComponentModel.DataAnnotations;

namespace HabitTracker.Web.Models.Entities;

/// <summary>Key/value configuration editable by administrators.</summary>
public class SystemSetting
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Key { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string Value { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Description { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
