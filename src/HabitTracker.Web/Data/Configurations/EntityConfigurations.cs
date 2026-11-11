using HabitTracker.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HabitTracker.Web.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> b)
    {
        b.Property(u => u.FullName).IsRequired().HasMaxLength(120);
        b.Property(u => u.Bio).HasMaxLength(500);
        b.Property(u => u.TimeZoneId).HasMaxLength(200);
        b.Property(u => u.TotalPoints).HasDefaultValue(0);
        b.HasIndex(u => u.IsActive);
    }
}

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.Property(c => c.Name).IsRequired().HasMaxLength(80);
        b.Property(c => c.Description).HasMaxLength(300);
        b.Property(c => c.Color).HasMaxLength(20);
        b.Property(c => c.Icon).HasMaxLength(60);
        b.HasIndex(c => c.Name).IsUnique();
    }
}

public class HabitConfiguration : IEntityTypeConfiguration<Habit>
{
    public void Configure(EntityTypeBuilder<Habit> b)
    {
        b.Property(h => h.Name).IsRequired().HasMaxLength(120);
        b.Property(h => h.Description).HasMaxLength(1000);
        b.Property(h => h.Color).HasMaxLength(20);
        b.Property(h => h.Icon).HasMaxLength(60);

        b.HasOne(h => h.User)
            .WithMany(u => u.Habits)
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(h => h.Category)
            .WithMany(c => c.Habits)
            .HasForeignKey(h => h.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(h => new { h.UserId, h.IsActive });
        b.HasIndex(h => h.CategoryId);
    }
}

public class HabitScheduleConfiguration : IEntityTypeConfiguration<HabitSchedule>
{
    public void Configure(EntityTypeBuilder<HabitSchedule> b)
    {
        b.HasOne(s => s.Habit)
            .WithMany(h => h.Schedules)
            .HasForeignKey(s => s.HabitId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(s => new { s.HabitId, s.DayOfWeek }).IsUnique();
    }
}

public class HabitLogConfiguration : IEntityTypeConfiguration<HabitLog>
{
    public void Configure(EntityTypeBuilder<HabitLog> b)
    {
        b.Property(l => l.Notes).HasMaxLength(500);

        b.HasOne(l => l.Habit)
            .WithMany(h => h.Logs)
            .HasForeignKey(l => l.HabitId)
            .OnDelete(DeleteBehavior.Cascade);

        // Business rule: at most one log per habit per date.
        b.HasIndex(l => new { l.HabitId, l.LogDate }).IsUnique();
        b.HasIndex(l => l.LogDate);
    }
}

public class ReminderConfiguration : IEntityTypeConfiguration<Reminder>
{
    public void Configure(EntityTypeBuilder<Reminder> b)
    {
        b.HasOne(r => r.Habit)
            .WithMany(h => h.Reminders)
            .HasForeignKey(r => r.HabitId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(r => new { r.IsEnabled, r.ReminderTime });
    }
}

public class AchievementConfiguration : IEntityTypeConfiguration<Achievement>
{
    public void Configure(EntityTypeBuilder<Achievement> b)
    {
        b.Property(a => a.Name).IsRequired().HasMaxLength(100);
        b.Property(a => a.Description).HasMaxLength(400);
        b.Property(a => a.Icon).HasMaxLength(60);
        b.Property(a => a.Color).HasMaxLength(20);
        b.HasIndex(a => a.Name).IsUnique();
    }
}

public class UserAchievementConfiguration : IEntityTypeConfiguration<UserAchievement>
{
    public void Configure(EntityTypeBuilder<UserAchievement> b)
    {
        b.HasOne(ua => ua.User)
            .WithMany(u => u.UserAchievements)
            .HasForeignKey(ua => ua.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(ua => ua.Achievement)
            .WithMany(a => a.UserAchievements)
            .HasForeignKey(ua => ua.AchievementId)
            .OnDelete(DeleteBehavior.Cascade);

        // Business rule: an achievement is earned only once per user.
        b.HasIndex(ua => new { ua.UserId, ua.AchievementId }).IsUnique();
    }
}

public class RewardTransactionConfiguration : IEntityTypeConfiguration<RewardTransaction>
{
    public void Configure(EntityTypeBuilder<RewardTransaction> b)
    {
        b.Property(t => t.Reason).IsRequired().HasMaxLength(200);

        b.HasOne(t => t.User)
            .WithMany(u => u.RewardTransactions)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(t => t.Log)
            .WithMany()
            .HasForeignKey(t => t.LogId)
            .OnDelete(DeleteBehavior.NoAction);

        b.HasIndex(t => new { t.UserId, t.CreatedAt });
    }
}

public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> b)
    {
        b.Property(a => a.Action).IsRequired().HasMaxLength(120);
        b.Property(a => a.Details).HasMaxLength(1000);
        b.Property(a => a.IpAddress).HasMaxLength(60);

        b.HasOne(a => a.User)
            .WithMany(u => u.ActivityLogs)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(a => a.CreatedAt);
    }
}

public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> b)
    {
        b.Property(s => s.Key).IsRequired().HasMaxLength(100);
        b.Property(s => s.Value).IsRequired().HasMaxLength(500);
        b.Property(s => s.Description).HasMaxLength(300);
        b.HasIndex(s => s.Key).IsUnique();
    }
}
