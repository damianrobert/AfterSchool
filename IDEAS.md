# AfterSchool Management System — Development Ideas

A brainstorm of features and improvements to make the app more complex, complete, and modern.

---

## 1. Attendance Tracking

**What:** A daily/session-based register where teachers or staff mark each student as Present, Absent, or Late for each scheduled slot.

**Why it matters:** Currently the app manages who is *enrolled* but not who actually *shows up*. Attendance data enables a whole new layer of analytics.

**Ideas:**
- Attendance sheet per schedule slot (auto-populated from enrolled students)
- Bulk mark all as present, then flag exceptions
- Attendance percentage per student shown in the Enrollment Center grid
- Alert when a student's attendance drops below a threshold (e.g. below 70%)
- Weekly/monthly attendance summary report exported to Excel

---

## 2. Grades & Performance Tracking

**What:** Allow teachers to record grades or evaluation scores for students per course.

**Ideas:**
- Configurable grading scale per course (numeric 1–10, letter A–F, Pass/Fail)
- Grade entry dialog accessible from the Enrollment Center
- Student transcript view — all courses + grades in one page
- Class average and ranking per course
- Grade trend chart per student over time
- Export grade sheets to Excel

---

## 3. Charts & Visual Analytics (embedded graphs)

**What:** Replace the plain stat cards on the Dashboard with actual charts using a charting library (e.g. `LiveChartsCore.SkiaSharpView.WinForms` or `ScottPlot`).

**Ideas:**
- Enrollment trend line chart (registrations per month over time)
- Course popularity bar chart (enrolled count per course)
- Attendance rate pie/donut chart per course
- Student status breakdown (Active / Inactive / Graduated) donut chart
- Daily class load bar chart (how many classes per day of the week)

---

## 4. Parent / Guardian Management

**What:** Each student can have one or more parent/guardian contacts linked to them.

**Ideas:**
- Guardians table: name, relationship, phone, email
- Link multiple guardians per student
- "Contact guardian" button that pre-fills a mailto: link
- Show guardian info in the student editor
- Include guardian contacts in the class-list Excel export

---

## 5. Fee & Payment Tracking

**What:** Track enrollment fees and payments per student.

**Ideas:**
- Fee amount configurable per course
- Payment records: date, amount, method (cash / transfer / card)
- Outstanding balance shown in the Enrollment Center grid
- Overdue payment alert (highlight rows with unpaid balance)
- Invoice/receipt generation exported to PDF or Excel
- Monthly revenue summary report

---

## 6. Teacher Profiles & Workload

**What:** Expand teacher records beyond just a username — full profiles with contact info, specialisation, and schedule load.

**Ideas:**
- Teachers table linked to the Users table
- Profile page: bio, subject areas, contact details
- Workload view: how many hours/slots per week each teacher is assigned
- Max-hours constraint with a warning when exceeded
- Substitute teacher assignment for a specific slot
- Teacher availability calendar (days/times they can teach)

---

## 7. Waitlist Management

**What:** When a course reaches capacity, students can be placed on a waitlist and automatically notified when a spot opens.

**Ideas:**
- Waitlist table: student, course, position, date added
- Auto-promote first waitlisted student when another student is transferred out
- Waitlist view in the Enrollment Center filter options
- "Promote from waitlist" button in the transfer dialog

---

## 8. Notifications & Announcements (in-app)

**What:** An in-app notification centre for administrative alerts and an announcements board for general notices.

**Ideas:**
- Notifications panel (bell icon in the top bar with badge count)
- Auto-generated notifications: low attendance alert, course at capacity, upcoming exam
- Announcements CRUD: create a notice with title, body, and expiry date
- Announcements visible on the Dashboard in a scrollable card
- Mark as read / dismiss

---

## 9. Document Attachments

**What:** Attach files (PDFs, images) directly to student records — e.g. consent forms, medical notes, ID copies.

**Ideas:**
- Store file path (or binary blob) in a new `StudentDocuments` table
- Upload button in the student editor opens a file picker
- Attachments list with open/remove actions
- Warning if a required document is missing (configurable per course)

