using HabitTracker.Web.Data;
using HabitTracker.Web.Models;
using HabitTracker.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HabitTracker.Web.Services;

public interface IActivityLogService
{
    Task LogAsync(string action, string? details = null, int? userId = null);
    Task<List<ActivityLog>> RecentAsync(int take = 10, int? userId = null);
}

public class ActivityLogService : IActivityLogService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _http;

    public ActivityLogService(ApplicationDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    public async Task LogAsync(string action, string? details = null, int? userId = null)
    {
        var id = userId;
        if (id is null)
        {
            var raw = _http.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(raw, out var parsed)) id = parsed;
        }

        _db.ActivityLogs.Add(new ActivityLog
        {
            UserId = id,
            Action = action.Length > 120 ? action[..120] : action,
            Details = details is { Length: > 1000 } ? details[..1000] : details,
            IpAddress = _http.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }

    public Task<List<ActivityLog>> RecentAsync(int take = 10, int? userId = null)
    {
        var q = _db.ActivityLogs.AsNoTracking().Include(a => a.User).AsQueryable();
        if (userId.HasValue) q = q.Where(a => a.UserId == userId.Value);
        return q.OrderByDescending(a => a.CreatedAt).Take(take).ToListAsync();
    }
}

public interface ISettingsService
{
    Task<string?> GetAsync(string key);
    Task<int> GetIntAsync(string key, int fallback);
    Task<bool> GetBoolAsync(string key, bool fallback);
    Task<Dictionary<string, string>> AllAsync();
    Task SetAsync(string key, string value);
}

public class SettingsService : ISettingsService
{
    private readonly ApplicationDbContext _db;
    private Dictionary<string, string>? _cache;

    public SettingsService(ApplicationDbContext db) => _db = db;

    private async Task<Dictionary<string, string>> LoadAsync()
    {
        _cache ??= await _db.SystemSettings.AsNoTracking()
            .ToDictionaryAsync(s => s.Key, s => s.Value);
        return _cache;
    }

    public async Task<string?> GetAsync(string key) =>
        (await LoadAsync()).TryGetValue(key, out var v) ? v : null;

    public async Task<int> GetIntAsync(string key, int fallback) =>
        int.TryParse(await GetAsync(key), out var v) ? v : fallback;

    public async Task<bool> GetBoolAsync(string key, bool fallback) =>
        bool.TryParse(await GetAsync(key), out var v) ? v : fallback;

    public async Task<Dictionary<string, string>> AllAsync() => new(await LoadAsync());

    public async Task SetAsync(string key, string value)
    {
        var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting is null)
        {
            _db.SystemSettings.Add(new SystemSetting { Key = key, Value = value });
        }
        else
        {
            setting.Value = value;
            setting.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
        _cache = null;
    }
}
