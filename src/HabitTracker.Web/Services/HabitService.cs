using HabitTracker.Web.Data;
using HabitTracker.Web.Models;
using HabitTracker.Web.Models.Entities;
using HabitTracker.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HabitTracker.Web.Services;

public record OperationResult(bool Success, string? Message = null, int? Id = null)
{
    public static OperationResult Ok(string? message = null, int? id = null) => new(true, message, id);
    public static OperationResult Fail(string message) => new(false, message);
}

public interface IHabitService
{
    Task<List<Habit>> GetUserHabitsAsync(int userId, bool includeInactive = false);
    Task<Habit?> GetOwnedHabitAsync(int habitId, int userId, bool tracking = false);
    Task<HabitListViewModel> GetListAsync(int userId, string? search, int? categoryId, string status);
    Task<HabitFormViewModel> BuildFormAsync(int userId, Habit? habit = null);
    Task<OperationResult> CreateAsync(int userId, HabitFormViewModel model);
    Task<OperationResult> UpdateAsync(int userId, HabitFormViewModel model);
    Task<OperationResult> ToggleActiveAsync(int habitId, int userId);
    Task<OperationResult> DeleteAsync(int habitId, int userId);
    Task<HabitDetailsViewModel?> GetDetailsAsync(int habitId, int userId);
    Task<DailyTrackerViewModel> GetTrackerAsync(int userId, DateOnly date);
    Task<OperationResult> RecordAsync(int userId, int habitId, DateOnly date, HabitLogStatus status, string? notes);
    Task<OperationResult> ClearLogAsync(int userId, int habitId, DateOnly date);
    Task<CalendarViewModel> GetCalendarAsync(int userId, int year, int month);
    Task<List<Category>> GetCategoriesAsync(bool activeOnly = true);
    static string DescribeSchedule(Habit habit) => string.Empty;
}

public class HabitService : IHabitService
{
    private readonly ApplicationDbContext _db;
    private readonly IStreakService _streaks;
    private readonly IAchievementService _achievements;
    private readonly ISettingsService _settings;
    private readonly IActivityLogService _activity;
    private readonly ILogger<HabitService> _logger;

    public HabitService(ApplicationDbContext db, IStreakService streaks, IAchievementService achievements,
        ISettingsService settings, IActivityLogService activity, ILogger<HabitService> logger)
    {
        _db = db;
        _streaks = streaks;
        _achievements = achievements;
        _settings = settings;
        _activity = activity;
        _logger = logger;
    }

    public Task<List<Category>> GetCategoriesAsync(bool activeOnly = true)
    {
        var q = _db.Categories.AsNoTracking().AsQueryable();
        if (activeOnly) q = q.Where(c => c.IsActive);
        return q.OrderBy(c => c.Name).ToListAsync();
    }

    public Task<List<Habit>> GetUserHabitsAsync(int userId, bool includeInactive = false)
    {
        var q = _db.Habits.AsNoTracking()
            .Include(h => h.Category)
            .Include(h => h.Schedules)
            .Where(h => h.UserId == userId);
        if (!includeInactive) q = q.Where(h => h.IsActive);
        return q.OrderBy(h => h.Name).ToListAsync();
    }

    public Task<Habit?> GetOwnedHabitAsync(int habitId, int userId, bool tracking = false)
    {
        var q = tracking ? _db.Habits.AsQueryable() : _db.Habits.AsNoTracking();
        return q.Include(h => h.Category)
                .Include(h => h.Schedules)
                .Include(h => h.Reminders)
                .FirstOrDefaultAsync(h => h.Id == habitId && h.UserId == userId);
    }

    public async Task<HabitListViewModel> GetListAsync(int userId, string? search, int? categoryId, string status)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var all = await _db.Habits.AsNoTracking()
            .Include(h => h.Category)
            .Include(h => h.Schedules)
            .Where(h => h.UserId == userId)
            .ToListAsync();

