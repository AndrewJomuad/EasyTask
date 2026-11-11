using System.ComponentModel.DataAnnotations;
using HabitTracker.Web.Models;
using HabitTracker.Web.Models.Entities;
using HabitTracker.Web.Services;

namespace HabitTracker.Web.ViewModels;

// ---------------------------------------------------------------- Auth
public class LoginViewModel
{
    [Required(ErrorMessage = "Enter your username or email.")]
    [Display(Name = "Username or email")]
    public string UserNameOrEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; } = true;

    public string? ReturnUrl { get; set; }
}


public class RegisterViewModel
{
    [Required(ErrorMessage = "Enter your full name.")]
    [StringLength(120, MinimumLength = 2)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose a username.")]
    [StringLength(40, MinimumLength = 3)]
    [RegularExpression(
        @"^[a-zA-Z0-9._-]+$",
        ErrorMessage = "Use letters, digits, dot, underscore or hyphen only."
    )]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose a password.")]
    [StringLength(100, MinimumLength = 8,
        ErrorMessage = "Use at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm your password.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password),
        ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "I accept the terms of use")]
    public bool AcceptTerms { get; set; }
}


public class ProfileViewModel
{
    [Required, StringLength(120, MinimumLength = 2)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Username")]
    public string UserName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Bio { get; set; }

    [Display(Name = "Time zone")]
    public string? TimeZoneId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int TotalPoints { get; set; }
    public int TotalHabits { get; set; }
    public int TotalCompletions { get; set; }
    public int AchievementsEarned { get; set; }
    public string? Role { get; set; }
}

public class ChangePasswordViewModel
{
    [Required, DataType(DataType.Password)]
    [Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8, ErrorMessage = "Use at least 8 characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Confirm new password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

// ---------------------------------------------------------------- Dashboard
public class DashboardViewModel
{
    public string FullName { get; set; } = string.Empty;
    public DateOnly Today { get; set; }
    public int ActiveHabits { get; set; }
    public int ScheduledToday { get; set; }
    public int CompletedToday { get; set; }
    public int SkippedToday { get; set; }
    public int PendingToday { get; set; }
    public double TodayCompletionRate { get; set; }
    public int CurrentDayStreak { get; set; }
    public int LongestStreak { get; set; }
    public int TotalPoints { get; set; }
    public int TotalCompletions { get; set; }
    public int AchievementsEarned { get; set; }
    public int AchievementsAvailable { get; set; }
    public List<TodayHabitItem> TodayHabits { get; set; } = new();
    public List<WeeklyPoint> WeeklyProgress { get; set; } = new();
    public List<HabitStreakRow> TopStreaks { get; set; } = new();
    public List<ActivityLog> RecentActivity { get; set; } = new();
    public List<Achievement> RecentAchievements { get; set; } = new();
    public List<Reminder> UpcomingReminders { get; set; } = new();
}

public class TodayHabitItem
{
    public int HabitId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public string CategoryColor { get; set; } = "#1D4ED8";
    public string Icon { get; set; } = "bi-check2-circle";
    public string Color { get; set; } = "#1D4ED8";
    public int CurrentStreak { get; set; }
    public int TargetPerPeriod { get; set; }
    public HabitLogStatus? Status { get; set; }
    public string? Notes { get; set; }
    public bool IsScheduled { get; set; }
}

public class WeeklyPoint
{
    public string Label { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public int Scheduled { get; set; }
    public int Completed { get; set; }
    public double Rate => Scheduled == 0 ? 0 : Math.Round(Completed * 100.0 / Scheduled, 1);
}

public class HabitStreakRow
{
    public int HabitId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#1D4ED8";
    public string Icon { get; set; } = "bi-check2-circle";
    public int Current { get; set; }
    public int Longest { get; set; }
    public double CompletionRate { get; set; }
}

// ---------------------------------------------------------------- Habits
public class HabitListViewModel
{
    public List<HabitCardItem> Habits { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public string Status { get; set; } = "active";
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
}

public class HabitCardItem
{
    public Habit Habit { get; set; } = null!;
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public double CompletionRate { get; set; }
    public int TotalCompletions { get; set; }
    public string ScheduleSummary { get; set; } = string.Empty;
    public bool ScheduledToday { get; set; }
    public HabitLogStatus? TodayStatus { get; set; }
}

public class HabitFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Give the habit a name.")]
    [StringLength(120, MinimumLength = 2)]
    [Display(Name = "Habit name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Choose a category.")]
    [Range(1, int.MaxValue, ErrorMessage = "Choose a category.")]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    [Display(Name = "Frequency")]
    public FrequencyType FrequencyType { get; set; } = FrequencyType.Daily;

    [Range(1, 100, ErrorMessage = "Target must be between 1 and 100.")]
    [Display(Name = "Target per period")]
    public int TargetPerPeriod { get; set; } = 1;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Start date")]
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [DataType(DataType.Date)]
    [Display(Name = "End date (optional)")]
    public DateOnly? EndDate { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [StringLength(20)]
    public string Color { get; set; } = "#1D4ED8";

    [StringLength(60)]
    public string Icon { get; set; } = "bi-check2-circle";

    [Display(Name = "Scheduled weekdays")]
    public List<DayOfWeek> SelectedDays { get; set; } = new();

    public List<Category> Categories { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext ctx)
    {
        if (EndDate.HasValue && EndDate.Value < StartDate)
            yield return new ValidationResult("The end date cannot be before the start date.", new[] { nameof(EndDate) });

        if (FrequencyType != FrequencyType.Daily && SelectedDays.Count == 0)
            yield return new ValidationResult("Select at least one weekday for a weekly or custom schedule.", new[] { nameof(SelectedDays) });
    }
}

public class HabitDetailsViewModel
{
    public Habit Habit { get; set; } = null!;
    public StreakResult Streak { get; set; } = new(0, 0, 0, 0, 0, 0);
    public List<HabitLog> RecentLogs { get; set; } = new();
    public List<Reminder> Reminders { get; set; } = new();
    public string ScheduleSummary { get; set; } = string.Empty;
    public List<WeeklyPoint> LastFourWeeks { get; set; } = new();
    public int TotalPointsEarned { get; set; }
}

// ---------------------------------------------------------------- Tracker
public class DailyTrackerViewModel
{
    public DateOnly Date { get; set; }
    public DateOnly Today { get; set; }
    public List<TrackerItem> Items { get; set; } = new();
    public int ScheduledCount => Items.Count;
    public int CompletedCount => Items.Count(i => i.Status == HabitLogStatus.Completed);
    public int SkippedCount => Items.Count(i => i.Status == HabitLogStatus.Skipped);
    public int PendingCount => Items.Count(i => i.Status is null);
    public double CompletionRate => ScheduledCount == 0 ? 0 : Math.Round(CompletedCount * 100.0 / ScheduledCount, 1);
    public bool IsFuture => Date > Today;
    public bool IsEditable => Date <= Today;
}

public class TrackerItem
{
    public int HabitId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = "#1D4ED8";
    public string Color { get; set; } = "#1D4ED8";
    public string Icon { get; set; } = "bi-check2-circle";
    public int TargetPerPeriod { get; set; }
    public int CurrentStreak { get; set; }
    public HabitLogStatus? Status { get; set; }
    public string? Notes { get; set; }
    public int? LogId { get; set; }
}

// ---------------------------------------------------------------- Calendar
public class CalendarViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public DateOnly FirstDay { get; set; }
    public DateOnly LastDay { get; set; }
    public DateOnly Today { get; set; }
    public List<CalendarDay> Days { get; set; } = new();
    public int TotalCompleted { get; set; }
    public int TotalSkipped { get; set; }
    public int TotalMissed { get; set; }
    public int PerfectDays { get; set; }
    public int PrevYear { get; set; }
    public int PrevMonth { get; set; }
    public int NextYear { get; set; }
    public int NextMonth { get; set; }
}

public class CalendarDay
{
    public DateOnly Date { get; set; }
    public bool IsCurrentMonth { get; set; }
    public bool IsToday { get; set; }
    public int Scheduled { get; set; }
    public int Completed { get; set; }
    public int Skipped { get; set; }
    public int Missed { get; set; }
    public string State => Scheduled == 0 ? "none"
        : Completed == Scheduled ? "complete"
        : Completed > 0 ? "partial"
        : Skipped == Scheduled ? "skipped"
        : "missed";
    public double Rate => Scheduled == 0 ? 0 : Math.Round(Completed * 100.0 / Scheduled, 1);
}

// ---------------------------------------------------------------- Analytics
public class AnalyticsViewModel
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public string Range { get; set; } = "30";
    public int? HabitId { get; set; }
    public int? CategoryId { get; set; }
    public List<Category> Categories { get; set; } = new();
    public List<Habit> Habits { get; set; } = new();

    public int TotalScheduled { get; set; }
    public int TotalCompleted { get; set; }
    public int TotalSkipped { get; set; }
    public int TotalMissed { get; set; }
    public double CompletionRate => TotalScheduled == 0 ? 0 : Math.Round(TotalCompleted * 100.0 / TotalScheduled, 1);
    public int PerfectDays { get; set; }
    public int ActiveDays { get; set; }
    public int BestStreak { get; set; }
    public int CurrentStreak { get; set; }

    public List<WeeklyPoint> DailySeries { get; set; } = new();
    public List<HabitComparisonRow> HabitComparison { get; set; } = new();
    public List<CategoryBreakdownRow> CategoryBreakdown { get; set; } = new();
    public List<WeekdayRow> WeekdayBreakdown { get; set; } = new();
}

public class HabitComparisonRow
{
    public int HabitId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#1D4ED8";
    public string CategoryName { get; set; } = string.Empty;
    public int Scheduled { get; set; }
    public int Completed { get; set; }
    public int Skipped { get; set; }
    public int Missed { get; set; }
    public double Rate => Scheduled == 0 ? 0 : Math.Round(Completed * 100.0 / Scheduled, 1);
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
}

public class CategoryBreakdownRow
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#1D4ED8";
    public int Scheduled { get; set; }
    public int Completed { get; set; }
    public double Rate => Scheduled == 0 ? 0 : Math.Round(Completed * 100.0 / Scheduled, 1);
}

public class WeekdayRow
{
    public string Day { get; set; } = string.Empty;
    public int Scheduled { get; set; }
    public int Completed { get; set; }
    public double Rate => Scheduled == 0 ? 0 : Math.Round(Completed * 100.0 / Scheduled, 1);
}

// ---------------------------------------------------------------- Reminders
public class ReminderListViewModel
{
    public List<ReminderRow> Reminders { get; set; } = new();
    public List<Habit> AvailableHabits { get; set; } = new();
    public int EnabledCount { get; set; }
    public int DisabledCount { get; set; }
    public List<ReminderRow> DueNow { get; set; } = new();
}

public class ReminderRow
{
    public Reminder Reminder { get; set; } = null!;
    public string HabitName { get; set; } = string.Empty;
    public string HabitColor { get; set; } = "#1D4ED8";
    public string HabitIcon { get; set; } = "bi-bell";
    public bool CompletedToday { get; set; }
    public bool ScheduledToday { get; set; }
    public bool IsDue { get; set; }
}

public class ReminderFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Choose a habit.")]
    [Range(1, int.MaxValue, ErrorMessage = "Choose a habit.")]
    [Display(Name = "Habit")]
    public int HabitId { get; set; }

