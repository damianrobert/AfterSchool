# AfterSchool Management System

A modern, professional desktop application for managing daily operations in an after-school facility. Built with .NET 8 and Windows Forms, it provides a central hub for student records, course management, scheduling, and reporting.

## 🚀 Features

- **Authentication & Security**: Secure login and signup with PBKDF2-SHA256 password hashing.
- **Student Administration**: Full CRUD operations for student records, enrollment, and course transfers.
- **Course & Curriculum Management**: Create and manage the catalog of courses, teachers, and capacities.
- **Weekly Schedule Planner**: A visual weekly grid to organize course slots, rooms, and timings.
- **Reporting & Analytics**: Export detailed student lists, class rosters, and schedules to Excel using `ClosedXML`.
- **Modern UI**: Professional aesthetic with a dark-themed sidebar, responsive layout, and interactive elements.
- **Self-Healing Database**: Automatic SQLite database initialization and schema creation on first launch.

## 🛠️ Technology Stack

- **Framework**: .NET 8.0 (Windows Forms)
- **Language**: C# 12
- **Database**: SQLite
- **Data Access**: Dapper (Micro-ORM)
- **Excel Processing**: ClosedXML
- **Styling**: Modern "Tailwind-inspired" custom UI components

## 📁 Project Structure

- `AfterSchool/Data/`: Database helper and Dapper repositories.
- `AfterSchool/Models/`: Core entity models and query view models.
- `AfterSchool/Forms/`: WinForms UI controls and main application windows.
- `AfterSchool/Services/`: Business logic, such as Excel export services.
- `AfterSchool/UI/`: Global theme definitions and session management.

## 🏁 Getting Started

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows OS (required for WinForms)

### Installation & Running

1. Clone the repository.
2. Open a terminal in the project root.
3. Build the project:
   ```bash
   dotnet build
   ```
4. Run the application:
   ```bash
   dotnet run --project AfterSchool
   ```

> **Note**: On the first run, if no users exist, the application will automatically prompt you to create an administrative account.

## 📊 Database Schema

The system uses a relational SQLite database (`afterschool.db`) with the following core entities:
- **Users**: Admin accounts with hashed credentials.
- **Students**: Comprehensive personal and enrollment data.
- **Courses**: Course metadata, teacher assignments, and capacity tracking.
- **Schedule**: Time-slot assignments linking courses to specific days, times, and rooms.

## 📄 License

This project is developed for administrative use within the AfterSchool organization.
