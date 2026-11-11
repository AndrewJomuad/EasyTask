using HabitTracker.Web.Data;
using HabitTracker.Web.Models;
using HabitTracker.Web.Models.Entities;
using HabitTracker.Web.Services;
using HabitTracker.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HabitTracker.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IActivityLogService _activity;

    public DashboardController(ApplicationDbContext db, IActivityLogService activity)
    {
        _db = db;
        _activity = activity;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var users = await _db.Users.AsNoTracking().ToListAsync();
        var habits = await _db.Habits.AsNoTracking().Include(h => h.Category).ToListAsync();
        var logs = await _db.HabitLogs.AsNoTracking().ToListAsync();

        var weekly = new List<WeeklyPoint>();
        for (var d = today.AddDays(-6); d <= today; d = d.AddDays(1))
        {
            var dayLogs = logs.Where(l => l.LogDate == d).ToList();
            weekly.Add(new WeeklyPoint
            {
                Label = d.ToString("ddd"),
                Date = d,
                Scheduled = dayLogs.Count,
                Completed = dayLogs.Count(l => l.Status == HabitLogStatus.Completed)
            });
        }

        var newest = users.OrderByDescending(u => u.CreatedAt).Take(5).ToList();
        var newestRows = new List<AdminUserRow>();
        foreach (var u in newest)
        {
            var roles = await _db.UserRoles.Where(ur => ur.UserId == u.Id)
                .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                .ToListAsync();

            newestRows.Add(new AdminUserRow
            {
                User = u,
                Role = roles.FirstOrDefault() ?? AppRoles.User,
                HabitCount = habits.Count(h => h.UserId == u.Id),
                LogCount = logs.Count(l => habits.Any(h => h.Id == l.HabitId && h.UserId == u.Id))
            });
        }

        var model = new AdminDashboardViewModel
        {
            TotalUsers = users.Count,
            ActiveUsers = users.Count(u => u.IsActive),
            TotalHabits = habits.Count,
            ActiveHabits = habits.Count(h => h.IsActive),
            TotalLogs = logs.Count,
            LogsToday = logs.Count(l => l.LogDate == today),
            TotalCategories = await _db.Categories.CountAsync(),
            TotalAchievements = await _db.Achievements.CountAsync(),
            TotalPointsAwarded = await _db.RewardTransactions.SumAsync(t => (int?)t.PointsChange) ?? 0,
            PlatformWeekly = weekly,
            RecentActivity = await _activity.RecentAsync(12),
            NewestUsers = newestRows,
            CategoryUsage = habits.GroupBy(h => h.Category?.Name ?? "Uncategorised")
                .Select(g => new CategoryBreakdownRow
                {
                    Name = g.Key,
                    Color = g.First().Category?.Color ?? "#64748B",
                    Scheduled = g.Count(),
                    Completed = g.Count(h => h.IsActive)
                })
                .OrderByDescending(c => c.Scheduled)
                .ToList()
        };

        return View(model);
    }
}

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class UsersController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IActivityLogService _activity;

    public UsersController(ApplicationDbContext db, UserManager<ApplicationUser> users, IActivityLogService activity)
    {
        _db = db;
        _users = users;
        _activity = activity;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? role, string? status)
    {
        var query = _db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u => u.FullName.Contains(term) || u.Email!.Contains(term) || u.UserName!.Contains(term));
        }

        if (status == "active") query = query.Where(u => u.IsActive);
        if (status == "inactive") query = query.Where(u => !u.IsActive);

        var users = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();

        var roleMap = await _db.UserRoles
            .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .ToListAsync();

        var habitCounts = await _db.Habits.GroupBy(h => h.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.UserId, x => x.Count);

        var logCounts = await _db.HabitLogs
            .GroupBy(l => l.Habit!.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count);

        var rows = users.Select(u => new AdminUserRow
        {
            User = u,
            Role = roleMap.FirstOrDefault(r => r.UserId == u.Id)?.Name ?? AppRoles.User,
            HabitCount = habitCounts.TryGetValue(u.Id, out var hc) ? hc : 0,
            LogCount = logCounts.TryGetValue(u.Id, out var lc) ? lc : 0,
            LastActivity = u.LastLoginAt
        }).ToList();

        if (!string.IsNullOrWhiteSpace(role))
            rows = rows.Where(r => r.Role == role).ToList();

        var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        return View(new AdminUsersViewModel
        {
            Users = rows,
            Search = search,
            Role = role,
            Status = status,
            TotalUsers = users.Count,
            ActiveUsers = users.Count(u => u.IsActive),
            AdminUsers = roleMap.Count(r => r.Name == AppRoles.Admin),
            NewThisMonth = users.Count(u => u.CreatedAt >= monthStart)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _users.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();

        var roles = await _users.GetRolesAsync(user);

        return View(new AdminUserEditViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            UserName = user.UserName ?? string.Empty,
            Role = roles.FirstOrDefault() ?? AppRoles.User,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            TotalPoints = user.TotalPoints,
            HabitCount = await _db.Habits.CountAsync(h => h.UserId == user.Id),
            LogCount = await _db.HabitLogs.CountAsync(l => l.Habit!.UserId == user.Id)
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AdminUserEditViewModel model)
    {
        var user = await _users.FindByIdAsync(model.Id.ToString());
        if (user is null) return NotFound();

        if (!ModelState.IsValid)
        {
            model.CreatedAt = user.CreatedAt;
            model.LastLoginAt = user.LastLoginAt;
            model.TotalPoints = user.TotalPoints;
            return View(model);
        }

        // Guard: never allow the last administrator to be demoted or deactivated.
        var currentRoles = await _users.GetRolesAsync(user);
        var isCurrentlyAdmin = currentRoles.Contains(AppRoles.Admin);
        var willBeAdmin = model.Role == AppRoles.Admin;

        if (isCurrentlyAdmin && (!willBeAdmin || !model.IsActive))
        {
            var adminCount = (await _users.GetUsersInRoleAsync(AppRoles.Admin)).Count(u => u.IsActive);
            if (adminCount <= 1)
            {
                ModelState.AddModelError(string.Empty,
                    "You cannot demote or deactivate the last active administrator.");
                model.CreatedAt = user.CreatedAt;
                model.LastLoginAt = user.LastLoginAt;
                model.TotalPoints = user.TotalPoints;
                return View(model);
            }
        }

        var emailOwner = await _users.FindByEmailAsync(model.Email);
        if (emailOwner is not null && emailOwner.Id != user.Id)
            ModelState.AddModelError(nameof(model.Email), "That email is already used by another account.");

        var nameOwner = await _users.FindByNameAsync(model.UserName);
        if (nameOwner is not null && nameOwner.Id != user.Id)
            ModelState.AddModelError(nameof(model.UserName), "That username is already taken.");

        if (!ModelState.IsValid)
        {
            model.CreatedAt = user.CreatedAt;
            model.LastLoginAt = user.LastLoginAt;
            model.TotalPoints = user.TotalPoints;
            return View(model);
        }

        user.FullName = model.FullName.Trim();
        user.Email = model.Email.Trim();
        user.UserName = model.UserName.Trim();
        user.IsActive = model.IsActive;

        var update = await _users.UpdateAsync(user);
        if (!update.Succeeded)
        {
            foreach (var e in update.Errors) ModelState.AddModelError(string.Empty, e.Description);
            return View(model);
        }

        if (!currentRoles.Contains(model.Role))
        {
            await _users.RemoveFromRolesAsync(user, currentRoles);
            await _users.AddToRoleAsync(user, model.Role);
        }

        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            var token = await _users.GeneratePasswordResetTokenAsync(user);
            var reset = await _users.ResetPasswordAsync(user, token, model.NewPassword);
            if (!reset.Succeeded)
            {
                foreach (var e in reset.Errors) ModelState.AddModelError(string.Empty, e.Description);
                return View(model);
            }
        }

        await _activity.LogAsync("Admin: user updated",
            $"Updated account '{user.UserName}' (role {model.Role}, active {model.IsActive}).");

        TempData["Success"] = "User account updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var user = await _users.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();

        var roles = await _users.GetRolesAsync(user);
        if (roles.Contains(AppRoles.Admin) && user.IsActive)
        {
            var adminCount = (await _users.GetUsersInRoleAsync(AppRoles.Admin)).Count(u => u.IsActive);
            if (adminCount <= 1)
            {
                TempData["Error"] = "You cannot deactivate the last active administrator.";
                return RedirectToAction(nameof(Index));
            }
        }

        user.IsActive = !user.IsActive;
        await _users.UpdateAsync(user);

        await _activity.LogAsync(user.IsActive ? "Admin: user activated" : "Admin: user deactivated",
            $"Account '{user.UserName}'.");

        TempData["Success"] = user.IsActive ? "Account activated." : "Account deactivated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var user = await _users.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();

        var roles = await _users.GetRolesAsync(user);
        var habits = await _db.Habits.AsNoTracking()
            .Include(h => h.Category)
            .Where(h => h.UserId == id)
            .ToListAsync();

        ViewBag.Role = roles.FirstOrDefault() ?? AppRoles.User;
        ViewBag.Habits = habits;
        ViewBag.Logs = await _db.HabitLogs.AsNoTracking()
            .Include(l => l.Habit)
            .Where(l => l.Habit!.UserId == id)
            .OrderByDescending(l => l.LogDate)
            .Take(25)
            .ToListAsync();
        ViewBag.Achievements = await _db.UserAchievements.AsNoTracking()
            .Include(ua => ua.Achievement)
            .Where(ua => ua.UserId == id)
            .ToListAsync();

        return View(user);
    }
}
