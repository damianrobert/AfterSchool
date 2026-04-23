using AfterSchool.Models;
using Dapper;

namespace AfterSchool.Data;

public static class ScheduleRepository
{
    private const string ViewQuery = @"
        SELECT s.Id, s.CourseId, c.Name AS CourseName, c.Teacher,
               s.DayOfWeek, s.StartTime, s.EndTime, s.Room
        FROM Schedule s
        INNER JOIN Courses c ON c.Id = s.CourseId";

    public static IEnumerable<ScheduleView> GetAll()
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.Query<ScheduleView>(ViewQuery + " ORDER BY s.DayOfWeek, s.StartTime").ToList();
    }

    public static IEnumerable<ScheduleView> GetForDay(string dayOfWeek)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.Query<ScheduleView>(
            ViewQuery + " WHERE s.DayOfWeek = @Day ORDER BY s.StartTime",
            new { Day = dayOfWeek }).ToList();
    }

    public static IEnumerable<Schedule> GetForCourse(int courseId)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.Query<Schedule>(
            "SELECT * FROM Schedule WHERE CourseId = @Id ORDER BY DayOfWeek, StartTime",
            new { Id = courseId }).ToList();
    }

    public static int Insert(Schedule slot)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.ExecuteScalar<int>(@"
            INSERT INTO Schedule (CourseId, DayOfWeek, StartTime, EndTime, Room)
            VALUES (@CourseId, @DayOfWeek, @StartTime, @EndTime, @Room);
            SELECT last_insert_rowid();", slot);
    }

    public static void Update(Schedule slot)
    {
        using var conn = DatabaseHelper.CreateConnection();
        conn.Execute(@"
            UPDATE Schedule
            SET CourseId = @CourseId, DayOfWeek = @DayOfWeek,
                StartTime = @StartTime, EndTime = @EndTime, Room = @Room
            WHERE Id = @Id", slot);
    }

    public static void Delete(int id)
    {
        using var conn = DatabaseHelper.CreateConnection();
        conn.Execute("DELETE FROM Schedule WHERE Id = @Id", new { Id = id });
    }
}
