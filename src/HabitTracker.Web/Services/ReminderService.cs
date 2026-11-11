using HabitTracker.Web.Data;
using HabitTracker.Web.Models;
using HabitTracker.Web.Models.Entities;
using HabitTracker.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace HabitTracker.Web.Services;

public interface IReminderService
{
    Task<ReminderListViewModel> GetListAsync(int userId);
    Task<ReminderFormViewModel> BuildFormAsync(int userId, Reminder? reminder = null);
    Task<OperationResult> SaveAsync(int userId, ReminderFormViewModel model);
    Task<OperationResult> ToggleAsync(int userId, int reminderId);
    Task<OperationResult> DeleteAsync(int userId, int reminderId);
    Task<List<ReminderRow>> GetDueAsync(int userId);
    Task MarkTriggeredAsync(IEnumerable<int> reminderIds);
}

public class ReminderService : IReminderService
{
    private readonly ApplicationDbContext _db;
    private readonly ISettingsService _settings;
    private readonly IActivityLogService _activity;

    public ReminderService(ApplicationDbContext db, ISettingsService settings, IActivityLogService activity)
    {
        _db = db;
        _settings = settings;
        _activity = activity;
    }

    public async Task<ReminderListViewModel> GetListAsync(int userId)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var now = DateTime.Now;

        var reminders = await _db.Reminders.AsNoTracking()
            .Include(r => r.Habit).ThenInclude(h => h!.Schedules)
            .Where(r => r.Habit!.UserId == userId)
            .OrderBy(r => r.ReminderTime)
            .ToListAsync();

        var habitIds = reminders.Select(r => r.HabitId).Distinct().ToList();
        var todayLogs = await _db.HabitLogs.AsNoTracking()
            .Where(l => habitIds.Contains(l.HabitId) && l.LogDate == today)
            .ToListAsync();

        var window = await _settings.GetIntAsync(SettingKeys.ReminderWindowMinutes, 30);

        var rows = reminders.Select(r =>
        {
            var log = todayLogs.FirstOrDefault(l => l.HabitId == r.HabitId);
            var scheduledToday = r.Habit?.IsScheduledOn(today) == true;
            var dueTime = today.ToDateTime(r.ReminderTime);
            var isDue = r.IsEnabled && scheduledToday && log is null
                        && now >= dueTime && now <= dueTime.AddMinutes(window);

            return new ReminderRow
            {
                Reminder = r,
                HabitName = r.Habit?.Name ?? "(deleted habit)",
                HabitColor = r.Habit?.Color ?? "#1D4ED8",
                HabitIcon = r.Habit?.Icon ?? "bi-bell",
                CompletedToday = log?.Status == HabitLogStatus.Completed,
                ScheduledToday = scheduledToday,
                IsDue = isDue
            };
        }).ToList();

        var available = await _db.Habits.AsNoTracking()
            .Where(h => h.UserId == userId && h.IsActive)
            .OrderBy(h => h.Name)
            .ToListAsync();

