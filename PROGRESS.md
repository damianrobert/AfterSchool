# AfterSchool Management System - Progress Log

## 2026-04-23

### Phase 1: Infrastructure — complete
- Added NuGet packages: Microsoft.Data.Sqlite 8.0.0, Dapper 2.1.72, ClosedXML 0.105.0.
- Organized project into folders: `Data/`, `Models/`, `Forms/`, `Services/`, `UI/`.
- Models: `Course`, `Schedule` (+ `ScheduleView`), `Student` (+ `StudentView`).
- `DatabaseHelper` — self-heals `afterschool.db` next to the binary
  (`AppDomain.CurrentDomain.BaseDirectory`), creates all 3 tables plus
  indexes, enables foreign keys. `ON DELETE CASCADE` from Schedule→Courses,
  `ON DELETE SET NULL` from Students→Courses.
- Repositories using Dapper: `CourseRepository`, `ScheduleRepository`,
  `StudentRepository`.

### Phase 2: Administrative UI — in progress
- Replaced scaffolded `Form1` with `MainForm` (modern sidebar-nav shell).
- Added `Theme` class (colors, fonts, grid/button/textbox styles) for a
  consistent modern professional look.
- **Course Manager** — done. Grid view, search, create/edit/delete via
  modal editor, shows live enrolled-student count.
- **Schedule Planner** — stubbed, up next.
- **Enrollment Center** — stubbed, up next.
- **Reports & Export** — stubbed, Phase 3.

### Build status
- `dotnet build`: succeeds with 0 warnings, 0 errors.
