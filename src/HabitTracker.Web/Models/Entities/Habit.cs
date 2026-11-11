using System.ComponentModel.DataAnnotations;

namespace HabitTracker.Web.Models.Entities;

/// <summary>A personal habit owned by exactly one user.</summary>
public class Habit
{
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }
    public ApplicationUser? User { get; set; }

    [Required]
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    [Required, StringLength(120)]
    [Display(Name = "Habit name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public FrequencyType FrequencyType { get; set; } = FrequencyType.Daily;

    [Range(1, 100)]
    [Display(Name = "Target per period")]
    public int TargetPerPeriod { get; set; } = 1;

    [DataType(DataType.Date)]
    [Display(Name = "Start date")]
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [DataType(DataType.Date)]
    [Display(Name = "End date (optional)")]
    public DateOnly? EndDate { get; set; }

    [StringLength(20)]
    public string Color { get; set; } = "#1D4ED8";

    [StringLength(60)]
    public string Icon { get; set; } = "bi-check2-circle";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<HabitSchedule> Schedules { get; set; } = new List<HabitSchedule>();
    public ICollection<HabitLog> Logs { get; set; } = new List<HabitLog>();
    public ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();

    /// <summary>True when the habit is expected on the supplied date (range + weekday).</summary>
    public bool IsScheduledOn(DateOnly date)
    {
        if (!IsActive) return false;
        if (date < StartDate) return false;
        if (EndDate.HasValue && date > EndDate.Value) return false;

        if (FrequencyType == FrequencyType.Daily) return true;

        return Schedules.Any(s => (int)s.DayOfWeek == (int)date.DayOfWeek);
    }
}
