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

            CREATE INDEX IF NOT EXISTS IX_Schedule_CourseId ON Schedule(CourseId);
            CREATE INDEX IF NOT EXISTS IX_Students_EnrolledCourseId ON Students(EnrolledCourseId);
        ";
        cmd.ExecuteNonQuery();
    }
}
