# HTMS — Setup & Deployment Guide

This guide takes you from a clean machine to a running HTMS instance, and then to a
production deployment.

---

## 1. Prerequisites

| Requirement | Version | Notes |
|---|---|---|
| Visual Studio 2022 | 17.8+ | Workload: **ASP.NET and web development** |
| .NET SDK | 8.0 | Included with the VS workload |
| SQL Server | 2016+ | LocalDB, Express, Developer or Standard |
| SQL Server Management Studio | 19+ | Optional, for inspecting data |
| Git | any | Optional |

Verify the SDK:

```bash
dotnet --list-sdks
# 8.0.x [C:\Program Files\dotnet\sdk]
```

---

## 2. Open and Restore

1. Double-click **`HTMS.sln`**.
2. Visual Studio restores NuGet packages on first build. To do it manually:

   ```bash
   dotnet restore
   ```

   Packages used: `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Design`,
   `Microsoft.EntityFrameworkCore.Tools`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore`,
   `QuestPDF`, `ClosedXML`.

---

## 3. Configure the Database Connection

Edit `src/HabitTracker.Web/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=HabitTrackerDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

Common alternatives:

```text
SQL Server Express   Server=.\SQLEXPRESS;Database=HabitTrackerDb;Trusted_Connection=True;TrustServerCertificate=True
SQL Server (SQL auth) Server=localhost;Database=HabitTrackerDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True
Named instance        Server=MYPC\INSTANCE;Database=HabitTrackerDb;Trusted_Connection=True;TrustServerCertificate=True
```

> `TrustServerCertificate=True` is required for LocalDB and for servers using a
> self-signed certificate. Remove it in production once a valid certificate is installed.

---

## 4. Create the Database

### Option A — Package Manager Console (recommended in Visual Studio)

```powershell
Update-Database -Project HabitTracker.Web
```

### Option B — .NET CLI

```bash
dotnet ef database update --project src/HabitTracker.Web
```

If `dotnet ef` is not installed:

```bash
dotnet tool install --global dotnet-ef --version 8.0.11
```

### Option C — SQL script (no EF tooling required)

Run `database/HTMS_Schema_SqlServer.sql` in SSMS against an empty database.
The script is idempotent and creates all 17 tables, indexes and foreign keys.

### Option D — Automatic

The application calls `Database.MigrateAsync()` at startup, so pressing **F5** against an
empty database creates and seeds it automatically. This is the simplest path for a first run.

---

## 5. Seed Data

On first startup `DbSeeder` creates:

- **Roles**: `Administrator`, `User`
- **Administrator**: `admin` / `Admin@12345` (`admin@htms.local`)
- **Demo user**: `demo` / `Demo@12345` with sample habits, logs, streaks, achievements
  and reward transactions
- **Categories**: Health, Fitness, Learning, Productivity, Mindfulness, Finance, Social, Creativity
- **Achievements**: 12 definitions across all criteria types
- **System settings**: site name, points per completion, streak thresholds, registration
  toggle, maintenance mode, page size

Control this from `appsettings.json`:

```json
"SeedAdmin": {
  "UserName": "admin",
  "Email": "admin@htms.local",
  "Password": "Admin@12345"
},
"SeedDemoData": true
```

> Set `SeedDemoData` to `false` for a clean production database.
> **Change the administrator password immediately after the first sign-in.**

---

## 6. Run the Application

Press **F5** in Visual Studio, or:

```bash
dotnet run --project src/HabitTracker.Web
```

The console prints the listening URLs, e.g. `https://localhost:7001` and `http://localhost:5001`.
Sign in with the administrator account and you land on the dashboard.

---

## 7. First-Run Checklist

1. Sign in as `admin` / `Admin@12345`.
2. Open **Profile → Change password** and set a strong password.
3. Open **Admin → Settings** and set the site name and points policy.
4. Open **Admin → Categories** and adjust the category list to your needs.
5. Open **Admin → Achievements** and tune the criteria thresholds.
6. Register a normal user (or use `demo`) and confirm the user experience.

---

## 8. Production Deployment

### 8.1 Publish

**Visual Studio**: right-click `HabitTracker.Web` → **Publish** → choose *Folder*,
*IIS / FTP*, or *Azure App Service*.

**.NET CLI**:

```bash
dotnet publish src/HabitTracker.Web -c Release -o ./publish
```

### 8.2 Hosting configuration

Set these environment variables on the server (double underscore = nested key):

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Server=...;Database=HabitTrackerDb;User Id=...;Password=...;
SeedAdmin__Password=<a strong password>
SeedDemoData=false
```

### 8.3 IIS

1. Install the **ASP.NET Core 8 Hosting Bundle** on the server.
2. Create an application pool with **No Managed Code**.
3. Point the site at the publish folder.
4. Grant the app pool identity read access to the folder.
5. Grant the identity `db_datareader` and `db_datawriter` on the database
   (add `db_ddladmin` only if the app should migrate on startup).

### 8.4 Database migration in production

Preferred: run migrations as a deployment step, not at startup.

```bash
dotnet ef migrations script --idempotent --project src/HabitTracker.Web --output migrate.sql
```

Then execute `migrate.sql` against the production database.

### 8.5 Post-deployment hardening

- Enforce HTTPS and HSTS (already enabled in `Program.cs` for Production).
- Remove or rotate the seeded administrator password.
- Set `SeedDemoData=false`.
- Restrict the `/Admin` area to trusted accounts.
- Enable database backups.

---

## 9. Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| `A network-related or instance-specific error` | SQL Server not reachable | Check the instance name and that the service is running |
| `Cannot open database "HabitTrackerDb"` | Database missing | Run `Update-Database` or the SQL script |
| `The certificate chain was issued by an authority that is not trusted` | Self-signed cert | Add `TrustServerCertificate=True` to the connection string |
| `Login failed for user` | Wrong credentials / no permissions | Verify the SQL login and grant `db_datareader` / `db_datawriter` |
| `dotnet ef` not recognised | EF tool not installed | `dotnet tool install --global dotnet-ef --version 8.0.11` |
| Icons render as empty boxes | Bootstrap Icons font path | Ensure `wwwroot/lib/bootstrap-icons/fonts/` contains the `.woff2` and `.woff` files |
| Charts do not render | Chart.js missing | Ensure `wwwroot/lib/chartjs/chart.umd.min.js` exists |
| `Invalid object name 'AspNetUsers'` | Migrations not applied | Run `Update-Database` |
| Port already in use | Another process on the port | Change the port in `Properties/launchSettings.json` |

---

## 10. Useful Commands

```bash
# Build
dotnet build

# Run
dotnet run --project src/HabitTracker.Web

# Add a migration after changing entities
dotnet ef migrations add <Name> --project src/HabitTracker.Web

# Apply migrations
dotnet ef database update --project src/HabitTracker.Web

# Generate an idempotent SQL script
dotnet ef migrations script --idempotent --project src/HabitTracker.Web --output database/HTMS_Schema_SqlServer.sql

# Drop the database (development only)
dotnet ef database drop --project src/HabitTracker.Web
```