    [Required]
    [DataType(DataType.Time)]
    [Display(Name = "Reminder time")]
    public TimeOnly ReminderTime { get; set; } = new(8, 0);

    [Display(Name = "Repeat")]
    public RepeatType RepeatType { get; set; } = RepeatType.Daily;

    [Display(Name = "Enabled")]
    public bool IsEnabled { get; set; } = true;

    public List<Habit> AvailableHabits { get; set; } = new();
}

// ---------------------------------------------------------------- Achievements
public class AchievementsViewModel
{
    public int TotalPoints { get; set; }
    public int EarnedCount { get; set; }
    public int TotalCount { get; set; }
    public List<AchievementCard> Achievements { get; set; } = new();
    public List<RewardTransaction> Transactions { get; set; } = new();
    public int TotalCompletions { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public int PerfectDays { get; set; }
    public int ActiveHabits { get; set; }
}

public class AchievementCard
{
    public Achievement Achievement { get; set; } = null!;
    public bool Earned { get; set; }
    public DateTime? EarnedAt { get; set; }
    public int Progress { get; set; }
    public int Target { get; set; }
    public double Percent => Target <= 0 ? 0 : Math.Min(100, Math.Round(Progress * 100.0 / Target, 1));
    public string CriteriaLabel { get; set; } = string.Empty;
}

// ---------------------------------------------------------------- Rewards
public class RewardsViewModel
{
    public int TotalPoints { get; set; }
    public int LifetimeEarned { get; set; }
    public int LifetimeSpent { get; set; }
    public int TransactionCount { get; set; }
    public string? Filter { get; set; }
    public List<RewardRow> Transactions { get; set; } = new();
    public List<RewardTransaction> Recent { get; set; } = new();
}

public class RewardRow
{
    public DateTime CreatedAt { get; set; }
    public int PointsChange { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? HabitName { get; set; }
    public bool IsCredit => PointsChange >= 0;
}

public class RedeemPointsViewModel
{
    [Required(ErrorMessage = "Enter the number of points to redeem.")]
    [Range(1, 1000000, ErrorMessage = "Enter a value between 1 and 1,000,000.")]
    [Display(Name = "Points")]
    public int Points { get; set; }

