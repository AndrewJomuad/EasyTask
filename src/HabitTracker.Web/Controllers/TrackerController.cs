using HabitTracker.Web.Models;
using HabitTracker.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitTracker.Web.Controllers;

[Authorize]
public class TrackerController : Controller
{
    private readonly IHabitService _habits;
    private readonly ICurrentUserService _currentUser;

    public TrackerController(IHabitService habits, ICurrentUserService currentUser)
    {
        _habits = habits;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? date)
    {
        var userId = _currentUser.RequireUserId();
        var today = DateOnly.FromDateTime(DateTime.Today);

        var selected = today;
        if (!string.IsNullOrWhiteSpace(date) && DateOnly.TryParse(date, out var parsed))
            selected = parsed;

        var model = await _habits.GetTrackerAsync(userId, selected);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Record(int habitId, string date, HabitLogStatus status, string? notes)
    {
        var userId = _currentUser.RequireUserId();

        if (!DateOnly.TryParse(date, out var parsed))
        {
            TempData["Error"] = "Invalid date.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _habits.RecordAsync(userId, habitId, parsed, status, notes);

        if (result.Success) TempData["Success"] = result.Message;
        else TempData["Error"] = result.Message;

        return RedirectToAction(nameof(Index), new { date = parsed.ToString("yyyy-MM-dd") });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Clear(int habitId, string date)
    {
        var userId = _currentUser.RequireUserId();

        if (!DateOnly.TryParse(date, out var parsed))
        {
            TempData["Error"] = "Invalid date.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _habits.ClearLogAsync(userId, habitId, parsed);

        if (result.Success) TempData["Success"] = result.Message;
        else TempData["Error"] = result.Message;

        return RedirectToAction(nameof(Index), new { date = parsed.ToString("yyyy-MM-dd") });
    }
}

[Authorize]
public class CalendarController : Controller
{
    private readonly IHabitService _habits;
    private readonly ICurrentUserService _currentUser;

    public CalendarController(IHabitService habits, ICurrentUserService currentUser)
    {
        _habits = habits;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? year, int? month)
    {
        var userId = _currentUser.RequireUserId();
        var today = DateTime.Today;

        var y = year ?? today.Year;
        var m = month ?? today.Month;

        if (y < 2000 || y > 2100) y = today.Year;
        if (m < 1 || m > 12) m = today.Month;

        var model = await _habits.GetCalendarAsync(userId, y, m);
        return View(model);
    }
}
