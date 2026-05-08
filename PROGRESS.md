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

### Phase 8: Grades & Performance Tracking (2026-04-27)

**Database**
- New `Grades` table: `StudentId`, `CourseId`, `Score`, `Notes`, `GradedDate`, `GradedBy`.
  UNIQUE constraint on `(StudentId, CourseId)` — one grade per student per course.
  ON DELETE CASCADE from both Students and Courses.
- `GradingScale` column added to `Courses` table via `MigrateSchema()` (idempotent `ALTER TABLE`).
  Supported values: `"Numeric"` (1–10), `"Letter"` (A–F), `"PassFail"`.
- `GradeRepository` — `GetByCourse` (LEFT JOINs all enrolled students so ungraded rows appear),
  `GetByStudent`, `Upsert` (INSERT … ON CONFLICT DO UPDATE), `Delete`.

**Course Manager**
- `CourseEditorDialog` gains a **Grading scale** ComboBox (Numeric / Letter / Pass-Fail).
  Stored as English key in DB; displayed translated.

**Grades screen** (`nav.grades` → `GradesControl`)
- Course selector dropdown at top; loads all enrolled students for the selected course.
- DataGridView shows: Rank | Student | Score | Notes | Date | Graded by.
  Rank is computed as dense rank (numeric desc); Letter/PassFail shows row number.
- **Stats bar** below toolbar adapts to scale type:
  - Numeric: Graded count, Average, High, Low.
  - Letter: distribution per letter grade.
  - Pass/Fail: pass count, fail count, pass rate %.
- **Add / Edit Grade** button opens `GradeEditorDialog`:
  - Score control adapts to scale: `NumericUpDown` (1–10, step 0.5) / `ComboBox` (A–F / Pass–Fail).
  - Notes, Date (DateTimePicker), Graded-by (auto-filled from session user).
  - Upsert on save.
- **Clear Grade** removes the selected student's grade after confirmation.
- **Export Grades** → `ExcelExportService.ExportGradeSheet(course)` — grade sheet with title,
  teacher, scale info, sorted student list, average row at bottom.
- **Transcript** button opens `TranscriptDialog`:
  - Student selector; shows all grades across courses.
  - Export Transcript → `ExcelExportService.ExportTranscript(student, grades)`.

**Localisation**
- Added `nav.grades.*`, `grades.*`, `course.field.gradingscale` keys to both `en.json` and `ro.json`.

### Build
- `dotnet build`: 0 warnings, 0 errors.

---

## 2026-04-29 — Calendar View for Schedule Planner

Replaced the static 6-column card layout with a proper **weekly calendar grid**:

**Visual layout**
- Vertical time axis (07:00–21:00) on the left, one column per weekday (Mon–Sat).
- Hour lines and half-hour dashed lines drawn via `Paint` events.
- Today's column highlighted with a faint blue tint and a solid accent bar under its header.
- Day headers show the translated weekday name + actual calendar date ("28 Apr").
- Slot cards are **absolutely positioned** by time: `top = (startMin − 420) × 1.2 px`,
  `height = durationMin × 1.2 px`. Shows time, course name, and teacher/room (if card tall enough).

**Week navigation**
- "‹ Prev Week" / "Next Week ›" buttons shift the display by 7 days.
- "Today" button jumps back to the current week.
- Week label in the nav bar shows the date range ("28 Apr – 2 May 2026").
- The underlying schedule data is still day-of-week based; navigation updates the
  displayed dates without touching the DB.

**Click-to-add**
- Clicking on empty space in a day column opens `ScheduleEditorDialog` pre-filled
  with that day and the snapped (nearest 30-min) start time.
- `ScheduleEditorDialog` constructor now accepts optional `prefilledDay` and
  `prefilledStartTime` parameters.

**Translations**
- Added `schedule.cal.prev`, `schedule.cal.next`, `schedule.cal.today` to both
  `en.json` and `ro.json`.
- Updated `schedule.hint` to mention the click-to-add gesture.

