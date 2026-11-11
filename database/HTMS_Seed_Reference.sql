/* ============================================================================
   HTMS - Reference Seed Data (SQL Server)
   ----------------------------------------------------------------------------
   The application seeds all of this automatically on first startup via
   Data/DbSeeder.cs. This script is provided for DBAs who prefer to prepare the
   reference data by hand, or who need to re-apply it after a manual cleanup.

   IMPORTANT
   ---------
   * Run database/HTMS_Schema_SqlServer.sql FIRST to create the schema.
   * Passwords are NOT inserted here: ASP.NET Core Identity hashes them with a
     per-user random salt, so a literal hash cannot be shared. Create the
     administrator through the application (it is seeded automatically), or use
     Admin -> Users -> New user.
   * This script is idempotent: it only inserts rows that do not already exist.
   ============================================================================ */

SET NOCOUNT ON;
GO

/* ---------------------------------------------------------------------------
   1. Roles
   --------------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM [AspNetRoles] WHERE [NormalizedName] = N'ADMINISTRATOR')
    INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
    VALUES (NEWID(), N'Administrator', N'ADMINISTRATOR', NEWID());
GO

IF NOT EXISTS (SELECT 1 FROM [AspNetRoles] WHERE [NormalizedName] = N'USER')
    INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
    VALUES (NEWID(), N'User', N'USER', NEWID());
GO

/* ---------------------------------------------------------------------------
   2. Categories
   --------------------------------------------------------------------------- */
DECLARE @Categories TABLE ([Name] nvarchar(80), [Description] nvarchar(300),
                           [ColorHex] nvarchar(9), [IconClass] nvarchar(60),
                           [DisplayOrder] int);

INSERT INTO @Categories VALUES
    (N'Health',       N'Physical and mental wellbeing',        N'#DC2626', N'bi-heart-pulse',   1),
    (N'Fitness',      N'Exercise and movement',                N'#EA580C', N'bi-bicycle',       2),
    (N'Learning',     N'Study, reading and skill building',    N'#2563EB', N'bi-book',          3),
    (N'Productivity', N'Focus, planning and output',           N'#7C3AED', N'bi-lightning',     4),
    (N'Mindfulness',  N'Meditation and reflection',            N'#0891B2', N'bi-flower1',       5),
    (N'Finance',      N'Money habits and saving',              N'#059669', N'bi-piggy-bank',    6),
    (N'Social',       N'Relationships and connection',         N'#DB2777', N'bi-people',        7),
    (N'Creativity',   N'Creative practice and making',         N'#CA8A04', N'bi-palette',       8);

INSERT INTO [Categories] ([Name], [Description], [ColorHex], [IconClass],
                          [DisplayOrder], [IsActive], [IsSystem], [CreatedAt])
SELECT c.[Name], c.[Description], c.[ColorHex], c.[IconClass],
       c.[DisplayOrder], 1, 1, SYSUTCDATETIME()
FROM @Categories c
WHERE NOT EXISTS (SELECT 1 FROM [Categories] x WHERE x.[Name] = c.[Name]);
GO

/* ---------------------------------------------------------------------------
   3. Achievements
   CriteriaType: 0 TotalCompletions, 1 CurrentStreak, 2 LongestStreak,
                 3 PerfectDays, 4 HabitsCreated, 5 PointsEarned
   --------------------------------------------------------------------------- */
DECLARE @Achievements TABLE ([Name] nvarchar(120), [Description] nvarchar(400),
                             [IconClass] nvarchar(60), [ColorHex] nvarchar(9),
                             [CriteriaType] int, [Threshold] int,
                             [PointsReward] int, [DisplayOrder] int);

