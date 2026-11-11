using HabitTracker.Web.Models;
using HabitTracker.Web.Models.Entities;
using HabitTracker.Web.Services;

namespace HabitTracker.Tests;

/// <summary>
/// Unit tests for the streak engine — the core business rule of HTMS.
///
/// Rule (specification section 9C): a streak is a run of consecutive
/// <em>scheduled opportunities</em> that were completed. Days the habit is not
/// scheduled on never break a streak; a skipped or missed scheduled opportunity does.
/// Today is still open, so an unlogged today neither extends nor breaks the streak.
/// </summary>
public class StreakServiceTests
{
    private static readonly DateOnly Today = new(2026, 11, 11); // Wednesday

    private static Habit DailyHabit(DateOnly? start = null, DateOnly? end = null) => new()
    {
        Id = 1,
        Name = "Test habit",
        FrequencyType = FrequencyType.Daily,
        StartDate = start ?? Today.AddDays(-30),
        EndDate = end,
        IsActive = true
    };

    private static HabitLog Log(int habitId, DateOnly date, HabitLogStatus status = HabitLogStatus.Completed) => new()
    {
        HabitId = habitId,
        LogDate = date,
        Status = status,
        CompletedAt = date.ToDateTime(TimeOnly.MinValue)
    };

    private readonly StreakService _sut = new(null!);

    // ---------------------------------------------------------------- daily habits

    [Fact]
    public void NoLogs_ProducesZeroStreak()
    {
        var result = _sut.Calculate(DailyHabit(), Array.Empty<HabitLog>(), Today);

        Assert.Equal(0, result.Current);
        Assert.Equal(0, result.Longest);
        Assert.Equal(0, result.Completed);
    }

    [Fact]
    public void ConsecutiveCompletions_EndingToday_CountAsCurrentStreak()
    {
        var habit = DailyHabit();
        var logs = new[]
        {
            Log(1, Today),
            Log(1, Today.AddDays(-1)),
            Log(1, Today.AddDays(-2))
        };

        var result = _sut.Calculate(habit, logs, Today);

        Assert.Equal(3, result.Current);
        Assert.Equal(3, result.Longest);
        Assert.Equal(3, result.Completed);
    }

    [Fact]
    public void UnloggedToday_DoesNotBreakStreak()
    {
        var habit = DailyHabit();
        var logs = new[]
        {
            Log(1, Today.AddDays(-1)),
            Log(1, Today.AddDays(-2))
        };

        var result = _sut.Calculate(habit, logs, Today);

        Assert.Equal(2, result.Current);
    }

    [Fact]
    public void SkippedDay_BreaksStreak()
    {
        var habit = DailyHabit();
        var logs = new[]
        {
            Log(1, Today),
            Log(1, Today.AddDays(-1), HabitLogStatus.Skipped),
            Log(1, Today.AddDays(-2))
        };

        var result = _sut.Calculate(habit, logs, Today);

        Assert.Equal(1, result.Current);
        Assert.Equal(1, result.Longest);
        Assert.Equal(1, result.Skipped);
    }

    [Fact]
    public void MissedDay_BreaksStreak_ButLongestIsRemembered()
    {
        // Habit starts 4 days ago, so exactly one scheduled day (yesterday) is missed.
        var habit = DailyHabit(start: Today.AddDays(-4));
        var logs = new[]
        {
            Log(1, Today),
            // Today-1 missed
            Log(1, Today.AddDays(-2)),
            Log(1, Today.AddDays(-3)),
            Log(1, Today.AddDays(-4))
        };

        var result = _sut.Calculate(habit, logs, Today);

        Assert.Equal(1, result.Current);
        Assert.Equal(3, result.Longest);
        Assert.Equal(1, result.Missed);
    }

    [Fact]
    public void CompletionRate_IsCompletedOverScheduled()
    {
        var habit = DailyHabit(start: Today.AddDays(-3));
        var logs = new[]
        {
            Log(1, Today),
            Log(1, Today.AddDays(-1))
        };

        var result = _sut.Calculate(habit, logs, Today);

        // 4 scheduled days (today-3 .. today), 2 completed.
        Assert.Equal(4, result.ScheduledOpportunities);
        Assert.Equal(50.0, result.CompletionRate);
    }

    [Fact]
    public void HabitNotYetStarted_ProducesZeroStreak()
    {
        var habit = DailyHabit(start: Today.AddDays(5));

        var result = _sut.Calculate(habit, Array.Empty<HabitLog>(), Today);

        Assert.Equal(0, result.Current);
        Assert.Equal(0, result.ScheduledOpportunities);
    }

