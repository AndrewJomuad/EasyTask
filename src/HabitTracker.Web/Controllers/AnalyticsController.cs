using HabitTracker.Web.Services;
using HabitTracker.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitTracker.Web.Controllers;

[Authorize]
public class AnalyticsController : Controller
{
    private readonly IAnalyticsService _analytics;
    private readonly ICurrentUserService _currentUser;

    public AnalyticsController(IAnalyticsService analytics, ICurrentUserService currentUser)
    {
        _analytics = analytics;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string range = "30", string? from = null, string? to = null,
        int? habitId = null, int? categoryId = null)
    {
        var userId = _currentUser.RequireUserId();
        var today = DateOnly.FromDateTime(DateTime.Today);

        DateOnly start, end;
        switch (range)
        {
            case "7":
                start = today.AddDays(-6); end = today; break;
            case "90":
                start = today.AddDays(-89); end = today; break;
            case "365":
                start = today.AddDays(-364); end = today; break;
            case "custom":
                start = DateOnly.TryParse(from, out var f) ? f : today.AddDays(-29);
                end = DateOnly.TryParse(to, out var t) ? t : today;
                break;
            default:
                range = "30";
                start = today.AddDays(-29); end = today; break;
        }

        if (end > today) end = today;
        if (start > end) start = end;

        var model = await _analytics.BuildAsync(userId, start, end, habitId, categoryId);
        model.Range = range;
        return View(model);
    }
}

[Authorize]
public class RemindersController : Controller
{
    private readonly IReminderService _reminders;
    private readonly ICurrentUserService _currentUser;

    public RemindersController(IReminderService reminders, ICurrentUserService currentUser)
    {
        _reminders = reminders;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = _currentUser.RequireUserId();
        var model = await _reminders.GetListAsync(userId);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var userId = _currentUser.RequireUserId();
        var model = await _reminders.BuildFormAsync(userId);

        if (model.AvailableHabits.Count == 0)
        {
            TempData["Error"] = "Create an active habit before adding a reminder.";
            return RedirectToAction("Create", "Habits");
        }

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReminderFormViewModel model)
    {
        var userId = _currentUser.RequireUserId();

        if (!ModelState.IsValid)
        {
            model.AvailableHabits = (await _reminders.BuildFormAsync(userId)).AvailableHabits;
            return View(model);
        }

        var result = await _reminders.SaveAsync(userId, model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message!);
            model.AvailableHabits = (await _reminders.BuildFormAsync(userId)).AvailableHabits;
            return View(model);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = _currentUser.RequireUserId();
        var list = await _reminders.GetListAsync(userId);
        var row = list.Reminders.FirstOrDefault(r => r.Reminder.Id == id);
        if (row is null) return NotFound();

        var model = await _reminders.BuildFormAsync(userId, row.Reminder);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ReminderFormViewModel model)
    {
        var userId = _currentUser.RequireUserId();

        if (!ModelState.IsValid)
        {
            model.AvailableHabits = (await _reminders.BuildFormAsync(userId)).AvailableHabits;
            return View(model);
        }

        var result = await _reminders.SaveAsync(userId, model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message!);
            model.AvailableHabits = (await _reminders.BuildFormAsync(userId)).AvailableHabits;
            return View(model);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var userId = _currentUser.RequireUserId();
        var result = await _reminders.ToggleAsync(userId, id);

        if (result.Success) TempData["Success"] = result.Message;
        else TempData["Error"] = result.Message;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _currentUser.RequireUserId();
        var result = await _reminders.DeleteAsync(userId, id);

        if (result.Success) TempData["Success"] = result.Message;
        else TempData["Error"] = result.Message;

        return RedirectToAction(nameof(Index));
    }
}

[Authorize]
public class AchievementsController : Controller
{
    private readonly IAchievementService _achievements;
    private readonly ICurrentUserService _currentUser;

    public AchievementsController(IAchievementService achievements, ICurrentUserService currentUser)
    {
        _achievements = achievements;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = _currentUser.RequireUserId();
        await _achievements.EvaluateAsync(userId);
        var model = await _achievements.GetAchievementsAsync(userId);
        return View(model);
    }
}