INSERT INTO @Achievements VALUES
    (N'First Step',        N'Complete your very first habit',                    N'bi-flag',        N'#2563EB', 0,   1,   10,  1),
    (N'Getting Started',   N'Complete 10 habits',                                N'bi-check2',      N'#0891B2', 0,  10,   25,  2),
    (N'Committed',         N'Complete 50 habits',                                N'bi-check2-all',  N'#7C3AED', 0,  50,   75,  3),
    (N'Centurion',         N'Complete 100 habits',                               N'bi-trophy',      N'#CA8A04', 0, 100,  150,  4),
    (N'Week Warrior',      N'Reach a 7-day streak',                              N'bi-fire',        N'#EA580C', 1,   7,   50,  5),
    (N'Fortnight Focus',   N'Reach a 14-day streak',                             N'bi-lightning',   N'#DC2626', 1,  14,  100,  6),
    (N'Monthly Master',    N'Reach a 30-day streak',                             N'bi-award',       N'#059669', 1,  30,  250,  7),
    (N'Unstoppable',       N'Reach a 100-day streak',                            N'bi-gem',         N'#DB2777', 2, 100,  500,  8),
    (N'Perfect Day',       N'Complete every scheduled habit in a single day',    N'bi-star',        N'#CA8A04', 3,   1,   30,  9),
    (N'Perfect Week',      N'Have 7 perfect days',                               N'bi-stars',       N'#7C3AED', 3,   7,  120, 10),
    (N'Habit Architect',   N'Create 5 habits',                                   N'bi-diagram-3',   N'#2563EB', 4,   5,   40, 11),
    (N'Point Collector',   N'Earn 1000 points',                                  N'bi-coin',        N'#059669', 5, 1000, 100, 12);

INSERT INTO [Achievements] ([Name], [Description], [IconClass], [ColorHex],
                            [CriteriaType], [Threshold], [PointsReward],
                            [DisplayOrder], [IsActive])
SELECT a.[Name], a.[Description], a.[IconClass], a.[ColorHex],
       a.[CriteriaType], a.[Threshold], a.[PointsReward],
       a.[DisplayOrder], 1
FROM @Achievements a
WHERE NOT EXISTS (SELECT 1 FROM [Achievements] x WHERE x.[Name] = a.[Name]);
GO

/* ---------------------------------------------------------------------------
   4. System settings
   --------------------------------------------------------------------------- */
DECLARE @Settings TABLE ([Key] nvarchar(80), [Value] nvarchar(500),
                         [Description] nvarchar(300), [Group] nvarchar(60));

INSERT INTO @Settings VALUES
    (N'SiteName',              N'Habit Tracking Management System', N'Brand name shown in the header',        N'General'),
    (N'PointsPerCompletion',   N'10',                                N'Default points for a new habit',        N'Rewards'),
    (N'StreakBonusThreshold',  N'7',                                 N'Days before a streak bonus applies',    N'Rewards'),
    (N'StreakBonusPoints',     N'25',                                N'Bonus points at the threshold',         N'Rewards'),
    (N'AllowRegistration',     N'true',                              N'Whether public registration is open',   N'Access'),
    (N'MaintenanceMode',       N'false',                             N'Show a maintenance notice',             N'Access'),
    (N'ItemsPerPage',          N'10',                                N'Default list page size',                N'General'),
    (N'MaxHabitsPerUser',      N'50',                                N'Habit cap per user',                    N'General');

INSERT INTO [SystemSettings] ([Key], [Value], [Description], [Group], [UpdatedAt])
SELECT s.[Key], s.[Value], s.[Description], s.[Group], SYSUTCDATETIME()
FROM @Settings s
WHERE NOT EXISTS (SELECT 1 FROM [SystemSettings] x WHERE x.[Key] = s.[Key]);
GO

/* ---------------------------------------------------------------------------
   5. Verification
   --------------------------------------------------------------------------- */
SELECT N'Roles'          AS [Entity], COUNT(*) AS [Rows] FROM [AspNetRoles]
UNION ALL SELECT N'Categories',     COUNT(*) FROM [Categories]
UNION ALL SELECT N'Achievements',   COUNT(*) FROM [Achievements]
UNION ALL SELECT N'SystemSettings', COUNT(*) FROM [SystemSettings];
GO
