using Microsoft.Data.Sqlite;

namespace AfterSchool.Data;

public static class DatabaseHelper
{
    private const string DbFileName = "afterschool.db";

    public static string DatabasePath =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DbFileName);

    public static string ConnectionString =>
        new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            ForeignKeys = true
        }.ToString();

    public static SqliteConnection CreateConnection()
    {
        var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var pragma = conn.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON;";
        pragma.ExecuteNonQuery();
        return conn;
    }

    public static void Initialize()
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Users (
                Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                Username     TEXT    NOT NULL UNIQUE COLLATE NOCASE,
                PasswordHash TEXT    NOT NULL,
                FullName     TEXT    NOT NULL DEFAULT '',
                Role         TEXT    NOT NULL DEFAULT 'Staff',
                CreatedDate  TEXT    NOT NULL DEFAULT '',
                IsActive     INTEGER NOT NULL DEFAULT 1
            );

            CREATE TABLE IF NOT EXISTS Courses (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                Name        TEXT    NOT NULL,
                Teacher     TEXT    NOT NULL DEFAULT '',
                Description TEXT    NOT NULL DEFAULT '',
                Capacity    INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS Schedule (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                CourseId  INTEGER NOT NULL,
                DayOfWeek TEXT    NOT NULL,
                StartTime TEXT    NOT NULL,
                EndTime   TEXT    NOT NULL,
                Room      TEXT    NOT NULL DEFAULT '',
                FOREIGN KEY (CourseId) REFERENCES Courses(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS Students (
                Id               INTEGER PRIMARY KEY AUTOINCREMENT,
                FirstName        TEXT    NOT NULL,
                LastName         TEXT    NOT NULL,
                Address          TEXT    NOT NULL DEFAULT '',
                Email            TEXT    NOT NULL DEFAULT '',
                BirthDate        TEXT    NOT NULL DEFAULT '',
                ContactNo        TEXT    NOT NULL DEFAULT '',
                Gender           TEXT    NOT NULL DEFAULT '',
                RegisterDate     TEXT    NOT NULL DEFAULT '',
                Status           TEXT    NOT NULL DEFAULT 'Active',
                EnrolledCourseId INTEGER,
                FOREIGN KEY (EnrolledCourseId) REFERENCES Courses(Id) ON DELETE SET NULL
            );

            CREATE TABLE IF NOT EXISTS Rooms (
                Id   INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT    NOT NULL UNIQUE COLLATE NOCASE
            );

            CREATE TABLE IF NOT EXISTS Grades (
                Id         INTEGER PRIMARY KEY AUTOINCREMENT,
                StudentId  INTEGER NOT NULL,
                CourseId   INTEGER NOT NULL,
                Score      TEXT    NOT NULL DEFAULT '',
                Notes      TEXT    NOT NULL DEFAULT '',
                GradedDate TEXT    NOT NULL DEFAULT '',
                GradedBy   TEXT    NOT NULL DEFAULT '',
                UNIQUE(StudentId, CourseId),
                FOREIGN KEY (StudentId) REFERENCES Students(Id) ON DELETE CASCADE,
                FOREIGN KEY (CourseId)  REFERENCES Courses(Id)  ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS IX_Schedule_CourseId        ON Schedule(CourseId);
            CREATE INDEX IF NOT EXISTS IX_Students_EnrolledCourseId ON Students(EnrolledCourseId);
            CREATE INDEX IF NOT EXISTS IX_Grades_CourseId           ON Grades(CourseId);
            CREATE INDEX IF NOT EXISTS IX_Grades_StudentId          ON Grades(StudentId);
        ";
        cmd.ExecuteNonQuery();

        MigrateSchema(conn);
        RoomRepository.SeedIfEmpty();
    }

    private static void MigrateSchema(SqliteConnection conn)
    {
        TryAlter(conn, "ALTER TABLE Courses ADD COLUMN GradingScale TEXT NOT NULL DEFAULT 'Numeric';");
        TryAlter(conn, "ALTER TABLE Users ADD COLUMN MustChangePassword INTEGER NOT NULL DEFAULT 0;");
        TryAlter(conn, "ALTER TABLE Users ADD COLUMN StudentId INTEGER REFERENCES Students(Id) ON DELETE SET NULL;");
    }

    private static void TryAlter(SqliteConnection conn, string sql)
    {
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }
        catch { /* column already exists */ }
    }
}
