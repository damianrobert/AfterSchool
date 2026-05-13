# AfterSchool Management System — Feature Ideas

This document collects ideas for expanding the system beyond its current state.
Current coverage: infrastructure, course manager, schedule planner (calendar), enrollment center,
reports/Excel export, authentication, dashboard, text-to-speech, EN/RO i18n, grades & transcripts.

---

## High-Value Next Steps

### 1. Attendance Tracking
Mark daily attendance per course session. Each record ties a student, a schedule slot, and a date
to a status: Present / Absent / Late / Excused.

- Attendance sheet view: one row per student, columns = days of the month.
- Quick-mark toolbar: "Mark all present" then flip individual cells.
- Summary stats: attendance rate per student and per course.
- Alerts when a student falls below a configurable threshold (e.g. < 75%).
- Excel export: monthly attendance sheet with color-coded cells.
- Translations: `attendance.*` key group.

### 2. Fee & Payment Tracking
Record tuition payments per student per month, track outstanding balances.

- `Payments` table: `StudentId`, `Amount`, `PaidDate`, `Month`, `Notes`, `RecordedBy`.
- Payment status on the Enrollment Center grid: green = paid, yellow = partial, red = overdue.
- Monthly fee configuration per course (store in `Courses.MonthlyFee`).
- "Overdue" filter to quickly find unpaid students.
- Excel export: payment history and balance summary.

### 3. Student Progress Report (PDF)
Combine grades, attendance, and notes into a single printable PDF per student.

- Requires a PDF library such as `QuestPDF` or `PdfSharp`.
- Sections: student info header, grade table (all courses), attendance summary, teacher notes.
- Available from both the Grades screen (Transcript button) and the Enrollment Center.
- Bilingual: generate in EN or RO based on active locale.

### 4. Bulk Import from Excel / CSV
Let staff paste a spreadsheet of new students rather than entering them one-by-one.

- `OpenFileDialog` filtered to `.xlsx` and `.csv`.
- Preview grid showing parsed rows before committing.
- Column-mapping step (handle variant column names).
- Duplicate detection by first + last name + birth date.
- Error rows highlighted with a reason; valid rows imported in a transaction.

### 5. Role-Based Access Control
The `Users` table already has a `role` column. Enforce it in the UI.

- **Administrator**: full access including user management.
- **Staff**: CRUD on students, courses, schedule, grades, attendance.
- **Teacher**: read-only on courses and schedule; can enter grades and attendance for their own courses only.
- Hide or disable nav items and action buttons based on `Session.CurrentUser.Role`.
- A **User Management** screen (admin-only): list users, create/deactivate accounts, change roles.

---

## Operational Improvements

### 6. Waitlist Management
When a course reaches `Capacity`, new enrollment requests go onto a waitlist.

- `Waitlist` table: `StudentId`, `CourseId`, `RequestedAt`.
- Enrollment Center shows a "Waitlisted" status badge.
- When a student is removed from a course, the next person on the waitlist is highlighted for promotion.

### 7. Teacher Profiles
Replace the free-text `Teacher` column in `Courses` with a proper foreign key to a `Teachers` table.

- `Teachers` table: `Id`, `FirstName`, `LastName`, `Email`, `Phone`, `Specialization`.
- Teacher picker in `CourseEditorDialog` (searchable ComboBox).
- New **Teachers** nav screen: list, add, edit, deactivate.
- Dashboard stat card: teacher count already present — link it to the Teachers screen.

### 8. Room / Resource Management
Replace the free-text `Room` column with a managed list of rooms.

- `Rooms` table: `Id`, `Name`, `Capacity`, `Notes`.
- Room picker in `ScheduleEditorDialog`.
- Conflict detection: warn if a room is already booked for the same slot.

### 9. Notifications & Reminders
In-app notification center for time-sensitive alerts.

- Overdue payments, low-attendance students, courses at capacity.
- Bell icon in the top bar with an unread badge count.
- Notification log stored in a `Notifications` table (shown and then marked read).

### 10. Event / Holiday Calendar
Track school-wide events and public holidays so the schedule planner can show them.

- `Events` table: `Date`, `Title`, `Type` (Holiday / Event / Exam).
- Calendar view highlights event days with a banner.
- "No class" indicator on holiday slots.

---

## Reporting Enhancements

### 11. Print Support
Add a **Print** button alongside **Export** on the Reports and Grades screens.

- Use `PrintDocument` + `PrintPreviewDialog` (already in .NET WinForms).
- Render class lists, schedule, and grade sheets to printer pages.
- No extra library required.

### 12. Dashboard Drill-Downs
Make the dashboard stat cards clickable to jump to filtered views.

- "Active students" card → Enrollment Center pre-filtered to Active.
- "Today's classes" card → Schedule Planner scrolled to today.
- "Top courses" bar chart → Course Manager filtered to the clicked course.

### 13. Monthly & Yearly Summary Reports
Aggregate data for a chosen period.

- Enrollment trend: new students per month (bar chart).
- Revenue summary (if fee tracking is added).
- Grade distribution across all courses.
- Export to a multi-sheet Excel workbook.

### 14. Course Completion Certificates
Generate a simple certificate document for students who finish a course.

- Template with student name, course name, teacher name, completion date, and a signature line.
- Generate as PDF (QuestPDF) or as a formatted Word document (OpenXml).
- Trigger from the Grades screen for students with a passing grade.

---

## UX / Interface

### 15. Dark Mode
Add a dark theme alongside the current light theme.

- `Theme` class already centralises all colors — add a `Theme.IsDark` flag and a second palette.
- Toggle button in the top bar (sun/moon icon) or in a Settings screen.
- Persist choice in `settings.json`.

### 16. Student Photo / Avatar
Store a profile photo for each student.

- Add `PhotoPath` (TEXT) to the Students table.
- Small avatar circle in the Enrollment Center grid and in the editor dialog.
- Fallback initials avatar when no photo is set.

### 17. Keyboard Shortcuts & Quick Search
Power-user features for staff who use the app all day.

- Global `Ctrl+F` focuses a search bar regardless of active screen.
- `Ctrl+N` opens the "Add" dialog for the active screen.
- Status bar tooltip listing available shortcuts on each screen.

### 18. Undo / Activity Log
Track every create / update / delete action with user, timestamp, and old value.

- `AuditLog` table: `UserId`, `Action`, `EntityType`, `EntityId`, `Detail`, `At`.
- **Audit Log** screen (admin-only): filterable by user, entity type, date range.
- Optional: "Undo last action" for accidental deletes (restore from log).

---

## Infrastructure & Reliability

### 19. Database Backup & Restore
Let staff back up the database without needing external tools.

- **Backup**: copy `afterschool.db` to a user-chosen folder with a timestamp suffix.
- **Restore**: open a backup file, validate it (check table names), replace the live DB after confirmation.
- Optional: automatic backup on every app launch, keeping the last 7 copies.

### 20. Settings Screen
Central place for configuration currently scattered across code.

- Default export folder.
- Automatic backup toggle and retention count.
- Language preference (already in `settings.json`, expose in UI).
- App version and "About" section.

### 21. CSV Export (Lightweight Alternative to Excel)
Some users just need plain CSV for imports into other systems.

- One-click CSV export from the Enrollment Center and Course Manager.
- No extra library — `StringBuilder` + `StreamWriter` is sufficient.

### 22. Multi-Language Expansion
The i18n framework is already in place. Add a third language with minimal effort.

- Candidate: French (many West-African afterschool contexts use French alongside English).
- Add `fr.json`, extend the language toggle to a three-way cycle or a dropdown.