    [Required(ErrorMessage = "Enter a reason for the redemption.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Use between 3 and 200 characters.")]
    [Display(Name = "Reason")]
    public string Reason { get; set; } = string.Empty;

    public int Balance { get; set; }
}

// ---------------------------------------------------------------- Reports
public class ReportsViewModel
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public string Range { get; set; } = "30";
    public AnalyticsViewModel Analytics { get; set; } = new();
    public List<HabitLog> Logs { get; set; } = new();
    public int LogCount { get; set; }
}

// ---------------------------------------------------------------- Admin
public class AdminUsersViewModel
{
    public List<AdminUserRow> Users { get; set; } = new();
    public string? Search { get; set; }
    public string? Role { get; set; }
    public string? Status { get; set; }
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int AdminUsers { get; set; }
    public int NewThisMonth { get; set; }
}

public class AdminUserRow
{
    public ApplicationUser User { get; set; } = null!;
    public string Role { get; set; } = string.Empty;
    public int HabitCount { get; set; }
    public int LogCount { get; set; }
    public DateTime? LastActivity { get; set; }
}

public class AdminUserEditViewModel
{
    public int Id { get; set; }

    [Required, StringLength(120, MinimumLength = 2)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(40, MinimumLength = 3)]
    [Display(Name = "Username")]
    public string UserName { get; set; } = string.Empty;