        var logs = await _db.HabitLogs.AsNoTracking()
            .Where(l => l.Habit!.UserId == userId)
            .ToListAsync();

        var streaks = await _streaks.CalculateForHabitsAsync(all, today);

        var filtered = all.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            filtered = filtered.Where(h =>
                h.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (h.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (h.Category?.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }
        if (categoryId.HasValue && categoryId > 0)
            filtered = filtered.Where(h => h.CategoryId == categoryId.Value);

        filtered = status switch
        {
            "inactive" => filtered.Where(h => !h.IsActive),
            "all" => filtered,
            _ => filtered.Where(h => h.IsActive)
        };

        var items = filtered
            .OrderByDescending(h => h.IsActive)
            .ThenBy(h => h.Name)
            .Select(h =>
            {
                var s = streaks.TryGetValue(h.Id, out var r) ? r : new StreakResult(0, 0, 0, 0, 0, 0);
                var todayLog = logs.FirstOrDefault(l => l.HabitId == h.Id && l.LogDate == today);
                return new HabitCardItem
                {
                    Habit = h,
                    CurrentStreak = s.Current,
                    LongestStreak = s.Longest,
                    CompletionRate = s.CompletionRate,
                    TotalCompletions = s.Completed,
                    ScheduleSummary = DescribeSchedule(h),
                    ScheduledToday = h.IsScheduledOn(today),
                    TodayStatus = todayLog?.Status
                };
            })
            .ToList();

        return new HabitListViewModel
        {
            Habits = items,
            Categories = await GetCategoriesAsync(false),
            Search = search,
            CategoryId = categoryId,
            Status = status,
            ActiveCount = all.Count(h => h.IsActive),
            InactiveCount = all.Count(h => !h.IsActive)
        };
    }

    public async Task<HabitFormViewModel> BuildFormAsync(int userId, Habit? habit = null)
    {
        var categories = await GetCategoriesAsync(false);
        if (habit is null)
        {
            return new HabitFormViewModel
            {
                Categories = categories,
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                SelectedDays = new List<DayOfWeek>()
            };
        }

        return new HabitFormViewModel
        {
            Id = habit.Id,
            Name = habit.Name,
            Description = habit.Description,
            CategoryId = habit.CategoryId,
            FrequencyType = habit.FrequencyType,
            TargetPerPeriod = habit.TargetPerPeriod,
            StartDate = habit.StartDate,
            EndDate = habit.EndDate,
            IsActive = habit.IsActive,
            Color = habit.Color,
            Icon = habit.Icon,
            SelectedDays = habit.Schedules.Select(s => s.DayOfWeek).Distinct().OrderBy(d => d).ToList(),
            Categories = categories
        };
    }

    public async Task<OperationResult> CreateAsync(int userId, HabitFormViewModel model)
    {
        if (model.EndDate.HasValue && model.EndDate.Value < model.StartDate)
            return OperationResult.Fail("The end date cannot be before the start date.");

        if (model.FrequencyType != FrequencyType.Daily && model.SelectedDays.Count == 0)
            return OperationResult.Fail("Select at least one weekday for a weekly or custom schedule.");

        if (!await _db.Categories.AnyAsync(c => c.Id == model.CategoryId))
            return OperationResult.Fail("The selected category does not exist.");

        var habit = new Habit
        {
            UserId = userId,
            CategoryId = model.CategoryId,
            Name = model.Name.Trim(),
            Description = model.Description?.Trim(),
            FrequencyType = model.FrequencyType,
            TargetPerPeriod = model.TargetPerPeriod,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            IsActive = model.IsActive,
            Color = model.Color,
            Icon = model.Icon,
            CreatedAt = DateTime.UtcNow
        };

        if (model.FrequencyType != FrequencyType.Daily)
        {
            habit.Schedules = model.SelectedDays.Distinct()
                .Select(d => new HabitSchedule { DayOfWeek = d })
                .ToList();
        }

        _db.Habits.Add(habit);
        await _db.SaveChangesAsync();

        await _activity.LogAsync("Habit created", $"Created habit '{habit.Name}' (#{habit.Id}).", userId);
        await _achievements.EvaluateAsync(userId);

        return OperationResult.Ok("Habit created successfully.", habit.Id);
    }

    public async Task<OperationResult> UpdateAsync(int userId, HabitFormViewModel model)
    {
        var habit = await _db.Habits
            .Include(h => h.Schedules)
            .FirstOrDefaultAsync(h => h.Id == model.Id && h.UserId == userId);

        if (habit is null) return OperationResult.Fail("Habit not found.");

        if (model.EndDate.HasValue && model.EndDate.Value < model.StartDate)
            return OperationResult.Fail("The end date cannot be before the start date.");

        if (model.FrequencyType != FrequencyType.Daily && model.SelectedDays.Count == 0)
            return OperationResult.Fail("Select at least one weekday for a weekly or custom schedule.");

        if (!await _db.Categories.AnyAsync(c => c.Id == model.CategoryId))
            return OperationResult.Fail("The selected category does not exist.");

        habit.CategoryId = model.CategoryId;
        habit.Name = model.Name.Trim();
        habit.Description = model.Description?.Trim();
        habit.FrequencyType = model.FrequencyType;
        habit.TargetPerPeriod = model.TargetPerPeriod;
        habit.StartDate = model.StartDate;
        habit.EndDate = model.EndDate;
        habit.IsActive = model.IsActive;
        habit.Color = model.Color;
        habit.Icon = model.Icon;
        habit.UpdatedAt = DateTime.UtcNow;

        _db.HabitSchedules.RemoveRange(habit.Schedules);
        habit.Schedules.Clear();

        if (model.FrequencyType != FrequencyType.Daily)
        {
            foreach (var day in model.SelectedDays.Distinct())
                habit.Schedules.Add(new HabitSchedule { DayOfWeek = day, HabitId = habit.Id });
        }

        await _db.SaveChangesAsync();
        await _activity.LogAsync("Habit updated", $"Updated habit '{habit.Name}' (#{habit.Id}).", userId);

        return OperationResult.Ok("Habit updated successfully.", habit.Id);
    }

    public async Task<OperationResult> ToggleActiveAsync(int habitId, int userId)
    {
        var habit = await _db.Habits.FirstOrDefaultAsync(h => h.Id == habitId && h.UserId == userId);
        if (habit is null) return OperationResult.Fail("Habit not found.");

        habit.IsActive = !habit.IsActive;
        habit.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _activity.LogAsync(habit.IsActive ? "Habit activated" : "Habit deactivated",
            $"Habit '{habit.Name}' (#{habit.Id}).", userId);

        return OperationResult.Ok(habit.IsActive ? "Habit reactivated." : "Habit deactivated.");
    }

    public async Task<OperationResult> DeleteAsync(int habitId, int userId)
    {
        var habit = await _db.Habits.FirstOrDefaultAsync(h => h.Id == habitId && h.UserId == userId);
        if (habit is null) return OperationResult.Fail("Habit not found.");

        var name = habit.Name;
        _db.Habits.Remove(habit);
        await _db.SaveChangesAsync();

        await _activity.LogAsync("Habit deleted", $"Deleted habit '{name}' (#{habitId}) and all its history.", userId);
        return OperationResult.Ok("Habit deleted.");
    }

    public async Task<HabitDetailsViewModel?> GetDetailsAsync(int habitId, int userId)
    {
        var habit = await GetOwnedHabitAsync(habitId, userId);
        if (habit is null) return null;

        var today = DateOnly.FromDateTime(DateTime.Today);
        var logs = await _db.HabitLogs.AsNoTracking()
            .Where(l => l.HabitId == habitId)
            .OrderByDescending(l => l.LogDate)
            .ToListAsync();

        var streak = _streaks.Calculate(habit, logs, today);

        var weeks = new List<WeeklyPoint>();
        for (var w = 3; w >= 0; w--)
        {
            var weekStart = today.AddDays(-(int)today.DayOfWeek - 7 * w);
            var weekEnd = weekStart.AddDays(6);
            int scheduled = 0, completed = 0;
            for (var d = weekStart; d <= weekEnd; d = d.AddDays(1))
            {
                if (!habit.IsScheduledOn(d)) continue;
                scheduled++;
                if (logs.Any(l => l.LogDate == d && l.Status == HabitLogStatus.Completed)) completed++;
            }
            weeks.Add(new WeeklyPoint
            {
                Label = $"{weekStart:dd MMM}",
                Date = weekStart,
                Scheduled = scheduled,
                Completed = completed
            });
        }

        var points = await _db.RewardTransactions.AsNoTracking()
            .Where(t => t.UserId == userId && t.LogId != null &&
                        _db.HabitLogs.Any(l => l.Id == t.LogId && l.HabitId == habitId))
            .SumAsync(t => (int?)t.PointsChange) ?? 0;

        return new HabitDetailsViewModel
        {
            Habit = habit,
            Streak = streak,
            RecentLogs = logs.Take(30).ToList(),
            Reminders = habit.Reminders.OrderBy(r => r.ReminderTime).ToList(),
            ScheduleSummary = DescribeSchedule(habit),
            LastFourWeeks = weeks,
            TotalPointsEarned = points
        };
    }

    public async Task<DailyTrackerViewModel> GetTrackerAsync(int userId, DateOnly date)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var habits = await _db.Habits.AsNoTracking()
            .Include(h => h.Category)
            .Include(h => h.Schedules)
            .Where(h => h.UserId == userId && h.IsActive)
            .ToListAsync();

        var scheduled = habits.Where(h => h.IsScheduledOn(date)).OrderBy(h => h.Name).ToList();
        var ids = scheduled.Select(h => h.Id).ToList();

        var logs = await _db.HabitLogs.AsNoTracking()
            .Where(l => ids.Contains(l.HabitId) && l.LogDate == date)
            .ToListAsync();

        var streaks = await _streaks.CalculateForHabitsAsync(scheduled, today);

        var items = scheduled.Select(h =>
        {
            var log = logs.FirstOrDefault(l => l.HabitId == h.Id);
            return new TrackerItem
            {
                HabitId = h.Id,
                Name = h.Name,
                Description = h.Description,
                CategoryName = h.Category?.Name ?? "Uncategorised",
                CategoryColor = h.Category?.Color ?? "#64748B",
                Color = h.Color,
                Icon = h.Icon,
                TargetPerPeriod = h.TargetPerPeriod,
                CurrentStreak = streaks.TryGetValue(h.Id, out var s) ? s.Current : 0,
                Status = log?.Status,
                Notes = log?.Notes,
                LogId = log?.Id
            };
        }).ToList();

        return new DailyTrackerViewModel { Date = date, Today = today, Items = items };
    }

