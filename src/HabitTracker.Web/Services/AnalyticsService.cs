using HabitTracker.Web.Data;
using HabitTracker.Web.Models;
using HabitTracker.Web.Models.Entities;
using HabitTracker.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HabitTracker.Web.Services;

public interface IAnalyticsService
{
    Task<AnalyticsViewModel> BuildAsync(int userId, DateOnly from, DateOnly to, int? habitId = null, int? categoryId = null);
    Task<DashboardViewModel> BuildDashboardAsync(int userId);
    Task<ReportsViewModel> BuildReportAsync(int userId, DateOnly from, DateOnly to);
}

public class AnalyticsService : IAnalyticsService
{
    private readonly ApplicationDbContext _db;
    private readonly IStreakService _streaks;
    private readonly IActivityLogService _activity;

    public AnalyticsService(ApplicationDbContext db, IStreakService streaks, IActivityLogService activity)
    {
        _db = db;
        _streaks = streaks;
        _activity = activity;
    }

    public async Task<AnalyticsViewModel> BuildAsync(int userId, DateOnly from, DateOnly to, int? habitId = null, int? categoryId = null)
    {
        if (to < from) (from, to) = (to, from);
        var today = DateOnly.FromDateTime(DateTime.Today);

        var habits = await _db.Habits.AsNoTracking()
            .Include(h => h.Category)
            .Include(h => h.Schedules)
            .Where(h => h.UserId == userId)
            .ToListAsync();

        var logs = await _db.HabitLogs.AsNoTracking()
            .Where(l => l.Habit!.UserId == userId && l.LogDate >= from && l.LogDate <= to)
            .ToListAsync();

        var scoped = habits.AsEnumerable();
        if (habitId.HasValue && habitId > 0) scoped = scoped.Where(h => h.Id == habitId.Value);
        if (categoryId.HasValue && categoryId > 0) scoped = scoped.Where(h => h.CategoryId == categoryId.Value);
        var scopedList = scoped.ToList();
        var scopedIds = scopedList.Select(h => h.Id).ToHashSet();

        var scopedLogs = logs.Where(l => scopedIds.Contains(l.HabitId)).ToList();

        // ---- Daily series -------------------------------------------------
        var daily = new List<WeeklyPoint>();
        for (var d = from; d <= to; d = d.AddDays(1))
        {
            int scheduled = 0, completed = 0;
            foreach (var h in scopedList)
            {
                if (!h.IsScheduledOn(d)) continue;
                scheduled++;
                if (scopedLogs.Any(l => l.HabitId == h.Id && l.LogDate == d && l.Status == HabitLogStatus.Completed))
                    completed++;
            }
            daily.Add(new WeeklyPoint
            {
                Label = d.ToString("dd MMM"),
                Date = d,
                Scheduled = scheduled,
                Completed = completed
            });
        }

        // ---- Per-habit comparison ----------------------------------------
        var streaks = await _streaks.CalculateForHabitsAsync(scopedList, today);
        var comparison = new List<HabitComparisonRow>();

        foreach (var h in scopedList.OrderBy(h => h.Name))
        {
            int scheduled = 0, completed = 0, skipped = 0, missed = 0;
            for (var d = from; d <= to; d = d.AddDays(1))
            {
                if (!h.IsScheduledOn(d)) continue;
                scheduled++;
                var log = scopedLogs.FirstOrDefault(l => l.HabitId == h.Id && l.LogDate == d);
                if (log is null) { if (d < today) missed++; }
                else if (log.Status == HabitLogStatus.Completed) completed++;
                else skipped++;
            }

            var s = streaks.TryGetValue(h.Id, out var r) ? r : new StreakResult(0, 0, 0, 0, 0, 0);
            comparison.Add(new HabitComparisonRow
            {
                HabitId = h.Id,
                Name = h.Name,
                Color = h.Color,
                CategoryName = h.Category?.Name ?? "Uncategorised",
                Scheduled = scheduled,
                Completed = completed,
                Skipped = skipped,
                Missed = missed,
                CurrentStreak = s.Current,
                LongestStreak = s.Longest
            });
        }

        // ---- Category breakdown ------------------------------------------
        var categoryRows = comparison
            .GroupBy(c => c.CategoryName)
            .Select(g =>
            {
                var habit = scopedList.First(h => (h.Category?.Name ?? "Uncategorised") == g.Key);
                return new CategoryBreakdownRow
                {
                    Name = g.Key,
                    Color = habit.Category?.Color ?? "#64748B",
                    Scheduled = g.Sum(x => x.Scheduled),
                    Completed = g.Sum(x => x.Completed)
                };
            })
            .OrderByDescending(c => c.Completed)
            .ToList();

        // ---- Weekday breakdown -------------------------------------------
        var weekdayRows = new List<WeekdayRow>();
        for (var i = 0; i < 7; i++)
        {
            var day = (DayOfWeek)i;
            int scheduled = 0, completed = 0;
            for (var d = from; d <= to; d = d.AddDays(1))
            {
                if (d.DayOfWeek != day) continue;
                foreach (var h in scopedList)
                {
                    if (!h.IsScheduledOn(d)) continue;
                    scheduled++;
                    if (scopedLogs.Any(l => l.HabitId == h.Id && l.LogDate == d && l.Status == HabitLogStatus.Completed))
                        completed++;
                }
            }
            weekdayRows.Add(new WeekdayRow { Day = day.ToString()[..3], Scheduled = scheduled, Completed = completed });
        }

        var totalScheduled = comparison.Sum(c => c.Scheduled);
        var totalCompleted = comparison.Sum(c => c.Completed);
        var totalSkipped = comparison.Sum(c => c.Skipped);
        var totalMissed = comparison.Sum(c => c.Missed);

        var perfectDays = daily.Count(d => d.Scheduled > 0 && d.Completed == d.Scheduled);
        var activeDays = daily.Count(d => d.Completed > 0);

        return new AnalyticsViewModel
        {
            From = from,
            To = to,
            HabitId = habitId,
            CategoryId = categoryId,
            Categories = await _db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync(),
            Habits = habits.OrderBy(h => h.Name).ToList(),
            TotalScheduled = totalScheduled,
            TotalCompleted = totalCompleted,
            TotalSkipped = totalSkipped,
            TotalMissed = totalMissed,
            PerfectDays = perfectDays,
            ActiveDays = activeDays,
            BestStreak = streaks.Count == 0 ? 0 : streaks.Values.Max(s => s.Longest),
            CurrentStreak = streaks.Count == 0 ? 0 : streaks.Values.Max(s => s.Current),
            DailySeries = daily,
            HabitComparison = comparison,
            CategoryBreakdown = categoryRows,
            WeekdayBreakdown = weekdayRows
        };
    }