---

## 10. Student Photo / Avatar

**What:** Store a profile photo per student, displayed in the editor and class lists.

**Ideas:**
- Photo stored as a path (relative to app data folder)
- Circular crop preview in the student editor
- Thumbnail column in the Enrollment Center grid
- Photo included in the printed class list

---

## 11. PDF Export

**What:** Export reports directly to PDF in addition to Excel — useful for printing or sharing without Excel installed.

**Ideas:**
- Use `QuestPDF` (MIT licence) for layout-rich PDFs
- PDF class lists with school header, student photos (optional), and signature lines
- PDF schedule grid (the weekly timetable as a printable page)
- PDF student transcript

---

## 12. Role-Based Access Control (RBAC)

**What:** The app already has Admin and Teacher roles but doesn't restrict UI access based on role.

**Ideas:**
- Teacher role sees only their assigned courses and those students
- Admin-only sections: user management, financial data, system settings
- Read-only mode for a "Viewer" role
- Audit log: every insert/update/delete records who did it and when
- User management screen (admin can create/deactivate accounts, change roles)

---

## 13. Global Search

**What:** A single search box (Ctrl+F or top-bar) that searches across students, courses, teachers, and schedule slots simultaneously.

**Ideas:**
- Results grouped by category (Students / Courses / Schedule)
- Click a result to navigate directly to that record
- Recent searches history

---

## 14. Drag-and-Drop Schedule Builder

**What:** Replace the current "Add slot" dialog with a drag-and-drop grid where you can drag a course card onto a day/time cell.

**Ideas:**
- Visual weekly grid with time rows (07:00 – 20:00)
- Course palette on the left; drag onto the grid to create a slot
- Drag existing slot to a new day/time to move it
- Colour-coded by course
- Conflict highlight (room double-booking shown in red)

---

## 15. Backup & Restore

**What:** One-click database backup and restore so administrators can protect data.

**Ideas:**
- "Backup now" button in a Settings screen: copies `afterschool.db` to a timestamped zip in Documents
- Restore from backup: file picker, confirmation dialog, app restart
- Automatic daily backup on launch (keep last 7 copies)
- Backup reminder if no backup in the last 7 days (shown on Dashboard)

---

## 16. Settings Screen

**What:** A dedicated settings panel for configurable app behaviour.

**Ideas:**
- School name and logo (shown in exports and print headers)
- Default course capacity
- Slot duration override (currently hard-coded to 90 minutes)
- Attendance threshold for low-attendance alerts
- Backup location and schedule
- Theme toggle (Light / Dark mode)

---

## 17. Dark Mode

**What:** A full dark theme switchable from Settings or a toggle button in the top bar.

**Ideas:**
- Add a `DarkTheme` colour set to `Theme.cs`
- Persist preference to a JSON settings file next to the DB
- Smooth transition (re-apply theme on all open controls)

---

## 18. Keyboard Shortcuts

**What:** Power-user keyboard shortcuts for common actions.

**Ideas:**
- Ctrl+N — New student / new course / new slot (context-aware)
- Ctrl+E — Edit selected row
- Delete — Delete selected row (with confirmation)
- Ctrl+F — Focus search box
- Ctrl+P — Print / export current view
- F5 — Refresh current view

---

## 19. Audit Log

**What:** A searchable log of every data change: who changed what and when.

**Ideas:**
- `AuditLog` table: timestamp, user, action (Insert/Update/Delete), entity, old value (JSON), new value (JSON)
- Audit log viewer screen with date-range filter and entity filter
- Highlight recent changes on record open ("Last edited by X on Y")

---

## 20. Multi-language / Localisation

**What:** Support Romanian (the local language) alongside English.

**Ideas:**
- Resource file approach (`Strings.resx`, `Strings.ro.resx`)
- Language toggle in Settings (takes effect on next launch)
- Romanian locale for date formatting and number formatting
- Export column headers in the selected language
