using HabitTracker.Web.Services;
using HabitTracker.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitTracker.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IAnalyticsService _analytics;
    private readonly ICurrentUserService _currentUser;
    private readonly IReminderService _reminders;

    public DashboardController(IAnalyticsService analytics, ICurrentUserService currentUser, IReminderService reminders)
    {
        _analytics = analytics;
        _currentUser = currentUser;
        _reminders = reminders;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = _currentUser.RequireUserId();
        var model = await _analytics.BuildDashboardAsync(userId);

        var due = await _reminders.GetDueAsync(userId);
        ViewBag.DueReminders = due;
        if (due.Count > 0)
            await _reminders.MarkTriggeredAsync(due.Select(d => d.Reminder.Id));

        return View(model);
    }
}

[Authorize]
public class HomeController : Controller
{
    [AllowAnonymous]
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        return View();
    }

    [AllowAnonymous]
    public IActionResult Privacy() => View();

    [AllowAnonymous]
    public new IActionResult StatusCode(int code)
    {
        ViewBag.Code = code;
        ViewBag.Message = code switch
        {
            400 => "The request could not be understood.",
            401 => "You need to sign in to view that page.",
            403 => "You do not have permission to view that page.",
            404 => "We could not find the page you were looking for.",
            500 => "Something went wrong on our side.",
            _ => "An unexpected error occurred."
        };
        return View();
    }

    [AllowAnonymous]
    public IActionResult Error() => View();
}
