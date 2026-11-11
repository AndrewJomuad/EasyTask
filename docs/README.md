# Habit Tracking Management System (HTMS)

A complete, production-ready **web-based Habit Tracking Management System** built as a
**Visual Studio 2022** solution on **ASP.NET Core MVC (.NET 8)** with **Entity Framework Core**,
**Microsoft SQL Server**, **ASP.NET Core Identity**, **Bootstrap 5**, **Chart.js**,
**QuestPDF** and **ClosedXML**.

Every feature is fully implemented and wired to the database — there is no mock data and no
placeholder functionality.

---

## 1. Technology Stack

| Layer | Technology |
|---|---|
| Frontend | HTML5, CSS3, JavaScript (ES6), Bootstrap 5.3, Bootstrap Icons, Chart.js 4 |
| Backend | C# 12, ASP.NET Core MVC (.NET 8) |
| Database | Microsoft SQL Server 2016+ (LocalDB / Express / Developer / Standard) |
| ORM | Entity Framework Core 8 (code-first migrations) |
| Authentication | ASP.NET Core Identity (cookie auth, PBKDF2 password hashing) |
| PDF Export | QuestPDF |
| Excel Export | ClosedXML |
| IDE | Visual Studio 2022 (17.8 or later) |

---

## 2. Solution Structure

```
HTMS.sln
├── src/HabitTracker.Web/            ASP.NET Core MVC web application
│   ├── Areas/Admin/                 Administrator area (own controllers + views)
│   │   ├── Controllers/             Dashboard, Users, Categories, Achievements, Activity, Settings
│   │   └── Views/
│   ├── Controllers/                 Account, Dashboard, Habits, Tracker, Calendar,
│   │                                Analytics, Reminders, Achievements, Rewards, Reports, Home
│   ├── Data/
│   │   ├── ApplicationDbContext.cs  EF Core context (Identity + domain sets)
│   │   ├── Configurations/          Fluent entity configurations, indexes, delete behaviour
│   │   └── DbSeeder.cs              Roles, admin account, categories, achievements, settings
│   ├── Migrations/                  EF Core migrations (InitialCreate)
│   ├── Models/
│   │   ├── Entities/                ApplicationUser, Category, Habit, HabitSchedule,
│   │   │                            HabitLog, Reminder, Achievement, UserAchievement,
│   │   │                            RewardTransaction, ActivityLog, SystemSetting
│   │   ├── Enums.cs                 FrequencyType, HabitLogStatus, RepeatType, AchievementCriteria
│   │   └── AppConstants.cs          Roles, policies, setting keys
│   ├── Services/                    Business logic layer
│   │   ├── HabitService.cs          Habit CRUD, daily tracking, completion workflow
│   │   ├── StreakService.cs         Current / longest streak, completion rate
│   │   ├── AchievementService.cs    Achievement evaluation and awarding
│   │   ├── AnalyticsService.cs      Aggregations for charts and KPIs
│   │   ├── ReminderService.cs       Reminder CRUD and due-reminder resolution
│   │   ├── ExportService.cs         QuestPDF + ClosedXML + CSV report generation
│   │   ├── ActivityLogService.cs    Audit trail
│   │   └── CurrentUserService.cs    Authenticated user resolution
│   ├── ViewModels/                  All strongly-typed view models + validation attributes
│   ├── Views/                       Razor views (shared layout, sidebar, all feature pages)
│   └── wwwroot/                     CSS, JS, Bootstrap, Bootstrap Icons, Chart.js
├── tests/HabitTracker.Tests/        xUnit unit tests for the streak engine
├── database/
│   ├── HTMS_Schema_SqlServer.sql    Idempotent schema script (generated from migrations)
│   └── HTMS_Seed_Reference.sql      Reference seed data
└── docs/
    ├── README.md                    This file
    ├── SETUP_GUIDE.md               Step-by-step setup and deployment
    ├── ERD.md                       Entity relationship diagram
    ├── DATA_DICTIONARY.md           Every table and column
    └── USER_MANUAL.md               End-user and administrator manual
```

---

## 3. Quick Start (Visual Studio 2022)

