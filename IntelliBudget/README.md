# IntelliBudget

IntelliBudget is an editable Windows Forms solution for Visual Studio 2022 and .NET Framework 4.8. It uses the existing SQL Server Express database, a dark theme, and LKR.

## Run it

1. Extract the entire ZIP into a regular folder. In Visual Studio Installer, install the **.NET desktop development** workload and the **.NET Framework 4.8 targeting pack** if needed.
2. Make sure SQL Server Express is running and that you can open `IntelliBudgetDB` from SQL Server Management Studio using Windows Authentication. The default instance in `App.config` is `localhost\SQLEXPRESS`. Edit that setting if your SQL Server instance has a different name. The Windows account running the app needs permission to read and write its data.
3. Open `IntelliBudget.sln` in Visual Studio 2022, select **Build > Build Solution**, and press **F5**. Use **Create account**, then sign in. Creating an account writes one row to the existing Users table.
4. Choose a month and save planned income. Add fixed expenses, savings goals, and allocations. Enter actual money received or spent under Transactions. Use the same category names to compare spending with the plan.
5. Open Reports for the chart and comparison table. **Export CSV** saves this month's transaction rows; **Export PDF** saves a printable monthly summary and category comparisons. The PDF supports English/ASCII text; the UTF-8 CSV preserves Unicode category names and notes.

The app does not run migrations, create tables, or insert sample data. It expects the six existing `dbo` tables from the project with identity keys, unique usernames, and one budget per user per month (stored as the first day). It expects `Fixed` / `Percentage` goals and `Income` / `Expense` transactions. `Users.PasswordHash` must fit the versioned PBKDF2 record (the original NVARCHAR(255) definition does). Existing accounts created by a different password system are not automatically migrated.

## How the planner works

- **Reserve savings** (default): subtract fixed expenses and reserved savings from income. A percentage goal is calculated from monthly planned income.
- **Track only**: calculate each goal and show it on the Savings goals screen without deducting it from available spending. Switch between the two modes in Settings. The preference is stored per app user in the current Windows profile, and applies to all months on this computer.
- **Remaining** is the available amount minus allocations. The app blocks changes that would overcommit the plan, and allows changes that improve an already overcommitted plan (for instance after switching savings modes).
- Spending alerts appear at 90% of a category's planned total, when spending exceeds that total, and when spending has no plan. Planned category totals include both fixed expenses and allocations.
- Deleting a monthly plan explicitly removes its fixed expenses, goals and allocations. Actual transactions remain.

## Editing the solution

The layout is written in editable C# using standard Windows Forms controls. Edit the source methods to move controls, change colors, or customize navigation; the layouts are not maintained as separate drag-and-drop designer files.

- `Program.cs` — app startup, account screens, and shared theme.
- `DashboardForm.cs` — navigation, planned income, fixed expenses, goals, allocations, transactions, and settings.
- `EditorForm.cs` — the add/edit dialog shared by expenses, goals, allocations, and transactions.
- `Reports.cs` — dashboard cards, alerts, monthly chart, and export writers.
- `Store.cs` — parameterized database commands, atomic plan edits, password hashing, and budget math.

## Smoke test on a copy of the database

Use a separate database copy and point App.config to it if you want to avoid writing test records to your real budget. Register two users and verify they cannot see each other's rows. For the first user, plan LKR 100,000, add LKR 30,000 in fixed costs and a 20% savings goal. Available spending should be LKR 50,000. Add a Food allocation of LKR 15,000, then record LKR 14,000 in Food spending; the dashboard should show a warning. Try an allocation that exceeds the remaining amount and confirm it is rejected. Export both formats, switch months, and verify the old records only appear when returning to their month.