    [Fact]
    public void EndedHabit_StopsCountingAfterEndDate()
    {
        var habit = DailyHabit(start: Today.AddDays(-10), end: Today.AddDays(-5));
        var logs = new[]
        {
            Log(1, Today.AddDays(-5)),
            Log(1, Today.AddDays(-6)),
            Log(1, Today.AddDays(-7))
        };

        var result = _sut.Calculate(habit, logs, Today);

        Assert.Equal(3, result.Current);
        Assert.Equal(3, result.Longest);
    }

    [Fact]
    public void InactiveHabit_IsNeverScheduled()
    {
        var habit = DailyHabit();
        habit.IsActive = false;

        var result = _sut.Calculate(habit, new[] { Log(1, Today) }, Today);

        Assert.Equal(0, result.ScheduledOpportunities);
    }

    // ---------------------------------------------------------------- custom schedules

    [Fact]
    public void CustomSchedule_OnlyScheduledWeekdaysCount()
    {
        // Monday / Wednesday / Friday only.
        var habit = DailyHabit(start: Today.AddDays(-14));
        habit.FrequencyType = FrequencyType.Custom;
        habit.Schedules = new List<HabitSchedule>
        {
            new() { HabitId = 1, DayOfWeek = DayOfWeek.Monday },
            new() { HabitId = 1, DayOfWeek = DayOfWeek.Wednesday },
            new() { HabitId = 1, DayOfWeek = DayOfWeek.Friday }
        };

        // Today is Wednesday. Complete today, last Monday and last Friday.
        var logs = new[]
        {
            Log(1, Today),                    // Wed
            Log(1, Today.AddDays(-2)),        // Mon
            Log(1, Today.AddDays(-5))         // Fri (previous week)
        };

        var result = _sut.Calculate(habit, logs, Today);

        Assert.Equal(3, result.Current);
        Assert.Equal(3, result.Completed);
        // Non-scheduled days (Tue, Thu, Sat, Sun) must not be counted as opportunities.
        // Range is Today-14 .. Today inclusive = 15 days, containing 3 Wednesdays,
        // 2 Mondays and 2 Fridays.
        Assert.Equal(7, result.ScheduledOpportunities);
    }

    [Fact]
    public void CustomSchedule_NonScheduledDayNeverBreaksStreak()
    {
        var habit = DailyHabit(start: Today.AddDays(-7));
        habit.FrequencyType = FrequencyType.Custom;
        habit.Schedules = new List<HabitSchedule>
        {
            new() { HabitId = 1, DayOfWeek = DayOfWeek.Wednesday }
        };

        // Only Wednesdays are scheduled: today and last Wednesday.
        var logs = new[]
        {
            Log(1, Today),
            Log(1, Today.AddDays(-7))
        };

        var result = _sut.Calculate(habit, logs, Today);

        Assert.Equal(2, result.Current);
        Assert.Equal(0, result.Missed);
    }

    // ---------------------------------------------------------------- IsScheduledOn

    [Fact]
    public void IsScheduledOn_DailyHabit_IsTrueWithinRange()
    {
        var habit = DailyHabit(start: Today.AddDays(-1), end: Today.AddDays(1));

        Assert.True(habit.IsScheduledOn(Today));
        Assert.True(habit.IsScheduledOn(Today.AddDays(-1)));
        Assert.True(habit.IsScheduledOn(Today.AddDays(1)));
        Assert.False(habit.IsScheduledOn(Today.AddDays(-2)));
        Assert.False(habit.IsScheduledOn(Today.AddDays(2)));
    }

    [Fact]
    public void IsScheduledOn_WeeklyHabit_IsTrueOnlyOnScheduledWeekday()
    {
        var habit = DailyHabit();
        habit.FrequencyType = FrequencyType.Weekly;
        habit.Schedules = new List<HabitSchedule>
        {
            new() { HabitId = 1, DayOfWeek = DayOfWeek.Monday }
        };

        Assert.True(habit.IsScheduledOn(new DateOnly(2026, 11, 9)));  // Monday
        Assert.False(habit.IsScheduledOn(new DateOnly(2026, 11, 10))); // Tuesday
    }

    // ---------------------------------------------------------------- StreakResult

    [Fact]
    public void CompletionRate_WithNoOpportunities_IsZeroNotDivideByZero()
    {
        var result = new StreakResult(0, 0, 0, 0, 0, 0);

        Assert.Equal(0, result.CompletionRate);
    }

    [Fact]
    public void CompletionRate_RoundsToOneDecimal()
    {
        var result = new StreakResult(0, 0, 3, 1, 0, 0);

        Assert.Equal(33.3, result.CompletionRate);
    }
}