    public async Task<DashboardViewModel> BuildDashboardAsync(int userId)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var user = await _db.Users.AsNoTracking().FirstAsync(u => u.Id == userId);

        var habits = await _db.Habits.AsNoTracking()
            .Include(h => h.Category)
            .Include(h => h.Schedules)
            .Where(h => h.UserId == userId)
            .ToListAsync();

        var active = habits.Where(h => h.IsActive).ToList();
        var scheduledToday = active.Where(h => h.IsScheduledOn(today)).ToList();

        var todayLogs = await _db.HabitLogs.AsNoTracking()
            .Where(l => l.Habit!.UserId == userId && l.LogDate == today)
            .ToListAsync();

        var streaks = await _streaks.CalculateForHabitsAsync(active, today);

        var items = scheduledToday.Select(h =>
        {
            var log = todayLogs.FirstOrDefault(l => l.HabitId == h.Id);
            return new TodayHabitItem
            {
                HabitId = h.Id,
                Name = h.Name,
                CategoryName = h.Category?.Name,
                CategoryColor = h.Category?.Color ?? "#64748B",
                Icon = h.Icon,
                Color = h.Color,
                CurrentStreak = streaks.TryGetValue(h.Id, out var s) ? s.Current : 0,
                TargetPerPeriod = h.TargetPerPeriod,
                Status = log?.Status,
                Notes = log?.Notes,
                IsScheduled = true
            };
        }).OrderBy(i => i.Status is null ? 0 : 1).ThenBy(i => i.Name).ToList();

