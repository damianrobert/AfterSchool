using AfterSchool.Models;
using Dapper;

namespace AfterSchool.Data;

public static class GradeRepository
{
    /// <summary>
    /// Returns all enrolled students for a course, LEFT JOINed with their grade.
    /// Rows with Score == "" have no grade recorded yet.
    /// </summary>
    public static IEnumerable<GradeView> GetByCourse(int courseId)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.Query<GradeView>(@"
            SELECT
                COALESCE(g.Id, 0)             AS Id,
                s.Id                           AS StudentId,
                s.FirstName                    AS StudentFirstName,
                s.LastName                     AS StudentLastName,
                c.Id                           AS CourseId,
                c.Name                         AS CourseName,
                c.GradingScale                 AS GradingScale,
                COALESCE(g.Score,      '')     AS Score,
                COALESCE(g.Notes,      '')     AS Notes,
                COALESCE(g.GradedDate, '')     AS GradedDate,
                COALESCE(g.GradedBy,   '')     AS GradedBy
            FROM Students s
            CROSS JOIN Courses c
            LEFT JOIN Grades g ON g.StudentId = s.Id AND g.CourseId = c.Id
            WHERE EXISTS (SELECT 1 FROM StudentCourses sc WHERE sc.StudentId = s.Id AND sc.CourseId = c.Id)
              AND c.Id = @CourseId
            ORDER BY s.LastName, s.FirstName",
            new { CourseId = courseId }).ToList();
    }

    public static IEnumerable<GradeView> GetByStudent(int studentId)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.Query<GradeView>(@"
            SELECT g.Id, g.StudentId,
                   s.FirstName AS StudentFirstName, s.LastName AS StudentLastName,
                   g.CourseId, c.Name AS CourseName, c.GradingScale,
                   g.Score, g.Notes, g.GradedDate, g.GradedBy
            FROM Grades g
            JOIN Students s ON s.Id = g.StudentId
            JOIN Courses c  ON c.Id = g.CourseId
            WHERE g.StudentId = @StudentId
            ORDER BY g.GradedDate DESC, c.Name",
            new { StudentId = studentId }).ToList();
    }

    public static void Upsert(Grade grade)
    {
        using var conn = DatabaseHelper.CreateConnection();
        conn.Execute(@"
            INSERT INTO Grades (StudentId, CourseId, Score, Notes, GradedDate, GradedBy)
            VALUES (@StudentId, @CourseId, @Score, @Notes, @GradedDate, @GradedBy)
            ON CONFLICT(StudentId, CourseId) DO UPDATE SET
                Score      = excluded.Score,
                Notes      = excluded.Notes,
                GradedDate = excluded.GradedDate,
                GradedBy   = excluded.GradedBy;",
            grade);
    }

    public static void Delete(int studentId, int courseId)
    {
        using var conn = DatabaseHelper.CreateConnection();
        conn.Execute(
            "DELETE FROM Grades WHERE StudentId = @StudentId AND CourseId = @CourseId",
            new { StudentId = studentId, CourseId = courseId });
    }
}
