# AfterSchool Management System - Rebuild Specification

This document provides instructions for rebuilding the AfterSchool Administrative System from scratch.

## Project Description

A comprehensive Management System designed for **Administrative Staff and Teachers**. It serves as the central hub for managing the afterschool's daily operations, including student records, course creation, and weekly scheduling.

## Technology Stack

- **Framework**: .NET 8.0 (Windows Forms)
- **Language**: C# 12
- **Database**: SQLite (using `Microsoft.Data.Sqlite` or `Dapper`)
- **JSON**: `System.Text.Json`
- **Excel Export**: `ClosedXML`

## Portability & Pathing Rules (CRITICAL)

1. **Relative Database Path**: Always use `AppDomain.CurrentDomain.BaseDirectory` to locate the SQLite file.
2. **Self-Healing Database**: App must automatically create `afterschool.db` and tables on first launch.
3. **Export Paths**: Default exports to the user's "Downloads" or "Documents" folder.

## Database Schema (SQLite)

### Table: Courses (Administrative)

- `Id` (INTEGER, PK)
- `Name` (TEXT) - e.g., "Chess", "English Level 1"
- `Teacher` (TEXT)
- `Description` (TEXT)
- `Capacity` (INTEGER)

### Table: Schedule (Administrative)

- `Id` (INTEGER, PK)
- `CourseId` (INTEGER, FK)
- `DayOfWeek` (TEXT)
- `StartTime` (TEXT)
- `EndTime` (TEXT)
- `Room` (TEXT)

### Table: Students

- `Id` (INTEGER, PK)
- `FirstName`, `LastName`, `Address`, `Email`, `BirthDate`, `ContactNo`, `Gender`, `RegisterDate`, `Status`
- `EnrolledCourseId` (INTEGER, FK)

## Core Features for Staff

1. **Course & Curriculum Management**
   - UI for staff to **Create, Update, and Delete courses**.
   - Manage the list of available programs offered by the afterschool.

2. **Weekly Scheduling System**
   - A management interface to set and update the **Weekly Schedule**.
   - Assign days and times to specific courses.

3. **Student Administration**
   - Full CRUD for Student records.
   - Enroll/Transfer students between courses.

4. **Reporting & Analytics**
   - Generate "Class Lists" (which students are in which course).
   - Export student/course data to Excel for physical record-keeping.

## Application style

The application needs to look modern and professional.

## Implementation Instructions for Claude Agent

### Phase 1: Infrastructure

1. Setup `DatabaseHelper.cs` with the 3 tables above (Courses, Schedule, Students).
2. Implement relational integrity (Foreign Keys) between Students and Courses.

### Phase 2: Administrative UI

1. **Course Manager**: A form for staff to build the course catalog.
2. **Schedule Planner**: A weekly grid view where staff can assign time slots.
3. **Enrollment Center**: The primary student management form.

## How to add translations to a new feature

Step 1 — pick a key following the naming convention:
nav.<page> sidebar label
nav.<page>.title top-bar title
common.<action> shared across many screens
<screen>.<element> screen-specific, e.g. "attendance.btn.mark"

Step 2 — add to both JSON files
// en.json
"attendance.btn.mark": "Mark Attendance"

// ro.json
"attendance.btn.mark": "Marchează prezența"

Step 3 — use in code
var btn = new Button { Text = Loc.T("attendance.btn.mark") };

That's it. Because MainForm.OnLanguageChanged() calls PerformClick() on the active nav button, any UserControl is destroyed
and recreated when the language switches — so every Loc.T() call in a constructor or BuildLayout() method automatically picks
up the new language with zero extra work.

The EN/RO toggle button appears in the top-right corner of the main window. The choice is saved to settings.json and restored
on the next launch.

### Git workflow

1. Make sure you are in the 'dev' branch.
2. After every successful feature implementation, make a commit to the dev branch.

### Progress logging

1. Keep logs of project progress and update it as you develop.
2. For the logging the PROGRESS.md file will be used, create it if not exists.

## Build & Run Commands

- Build: `dotnet build`
- Run: `dotnet run`
