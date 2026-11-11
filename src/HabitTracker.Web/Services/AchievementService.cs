using HabitTracker.Web.Data;
using HabitTracker.Web.Models;
using HabitTracker.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HabitTracker.Web.Services;

public interface IAchievementService
{
    /// <summary>Re-evaluates every active achievement for the user and awards new ones.</summary>
    Task<List<Achievement>> EvaluateAsync(int userId);
    Task<ViewModels.AchievementsViewModel> GetAchievementsAsync(int userId);
    Task<Dictionary<AchievementCriteria, int>> GetMetricsAsync(int userId);
}

public class AchievementService : IAchievementService
{
    private readonly ApplicationDbContext _db;
    private readonly IStreakService _streaks;
    private readonly IActivityLogService _activity;

    public AchievementService(ApplicationDbContext db, IStreakService streaks, IActivityLogService activity)
    {
        _db = db;
        _streaks = streaks;
        _activity = activity;
    }

    public async Task<Dictionary<AchievementCriteria, int>> GetMetricsAsync(int userId)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var user = await _db.Users.AsNoTracking().FirstAsync(u => u.Id == userId);
        var habits = await _db.Habits.AsNoTracking()
            .Include(h => h.Schedules)
            .Where(h => h.UserId == userId)
            .ToListAsync();

        var logs = await _db.HabitLogs.AsNoTracking()
            .Where(l => l.Habit!.UserId == userId)
            .ToListAsync();

        var completions = logs.Count(l => l.Status == HabitLogStatus.Completed);

        var streaks = await _streaks.CalculateForHabitsAsync(habits, today);
        var currentStreak = streaks.Count == 0 ? 0 : streaks.Values.Max(s => s.Current);
        var longestStreak = streaks.Count == 0 ? 0 : streaks.Values.Max(s => s.Longest);

        // Perfect days: every scheduled habit completed on that date.
        var perfectDays = 0;
        var completedByDate = logs
            .Where(l => l.Status == HabitLogStatus.Completed)
            .GroupBy(l => l.LogDate)
            .ToDictionary(g => g.Key, g => g.Select(x => x.HabitId).ToHashSet());

        foreach (var (date, completedIds) in completedByDate)
        {
            var scheduled = habits.Where(h => h.IsScheduledOn(date)).Select(h => h.Id).ToList();
            if (scheduled.Count > 0 && scheduled.All(completedIds.Contains)) perfectDays++;
        }

        return new Dictionary<AchievementCriteria, int>
        {
            [AchievementCriteria.TotalPoints] = user.TotalPoints,
            [AchievementCriteria.TotalCompletions] = completions,
            [AchievementCriteria.CurrentStreak] = currentStreak,
            [AchievementCriteria.LongestStreak] = longestStreak,
            [AchievementCriteria.PerfectDays] = perfectDays,
            [AchievementCriteria.ActiveHabits] = habits.Count(h => h.IsActive)
        };
    }

    public async Task<List<Achievement>> EvaluateAsync(int userId)
    {
        var metrics = await GetMetricsAsync(userId);

        var definitions = await _db.Achievements
            .Where(a => a.IsActive)
            .ToListAsync();

        var earnedIds = await _db.UserAchievements
            .Where(ua => ua.UserId == userId)
            .Select(ua => ua.AchievementId)
            .ToListAsync();

        var newlyEarned = new List<Achievement>();

        foreach (var definition in definitions)
        {
            if (earnedIds.Contains(definition.Id)) continue;
            if (!metrics.TryGetValue(definition.Criteria, out var value)) continue;
            if (value < definition.PointsRequired) continue;

            _db.UserAchievements.Add(new UserAchievement
            {
                UserId = userId,
                AchievementId = definition.Id,
                EarnedAt = DateTime.UtcNow
            });
            newlyEarned.Add(definition);
        }

        if (newlyEarned.Count > 0)
        {
            await _db.SaveChangesAsync();
            await _activity.LogAsync("Achievement unlocked",
                string.Join(", ", newlyEarned.Select(a => a.Name)), userId);
        }

        return newlyEarned;
    }

    public async Task<ViewModels.AchievementsViewModel> GetAchievementsAsync(int userId)
    {
        var metrics = await GetMetricsAsync(userId);

        var definitions = await _db.Achievements.AsNoTracking()
            .Where(a => a.IsActive)
            .OrderBy(a => a.Criteria)
            .ThenBy(a => a.PointsRequired)
            .ToListAsync();

        var earned = await _db.UserAchievements.AsNoTracking()
            .Where(ua => ua.UserId == userId)
            .ToDictionaryAsync(ua => ua.AchievementId, ua => ua.EarnedAt);

        var user = await _db.Users.AsNoTracking().FirstAsync(u => u.Id == userId);

        var transactions = await _db.RewardTransactions.AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(50)
            .ToListAsync();

        var cards = definitions.Select(a =>
        {
            var progress = metrics.TryGetValue(a.Criteria, out var v) ? v : 0;
            return new ViewModels.AchievementCard
            {
                Achievement = a,
                Earned = earned.ContainsKey(a.Id),
                EarnedAt = earned.TryGetValue(a.Id, out var at) ? at : null,
                Progress = Math.Min(progress, a.PointsRequired),
                Target = a.PointsRequired,
                CriteriaLabel = a.Criteria switch
                {
                    AchievementCriteria.TotalPoints => "points earned",
                    AchievementCriteria.TotalCompletions => "habit completions",
                    AchievementCriteria.CurrentStreak => "day current streak",
                    AchievementCriteria.LongestStreak => "day longest streak",
                    AchievementCriteria.PerfectDays => "perfect days",
                    AchievementCriteria.ActiveHabits => "active habits",
                    _ => "progress"
                }
            };
        }).ToList();

        return new ViewModels.AchievementsViewModel
        {
            TotalPoints = user.TotalPoints,
            EarnedCount = cards.Count(c => c.Earned),
            TotalCount = cards.Count,
            Achievements = cards,
            Transactions = transactions,
            TotalCompletions = metrics[AchievementCriteria.TotalCompletions],
            CurrentStreak = metrics[AchievementCriteria.CurrentStreak],
            LongestStreak = metrics[AchievementCriteria.LongestStreak],
            PerfectDays = metrics[AchievementCriteria.PerfectDays],
            ActiveHabits = metrics[AchievementCriteria.ActiveHabits]
        };
    }
}