    [Display(Name = "Role")]
    public string Role { get; set; } = AppRoles.User;

    [Display(Name = "Account active")]
    public bool IsActive { get; set; } = true;

    [StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "Reset password (leave blank to keep current)")]
    public string? NewPassword { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int TotalPoints { get; set; }
    public int HabitCount { get; set; }
    public int LogCount { get; set; }
}

public class CategoryFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(80, MinimumLength = 2)]
    [Display(Name = "Category name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Description { get; set; }

    [StringLength(20)]
    public string Color { get; set; } = "#1D4ED8";

    [StringLength(60)]
    public string Icon { get; set; } = "bi-tag";

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}

public class AchievementFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(100, MinimumLength = 2)]
    [Display(Name = "Achievement name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(400)]
    public string? Description { get; set; }

    [Display(Name = "Criteria")]
    public AchievementCriteria Criteria { get; set; } = AchievementCriteria.TotalCompletions;

    [Range(1, 100000, ErrorMessage = "Threshold must be at least 1.")]
    [Display(Name = "Threshold")]
    public int PointsRequired { get; set; } = 10;

    [StringLength(60)]
    public string Icon { get; set; } = "bi-award";

    [StringLength(20)]
    public string Color { get; set; } = "#F59E0B";

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;
}

public class AdminAchievementsViewModel
{
    public List<AchievementAdminRow> Achievements { get; set; } = new();
}

public class AchievementAdminRow
{
    public Achievement Achievement { get; set; } = null!;
    public int EarnedBy { get; set; }
}

public class AdminCategoriesViewModel
{
    public List<CategoryAdminRow> Categories { get; set; } = new();
}

public class CategoryAdminRow
{
    public Category Category { get; set; } = null!;
    public int HabitCount { get; set; }
}

public class AdminActivityViewModel
{
    public List<ActivityLog> Logs { get; set; } = new();
    public string? Search { get; set; }
    public string? Action { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public List<string> Actions { get; set; } = new();
}

public class AdminSettingsViewModel
{
    public Dictionary<string, string> Values { get; set; } = new();
    public List<SystemSetting> Settings { get; set; } = new();
    public string DatabaseProvider { get; set; } = "SQL Server";
    public bool DatabaseConnected { get; set; }
    public int PendingMigrations { get; set; }
    public int AppliedMigrations { get; set; }
}

public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalHabits { get; set; }
    public int ActiveHabits { get; set; }
    public int TotalLogs { get; set; }
    public int LogsToday { get; set; }
    public int TotalCategories { get; set; }
    public int TotalAchievements { get; set; }
    public int TotalPointsAwarded { get; set; }
    public List<WeeklyPoint> PlatformWeekly { get; set; } = new();
    public List<ActivityLog> RecentActivity { get; set; } = new();
    public List<AdminUserRow> NewestUsers { get; set; } = new();
    public List<CategoryBreakdownRow> CategoryUsage { get; set; } = new();
}
