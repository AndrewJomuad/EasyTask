using HabitTracker.Web.Models;
using HabitTracker.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HabitTracker.Web.Data;

/// <summary>
/// Creates the database schema (when migrations are unavailable), roles, the
/// administrator account, reference data and an optional demo dataset.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config, ILogger logger)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<ApplicationDbContext>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole<int>>>();

        // SQL Server uses the EF Core migrations shipped in /Migrations.
        // The SQLite provider is only used for local smoke-testing, where the
        // schema is created directly from the model.
        if (db.Database.IsSqlite())
            await db.Database.EnsureCreatedAsync();
        else
            await db.Database.MigrateAsync();

        // ---- Roles -------------------------------------------------------
        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<int>(role));
        }

        // ---- System settings --------------------------------------------
        var defaults = new (string Key, string Value, string Description)[]
        {
            (SettingKeys.PointsPerCompletion, "10", "Points awarded for each completed habit."),
            (SettingKeys.PointsPerPerfectDay, "25", "Bonus points when every scheduled habit is completed in a day."),
            (SettingKeys.PointsPerSkipped, "0", "Points awarded for a skipped habit (0 = none)."),
            (SettingKeys.AllowSelfRegistration, "true", "Allow visitors to create their own accounts."),
            (SettingKeys.SiteName, "Habit Tracking Management System", "Application display name."),
            (SettingKeys.ReminderWindowMinutes, "30", "Minutes after the reminder time during which it is still shown.")
        };

        foreach (var (key, value, description) in defaults)
        {
            if (!await db.SystemSettings.AnyAsync(s => s.Key == key))
                db.SystemSettings.Add(new SystemSetting { Key = key, Value = value, Description = description });
        }
        await db.SaveChangesAsync();

        // ---- Categories --------------------------------------------------
        if (!await db.Categories.AnyAsync())
        {
            db.Categories.AddRange(
                new Category { Name = "Health", Description = "Physical and mental wellbeing.", Color = "#16A34A", Icon = "bi-heart-pulse" },
                new Category { Name = "Fitness", Description = "Exercise, movement and strength.", Color = "#EA580C", Icon = "bi-activity" },
                new Category { Name = "Education", Description = "Learning, reading and study.", Color = "#1D4ED8", Icon = "bi-book" },
                new Category { Name = "Productivity", Description = "Focus, planning and deep work.", Color = "#7C3AED", Icon = "bi-lightning-charge" },
                new Category { Name = "Mindfulness", Description = "Meditation, gratitude and rest.", Color = "#0EA5E9", Icon = "bi-flower1" },
                new Category { Name = "Finance", Description = "Saving, budgeting and spending habits.", Color = "#0F766E", Icon = "bi-piggy-bank" },
                new Category { Name = "Social", Description = "Family, friends and community.", Color = "#DB2777", Icon = "bi-people" });
            await db.SaveChangesAsync();
        }

        // ---- Achievements ------------------------------------------------
        if (!await db.Achievements.AnyAsync())
        {
            db.Achievements.AddRange(
                new Achievement { Name = "First Step", Description = "Complete your very first habit.", Criteria = AchievementCriteria.TotalCompletions, PointsRequired = 1, Icon = "bi-flag", Color = "#38BDF8" },
                new Achievement { Name = "Getting Started", Description = "Complete 10 habits.", Criteria = AchievementCriteria.TotalCompletions, PointsRequired = 10, Icon = "bi-rocket", Color = "#1D4ED8" },
                new Achievement { Name = "Habit Builder", Description = "Complete 50 habits.", Criteria = AchievementCriteria.TotalCompletions, PointsRequired = 50, Icon = "bi-bricks", Color = "#7C3AED" },
                new Achievement { Name = "Centurion", Description = "Complete 100 habits.", Criteria = AchievementCriteria.TotalCompletions, PointsRequired = 100, Icon = "bi-shield-check", Color = "#0F766E" },
                new Achievement { Name = "Week Warrior", Description = "Reach a 7-day streak.", Criteria = AchievementCriteria.CurrentStreak, PointsRequired = 7, Icon = "bi-fire", Color = "#EA580C" },
                new Achievement { Name = "Fortnight Focus", Description = "Reach a 14-day streak.", Criteria = AchievementCriteria.CurrentStreak, PointsRequired = 14, Icon = "bi-calendar-check", Color = "#DB2777" },
                new Achievement { Name = "Monthly Master", Description = "Reach a 30-day streak.", Criteria = AchievementCriteria.CurrentStreak, PointsRequired = 30, Icon = "bi-trophy", Color = "#F59E0B" },
                new Achievement { Name = "Perfect Day", Description = "Complete every scheduled habit in a single day.", Criteria = AchievementCriteria.PerfectDays, PointsRequired = 1, Icon = "bi-star", Color = "#FACC15" },
                new Achievement { Name = "Perfect Ten", Description = "Record 10 perfect days.", Criteria = AchievementCriteria.PerfectDays, PointsRequired = 10, Icon = "bi-stars", Color = "#F59E0B" },
                new Achievement { Name = "Point Collector", Description = "Earn 500 points.", Criteria = AchievementCriteria.TotalPoints, PointsRequired = 500, Icon = "bi-gem", Color = "#0EA5E9" },
                new Achievement { Name = "Point Hoarder", Description = "Earn 2,000 points.", Criteria = AchievementCriteria.TotalPoints, PointsRequired = 2000, Icon = "bi-diamond", Color = "#7C3AED" },
                new Achievement { Name = "Habit Architect", Description = "Keep 5 active habits at once.", Criteria = AchievementCriteria.ActiveHabits, PointsRequired = 5, Icon = "bi-diagram-3", Color = "#16A34A" });
            await db.SaveChangesAsync();
        }

        // ---- Administrator ----------------------------------------------
        var adminEmail = config["Seed:AdminEmail"] ?? "admin@habittracker.local";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = config["Seed:AdminUserName"] ?? "admin",
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = config["Seed:AdminFullName"] ?? "System Administrator",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(adminUser, config["Seed:AdminPassword"] ?? "Admin@12345");
            if (!result.Succeeded)
                throw new InvalidOperationException("Could not create administrator: " +
                    string.Join("; ", result.Errors.Select(e => e.Description)));
            await userManager.AddToRoleAsync(adminUser, AppRoles.Admin);
            logger.LogInformation("Administrator account created: {Email}", adminEmail);
        }
        else if (!await userManager.IsInRoleAsync(adminUser, AppRoles.Admin))
        {
            await userManager.AddToRoleAsync(adminUser, AppRoles.Admin);
        }

        // ---- Demo user + sample history ---------------------------------
        if (config.GetValue("Seed:CreateDemoData", true))
        {
            var demoEmail = config["Seed:DemoUserEmail"] ?? "demo@habittracker.local";
            var demo = await userManager.FindByEmailAsync(demoEmail);
            if (demo is null)
            {
                demo = new ApplicationUser
                {
                    UserName = config["Seed:DemoUserName"] ?? "demo",
                    Email = demoEmail,
                    EmailConfirmed = true,
                    FullName = config["Seed:DemoFullName"] ?? "Demo User",
                    Bio = "Sample account pre-loaded with 60 days of habit history.",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-60)
                };
                var result = await userManager.CreateAsync(demo, config["Seed:DemoPassword"] ?? "Demo@12345");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(demo, AppRoles.User);
                    await SeedDemoHistoryAsync(db, demo.Id);
                }
            }
        }
    }

    private static async Task SeedDemoHistoryAsync(ApplicationDbContext db, int userId)
    {
        var categories = await db.Categories.ToListAsync();
        int Cat(string name) => categories.First(c => c.Name == name).Id;

        var habits = new List<Habit>
        {
            new() { UserId = userId, CategoryId = Cat("Health"), Name = "Drink 8 glasses of water", Description = "Stay hydrated throughout the day.", FrequencyType = FrequencyType.Daily, TargetPerPeriod = 8, StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-60)), Color = "#0EA5E9", Icon = "bi-droplet" },
            new() { UserId = userId, CategoryId = Cat("Fitness"), Name = "Morning run", Description = "30 minutes of easy running before breakfast.", FrequencyType = FrequencyType.Weekly, TargetPerPeriod = 1, StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-60)), Color = "#EA580C", Icon = "bi-activity" },
            new() { UserId = userId, CategoryId = Cat("Education"), Name = "Read 20 pages", Description = "Read a non-fiction book.", FrequencyType = FrequencyType.Daily, TargetPerPeriod = 20, StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-60)), Color = "#1D4ED8", Icon = "bi-book" },
            new() { UserId = userId, CategoryId = Cat("Productivity"), Name = "Plan tomorrow", Description = "Write the top three priorities for the next day.", FrequencyType = FrequencyType.Weekly, TargetPerPeriod = 1, StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-60)), Color = "#7C3AED", Icon = "bi-list-check" },
            new() { UserId = userId, CategoryId = Cat("Mindfulness"), Name = "Meditate 10 minutes", Description = "Guided breathing session.", FrequencyType = FrequencyType.Daily, TargetPerPeriod = 10, StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-60)), Color = "#16A34A", Icon = "bi-flower1" }
        };

        // Weekly habits: Mon/Wed/Fri and Tue/Thu respectively.
        habits[1].Schedules = new List<HabitSchedule>
        {
            new() { DayOfWeek = DayOfWeek.Monday },
            new() { DayOfWeek = DayOfWeek.Wednesday },
            new() { DayOfWeek = DayOfWeek.Friday }
        };
        habits[3].Schedules = new List<HabitSchedule>
        {
            new() { DayOfWeek = DayOfWeek.Tuesday },
            new() { DayOfWeek = DayOfWeek.Thursday }
        };

        db.Habits.AddRange(habits);
        await db.SaveChangesAsync();

        db.Reminders.AddRange(
            new Reminder { HabitId = habits[0].Id, ReminderTime = new TimeOnly(9, 0), RepeatType = RepeatType.Daily, IsEnabled = true },
            new Reminder { HabitId = habits[2].Id, ReminderTime = new TimeOnly(20, 30), RepeatType = RepeatType.Daily, IsEnabled = true },
            new Reminder { HabitId = habits[4].Id, ReminderTime = new TimeOnly(7, 0), RepeatType = RepeatType.Daily, IsEnabled = false });

        // Deterministic pseudo-random history so the demo looks realistic.
        var rng = new Random(20261111);
        var logs = new List<HabitLog>();
        var today = DateOnly.FromDateTime(DateTime.Today);

        for (var offset = 60; offset >= 0; offset--)
        {
            var date = today.AddDays(-offset);
            foreach (var habit in habits)
            {
                if (!habit.IsScheduledOn(date)) continue;
                if (offset == 0 && habit.Id == habits[2].Id) continue; // leave one pending today

                var roll = rng.Next(100);
                if (roll < 78)
                {
                    logs.Add(new HabitLog
                    {
                        HabitId = habit.Id,
                        LogDate = date,
                        Status = HabitLogStatus.Completed,
                        CompletedAt = date.ToDateTime(new TimeOnly(8, 0)).AddMinutes(rng.Next(0, 600)),
                        Notes = roll < 8 ? "Felt great today." : null
                    });
                }
                else if (roll < 88)
                {
                    logs.Add(new HabitLog
                    {
                        HabitId = habit.Id,
                        LogDate = date,
                        Status = HabitLogStatus.Skipped,
                        CompletedAt = date.ToDateTime(new TimeOnly(21, 0)),
                        Notes = "Ran out of time."
                    });
                }
            }
        }

        db.HabitLogs.AddRange(logs);
        await db.SaveChangesAsync();

        // Points ledger for the demo history.
        var transactions = logs
            .Where(l => l.Status == HabitLogStatus.Completed)
            .Select(l => new RewardTransaction
            {
                UserId = userId,
                LogId = l.Id,
                PointsChange = 10,
                Reason = "Habit completed",
                CreatedAt = l.CompletedAt
            })
            .ToList();

        db.RewardTransactions.AddRange(transactions);
        await db.SaveChangesAsync();

        var user = await db.Users.FirstAsync(u => u.Id == userId);
        user.TotalPoints = transactions.Sum(t => t.PointsChange);
        await db.SaveChangesAsync();
    }
}
