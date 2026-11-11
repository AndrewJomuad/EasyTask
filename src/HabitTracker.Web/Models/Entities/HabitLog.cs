using System.ComponentModel.DataAnnotations;

namespace HabitTracker.Web.Models.Entities;

/// <summary>Daily record for a habit. Unique per (HabitId, LogDate).</summary>
public class HabitLog
{
    public int Id { get; set; }

    public int HabitId { get; set; }
    public Habit? Habit { get; set; }

    [DataType(DataType.Date)]
    public DateOnly LogDate { get; set; }

    public HabitLogStatus Status { get; set; } = HabitLogStatus.Completed;

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
