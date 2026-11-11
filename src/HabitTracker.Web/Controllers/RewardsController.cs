using HabitTracker.Web.Data;
using HabitTracker.Web.Models;
using HabitTracker.Web.Models.Entities;
using HabitTracker.Web.Services;
using HabitTracker.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HabitTracker.Web.Controllers;

/// <summary>
/// Reward transaction ledger: points earned from habit completions and achievements,
/// points spent on redemptions, and the running balance.
/// Every query is scoped to the signed-in user.
/// </summary>
[Authorize]
public class RewardsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IActivityLogService _activity;

    public RewardsController(ApplicationDbContext db, ICurrentUserService currentUser,
        IActivityLogService activity)
    {
        _db = db;
        _currentUser = currentUser;
        _activity = activity;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? filter = null)
    {
        var userId = _currentUser.RequireUserId();
        var model = await BuildAsync(userId, filter);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Redeem()
    {
        var userId = _currentUser.RequireUserId();
        var balance = await _db.Users.Where(u => u.Id == userId)
            .Select(u => u.TotalPoints).FirstOrDefaultAsync();

        return View(new RedeemPointsViewModel { Balance = balance });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Redeem(RedeemPointsViewModel model)
    {
        var userId = _currentUser.RequireUserId();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return Challenge();

        model.Balance = user.TotalPoints;

        if (model.Points > user.TotalPoints)
        {
            ModelState.AddModelError(nameof(model.Points),
                $"You only have {user.TotalPoints:N0} points available.");
        }

        if (!ModelState.IsValid) return View(model);

        // Debit the balance and record the ledger entry in one unit of work.
        user.TotalPoints -= model.Points;
        _db.RewardTransactions.Add(new RewardTransaction
        {
            UserId = userId,
            PointsChange = -model.Points,
            Reason = model.Reason.Trim(),
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        await _activity.LogAsync("Points redeemed",
            $"{model.Points} points redeemed: {model.Reason.Trim()}", userId);

        TempData["Success"] = $"Redeemed {model.Points:N0} points. New balance: {user.TotalPoints:N0}.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<RewardsViewModel> BuildAsync(int userId, string? filter)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);

        var query = _db.RewardTransactions.AsNoTracking()
            .Include(t => t.Log).ThenInclude(l => l!.Habit)
            .Where(t => t.UserId == userId);

        var all = await query.ToListAsync();

        var model = new RewardsViewModel
        {
            TotalPoints = user?.TotalPoints ?? 0,
            LifetimeEarned = all.Where(t => t.PointsChange > 0).Sum(t => t.PointsChange),
            LifetimeSpent = all.Where(t => t.PointsChange < 0).Sum(t => -t.PointsChange),
            TransactionCount = all.Count,
            Filter = filter,
            Recent = all.OrderByDescending(t => t.CreatedAt).Take(5).ToList()
        };

        var rows = all.AsEnumerable();

        rows = filter switch
        {
            "earned" => rows.Where(t => t.PointsChange > 0),
            "spent" => rows.Where(t => t.PointsChange < 0),
            _ => rows
        };

        model.Transactions = rows
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new RewardRow
            {
                CreatedAt = t.CreatedAt,
                PointsChange = t.PointsChange,
                Reason = t.Reason,
                HabitName = t.Log?.Habit?.Name
            })
            .ToList();

        return model;
    }
}