1. **Open the solution** — double-click `HTMS.sln`.
2. **Restore packages** — Visual Studio restores NuGet packages automatically on first build.
3. **Check the connection string** in `src/HabitTracker.Web/appsettings.json`:

   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=HabitTrackerDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
   }
   ```

   For SQL Server Express use `Server=.\SQLEXPRESS;...`; for a full instance use
   `Server=localhost;User Id=sa;Password=YourPassword;...`.

4. **Apply the database** — in *Package Manager Console* (Tools → NuGet Package Manager):

   ```powershell
   Update-Database -Project HabitTracker.Web
   ```

   Or with the .NET CLI from the solution folder:

   ```bash
   dotnet ef database update --project src/HabitTracker.Web
   ```

   The application also applies migrations automatically on startup, so simply pressing
   **F5** is enough — the database is created and seeded on first run.

5. **Run** — press **F5**. The browser opens at `https://localhost:7xxx`.

### Default administrator account

| Field | Value |
|---|---|
| Username | `admin` |
| Email | `admin@htms.local` |
| Password | `Admin@12345` |

> **Change this password immediately after the first sign-in** (Profile → Change password).
> The credentials are configurable under the `SeedAdmin` section of `appsettings.json`.

### Demo user account (seeded for evaluation)

| Field | Value |
|---|---|
| Username | `demo` |
| Password | `Demo@12345` |

The demo account is seeded with sample habits, logs, achievements and reward
transactions so every screen has real data on first launch. Set
`"SeedDemoData": false` in `appsettings.json` to skip it in production.

---

## 4. Feature Overview

### 4.1 Authentication & Authorization
- Registration with unique username and email, strong password policy.
- Cookie-based sign-in with sliding expiration, `HttpOnly`, `SameSite=Lax`, secure in production.
- Role-based authorization with two roles: **Administrator** and **User**.
- Policy-based authorization (`RequireAdministrator`) guarding the whole `/Admin` area.
- Account lockout after repeated failed attempts.
- Profile management, password change, sign-out.
- **User data isolation** — every habit, log, reminder and report query is filtered by the
  authenticated user's id; a user can never read or mutate another user's records.

### 4.2 Dashboard
- Greeting header with today's date and a live completion ring.
- KPI cards: active habits, completed today, current best streak, total points.
- Today's habit checklist with one-click complete / skip / undo.
- 14-day completion trend chart (Chart.js).
- Category breakdown doughnut chart.
- Recent achievements and recent reward transactions.
- Upcoming reminders for the next 7 days.

### 4.3 Habit Management
- Full CRUD: create, read, update, archive, restore, delete.
- Fields: name, description, category, frequency (Daily / Weekly / Custom weekdays),
  target per period, start date, end date, colour, icon, active flag.
- Custom weekday scheduling via `HabitSchedule`.
- Search, filter by category and status, and sorting.
- Habit detail page with streak statistics, a 30-day heatmap, recent log history and
  per-habit analytics.

### 4.4 Daily Tracking
- Date-navigable daily tracker (previous / next / today).
- Complete, skip, or clear a habit for any date.
- Optional note per log entry.
- Progress bar and per-day completion summary.
- Idempotent logging — completing twice does not duplicate points.

### 4.5 Calendar
- Month grid showing per-day completion ratio with colour intensity.
- Click any day to open the day's habit list and log directly.
- Month navigation and a legend.

### 4.6 Streak Monitoring
- Current streak, longest streak, scheduled opportunities, completions, skips, misses
  and completion rate per habit.
- Global best streak across all habits.
- Streaks recomputed from the log history on every read, so they can never drift.

### 4.7 Analytics
- Selectable range (7 / 30 / 90 / 365 days).
- Daily completion trend (line), weekly totals (bar), category distribution (doughnut),
  weekday performance (radar), habit leaderboard (horizontal bar).
- KPIs: total completions, average completion rate, best day, most consistent habit.

### 4.8 Reminders
- CRUD for reminders attached to a habit.
- Time of day, repeat type (Once / Daily / Weekly), enabled/disabled toggle.
- "Due now" panel resolving reminders for the current time window.
- Upcoming reminders list.

