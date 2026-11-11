# HTMS — Data Dictionary

Complete column-level reference for every table in the Habit Tracking Management System.

Legend: **PK** = primary key, **FK** = foreign key, **UK** = unique, **NN** = not null.

---

## 1. Users (Identity)

Extends `IdentityUser<int>` with HTMS profile and gamification fields.

| Column | Type | Null | Key | Description |
|---|---|---|---|---|
| Id | int | NN | PK | Identity user id |
| UserName | nvarchar(256) | NN | UK | Login name |
| NormalizedUserName | nvarchar(256) | | UK | Upper-case login name for lookups |
| Email | nvarchar(256) | NN | UK | Email address |
| NormalizedEmail | nvarchar(256) | | UK | Upper-case email for lookups |
| EmailConfirmed | bit | NN | | Email confirmation flag |
| PasswordHash | nvarchar(max) | | | PBKDF2 hash (never plaintext) |
| SecurityStamp | nvarchar(max) | | | Invalidates cookies on credential change |
| ConcurrencyStamp | nvarchar(max) | | | Optimistic concurrency token |
| PhoneNumber | nvarchar(max) | | | Optional phone |
| PhoneNumberConfirmed | bit | NN | | Phone confirmation flag |
| TwoFactorEnabled | bit | NN | | 2FA flag |
| LockoutEnd | datetimeoffset | | | Lockout expiry |
| LockoutEnabled | bit | NN | | Whether lockout applies |
| AccessFailedCount | int | NN | | Consecutive failed sign-ins |
| FullName | nvarchar(120) | NN | | Display name |
| Bio | nvarchar(500) | | | Optional short biography |
| TimeZoneId | nvarchar(64) | | | Time zone (default `Asia/Singapore`) |
| IsActive | bit | NN | | Soft enable/disable by administrator |
| CreatedAt | datetime2 | NN | | Registration timestamp |
| LastLoginAt | datetime2 | | | Last successful sign-in |
| TotalPoints | int | NN | | Current reward point balance |

---

## 2. Roles / UserRoles / UserClaims / RoleClaims / UserLogins / UserTokens

Standard ASP.NET Core Identity tables. `Roles` is seeded with:

| Name | NormalizedName | Purpose |
|---|---|---|
| Administrator | ADMINISTRATOR | Full access including `/Admin` |
| User | USER | Standard habit-tracking access |

---

## 3. Categories

Habit classification, managed by administrators.

| Column | Type | Null | Key | Description |
|---|---|---|---|---|
| Id | int | NN | PK | Identity |
| Name | nvarchar(80) | NN | UK | Category name |
| Description | nvarchar(300) | | | Optional description |
| Color | nvarchar(20) | NN | | Display colour, e.g. `#1D4ED8` |
| Icon | nvarchar(60) | NN | | Bootstrap Icons class, e.g. `bi-heart-pulse` |
| IsActive | bit | NN | | Hidden from selection when false |
| CreatedAt | datetime2 | NN | | Creation timestamp |

**Seeded rows**: Health, Fitness, Learning, Productivity, Mindfulness, Finance, Social, Creativity.

---

## 4. Habits

The core entity — one habit owned by one user.

| Column | Type | Null | Key | Description |
|---|---|---|---|---|
| Id | int | NN | PK | Identity |
| UserId | int | NN | FK → Users | Owner (cascade delete) |
| CategoryId | int | NN | FK → Categories | Classification (restrict delete) |
| Name | nvarchar(120) | NN | | Habit name |
| Description | nvarchar(1000) | | | Optional description |
| FrequencyType | int | NN | | `0` Daily, `1` Weekly, `2` Custom |
| TargetPerPeriod | int | NN | | Target count per period (1–100) |
| StartDate | date | NN | | First day the habit applies |
| EndDate | date | | | Optional end date |
| Color | nvarchar(20) | NN | | Display colour |
| Icon | nvarchar(60) | NN | | Bootstrap Icons class |
| IsActive | bit | NN | | `false` = archived |
| CreatedAt | datetime2 | NN | | Creation timestamp |
| UpdatedAt | datetime2 | | | Last modification timestamp |

**Indexes**: `(UserId, IsActive)`, `(UserId, CategoryId)`.

**Scheduling rule** (`Habit.IsScheduledOn`): a habit is due on a date when it is active,
the date is within `[StartDate, EndDate]`, and — for `Weekly`/`Custom` frequencies — the
date's weekday appears in `HabitSchedules`.

---

## 5. HabitSchedules

One row per weekday a weekly/custom habit is scheduled on.

| Column | Type | Null | Key | Description |
|---|---|---|---|---|
| Id | int | NN | PK | Identity |
| HabitId | int | NN | FK → Habits | Parent habit (cascade delete) |
| DayOfWeek | int | NN | | `0` Sunday … `6` Saturday |

**Index**: `(HabitId, DayOfWeek)` UNIQUE — no duplicate weekday rows.

---

## 6. HabitLogs

One row per habit per calendar day.

