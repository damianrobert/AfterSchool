using AfterSchool.Models;
using Dapper;

namespace AfterSchool.Data;

public static class StudentRepository
{
    private const string ViewQuery = @"
        SELECT s.Id, s.FirstName, s.LastName, s.Address, s.Email, s.BirthDate,
               s.ContactNo, s.Gender, s.RegisterDate, s.Status,
               GROUP_CONCAT(c.Name, ', ') AS CourseName,
               GROUP_CONCAT(CAST(sc.CourseId AS TEXT), ',') AS CourseIdList
        FROM Students s
        LEFT JOIN StudentCourses sc ON sc.StudentId = s.Id
        LEFT JOIN Courses c ON c.Id = sc.CourseId";

    public static IEnumerable<StudentView> GetAll()
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.Query<StudentView>(
            ViewQuery + " GROUP BY s.Id ORDER BY s.LastName, s.FirstName").ToList();
    }

    public static IEnumerable<StudentView> GetByCourse(int courseId)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.Query<StudentView>(@"
            SELECT s.Id, s.FirstName, s.LastName, s.Address, s.Email, s.BirthDate,
                   s.ContactNo, s.Gender, s.RegisterDate, s.Status,
                   GROUP_CONCAT(c2.Name, ', ') AS CourseName,
                   GROUP_CONCAT(CAST(sc2.CourseId AS TEXT), ',') AS CourseIdList
            FROM Students s
            JOIN StudentCourses filter_sc ON filter_sc.StudentId = s.Id AND filter_sc.CourseId = @CourseId
            LEFT JOIN StudentCourses sc2 ON sc2.StudentId = s.Id
            LEFT JOIN Courses c2 ON c2.Id = sc2.CourseId
            GROUP BY s.Id
            ORDER BY s.LastName, s.FirstName",
            new { CourseId = courseId }).ToList();
    }

    public static Student? GetById(int id)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.QueryFirstOrDefault<Student>(
            "SELECT * FROM Students WHERE Id = @Id", new { Id = id });
    }

    public static List<int> GetEnrolledCourseIds(int studentId)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.Query<int>(
            "SELECT CourseId FROM StudentCourses WHERE StudentId = @Id ORDER BY CourseId",
            new { Id = studentId }).ToList();
    }

    public static int Insert(Student student)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.ExecuteScalar<int>(@"
            INSERT INTO Students
                (FirstName, LastName, Address, Email, BirthDate, ContactNo,
                 Gender, RegisterDate, Status)
            VALUES
                (@FirstName, @LastName, @Address, @Email, @BirthDate, @ContactNo,
                 @Gender, @RegisterDate, @Status);
            SELECT last_insert_rowid();", student);
    }

    public static void Update(Student student)
    {
        using var conn = DatabaseHelper.CreateConnection();
        conn.Execute(@"
            UPDATE Students SET
                FirstName = @FirstName, LastName = @LastName, Address = @Address,
                Email = @Email, BirthDate = @BirthDate, ContactNo = @ContactNo,
                Gender = @Gender, RegisterDate = @RegisterDate, Status = @Status
            WHERE Id = @Id", student);
    }

    public static void Delete(int id)
    {
        using var conn = DatabaseHelper.CreateConnection();
        conn.Execute("DELETE FROM Students WHERE Id = @Id", new { Id = id });
    }

    public static void SetEnrollments(int studentId, IEnumerable<int> courseIds)
    {
        using var conn = DatabaseHelper.CreateConnection();
        conn.Execute("DELETE FROM StudentCourses WHERE StudentId = @Id", new { Id = studentId });
        foreach (var courseId in courseIds)
            conn.Execute(
                "INSERT OR IGNORE INTO StudentCourses (StudentId, CourseId) VALUES (@StudentId, @CourseId)",
                new { StudentId = studentId, CourseId = courseId });
    }
}
