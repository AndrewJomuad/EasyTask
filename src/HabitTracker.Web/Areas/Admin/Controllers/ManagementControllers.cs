using HabitTracker.Web.Data;
using HabitTracker.Web.Models;
using HabitTracker.Web.Models.Entities;
using HabitTracker.Web.Services;
using HabitTracker.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HabitTracker.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class CategoriesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IActivityLogService _activity;

    public CategoriesController(ApplicationDbContext db, IActivityLogService activity)
    {
        _db = db;
        _activity = activity;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var categories = await _db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
        var counts = await _db.Habits.GroupBy(h => h.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count);

        return View(new AdminCategoriesViewModel
        {
            Categories = categories.Select(c => new CategoryAdminRow
            {
                Category = c,
                HabitCount = counts.TryGetValue(c.Id, out var n) ? n : 0
            }).ToList()
        });
    }

    [HttpGet]
    public IActionResult Create() => View("Form", new CategoryFormViewModel());

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category is null) return NotFound();

        return View("Form", new CategoryFormViewModel
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            Color = category.Color,
            Icon = category.Icon,
            IsActive = category.IsActive
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(CategoryFormViewModel model)
    {
        if (!ModelState.IsValid) return View("Form", model);

        var duplicate = await _db.Categories.AnyAsync(c => c.Name == model.Name && c.Id != model.Id);
        if (duplicate)
        {
            ModelState.AddModelError(nameof(model.Name), "A category with that name already exists.");
            return View("Form", model);
        }

        if (model.Id == 0)
        {
            _db.Categories.Add(new Category
            {
                Name = model.Name.Trim(),
                Description = model.Description?.Trim(),
                Color = model.Color,
                Icon = model.Icon,
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
            await _activity.LogAsync("Admin: category created", $"'{model.Name}'.");
            TempData["Success"] = "Category created.";
        }
        else
        {
            var category = await _db.Categories.FindAsync(model.Id);
            if (category is null) return NotFound();

            category.Name = model.Name.Trim();
            category.Description = model.Description?.Trim();
            category.Color = model.Color;
            category.Icon = model.Icon;
            category.IsActive = model.IsActive;
            await _db.SaveChangesAsync();

            await _activity.LogAsync("Admin: category updated", $"'{category.Name}'.");
            TempData["Success"] = "Category updated.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category is null) return NotFound();

        category.IsActive = !category.IsActive;
        await _db.SaveChangesAsync();

        await _activity.LogAsync(category.IsActive ? "Admin: category activated" : "Admin: category deactivated",
            $"'{category.Name}'.");

        TempData["Success"] = category.IsActive ? "Category activated." : "Category deactivated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category is null) return NotFound();

        var inUse = await _db.Habits.CountAsync(h => h.CategoryId == id);
        if (inUse > 0)
        {
            TempData["Error"] = $"'{category.Name}' is used by {inUse} habit(s). Deactivate it instead of deleting.";
            return RedirectToAction(nameof(Index));
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        await _activity.LogAsync("Admin: category deleted", $"'{category.Name}'.");
        TempData["Success"] = "Category deleted.";
        return RedirectToAction(nameof(Index));
    }
}

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class AchievementsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IActivityLogService _activity;

    public AchievementsController(ApplicationDbContext db, IActivityLogService activity)
    {
        _db = db;
        _activity = activity;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var achievements = await _db.Achievements.AsNoTracking()
            .OrderBy(a => a.Criteria).ThenBy(a => a.PointsRequired).ToListAsync();

        var counts = await _db.UserAchievements.GroupBy(ua => ua.AchievementId)
            .Select(g => new { AchievementId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.AchievementId, x => x.Count);

        return View(new AdminAchievementsViewModel
        {
            Achievements = achievements.Select(a => new AchievementAdminRow
            {
                Achievement = a,
                EarnedBy = counts.TryGetValue(a.Id, out var n) ? n : 0
            }).ToList()
        });
    }

    [HttpGet]
    public IActionResult Create() => View("Form", new AchievementFormViewModel());

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var achievement = await _db.Achievements.FindAsync(id);
        if (achievement is null) return NotFound();

        return View("Form", new AchievementFormViewModel
        {
            Id = achievement.Id,
            Name = achievement.Name,
            Description = achievement.Description,
            Criteria = achievement.Criteria,
            PointsRequired = achievement.PointsRequired,
            Icon = achievement.Icon,
            Color = achievement.Color,
            IsActive = achievement.IsActive
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(AchievementFormViewModel model)
    {
        if (!ModelState.IsValid) return View("Form", model);

        var duplicate = await _db.Achievements.AnyAsync(a => a.Name == model.Name && a.Id != model.Id);
        if (duplicate)
        {
            ModelState.AddModelError(nameof(model.Name), "An achievement with that name already exists.");
            return View("Form", model);
        }

        if (model.Id == 0)
        {
            _db.Achievements.Add(new Achievement
            {
                Name = model.Name.Trim(),
                Description = model.Description?.Trim(),
                Criteria = model.Criteria,
                PointsRequired = model.PointsRequired,
                Icon = model.Icon,
                Color = model.Color,
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
            await _activity.LogAsync("Admin: achievement created", $"'{model.Name}'.");
            TempData["Success"] = "Achievement created.";
        }
        else
        {
            var achievement = await _db.Achievements.FindAsync(model.Id);
            if (achievement is null) return NotFound();

            achievement.Name = model.Name.Trim();
            achievement.Description = model.Description?.Trim();
            achievement.Criteria = model.Criteria;
            achievement.PointsRequired = model.PointsRequired;
            achievement.Icon = model.Icon;
            achievement.Color = model.Color;
            achievement.IsActive = model.IsActive;
            await _db.SaveChangesAsync();

            await _activity.LogAsync("Admin: achievement updated", $"'{achievement.Name}'.");
            TempData["Success"] = "Achievement updated.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var achievement = await _db.Achievements.FindAsync(id);
        if (achievement is null) return NotFound();

        achievement.IsActive = !achievement.IsActive;
        await _db.SaveChangesAsync();

        await _activity.LogAsync(achievement.IsActive ? "Admin: achievement activated" : "Admin: achievement deactivated",
            $"'{achievement.Name}'.");

        TempData["Success"] = achievement.IsActive ? "Achievement activated." : "Achievement deactivated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var achievement = await _db.Achievements.FindAsync(id);
        if (achievement is null) return NotFound();

        var earned = await _db.UserAchievements.CountAsync(ua => ua.AchievementId == id);
        if (earned > 0)
        {
            TempData["Error"] = $"'{achievement.Name}' has already been earned by {earned} user(s). Deactivate it instead.";
            return RedirectToAction(nameof(Index));
        }

        _db.Achievements.Remove(achievement);
        await _db.SaveChangesAsync();

        await _activity.LogAsync("Admin: achievement deleted", $"'{achievement.Name}'.");
        TempData["Success"] = "Achievement deleted.";
        return RedirectToAction(nameof(Index));
    }
}

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class ActivityController : Controller
{
    private readonly ApplicationDbContext _db;

    public ActivityController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? action, string? from, string? to, int page = 1)
    {
        if (page < 1) page = 1;

        var query = _db.ActivityLogs.AsNoTracking().Include(a => a.User).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(a => a.Action.Contains(term)
                                     || (a.Details != null && a.Details.Contains(term))
                                     || (a.User != null && a.User.UserName!.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action);

        if (DateOnly.TryParse(from, out var f))
        {
            var start = f.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(a => a.CreatedAt >= start);
        }

        if (DateOnly.TryParse(to, out var t))
        {
            var end = t.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            query = query.Where(a => a.CreatedAt <= end);
        }

        var total = await query.CountAsync();
        const int pageSize = 25;

        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var actions = await _db.ActivityLogs.AsNoTracking()
            .Select(a => a.Action).Distinct().OrderBy(a => a).ToListAsync();

        return View(new AdminActivityViewModel
        {
            Logs = logs,
            Search = search,
            Action = action,
            From = DateOnly.TryParse(from, out var ff) ? ff : null,
            To = DateOnly.TryParse(to, out var tt) ? tt : null,
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            Actions = actions
        });
    }
}

[Area("Admin")]
[Authorize(Roles = AppRoles.Admin)]
public class SettingsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ISettingsService _settings;
    private readonly IActivityLogService _activity;

    public SettingsController(ApplicationDbContext db, ISettingsService settings, IActivityLogService activity)
    {
        _db = db;
        _settings = settings;
        _activity = activity;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var settings = await _db.SystemSettings.AsNoTracking().OrderBy(s => s.Key).ToListAsync();

        var model = new AdminSettingsViewModel
        {
            Settings = settings,
            Values = settings.ToDictionary(s => s.Key, s => s.Value),
            DatabaseProvider = _db.Database.ProviderName?.Contains("SqlServer") == true ? "Microsoft SQL Server" : _db.Database.ProviderName ?? "Unknown"
        };

        try
        {
            model.DatabaseConnected = await _db.Database.CanConnectAsync();
            var applied = (await _db.Database.GetAppliedMigrationsAsync()).ToList();
            var pending = (await _db.Database.GetPendingMigrationsAsync()).ToList();
            model.AppliedMigrations = applied.Count;
            model.PendingMigrations = pending.Count;
        }
        catch
        {
            model.DatabaseConnected = false;
        }

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(Dictionary<string, string> values)
    {
        var settings = await _db.SystemSettings.ToListAsync();

        foreach (var setting in settings)
        {
            // An unchecked checkbox posts nothing: treat a missing boolean as "false".
            if (setting.Key == SettingKeys.AllowSelfRegistration && !values.ContainsKey(setting.Key))
            {
                setting.Value = "false";
                setting.UpdatedAt = DateTime.UtcNow;
                continue;
            }

            if (!values.TryGetValue(setting.Key, out var value)) continue;

            if (setting.Key == SettingKeys.PointsPerCompletion ||
                setting.Key == SettingKeys.PointsPerPerfectDay ||
                setting.Key == SettingKeys.PointsPerSkipped ||
                setting.Key == SettingKeys.ReminderWindowMinutes)
            {
                if (!int.TryParse(value, out var number) || number < 0 || number > 100000)
                {
                    TempData["Error"] = $"'{setting.Key}' must be a whole number between 0 and 100000.";
                    return RedirectToAction(nameof(Index));
                }
            }

            if (setting.Key == SettingKeys.AllowSelfRegistration && !bool.TryParse(value, out _))
            {
                TempData["Error"] = "Registration.AllowSelfService must be true or false.";
                return RedirectToAction(nameof(Index));
            }

            setting.Value = value;
            setting.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        await _activity.LogAsync("Admin: settings updated", $"{settings.Count} setting(s) saved.");

        TempData["Success"] = "System settings saved.";
        return RedirectToAction(nameof(Index));
    }
}
