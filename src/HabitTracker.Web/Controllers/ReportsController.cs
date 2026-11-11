using HabitTracker.Web.Services;
using HabitTracker.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitTracker.Web.Controllers;

[Authorize]
public class ReportsController : Controller
{
    private readonly IAnalyticsService _analytics;
    private readonly IExportService _export;
    private readonly ICurrentUserService _currentUser;
    private readonly IActivityLogService _activity;

    public ReportsController(IAnalyticsService analytics, IExportService export,
        ICurrentUserService currentUser, IActivityLogService activity)
    {
        _analytics = analytics;
        _export = export;
        _currentUser = currentUser;
        _activity = activity;
    }

    private (DateOnly From, DateOnly To) ResolveRange(string range, string? from, string? to)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        DateOnly start, end;

        switch (range)
        {
            case "7": start = today.AddDays(-6); end = today; break;
            case "90": start = today.AddDays(-89); end = today; break;
            case "365": start = today.AddDays(-364); end = today; break;
            case "custom":
                start = DateOnly.TryParse(from, out var f) ? f : today.AddDays(-29);
                end = DateOnly.TryParse(to, out var t) ? t : today;
                break;
            default: start = today.AddDays(-29); end = today; break;
        }

        if (end > today) end = today;
        if (start > end) start = end;
        return (start, end);
    }

    [HttpGet]
    public async Task<IActionResult> Index(string range = "30", string? from = null, string? to = null)
    {
        var userId = _currentUser.RequireUserId();
        var (start, end) = ResolveRange(range, from, to);

        var model = await _analytics.BuildReportAsync(userId, start, end);
        model.Range = range;
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ExportPdf(string range = "30", string? from = null, string? to = null)
    {
        var userId = _currentUser.RequireUserId();
        var (start, end) = ResolveRange(range, from, to);

        var report = await _analytics.BuildReportAsync(userId, start, end);
        var bytes = _export.BuildPdf(report, _currentUser.FullName ?? _currentUser.UserName ?? "User");

        await _activity.LogAsync("Report exported (PDF)", $"Range {start:yyyy-MM-dd} to {end:yyyy-MM-dd}.", userId);

        var name = $"HTMS-Report-{start:yyyyMMdd}-{end:yyyyMMdd}.pdf";
        return File(bytes, "application/pdf", name);
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel(string range = "30", string? from = null, string? to = null)
    {
        var userId = _currentUser.RequireUserId();
        var (start, end) = ResolveRange(range, from, to);

        var report = await _analytics.BuildReportAsync(userId, start, end);
        var bytes = _export.BuildExcel(report, _currentUser.FullName ?? _currentUser.UserName ?? "User");

        await _activity.LogAsync("Report exported (Excel)", $"Range {start:yyyy-MM-dd} to {end:yyyy-MM-dd}.", userId);

        var name = $"HTMS-Report-{start:yyyyMMdd}-{end:yyyyMMdd}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", name);
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(string range = "30", string? from = null, string? to = null)
    {
        var userId = _currentUser.RequireUserId();
        var (start, end) = ResolveRange(range, from, to);

        var report = await _analytics.BuildReportAsync(userId, start, end);
        var bytes = _export.BuildCsv(report);

        await _activity.LogAsync("Report exported (CSV)", $"Range {start:yyyy-MM-dd} to {end:yyyy-MM-dd}.", userId);

        var name = $"HTMS-Report-{start:yyyyMMdd}-{end:yyyyMMdd}.csv";
        return File(bytes, "text/csv", name);
    }
}