    /// <summary>
    /// Completion workflow (specification 9A): ownership, active state, date range,
    /// schedule, duplicate prevention, points ledger, achievements and activity log —
    /// all inside one database transaction.
    /// </summary>

    public async Task<OperationResult> RecordAsync(
        int userId,
        int habitId,
        DateOnly date,
        HabitLogStatus status,
        string? notes)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        if (date > today)
            return OperationResult.Fail("You cannot record a habit for a future date.");

        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                var habit = await _db.Habits
                    .Include(h => h.Schedules)
                    .FirstOrDefaultAsync(h => h.Id == habitId && h.UserId == userId);

                if (habit is null)
                    return OperationResult.Fail("Habit not found.");

                if (!habit.IsActive)
                    return OperationResult.Fail(
                        "This habit is inactive. Reactivate it before recording progress.");

                if (date < habit.StartDate)
                    return OperationResult.Fail(
                        "The selected date is before the habit's start date.");

                if (habit.EndDate.HasValue && date > habit.EndDate.Value)
                    return OperationResult.Fail(
                        "The selected date is after the habit's end date.");

                if (!habit.IsScheduledOn(date))
                    return OperationResult.Fail(
                        "This habit is not scheduled on the selected date.");

                var existing = await _db.HabitLogs
                    .FirstOrDefaultAsync(l =>
                        l.HabitId == habitId && l.LogDate == date);

