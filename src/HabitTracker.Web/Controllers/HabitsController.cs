using HabitTracker.Web.Models;
using HabitTracker.Web.Services;
using HabitTracker.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HabitTracker.Web.Controllers;

[Authorize]
public class HabitsController : Controller
{
    private readonly IHabitService _habits;
    private readonly ICurrentUserService _currentUser;

    public HabitsController(IHabitService habits, ICurrentUserService currentUser)
    {
        _habits = habits;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, int? categoryId, string status = "active")
    {
        var userId = _currentUser.RequireUserId();
        var model = await _habits.GetListAsync(userId, search, categoryId, status);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var userId = _currentUser.RequireUserId();
        var model = await _habits.BuildFormAsync(userId);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(HabitFormViewModel model)
    {
        var userId = _currentUser.RequireUserId();

        if (!ModelState.IsValid)
        {
            model.Categories = await _habits.GetCategoriesAsync(false);
            return View(model);
        }

        var result = await _habits.CreateAsync(userId, model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message!);
            model.Categories = await _habits.GetCategoriesAsync(false);
            return View(model);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = result.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = _currentUser.RequireUserId();
        var habit = await _habits.GetOwnedHabitAsync(id, userId);
        if (habit is null) return NotFound();

        var model = await _habits.BuildFormAsync(userId, habit);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(HabitFormViewModel model)
    {
        var userId = _currentUser.RequireUserId();

        if (!ModelState.IsValid)
        {
            model.Categories = await _habits.GetCategoriesAsync(false);
            return View(model);
        }

        var result = await _habits.UpdateAsync(userId, model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message!);
            model.Categories = await _habits.GetCategoriesAsync(false);
            return View(model);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var userId = _currentUser.RequireUserId();
        var model = await _habits.GetDetailsAsync(id, userId);
        if (model is null) return NotFound();
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id, string? returnUrl)
    {
        var userId = _currentUser.RequireUserId();
        var result = await _habits.ToggleActiveAsync(id, userId);

        if (result.Success) TempData["Success"] = result.Message;
        else TempData["Error"] = result.Message;

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = _currentUser.RequireUserId();
        var result = await _habits.DeleteAsync(id, userId);

        if (result.Success) TempData["Success"] = result.Message;
        else TempData["Error"] = result.Message;

        return RedirectToAction(nameof(Index));
    }
}
