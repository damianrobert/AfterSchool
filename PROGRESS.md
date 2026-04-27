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

### Phase 6: Text-to-Speech module (2026-04-27)
- Added `System.Speech` 8.0.0 NuGet package.
- **`TextToSpeechControl`** — full TTS panel wired as a "Text to Speech" nav
  item in MainForm:
  - Multiline text editor with placeholder text; accepts Return key.
  - **Play** (`SpeakAsync`) — async so the UI stays responsive.
  - **Pause** / **Resume** — via `SpeechSynthesizer.Pause()` / `.Resume()`.
  - **Stop** — resumes a paused synth first, then `SpeakAsyncCancelAll()`.
  - **Load from File** — `OpenFileDialog` filtered to `.txt`; loads with
    `File.ReadAllText`; try/catch for locked or missing files.
  - **Save to WAV** — `SaveFileDialog`; redirects output with
    `SetOutputToWaveFile`, synthesizes synchronously, then restores the
    default audio device; output is always restored even on error.
  - **Speed slider** (TrackBar, −10 to +10) — live-updates `synth.Rate`.
  - **Volume slider** (TrackBar, 0 to 100) — live-updates `synth.Volume`.
  - Status label reflects current state (Idle / Speaking… / Paused) with
    color feedback using `Theme.Primary` and `Theme.TextSecondary`.
  - Button enable/disable state enforces valid action sequences.
  - `Dispose` stops speech and cleans up the `SpeechSynthesizer` when the
    tab is switched or the app closes.
  - `SpeakCompleted` handler guards against post-dispose invocation with
    `IsDisposed` / `IsHandleCreated` checks before `BeginInvoke`.

### Phase 7: Full EN/RO Language Support (2026-04-27)
- Added `Loc.Culture` property (`CultureInfo`) so date formatting respects the
  active language (e.g. `DateTime.Today.ToString("dddd, MMMM d, yyyy", Loc.Culture)`
  yields Romanian weekday/month names when RO is active).
- Expanded `en.json` and `ro.json` from 38 keys to ~180 keys, covering every
  screen: Dashboard, Schedule Planner, Enrollment Center, Course Manager,
  Reports, Text-to-Speech, Login, and Signup.
- Key groups added: `days.*`, `dashboard.*`, `schedule.*`, `rooms.*`,
  `enrollment.*`, `course.*`, `reports.*`, `tts.*`, `login.*`, `signup.*`.
- Updated all 8 form files (`DashboardControl`, `SchedulePlannerControl`,
  `EnrollmentControl`, `CourseManagerControl`, `ReportsControl`,
  `TextToSpeechControl`, `LoginForm`, `SignupForm`) to call `Loc.T("key")`
  or `string.Format(Loc.T("key"), args)` instead of hardcoded strings.
- `SchedulePlannerControl`: introduced private `DayItem(Value, Label)` record
  so the day dropdown shows translated labels (Luni/Marți/…) while storing
  the English value used for DB queries, preserving data integrity.
- Role and status values stored in the database (Staff/Teacher/Administrator,
  Active/Inactive/Graduated) are intentionally left untranslated to avoid
  corrupting existing records.
- Because `MainForm.OnLanguageChanged()` already destroys and recreates the
  active `UserControl`, all translated strings are automatically picked up
  in every control's constructor with zero extra wiring.

### Build
- `dotnet build`: 0 warnings, 0 errors.
