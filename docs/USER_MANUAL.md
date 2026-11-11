# HTMS — User Manual

A complete guide to using the Habit Tracking Management System, for both end users and
administrators.

---

## Part 1 — Getting Started

### 1.1 Creating an account

1. Open the application and click **Create one** on the sign-in page.
2. Fill in **Full name**, **Username**, **Email**, **Password** and **Confirm password**.
3. The password must be at least 8 characters and include an uppercase letter, a lowercase
   letter, a digit and a non-alphanumeric character.
4. Click **Create account**. You are signed in automatically and land on the dashboard.

### 1.2 Signing in

1. Enter your **username or email** and **password**.
2. Optionally tick **Remember me** to stay signed in on this device.
3. Click **Sign in**.

After five consecutive failed attempts the account is locked for 15 minutes.

### 1.3 Signing out

Click your avatar in the top-right corner and choose **Sign out**.

---

## Part 2 — The Dashboard

The dashboard is your daily command centre.

| Element | What it shows |
|---|---|
| Greeting header | Your name, today's date, and a completion ring for today |
| Active habits | Number of habits currently active |
| Completed today | How many you have completed today, out of the total due |
| Best streak | Your longest current streak across all habits |
| Total points | Your current reward point balance |
| Today's habits | A checklist — complete, skip or undo each habit |
| 14-day trend | Line chart of daily completions |
| Category breakdown | Doughnut chart of completions by category |
| Recent achievements | Your latest unlocked badges |
| Recent rewards | Your latest point transactions |
| Upcoming reminders | Reminders due in the next 7 days |

**To complete a habit**: click the **Complete** button on its row. The ring, KPIs, charts
and points update immediately.

---

## Part 3 — Managing Habits

### 3.1 Creating a habit

1. Go to **Habits** in the sidebar and click **New habit**.
2. Fill in the form:

   | Field | Notes |
   |---|---|
   | Name | Required, up to 120 characters |
   | Description | Optional |
   | Category | Required — pick from the administrator's list |
   | Type | **Build** (do more), **Quit** (do less), **Measurable** (track a number) |
   | Frequency | **Daily**, **Weekly**, or **Custom** (choose weekdays) |
   | Target value / Unit | Shown for measurable habits, e.g. `30` `minutes` |
   | Start date | Required — the habit is not due before this date |
   | End date | Optional |
   | Reminder time | Optional daily reminder |
   | Priority | Low / Medium / High |
   | Points per completion | How many points a completion is worth |
   | Colour and icon | Used throughout the interface |

3. Click **Create habit**.

### 3.2 Editing, archiving and deleting

- **Edit** — open the habit and click **Edit**, change the fields, then **Save changes**.
- **Archive** — hides the habit from the daily tracker without losing its history.
  Restore it at any time from the habit list filter.
- **Delete** — permanently removes the habit and all of its logs. You are asked to confirm.

### 3.3 Finding habits

Use the search box, the category filter, the status filter and the frequency filter at the
top of the habit list. Click any column header to sort.

### 3.4 Habit details

Open a habit to see:

- Current streak, longest streak, total completions and completion rate
- A 30-day heatmap of completions
- Recent log history with notes and values
- Per-habit analytics charts

---

## Part 4 — Daily Tracking

Open **Tracker** in the sidebar.

1. Use **◀** and **▶** to move between days, or click **Today** to jump back.
2. Each habit due on that day is listed with its status.
3. Click **Complete** to mark it done, **Skip** to mark it skipped, or **Clear** to reset it.
4. For measurable habits, enter the measured value and an optional note before completing.
5. The progress bar at the top shows how much of the day is done.

> Completing a habit twice on the same day does not award points twice — the system
> updates the existing entry.

---

## Part 5 — Calendar

Open **Calendar** in the sidebar.

- Each day cell is shaded by how many habits were completed that day.
- Click a day to see its habit list and log directly from the panel.
- Use **◀** and **▶** to change month, and **Today** to return to the current month.
- The legend explains the shading levels.

---

## Part 6 — Streaks

Streaks are calculated automatically:

- **Current streak** — consecutive scheduled days completed up to today.
- **Longest streak** — the best run you have ever achieved for that habit.
- **Completion rate** — completions divided by scheduled days.

A day counts toward a streak only if the habit was **scheduled** for that day and was
**completed**. Skipped days break the streak. Streaks are recalculated every time you log
a habit, so the numbers are always current.

---

## Part 7 — Analytics

Open **Analytics** in the sidebar and choose a range: **7**, **30**, **90** or **365** days.

| Chart | Meaning |
|---|---|
| Daily completion trend | Completions per day over the range |
| Weekly totals | Completions grouped by week |
| Category distribution | Share of completions per category |
| Weekday performance | Which weekdays you perform best on |
| Habit leaderboard | Your habits ranked by completions |

KPI cards above the charts show total completions, average completion rate, your best
weekday and your most consistent habit.

---

## Part 8 — Reminders

Open **Reminders** in the sidebar.

### 8.1 Creating a reminder

1. Click **New reminder**.
2. Enter a **Title**, an optional **Message**, and choose the **Time of day**.
3. Tick the weekdays it should repeat on.
4. Optionally link it to a habit.
5. Click **Create reminder**.

### 8.2 Managing reminders

- Toggle **Enabled** to pause a reminder without deleting it.
- **Edit** to change the time, days or message.
- **Delete** to remove it permanently.

