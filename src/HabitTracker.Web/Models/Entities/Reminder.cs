using System.ComponentModel.DataAnnotations;

namespace HabitTracker.Web.Models.Entities;

/// <summary>Reminder configuration attached to a habit.</summary>
public class Reminder
{
    public int Id { get; set; }

    public int HabitId { get; set; }
    public Habit? Habit { get; set; }

    [DataType(DataType.Time)]
    [Display(Name = "Reminder time")]
    public TimeOnly ReminderTime { get; set; } = new TimeOnly(8, 0);

    public RepeatType RepeatType { get; set; } = RepeatType.Daily;

    public bool IsEnabled { get; set; } = true;

    /// <summary>Last time the reminder was surfaced, used to suppress duplicates.</summary>
    public DateTime? LastTriggeredAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