| Column | Type | Null | Key | Description |
|---|---|---|---|---|
| Id | int | NN | PK | Identity |
| HabitId | int | NN | FK → Habits | Parent habit (cascade delete) |
| LogDate | date | NN | | The calendar day |
| Status | int | NN | | `0` Completed, `1` Skipped |
| Notes | nvarchar(500) | | | Optional note |
| CompletedAt | datetime2 | NN | | When the entry was recorded |
| CreatedAt | datetime2 | NN | | Creation timestamp |

**Indexes**: `(HabitId, LogDate)` UNIQUE, `(LogDate)`.

---

## 7. Reminders

| Column | Type | Null | Key | Description |
|---|---|---|---|---|
| Id | int | NN | PK | Identity |
| HabitId | int | NN | FK → Habits | Linked habit (cascade delete) |
| ReminderTime | time | NN | | Time the reminder fires (default 08:00) |
| RepeatType | int | NN | | `0` Once, `1` Daily, `2` Weekly |
| IsEnabled | bit | NN | | Active flag |
| LastTriggeredAt | datetime2 | | | Last time it was shown as due |
| CreatedAt | datetime2 | NN | | Creation timestamp |

**Index**: `(HabitId, IsEnabled)`.

---

## 8. Achievements

Achievement definitions, managed by administrators.

| Column | Type | Null | Key | Description |
|---|---|---|---|---|
| Id | int | NN | PK | Identity |
| Name | nvarchar(120) | NN | UK | Achievement name |
| Description | nvarchar(400) | | | What it takes to unlock |
| Criteria | int | NN | | `0` TotalPoints, `1` TotalCompletions, `2` CurrentStreak, `3` LongestStreak, `4` PerfectDays, `5` ActiveHabits |
| PointsRequired | int | NN | | Threshold value required to unlock |
| Icon | nvarchar(60) | NN | | Bootstrap Icons class |
| Color | nvarchar(20) | NN | | Badge colour |
| IsActive | bit | NN | | Whether it can be awarded |
| CreatedAt | datetime2 | NN | | Creation timestamp |

**Seeded rows**: 12 achievements spanning all criteria types.

---

## 9. UserAchievements

| Column | Type | Null | Key | Description |
|---|---|---|---|---|
| Id | int | NN | PK | Identity |
| UserId | int | NN | FK → Users | Owner (cascade delete) |
| AchievementId | int | NN | FK → Achievements | Unlocked achievement (cascade delete) |
| EarnedAt | datetime2 | NN | | Unlock timestamp |

**Index**: `(UserId, AchievementId)` UNIQUE — an achievement is unlocked once.

---

## 10. RewardTransactions

Points ledger.

| Column | Type | Null | Key | Description |
|---|---|---|---|---|
| Id | int | NN | PK | Identity |
| UserId | int | NN | FK → Users | Owner (cascade delete) |
| LogId | int | | FK → HabitLogs | Optional source log (set null on delete) |
| PointsChange | int | NN | | Signed point amount |
| Reason | nvarchar(300) | NN | | Human-readable reason |
| CreatedAt | datetime2 | NN | | Transaction timestamp |

**Index**: `(UserId, CreatedAt)`.

---

## 11. ActivityLogs

Audit trail of significant actions.

| Column | Type | Null | Key | Description |
|---|---|---|---|---|
| Id | int | NN | PK | Identity |
| UserId | int | | FK → Users | Actor (cascade delete) |
| Action | nvarchar(120) | NN | | e.g. `Habit created`, `User signed in` |
| Details | nvarchar(1000) | | | Free-text detail |
| IpAddress | nvarchar(64) | | | Client IP |
| CreatedAt | datetime2 | NN | | Timestamp |

**Index**: `(UserId, CreatedAt)`.

---

## 12. SystemSettings

Key/value configuration editable from `/Admin/Settings`.

| Column | Type | Null | Key | Description |
|---|---|---|---|---|
| Id | int | NN | PK | Identity |
| Key | nvarchar(80) | NN | UK | Setting key |
| Value | nvarchar(500) | NN | | Setting value (stored as text) |
| Description | nvarchar(300) | | | Human-readable description |
| UpdatedAt | datetime2 | NN | | Last modification |

**Seeded keys**

| Key | Default | Meaning |
|---|---|---|
| `SiteName` | Habit Tracking Management System | Displayed brand name |
| `PointsPerCompletion` | 10 | Default points for a new habit |
| `StreakBonusThreshold` | 7 | Days before a streak bonus applies |
| `StreakBonusPoints` | 25 | Bonus points at the threshold |
| `AllowRegistration` | true | Whether public registration is open |
| `MaintenanceMode` | false | Show a maintenance notice |
| `ItemsPerPage` | 10 | Default list page size |
| `MaxHabitsPerUser` | 50 | Habit cap per user |

---

## 13. Enum Reference

| Enum | Values |
|---|---|
| `FrequencyType` | `Daily = 0`, `Weekly = 1`, `Custom = 2` |
| `HabitLogStatus` | `Completed = 0`, `Skipped = 1` |
| `RepeatType` | `Once = 0`, `Daily = 1`, `Weekly = 2` |
| `AchievementCriteria` | `TotalPoints = 0`, `TotalCompletions = 1`, `CurrentStreak = 2`, `LongestStreak = 3`, `PerfectDays = 4`, `ActiveHabits = 5` |
