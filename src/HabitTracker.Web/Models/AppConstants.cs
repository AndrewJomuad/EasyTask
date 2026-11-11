namespace HabitTracker.Web.Models;

/// <summary>Well-known role names used across the application.</summary>
public static class AppRoles
{
    public const string Admin = "Administrator";
    public const string User = "User";

    public static readonly string[] All = { Admin, User };
}

/// <summary>Well-known system setting keys.</summary>
public static class SettingKeys
{
    public const string PointsPerCompletion = "Points.PerCompletion";
    public const string PointsPerPerfectDay = "Points.PerfectDay";
    public const string PointsPerSkipped = "Points.Skipped";
    public const string AllowSelfRegistration = "Registration.AllowSelfService";
    public const string SiteName = "Site.Name";
    public const string ReminderWindowMinutes = "Reminders.WindowMinutes";
}
