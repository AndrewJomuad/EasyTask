using System.Security.Claims;
using HabitTracker.Web.Models;
using HabitTracker.Web.Models.Entities;
using HabitTracker.Web.Services;
using HabitTracker.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HabitTracker.Web.Controllers;

[Authorize]
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IActivityLogService _activity;
    private readonly ISettingsService _settings;
    private readonly Data.ApplicationDbContext _db;

    public AccountController(SignInManager<ApplicationUser> signIn, UserManager<ApplicationUser> users,
        IActivityLogService activity, ISettingsService settings, Data.ApplicationDbContext db)
    {
        _signIn = signIn;
        _users = users;
        _activity = activity;
        _settings = settings;
        _db = db;
    }

    // ------------------------------------------------------------ Login
    [HttpGet, AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        ViewData["ReturnUrl"] = model.ReturnUrl;

        if (!ModelState.IsValid) return View(model);

        var identifier = model.UserNameOrEmail.Trim();
        var user = identifier.Contains('@')
            ? await _users.FindByEmailAsync(identifier)
            : await _users.FindByNameAsync(identifier);

        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "This account has been deactivated. Contact an administrator.");
            return View(model);
        }

        var result = await _signIn.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _users.UpdateAsync(user);
            await _activity.LogAsync("Login", $"User '{user.UserName}' signed in.", user.Id);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            return RedirectToAction("Index", "Dashboard");
        }

        if (result.IsLockedOut)
        {
            await _activity.LogAsync("Login locked out", $"Account '{user.UserName}' locked out.", user.Id);
            ModelState.AddModelError(string.Empty, "This account is temporarily locked after too many failed attempts. Try again in 15 minutes.");
            return View(model);
        }

        await _activity.LogAsync("Login failed", $"Failed sign-in for '{identifier}'.", user.Id);
        ModelState.AddModelError(string.Empty, "Invalid username or password.");
        return View(model);
    }

    // ------------------------------------------------------------ Register
    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        if (!await _settings.GetBoolAsync(SettingKeys.AllowSelfRegistration, true))
        {
            TempData["Error"] = "Self-service registration is currently disabled. Please contact an administrator.";
            return RedirectToAction(nameof(Login));
        }

        return View(new RegisterViewModel());
    }


    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        // Check whether self-registration is enabled
        if (!await _settings.GetBoolAsync(
            SettingKeys.AllowSelfRegistration, true))
        {
            TempData["Error"] =
                "Self-service registration is currently disabled.";

            return RedirectToAction(nameof(Login));
        }

        // Validate terms acceptance
        if (!model.AcceptTerms)
        {
            ModelState.AddModelError(
                nameof(model.AcceptTerms),
                "You must accept the terms to continue."
            );
        }

        // Validate registration fields
        if (!ModelState.IsValid)
            return View(model);

        // Check duplicate username
        if (await _users.FindByNameAsync(model.UserName.Trim()) is not null)
        {
            ModelState.AddModelError(
                nameof(model.UserName),
                "That username is already taken."
            );
        }

        // Check duplicate email
        if (await _users.FindByEmailAsync(model.Email.Trim()) is not null)
        {
            ModelState.AddModelError(
                nameof(model.Email),
                "An account with that email already exists."
            );
        }

        if (!ModelState.IsValid)
            return View(model);

        // Create new user account
        var user = new ApplicationUser
        {
            UserName = model.UserName.Trim(),
            Email = model.Email.Trim(),
            FullName = model.FullName.Trim(),
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Save account using ASP.NET Core Identity
        var result = await _users.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description
                );
            }

            return View(model);
        }

        // Assign regular user role
        var roleResult = await _users.AddToRoleAsync(user, AppRoles.User);

        if (!roleResult.Succeeded)
        {
            await _users.DeleteAsync(user);

            foreach (var error in roleResult.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description
                );
            }

            return View(model);
        }

        // Sign in the newly registered user
        await _signIn.SignInAsync(user, isPersistent: false);

        // Record registration activity
        await _activity.LogAsync(
            "Account registered",
            $"New account '{user.UserName}'.",
            user.Id
        );

        TempData["Success"] =
            "Welcome to HTMS! Start by creating your first habit.";

        return RedirectToAction("Index", "Dashboard");
    }


    // ------------------------------------------------------------ Logout
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _activity.LogAsync("Logout", "User signed out.");
        await _signIn.SignOutAsync();
        TempData["Success"] = "You have been signed out.";
        return RedirectToAction(nameof(Login));
    }

    // ------------------------------------------------------------ Profile
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return Challenge();

        var roles = await _users.GetRolesAsync(user);

        var vm = new ProfileViewModel
        {
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            UserName = user.UserName ?? string.Empty,
            Bio = user.Bio,
            TimeZoneId = user.TimeZoneId,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            TotalPoints = user.TotalPoints,
            TotalHabits = _db.Habits.Count(h => h.UserId == user.Id),
            TotalCompletions = _db.HabitLogs.Count(l => l.Habit!.UserId == user.Id && l.Status == HabitLogStatus.Completed),
            AchievementsEarned = _db.UserAchievements.Count(ua => ua.UserId == user.Id),
            Role = roles.FirstOrDefault() ?? AppRoles.User
        };

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return Challenge();

        ModelState.Remove(nameof(model.Email));
        ModelState.Remove(nameof(model.UserName));

        if (!ModelState.IsValid)
        {
            model.Email = user.Email ?? string.Empty;
            model.UserName = user.UserName ?? string.Empty;
            model.CreatedAt = user.CreatedAt;
            model.LastLoginAt = user.LastLoginAt;
            model.TotalPoints = user.TotalPoints;
            return View(model);
        }

        user.FullName = model.FullName.Trim();
        user.Bio = model.Bio?.Trim();
        user.TimeZoneId = model.TimeZoneId;

        var result = await _users.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
            return View(model);
        }

        await _signIn.RefreshSignInAsync(user);
        await _activity.LogAsync("Profile updated", "Profile details changed.", user.Id);

        TempData["Success"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Profile));
    }

    // ------------------------------------------------------------ Password
    [HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _users.GetUserAsync(User);
        if (user is null) return Challenge();

        var result = await _users.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
            return View(model);
        }

        await _signIn.RefreshSignInAsync(user);
        await _activity.LogAsync("Password changed", "User changed their password.", user.Id);

        TempData["Success"] = "Your password has been changed.";
        return RedirectToAction(nameof(Profile));
    }

    // ------------------------------------------------------------ Access denied
    [HttpGet, AllowAnonymous]
    public IActionResult AccessDenied() => View();
}
