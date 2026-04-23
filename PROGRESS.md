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

### Build
- `dotnet build`: 0 warnings, 0 errors.