        return new ReminderListViewModel
        {
            Reminders = rows,
            AvailableHabits = available,
            EnabledCount = rows.Count(r => r.Reminder.IsEnabled),
            DisabledCount = rows.Count(r => !r.Reminder.IsEnabled),
            DueNow = rows.Where(r => r.IsDue).ToList()
        };
    }

    public async Task<ReminderFormViewModel> BuildFormAsync(int userId, Reminder? reminder = null)
    {
        var habits = await _db.Habits.AsNoTracking()
            .Where(h => h.UserId == userId && h.IsActive)
            .OrderBy(h => h.Name)
            .ToListAsync();

        if (reminder is null)
            return new ReminderFormViewModel { AvailableHabits = habits, ReminderTime = new TimeOnly(8, 0) };

        return new ReminderFormViewModel
        {
            Id = reminder.Id,
            HabitId = reminder.HabitId,
            ReminderTime = reminder.ReminderTime,
            RepeatType = reminder.RepeatType,
            IsEnabled = reminder.IsEnabled,
            AvailableHabits = habits
        };
    }

    public async Task<OperationResult> SaveAsync(int userId, ReminderFormViewModel model)
    {
        var habit = await _db.Habits.FirstOrDefaultAsync(h => h.Id == model.HabitId && h.UserId == userId);
        if (habit is null) return OperationResult.Fail("Choose one of your own habits.");

        if (model.Id == 0)
        {
            var duplicate = await _db.Reminders.AnyAsync(r =>
                r.HabitId == model.HabitId && r.ReminderTime == model.ReminderTime);
            if (duplicate)
                return OperationResult.Fail("A reminder already exists for this habit at that time.");

            var reminder = new Reminder
            {
                HabitId = model.HabitId,
                ReminderTime = model.ReminderTime,
                RepeatType = model.RepeatType,
                IsEnabled = model.IsEnabled,
                CreatedAt = DateTime.UtcNow
            };
            _db.Reminders.Add(reminder);
            await _db.SaveChangesAsync();

            await _activity.LogAsync("Reminder created",
                $"Reminder at {model.ReminderTime:HH\\:mm} for '{habit.Name}'.", userId);
            return OperationResult.Ok("Reminder created.", reminder.Id);
        }

        var existing = await _db.Reminders
            .FirstOrDefaultAsync(r => r.Id == model.Id && r.Habit!.UserId == userId);
        if (existing is null) return OperationResult.Fail("Reminder not found.");

        existing.HabitId = model.HabitId;
        existing.ReminderTime = model.ReminderTime;
        existing.RepeatType = model.RepeatType;
        existing.IsEnabled = model.IsEnabled;
        await _db.SaveChangesAsync();

        await _activity.LogAsync("Reminder updated",
            $"Reminder at {model.ReminderTime:HH\\:mm} for '{habit.Name}'.", userId);
        return OperationResult.Ok("Reminder updated.", existing.Id);
    }

    public async Task<OperationResult> ToggleAsync(int userId, int reminderId)
    {
        var reminder = await _db.Reminders
            .Include(r => r.Habit)
            .FirstOrDefaultAsync(r => r.Id == reminderId && r.Habit!.UserId == userId);
        if (reminder is null) return OperationResult.Fail("Reminder not found.");

        reminder.IsEnabled = !reminder.IsEnabled;
        await _db.SaveChangesAsync();

        await _activity.LogAsync(reminder.IsEnabled ? "Reminder enabled" : "Reminder disabled",
            $"'{reminder.Habit?.Name}' at {reminder.ReminderTime:HH\\:mm}.", userId);

        return OperationResult.Ok(reminder.IsEnabled ? "Reminder enabled." : "Reminder disabled.");
    }

    public async Task<OperationResult> DeleteAsync(int userId, int reminderId)
    {
        var reminder = await _db.Reminders
            .Include(r => r.Habit)
            .FirstOrDefaultAsync(r => r.Id == reminderId && r.Habit!.UserId == userId);
        if (reminder is null) return OperationResult.Fail("Reminder not found.");

        _db.Reminders.Remove(reminder);
        await _db.SaveChangesAsync();

        await _activity.LogAsync("Reminder deleted", $"'{reminder.Habit?.Name}'.", userId);
        return OperationResult.Ok("Reminder deleted.");
    }

    public async Task<List<ReminderRow>> GetDueAsync(int userId)
    {
        var list = await GetListAsync(userId);
        return list.DueNow;
    }

    public async Task MarkTriggeredAsync(IEnumerable<int> reminderIds)
    {
        var ids = reminderIds.ToList();
        if (ids.Count == 0) return;

        var reminders = await _db.Reminders.Where(r => ids.Contains(r.Id)).ToListAsync();
        foreach (var r in reminders) r.LastTriggeredAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