The **Due now** panel at the top lists reminders whose time has arrived today.

---

## Part 9 — Achievements

Open **Achievements** in the sidebar.

- **Unlocked** tab — badges you have earned, with the unlock date.
- **Locked** tab — badges still to earn, each with a progress bar showing how close you are.

Achievements are evaluated automatically after every habit completion. When you unlock one
you receive its bonus points and it appears on your dashboard.

Achievement criteria types:

| Type | Unlocks when |
|---|---|
| Total completions | You reach a total number of completions |
| Current streak | A habit's current streak reaches a threshold |
| Longest streak | A habit's longest streak reaches a threshold |
| Perfect days | You complete every scheduled habit on a day, N times |
| Habits created | You have created N habits |
| Points earned | Your point balance reaches a threshold |

---

## Part 10 — Rewards

Open **Rewards** in the sidebar.

- Four cards show your **current balance**, **lifetime earned**, **lifetime spent** and
  **transaction count**.
- The table lists every transaction: date, reason, the habit it came from, and the signed
  points amount (green for earned, red for spent).
- Use the **All / Earned / Spent** buttons to filter the history.
- Click **Redeem points** to record a redemption — enter the points and a reason.

Points are earned by completing habits and unlocking achievements. A redemption cannot
exceed your current balance: the server rejects it and shows how many points you actually
have, so the balance can never go negative.

---

## Part 11 — Reports

Open **Reports** in the sidebar.

1. Choose a range: **7 days**, **30 days**, **90 days**, **1 year**, or a custom
   **From** / **To** date pair.
2. Click **Apply** to regenerate the report.

The report shows summary KPIs and a per-habit breakdown. Export it as:

| Format | Contents |
|---|---|
| **PDF** | Summary KPIs, habit performance table and daily detail (QuestPDF) |
| **Excel** | Three sheets: Summary, Habit Performance, Daily Records (ClosedXML) |
| **CSV** | Raw log rows for further analysis (UTF-8 with BOM, RFC 4180 quoting) |

Exports respect the range you selected, and every export is recorded in the activity log.

---

## Part 12 — Your Profile

Click your avatar → **Profile**.

- Update your **full name**, **email**, **time zone** and **date format**.
- View your account statistics: member since, total points, best streak, habits created.
- Click **Change password** to set a new password (current password required).

---

## Part 13 — Administrator Guide

Administrators see an extra **Administration** section in the sidebar.

### 13.1 Admin dashboard

Platform-wide KPIs: total users, active users, total habits, total logs, points issued.
A 7-day activity chart, a list of the most active users, and the latest audit entries.

### 13.2 Users

| Action | How |
|---|---|
| Search | Type in the search box |
| Create | **New user** — set name, username, email, password and role |
| Edit | **Edit** — change profile fields and role |
| Lock / unlock | Toggle **Active** on the edit page |
| Reset password | **Reset password** — set a new password for the user |
| View details | **Details** — the user's habits, logs and activity |
| Delete | **Delete** — removes the user and all of their data (confirm required) |

> You cannot delete or deactivate your own administrator account.

### 13.3 Categories

Create, edit, reorder, activate/deactivate and delete habit categories. Each category has a
name, description, colour and icon. A category that still has habits cannot be deleted —
reassign those habits first.

### 13.4 Achievements

Create and edit achievement definitions: name, description, icon, colour, criteria type,
threshold, bonus points and display order. Deactivate an achievement to stop awarding it
without deleting the users who already earned it.

### 13.5 Activity log

A filterable audit trail: who did what, to which entity, from which IP, and when. Filter by
user, action or date range.

### 13.6 Settings

| Setting | Effect |
|---|---|
| Site name | Brand name shown in the header and on the landing page |
| Points per completion | Default points for newly created habits |
| Streak bonus threshold | Days required before a streak bonus applies |
| Streak bonus points | Bonus awarded at the threshold |
| Allow registration | Whether the public registration page accepts new accounts |
| Maintenance mode | Shows a maintenance notice to non-administrators |
| Items per page | Default page size for all lists |
| Max habits per user | Cap on active habits per account |

Click **Save settings** to apply. Changes take effect immediately.

---

## Part 14 — Tips and Troubleshooting

**My streak reset unexpectedly.** A streak breaks when a scheduled day is not completed.
Check the calendar for the missed day.

**I completed a habit but got no points.** Points are awarded once per habit per day.
If you had already completed it, the entry was updated rather than duplicated.

**A habit is not showing in today's tracker.** Check its start date, its frequency
(a weekly or custom habit is only due on its scheduled days), and that it is not archived.

**Charts are empty.** Charts need data — complete a few habits and they will populate.

**I forgot my password.** Ask an administrator to reset it from **Admin → Users**.

**The page looks unstyled.** Ensure `wwwroot/lib/bootstrap/`, `wwwroot/lib/bootstrap-icons/`
and `wwwroot/lib/chartjs/` are present in the published output.

---

## Part 15 — Keyboard and Accessibility

- All interactive elements are reachable with **Tab** and activatable with **Enter** or **Space**.
- Forms use explicit `<label>` elements associated with their inputs.
- Status is never conveyed by colour alone — icons and text accompany every colour cue.
- The layout is responsive from 320 px mobile up to wide desktop.
- Charts include a text summary in the surrounding cards.
