# AfterSchool Management System - Progress Log

## 2026-04-23 — All three phases complete

### Phase 1: Infrastructure
- Packages: `Microsoft.Data.Sqlite` 8.0.0, `Dapper` 2.1.72, `ClosedXML` 0.105.0.
- Layout: `Data/` (DB helper + repositories), `Models/`, `Forms/`, `Services/`, `UI/`.
- **`DatabaseHelper`** — self-heals `afterschool.db` next to the binary
  (`AppDomain.CurrentDomain.BaseDirectory`), creates tables + indexes on first
  launch, enables foreign keys. `ON DELETE CASCADE` Schedule→Courses,
  `ON DELETE SET NULL` Students→Courses.
- Dapper-based repositories: `CourseRepository`, `ScheduleRepository`,
  `StudentRepository` (plus enriched `*View` query models).
- `.gitignore` added; IDE/build artifacts untracked.

### Phase 2: Administrative UI
- Replaced scaffolded `Form1` with modern `MainForm` shell (sidebar nav,
  top title bar, swappable content controls).
- `Theme` class providing a consistent professional look (deep-navy sidebar,
  Tailwind-style blues, white cards, subtle borders, Segoe UI Semibold).
- **Course Manager** — grid + search, CRUD via modal editor, live enrolled-
  student count per course.
- **Schedule Planner** — weekly grid with 6-day columns, color-coded slot
  cards showing time / course / teacher / room, add/edit/delete flows.
- **Enrollment Center** — student grid with search + course filter, full
  editor dialog (all fields), dedicated transfer dialog.

### Phase 3: Reporting & Analytics
- `ExcelExportService` (ClosedXML) writing to `~/Downloads` (falls back to
  `~/Documents`) with timestamped filenames.
- Exports: all students, all courses (with enrolled counts), class lists
  (overview + one sheet per course), schedule, and single class list.
- **Reports tab** — one-click exports plus a live class-list preview
  (per-course grid with summary + "open file" prompt after export).

### Phase 4: Authentication
- New `Users` table (unique username NOCASE, password hash, full name,
  role, created date, active flag). Created by `DatabaseHelper.Initialize`.
- `PasswordHasher` using PBKDF2-SHA256 (100k iterations, 16-byte salt,
  32-byte hash) stored as `iterations.salt.hash` base64 triplet.
  Constant-time comparison on verify.
- `UserRepository` — count, exists (case-insensitive), get by username,
  insert, authenticate.
- `Session` static holder for current user + sign-out flag.
- **LoginForm** — two-panel hero + form layout, auto-opens signup when
  no users exist yet, link to switch to signup.
- **SignupForm** — first-run variant prompts for the initial admin,
  validates username length, password length, confirm match, and
  username uniqueness.
- `Program.cs` runs an auth loop: login → `Application.Run(MainForm)` →
  if sign-out requested, loop back to login; otherwise exit.
- MainForm sidebar footer shows the signed-in user with a "Sign out"
  button; top bar shows "Signed in as …".

### Phase 5: Dashboard & Bug Fixes (2026-04-27)
- **Dashboard** — landing screen with four stat cards (total students / active
  count, course count / teacher count, today's scheduled classes, classrooms),
  a "Today's Schedule" card listing time slots for the current weekday, a "Top
  Courses by Enrollment" bar-chart card, and a "Recent Registrations" grid
  showing the latest 8 students. Wired into MainForm as the first nav item so
  it opens on launch.
- **Bug fix** — `StudentEditorDialog` and `TransferDialog` course ComboBoxes
  crashed with `ArgumentOutOfRangeException` (SelectedIndex=0 on empty list)
  because `DataSource`/`DisplayMember`/`ValueMember` binding with a nullable
  `int?` ValueMember can silently clear items. Fixed by replacing DataSource
  binding with plain `Items.Add` calls.
- **UI fix** — `Theme.StyleButton` now sets `AutoSize = true` with
  `MinimumSize`/`MaximumSize` locking height at 36 px, so button text is
  never clipped regardless of label length.

### Build
- `dotnet build`: 0 warnings, 0 errors.