                if (existing is not null)
                {
                    if (existing.Status == status)
                        return OperationResult.Fail(
                            "This habit has already been recorded with that status for this date.");

                    var previous = existing.Status;

                    existing.Status = status;
                    existing.Notes = notes?.Trim();
                    existing.CompletedAt = DateTime.UtcNow;

                    if (previous == HabitLogStatus.Completed &&
                        status != HabitLogStatus.Completed)
                    {
                        var reward = await _db.RewardTransactions
                            .FirstOrDefaultAsync(t => t.LogId == existing.Id);

                        if (reward is not null)
                        {
                            var pointsToRemove = reward.PointsChange;

                            _db.RewardTransactions.Remove(reward);

                            var owner = await _db.Users
                                .FirstAsync(u => u.Id == userId);

                            owner.TotalPoints = Math.Max(
                                0,
                                owner.TotalPoints - pointsToRemove);
                        }
                    }
                    else if (previous != HabitLogStatus.Completed &&
                             status == HabitLogStatus.Completed)
                    {
                        await AwardPointsAsync(
                            userId,
                            existing.Id,
                            habit.Name);
                    }

                    await _db.SaveChangesAsync();
                    await tx.CommitAsync();

                    await _activity.LogAsync(
                        "Habit record updated",
                        $"Habit '{habit.Name}' on {date:yyyy-MM-dd} changed from {previous} to {status}.",
                        userId);

                    await _achievements.EvaluateAsync(userId);

                    return OperationResult.Ok(
                        $"Record updated to {status}.");
                }

