using System.ComponentModel.DataAnnotations;

namespace HabitTracker.Web.Models;

/// <summary>How often a habit is expected to be performed.</summary>
public enum FrequencyType
{
    [Display(Name = "Every day")]
    Daily = 0,

    [Display(Name = "Specific weekdays")]
    Weekly = 1,

    [Display(Name = "Custom schedule")]
    Custom = 2
}

/// <summary>Result recorded for a habit on a given date.</summary>
public enum HabitLogStatus
{
    [Display(Name = "Completed")]
    Completed = 0,

    [Display(Name = "Skipped")]
    Skipped = 1
}

/// <summary>How a reminder repeats.</summary>
public enum RepeatType
{
    [Display(Name = "Once")]
    Once = 0,

    [Display(Name = "Every scheduled day")]
    Daily = 1,

    [Display(Name = "Weekly")]
    Weekly = 2
}

/// <summary>The measurable criterion used to unlock an achievement.</summary>
public enum AchievementCriteria
{
    [Display(Name = "Total points earned")]
    TotalPoints = 0,

    [Display(Name = "Total habit completions")]
    TotalCompletions = 1,

    [Display(Name = "Current streak (days)")]
    CurrentStreak = 2,

    [Display(Name = "Longest streak (days)")]
    LongestStreak = 3,

    [Display(Name = "Perfect days (100% completion)")]
    PerfectDays = 4,

    [Display(Name = "Active habits created")]
    ActiveHabits = 5
}