**Build**: `dotnet build`: 0 warnings, 0 errors.

---

## 2026-04-29 — Bug Fix: Schedule Planner start-time ComboBox unresponsive

**Root cause** — In `ScheduleEditorDialog.BuildLayout`, the `_startTime` ComboBox
was docked with `DockStyle.Bottom` inside `startCol` (a `Fill`-docked Panel living
inside the `times` TableLayoutPanel). Without explicit `RowCount`/`RowStyles` on
the `times` TLP, the row height was indeterminate, which starved the bottom-docked
ComboBox of space and made it unclickable.

**Fix**
- Changed `_startTime.Dock` from `DockStyle.Bottom` → `DockStyle.Top`, matching
  the layout pattern used by every other control in the dialog.
- Added `RowCount = 1` and an explicit `RowStyle(SizeType.Percent, 100)` to the
  `times` TableLayoutPanel so its single row reliably fills the full 62 px height.

**Result** — Start-time dropdown is now fully selectable; end time auto-updates
correctly on selection change.

- `dotnet build`: 0 warnings, 0 errors.

---

## 2026-05-08 — In-App Notification System

### Architecture

**Database**
- New `Notifications` table: `UserId` (FK→Users), `Type`, `Message`, `IsRead`, `CreatedDate`.
  Index on `UserId` for fast unread-count queries.
- Added via `CREATE TABLE IF NOT EXISTS` in `DatabaseHelper.Initialize()` (self-healing on existing DBs).

**Data layer**
- `NotificationRepository` — `Insert`, `GetForUser(limit 30)`, `GetUnreadCount`, `MarkRead`, `MarkAllRead`.

**Service layer**
- `NotificationService` — three helpers used by trigger points:
  - `Send(userId, type, message)` — inserts one notification; never throws.
  - `NotifyEnrolledStudents(courseId, type, message)` — joins `StudentCourses` → `Users` to find all student accounts enrolled in a course.
  - `NotifyTeacherOfCourse(courseId, type, message)` — looks up the teacher user by matching `FullName` to `Course.Teacher`.
  - `NotifyStudentUser(studentId, type, message)` — finds the user account linked to a student record via `Users.StudentId`.

### UI

**Bell button + badge**
- 🔔 button added to the top bar in both `MainForm` (admin/staff) and `StudentMainForm`.
- Red badge label overlaid in the top-right corner of the bell showing unread count; hidden when count = 0.
- Clicking the bell toggles a `NotificationPanel` dropdown (click again to close).
- A `System.Windows.Forms.Timer` at 30-second interval refreshes the badge count automatically.

**NotificationPanel**
- Drop-down panel (340 px wide, max 420 px tall, scrollable) with:
  - Header: "Notifications" title + "Mark all read" button.
  - Each item: type emoji icon (💬 message / 📝 grade / 📋 assignment / 📤 submission / 🎓 enrollment / 📅 schedule), message text, relative timestamp.
  - Unread items have a blue left-border accent and bold text; clicking marks them read.

### Trigger points (when notifications fire)

| Event | Trigger location | Recipient |
|---|---|---|
| Direct message sent | `DirectMessageRepository.SendMessage` | Conversation partner |
| Grade updated | `GradeEditorDialog.Save` in `GradesControl` | Student linked to the grade |
| New assignment created | `AssignmentEditorDialog.Save` in `AssignmentsControl` | All students enrolled in that course |
| Assignment submitted | `StudentAssignmentsControl.SubmitFile` | Course teacher |
| Student enrolled | `ManageEnrollmentDialog` ok.Click in `EnrollmentControl` | Course teachers |
| Schedule slot added/updated | `ScheduleEditorDialog.Save` in `SchedulePlannerControl` | All enrolled students |

### Localisation
- Added 11 keys to both `en.json` and `ro.json`: `notifications.title`, `notifications.markallread`, `notifications.empty`, `notifications.msg.*`.

### Build
- `dotnet build`: 0 warnings, 0 errors.