                var log = new HabitLog
                {
                    HabitId = habitId,
                    LogDate = date,
                    Status = status,
                    Notes = notes?.Trim(),
                    CompletedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                _db.HabitLogs.Add(log);

                await _db.SaveChangesAsync();

                if (status == HabitLogStatus.Completed)
                {
                    await AwardPointsAsync(
                        userId,
                        log.Id,
                        habit.Name);
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                await _activity.LogAsync(
                    status == HabitLogStatus.Completed
                        ? "Habit completed"
                        : "Habit skipped",
                    $"Habit '{habit.Name}' on {date:yyyy-MM-dd}.",
                    userId);

                var unlocked = await _achievements.EvaluateAsync(userId);

                var message = status == HabitLogStatus.Completed
                    ? "Habit marked as completed."
                    : "Habit marked as skipped.";

                if (unlocked.Count > 0)
                {
                    message += " Achievement unlocked: " +
                        string.Join(", ", unlocked.Select(a => a.Name)) + "!";
                }

                return OperationResult.Ok(message);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        });
    }


    private async Task AwardPointsAsync(int userId, int logId, string habitName)
    {
        var points = await _settings.GetIntAsync(SettingKeys.PointsPerCompletion, 10);
        if (points == 0) return;

        _db.RewardTransactions.Add(new RewardTransaction
        {
            UserId = userId,
            LogId = logId,
            PointsChange = points,
            Reason = $"Habit completed: {habitName}",
            CreatedAt = DateTime.UtcNow
        });

        var user = await _db.Users.FirstAsync(u => u.Id == userId);
        user.TotalPoints += points;
    }

    public async Task<OperationResult> ClearLogAsync(int userId, int habitId, DateOnly date)
    {
        var habit = await _db.Habits.FirstOrDefaultAsync(h => h.Id == habitId && h.UserId == userId);
        if (habit is null) return OperationResult.Fail("Habit not found.");

        var log = await _db.HabitLogs.FirstOrDefaultAsync(l => l.HabitId == habitId && l.LogDate == date);
        if (log is null) return OperationResult.Fail("There is no record to clear for that date.");

        var reward = await _db.RewardTransactions.FirstOrDefaultAsync(t => t.LogId == log.Id);
        if (reward is not null)
        {
            _db.RewardTransactions.Remove(reward);
            var user = await _db.Users.FirstAsync(u => u.Id == userId);
            user.TotalPoints = Math.Max(0, user.TotalPoints - reward.PointsChange);
        }

        _db.HabitLogs.Remove(log);
        await _db.SaveChangesAsync();

        await _activity.LogAsync("Habit record cleared", $"Cleared record for '{habit.Name}' on {date:yyyy-MM-dd}.", userId);
        return OperationResult.Ok("Record cleared.");
    }

    public async Task<CalendarViewModel> GetCalendarAsync(int userId, int year, int month)
    {
        if (month < 1) { month = 12; year--; }
        if (month > 12) { month = 1; year++; }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var first = new DateOnly(year, month, 1);
        var last = first.AddMonths(1).AddDays(-1);

        var habits = await _db.Habits.AsNoTracking()
            .Include(h => h.Schedules)
            .Where(h => h.UserId == userId)
            .ToListAsync();

        var logs = await _db.HabitLogs.AsNoTracking()
            .Where(l => l.Habit!.UserId == userId && l.LogDate >= first && l.LogDate <= last)
            .ToListAsync();

        var days = new List<CalendarDay>();
        var gridStart = first.AddDays(-(int)first.DayOfWeek);
        var gridEnd = last.AddDays(6 - (int)last.DayOfWeek);

        for (var d = gridStart; d <= gridEnd; d = d.AddDays(1))
        {
            var inMonth = d.Month == month && d.Year == year;
            int scheduled = 0, completed = 0, skipped = 0, missed = 0;

            if (inMonth)
            {
                foreach (var h in habits)
                {
                    if (!h.IsScheduledOn(d)) continue;
                    scheduled++;
                    var log = logs.FirstOrDefault(l => l.HabitId == h.Id && l.LogDate == d);
                    if (log is null)
                    {
                        if (d < today) missed++;
                    }
                    else if (log.Status == HabitLogStatus.Completed) completed++;
                    else skipped++;
                }
            }

            days.Add(new CalendarDay
            {
                Date = d,
                IsCurrentMonth = inMonth,
                IsToday = d == today,
                Scheduled = scheduled,
                Completed = completed,
                Skipped = skipped,
                Missed = missed
            });
        }

        var monthDays = days.Where(d => d.IsCurrentMonth).ToList();

        return new CalendarViewModel
        {
            Year = year,
            Month = month,
            MonthName = first.ToString("MMMM yyyy"),
            FirstDay = first,
            LastDay = last,
            Today = today,
            Days = days,
            TotalCompleted = monthDays.Sum(d => d.Completed),
            TotalSkipped = monthDays.Sum(d => d.Skipped),
            TotalMissed = monthDays.Sum(d => d.Missed),
            PerfectDays = monthDays.Count(d => d.Scheduled > 0 && d.Completed == d.Scheduled),
            PrevYear = first.AddMonths(-1).Year,
            PrevMonth = first.AddMonths(-1).Month,
            NextYear = first.AddMonths(1).Year,
            NextMonth = first.AddMonths(1).Month
        };
    }

    public static string DescribeSchedule(Habit habit)
    {
        if (habit.FrequencyType == FrequencyType.Daily) return "Every day";

        var days = habit.Schedules.Select(s => s.DayOfWeek).Distinct().OrderBy(d => d).ToList();
        if (days.Count == 0) return "No weekdays selected";
        if (days.Count == 7) return "Every day";

        var names = days.Select(d => d.ToString()[..3]);
        return string.Join(", ", names);
    }
}
