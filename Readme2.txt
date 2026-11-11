HTMS — Complete Visual Studio 2022 Solution
The full Habit Tracking Management System is built, tested and packaged as a downloadable ZIP.

Download: HTMS_VisualStudio_Solution.zip (valid for 24h) — 1.3 MB, 181 files

Stack (exactly as specified)
ASP.NET Core MVC (.NET 8) · C# 12 · Entity Framework Core 8 · SQL Server · ASP.NET Core Identity · Bootstrap 5.3 · Chart.js 4 · QuestPDF · ClosedXML · Visual Studio 2022

What's inside
11 domain entities + 6 Identity tables, 17 tables total, with fluent configurations, unique indexes and correct delete behaviour
8 services — habit/tracking workflow, streak engine, achievements, analytics, reminders, exports, activity log, current-user resolution
12 controllers including a fully separated /Admin area (users, categories, achievements, activity log, settings)
40+ Razor views — dashboard, habits CRUD, daily tracker, calendar, analytics, reminders, achievements, rewards, reports, profile, admin
EF Core migration (InitialCreate) + idempotent SQL Server schema script
15 xUnit tests for the streak engine
Docs: README, SETUP_GUIDE, ERD (Mermaid), DATA_DICTIONARY, USER_MANUAL
Verified, not assumed
dotnet build -c Release → 0 errors, 0 warnings (also re-verified from a clean unzip of the delivered ZIP)
dotnet test → 15/15 passing
79 automated HTTP checks → 79 passed, 0 failed, covering registration, sign-in, authorization boundaries, habit CRUD, tracking, calendar, analytics, reminders, achievements, rewards, PDF/Excel/CSV export, admin CRUD and settings
Every page rendered in real Chromium and visually inspected at 1440×900 and 390×844
Two things I fixed rather than documented around
Rewards was missing as a page. The spec requires reward transactions, but only a summary list existed. I built a real /Rewards ledger (balance, lifetime earned/spent, filters) plus /Rewards/Redeem with server-side balance enforcement — verified that redeeming 999,999 points is rejected and the balance stays intact.
CSV export was claimed but absent. Added a proper RFC 4180 implementation with UTF-8 BOM, and corrected the docs that overstated the Excel sheet count.
I also corrected the documentation where it had drifted from the code (Identity tables are Users/Roles, not AspNetUsers; the streak rule and column lists now match the actual schema).

Run it
Open HabitTracker.sln in Visual Studio 2022
Check the connection string in appsettings.json
Update-Database -Project HabitTracker.Web (optional — migrations run on startup)
Press F5
Admin: admin / Admin@12345 · Demo: demo / Demo@12345 (seeded with real habits, logs and points)

Change the admin password after first sign-in. Set "SeedDemoData": false for production.

What I could not verify
I ran the app against SQLite in this Linux sandbox — SQL Server cannot run here. The EF Core model, migration and SQL script are SQL Server-targeted and the build is clean, but the first Update-Database against a real SQL Server instance is the one step you should confirm on your machine. Everything else was exercised live.