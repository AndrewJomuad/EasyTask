using Microsoft.EntityFrameworkCore;
using HabitTracker.Web.Models;
using HabitTracker.Web.Models.Entities;

namespace HabitTracker.Web.Services;

/// <summary>Aggregated streak statistics for one habit.</summary>
public record StreakResult(
    int Current,
    int Longest,
    int ScheduledOpportunities,
    int Completed,
    int Skipped,
    int Missed)
{
    public double CompletionRate =>
        ScheduledOpportunities == 0 ? 0 : Math.Round(Completed * 100.0 / ScheduledOpportunities, 1);
}

/// <summary>
/// Streak rules (specification section 9C): a streak is a run of consecutive
/// <em>scheduled opportunities</em> that were completed. Non-scheduled days never
/// break a streak; a skipped or missed scheduled opportunity does.
/// </summary>
public interface IStreakService
{
    StreakResult Calculate(Habit habit, IReadOnlyCollection<HabitLog> logs, DateOnly today);
    Task<StreakResult> CalculateAsync(Habit habit, DateOnly today);
    Task<Dictionary<int, StreakResult>> CalculateForHabitsAsync(IEnumerable<Habit> habits, DateOnly today);
    Task<int> CurrentDayStreakAsync(int userId, DateOnly today);
}

public class StreakService : IStreakService
{
    private readonly Data.ApplicationDbContext _db;

    public StreakService(Data.ApplicationDbContext db) => _db = db;

    public StreakResult Calculate(Habit habit, IReadOnlyCollection<HabitLog> logs, DateOnly today)
    {
        var byDate = logs
            .Where(l => l.HabitId == habit.Id)
            .GroupBy(l => l.LogDate)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CompletedAt).First());

        var start = habit.StartDate;
        var end = habit.EndDate.HasValue && habit.EndDate.Value < today ? habit.EndDate.Value : today;
        if (end < start)
            return new StreakResult(0, 0, 0, 0, 0, 0);

        int scheduled = 0, completed = 0, skipped = 0, missed = 0;
        int current = 0, longest = 0, run = 0;

        for (var date = start; date <= end; date = date.AddDays(1))
        {
            if (!habit.IsScheduledOn(date)) continue;
            scheduled++;

            if (byDate.TryGetValue(date, out var log))
            {
                if (log.Status == HabitLogStatus.Completed)
                {
                    completed++;
                    run++;
                    if (run > longest) longest = run;
                }
                else
                {
                    skipped++;
                    run = 0;
                }
            }
            else if (date == today)
            {
                // Today is still open: it neither extends nor breaks the streak yet.
            }
            else
            {
                missed++;
                run = 0;
            }
        }

        // Current streak = the run ending at the most recent scheduled opportunity
        // (today counts only when it has already been completed).
        current = 0;
        for (var date = end; date >= start; date = date.AddDays(-1))
        {
            if (!habit.IsScheduledOn(date)) continue;

            if (byDate.TryGetValue(date, out var log) && log.Status == HabitLogStatus.Completed)
            {
                current++;
                continue;
            }

            if (date == today) continue; // pending today does not break the streak
            break;
        }

        return new StreakResult(current, longest, scheduled, completed, skipped, missed);
    }

    public async Task<StreakResult> CalculateAsync(Habit habit, DateOnly today)
    {
        var logs = await _db.HabitLogs.AsNoTracking()
            .Where(l => l.HabitId == habit.Id)
            .ToListAsync();
        return Calculate(habit, logs, today);
    }

    public async Task<Dictionary<int, StreakResult>> CalculateForHabitsAsync(IEnumerable<Habit> habits, DateOnly today)
    {
        var list = habits.ToList();
        var ids = list.Select(h => h.Id).ToList();
        var logs = await _db.HabitLogs.AsNoTracking()
            .Where(l => ids.Contains(l.HabitId))
            .ToListAsync();

        var grouped = logs.GroupBy(l => l.HabitId).ToDictionary(g => g.Key, g => (IReadOnlyCollection<HabitLog>)g.ToList());
        return list.ToDictionary(
            h => h.Id,
            h => Calculate(h, grouped.TryGetValue(h.Id, out var l) ? l : Array.Empty<HabitLog>(), today));
    }

    /// <summary>
    /// Consecutive days (ending today or yesterday) on which the user completed at
    /// least one scheduled habit. Used for the dashboard "day streak" highlight.
    /// </summary>
    public async Task<int> CurrentDayStreakAsync(int userId, DateOnly today)
    {
        var completedDates = await _db.HabitLogs.AsNoTracking()
            .Where(l => l.Habit!.UserId == userId && l.Status == HabitLogStatus.Completed)
            .Select(l => l.LogDate)
            .Distinct()
            .ToListAsync();

        var set = completedDates.ToHashSet();
        var cursor = today;
        if (!set.Contains(cursor)) cursor = cursor.AddDays(-1);

        var streak = 0;
        while (set.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }
        return streak;
    }
}