        // ---- Weekly progress (last 7 days) -------------------------------
        var weekLogs = await _db.HabitLogs.AsNoTracking()
            .Where(l => l.Habit!.UserId == userId && l.LogDate >= today.AddDays(-6) && l.LogDate <= today)
            .ToListAsync();

        var weekly = new List<WeeklyPoint>();
        for (var d = today.AddDays(-6); d <= today; d = d.AddDays(1))
        {
            int scheduled = 0, completed = 0;
            foreach (var h in active)
            {
                if (!h.IsScheduledOn(d)) continue;
                scheduled++;
                if (weekLogs.Any(l => l.HabitId == h.Id && l.LogDate == d && l.Status == HabitLogStatus.Completed))
                    completed++;
            }
            weekly.Add(new WeeklyPoint { Label = d.ToString("ddd"), Date = d, Scheduled = scheduled, Completed = completed });
        }

        var totalCompletions = await _db.HabitLogs.AsNoTracking()
            .CountAsync(l => l.Habit!.UserId == userId && l.Status == HabitLogStatus.Completed);

        var earned = await _db.UserAchievements.AsNoTracking()
            .Where(ua => ua.UserId == userId)
            .Include(ua => ua.Achievement)
            .OrderByDescending(ua => ua.EarnedAt)
            .ToListAsync();

        var totalAchievements = await _db.Achievements.CountAsync(a => a.IsActive);

        var upcoming = await _db.Reminders.AsNoTracking()
            .Include(r => r.Habit)
            .Where(r => r.IsEnabled && r.Habit!.UserId == userId && r.Habit.IsActive)
            .OrderBy(r => r.ReminderTime)
            .Take(5)
            .ToListAsync();

        var topStreaks = active
            .Select(h =>
            {
                var s = streaks.TryGetValue(h.Id, out var r) ? r : new StreakResult(0, 0, 0, 0, 0, 0);
                return new HabitStreakRow
                {
                    HabitId = h.Id,
                    Name = h.Name,
                    Color = h.Color,
                    Icon = h.Icon,
                    Current = s.Current,
                    Longest = s.Longest,
                    CompletionRate = s.CompletionRate
                };
            })
            .OrderByDescending(x => x.Current)
            .ThenByDescending(x => x.CompletionRate)
            .Take(5)
            .ToList();

        var completedToday = items.Count(i => i.Status == HabitLogStatus.Completed);

        return new DashboardViewModel
        {
            FullName = user.FullName,
            Today = today,
            ActiveHabits = active.Count,
            ScheduledToday = items.Count,
            CompletedToday = completedToday,
            SkippedToday = items.Count(i => i.Status == HabitLogStatus.Skipped),
            PendingToday = items.Count(i => i.Status is null),
            TodayCompletionRate = items.Count == 0 ? 0 : Math.Round(completedToday * 100.0 / items.Count, 1),
            CurrentDayStreak = await _streaks.CurrentDayStreakAsync(userId, today),
            LongestStreak = streaks.Count == 0 ? 0 : streaks.Values.Max(s => s.Longest),
            TotalPoints = user.TotalPoints,
            TotalCompletions = totalCompletions,
            AchievementsEarned = earned.Count,
            AchievementsAvailable = totalAchievements,
            TodayHabits = items,
            WeeklyProgress = weekly,
            TopStreaks = topStreaks,
            RecentActivity = await _activity.RecentAsync(8, userId),
            RecentAchievements = earned.Take(4).Select(e => e.Achievement!).ToList(),
            UpcomingReminders = upcoming
        };
    }

    public async Task<ReportsViewModel> BuildReportAsync(int userId, DateOnly from, DateOnly to)
    {
        var analytics = await BuildAsync(userId, from, to);

        var logs = await _db.HabitLogs.AsNoTracking()
            .Include(l => l.Habit).ThenInclude(h => h!.Category)
            .Where(l => l.Habit!.UserId == userId && l.LogDate >= from && l.LogDate <= to)
            .OrderByDescending(l => l.LogDate)
            .ThenBy(l => l.Habit!.Name)
            .ToListAsync();

        return new ReportsViewModel
        {
            From = from,
            To = to,
            Analytics = analytics,
            Logs = logs,
            LogCount = logs.Count
        };
    }
}
