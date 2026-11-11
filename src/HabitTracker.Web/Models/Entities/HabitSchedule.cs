namespace HabitTracker.Web.Models.Entities;

/// <summary>One weekday on which a habit is scheduled. A habit may have several.</summary>
public class HabitSchedule
{
    public int Id { get; set; }

    public int HabitId { get; set; }
    public Habit? Habit { get; set; }

    public DayOfWeek DayOfWeek { get; set; }
}
