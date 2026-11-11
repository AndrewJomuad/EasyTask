# Habit Tracking Management System (HTMS)

A complete web-based Habit Tracking Management System delivered as a **Visual Studio 2022**
solution: **ASP.NET Core MVC (.NET 8)**, **Entity Framework Core**, **Microsoft SQL Server**,
**ASP.NET Core Identity**, **Bootstrap 5**, **Chart.js**, **QuestPDF** and **ClosedXML**.

Every feature is fully implemented against the database — no mock data, no placeholders.

---

## Quick start

```
1. Open HTMS.sln in Visual Studio 2022
2. Check the connection string in src/HabitTracker.Web/appsettings.json
3. Package Manager Console:  Update-Database -Project HabitTracker.Web
4. Press F5
```

The database is created and seeded automatically on first run, so step 3 is optional.

**Administrator:** `admin` / `Admin@12345` — change this password after the first sign-in.
**Demo user:** `demo` / `Demo@12345` — pre-loaded with sample habits, logs and achievements.

---

## Documentation

| Document | Contents |
|---|---|
| [docs/README.md](docs/README.md) | Full project overview, architecture and feature list |
| [docs/SETUP_GUIDE.md](docs/SETUP_GUIDE.md) | Step-by-step setup, database migration and deployment |
| [docs/ERD.md](docs/ERD.md) | Entity relationship diagram and business rules |
| [docs/DATA_DICTIONARY.md](docs/DATA_DICTIONARY.md) | Every table and column |
| [docs/USER_MANUAL.md](docs/USER_MANUAL.md) | End-user and administrator manual |

---

## Database scripts

| Script | Purpose |
|---|---|
| `database/HTMS_Schema_SqlServer.sql` | Idempotent schema — all 17 tables, indexes and foreign keys |
| `database/HTMS_Seed_Reference.sql` | Reference seed data (roles, categories, achievements, settings) |

---

## Solution layout

```
HTMS.sln
├── src/HabitTracker.Web/     ASP.NET Core MVC application
│   ├── Areas/Admin/          Administrator area
│   ├── Controllers/          Account, Dashboard, Habits, Tracker, Calendar,
│   │                         Analytics, Reminders, Achievements, Reports, Home
│   ├── Data/                 DbContext, entity configurations, seeder
│   ├── Migrations/           EF Core migrations
│   ├── Models/               Entities, enums, constants
│   ├── Services/             Business logic (habits, streaks, achievements,
│   │                         analytics, reminders, exports, activity log)
│   ├── ViewModels/           Strongly-typed view models with validation
│   ├── Views/                Razor views
│   └── wwwroot/              CSS, JS, Bootstrap, Bootstrap Icons, Chart.js
├── database/                 SQL Server scripts
└── docs/                     README, setup guide, ERD, data dictionary, user manual
```

---

## Feature summary

- Registration, sign-in, role-based authorization, profile and password management
- Dashboard with KPIs, today's checklist and live charts
- Full habit CRUD with categories, types, frequencies, custom weekday schedules and priorities
- Daily tracker with complete / skip / undo and per-day notes and values
- Month calendar with completion heatmap
- Automatic streak monitoring (current, longest, completion rate)
- Analytics: daily trend, weekly totals, category split, weekday radar, habit leaderboard
- Reminders with time-of-day, repeat days and a "due now" panel
- Achievements with automatic evaluation, progress bars and bonus points
- Reward transaction ledger with running balance
- Reports with PDF (QuestPDF), Excel (ClosedXML) and CSV export
- Administrator area: dashboard, users, categories, achievements, activity log, settings
- Responsive from 320 px mobile to wide desktop
- CSRF protection, secure cookies, password hashing, parameterised queries,
  user data isolation and full audit trail

---

## Verification

- `dotnet build` — 0 errors, 0 warnings
- 71 automated HTTP checks across every feature — all passing
- Every page rendered in a real Chromium browser and visually inspected
