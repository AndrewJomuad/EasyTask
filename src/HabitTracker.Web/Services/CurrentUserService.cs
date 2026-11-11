using System.Security.Claims;
using HabitTracker.Web.Models;

namespace HabitTracker.Web.Services;

public interface ICurrentUserService
{
    int? UserId { get; }
    string? UserName { get; }
    string? FullName { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
    int RequireUserId();
}

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserService(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public int? UserId
    {
        get
        {
            var raw = Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(raw, out var id) ? id : null;
        }
    }

    public string? UserName => Principal?.Identity?.Name;

    public string? FullName => Principal?.FindFirstValue("FullName");

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public bool IsAdmin => Principal?.IsInRole(AppRoles.Admin) == true;

    public int RequireUserId() =>
        UserId ?? throw new InvalidOperationException("No authenticated user in the current request.");
}
