using AfterSchool.Models;
using Dapper;

namespace AfterSchool.Data;

public static class CourseRepository
{
    public static IEnumerable<Course> GetAll()
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.Query<Course>("SELECT * FROM Courses ORDER BY Name").ToList();
    }

    public static Course? GetById(int id)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.QueryFirstOrDefault<Course>(
            "SELECT * FROM Courses WHERE Id = @Id", new { Id = id });
    }

    public static int Insert(Course course)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.ExecuteScalar<int>(@"
            INSERT INTO Courses (Name, Teacher, Description, Capacity, GradingScale)
            VALUES (@Name, @Teacher, @Description, @Capacity, @GradingScale);
            SELECT last_insert_rowid();", course);
    }

    public static void Update(Course course)
    {
        using var conn = DatabaseHelper.CreateConnection();
        conn.Execute(@"
            UPDATE Courses
            SET Name = @Name, Teacher = @Teacher, Description = @Description,
                Capacity = @Capacity, GradingScale = @GradingScale
            WHERE Id = @Id", course);
    }

    public static void Delete(int id)
    {
        using var conn = DatabaseHelper.CreateConnection();
        conn.Execute("DELETE FROM Courses WHERE Id = @Id", new { Id = id });
    }

    public static int GetEnrolledCount(int courseId)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.ExecuteScalar<int>(@"
            SELECT COUNT(*) FROM StudentCourses sc
            JOIN Students s ON s.Id = sc.StudentId
            WHERE sc.CourseId = @Id AND s.Status = 'Active'",
            new { Id = courseId });
    }
}