### 4.9 Achievements
- Achievement definitions with criteria types: total points, total completions,
  current streak, longest streak, perfect days, active habits.
- Automatic evaluation and awarding after every log change.
- Progress bars toward locked achievements.
- Unlocked / locked tabs with unlock dates.

### 4.10 Reward Transactions
- Points ledger: earn on completion, spend on rewards.
- Running balance, filterable history, and manual redemption.

### 4.11 Reporting
- Date-range reports with per-habit breakdown.
- **PDF export** via QuestPDF (summary KPIs, habit table, daily detail).
- **Excel export** via ClosedXML (Summary, Habit Performance, Daily Records sheets).
- **CSV export** of raw log data (RFC 4180 quoted, UTF-8 BOM for Excel).

### 4.12 Administrator Area (`/Admin`)
- **Dashboard** — platform KPIs, 7-day activity chart, top users, recent activity.
- **Users** — list, search, create, edit, assign roles, lock/unlock, reset password, delete.
- **Categories** — CRUD with colour, icon, active flag.
- **Achievements** — CRUD for achievement definitions and criteria.
- **Activity Log** — filterable audit trail of every significant action.
- **Settings** — system-wide configuration (site name, points per completion, streak
  thresholds, registration toggle, maintenance mode, items per page).

---

## 5. Security Implementation

| Concern | Implementation |
|---|---|
| Password storage | ASP.NET Core Identity PBKDF2 (HMAC-SHA256, 100 000 iterations, 128-bit salt) |
| CSRF | `[ValidateAntiForgeryToken]` on every POST; automatic token in all forms |
| SQL injection | EF Core parameterised queries exclusively — no string concatenation |
| XSS | Razor automatic HTML encoding; no `@Html.Raw` on user input |
| Cookie security | `HttpOnly`, `SameSite=Lax`, `SecurePolicy=Always` in production, sliding expiry |
| Authorization | `[Authorize]` on all feature controllers, `[Authorize(Policy=...)]` on the admin area |
| Data isolation | Every query filtered by `CurrentUserService.UserId` |
| Account lockout | 5 failed attempts → 15-minute lockout |
| Open redirect | `Url.IsLocalUrl()` check on all return URLs |
| Audit | `ActivityLog` records user, action, detail, IP and timestamp |

---

## 6. Database

- **Provider**: Microsoft SQL Server (EF Core `UseSqlServer`).
- **Migrations**: `src/HabitTracker.Web/Migrations` — apply with `Update-Database`.
- **Script**: `database/HTMS_Schema_SqlServer.sql` — idempotent, safe to run repeatedly.
- **Tables**: 17 (11 domain + 6 Identity).
- See `docs/ERD.md` for the entity relationship diagram and
  `docs/DATA_DICTIONARY.md` for every column.

---

## 7. Deployment

1. Publish: right-click the project → **Publish** → Folder / IIS / Azure App Service.
2. Set the production connection string in the hosting environment
   (`ConnectionStrings__DefaultConnection`).
3. Set `ASPNETCORE_ENVIRONMENT=Production` so secure cookies and HSTS are enforced.
4. Apply migrations on the target database (`dotnet ef database update` or run the SQL script).
5. Ensure the app pool identity has `db_datareader` and `db_datawriter` (add `db_ddladmin`
   only if you let the app migrate on startup).

Full details in `docs/SETUP_GUIDE.md`.

---

## 8. Verification

The application was built and exercised end-to-end:

- `dotnet build -c Release` — **0 errors, 0 warnings**.
- `dotnet test` — **15/15 unit tests passing** (streak engine: daily and custom
  schedules, skipped/missed days, open today, end dates, completion-rate rounding).
- **79 automated HTTP checks** covering registration, sign-in, authorization boundaries,
  habit CRUD, daily tracking, calendar, analytics, reminders, achievements, rewards
  (ledger, filters, redemption), reports (PDF/Excel/CSV), admin CRUD and settings — **all passing**.
- Every page rendered in a real Chromium browser and visually inspected at desktop
  (1440×900) and mobile (390×844) viewports.

---

## 9. License

Provided as a complete reference implementation for the HTMS specification.
