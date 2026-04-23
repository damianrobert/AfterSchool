using AfterSchool.Models;
using Dapper;

namespace AfterSchool.Data;

public static class StudentRepository
{
    private const string ViewQuery = @"
        SELECT s.Id, s.FirstName, s.LastName, s.Address, s.Email, s.BirthDate,
               s.ContactNo, s.Gender, s.RegisterDate, s.Status, s.EnrolledCourseId,
               c.Name AS CourseName
        FROM Students s
        LEFT JOIN Courses c ON c.Id = s.EnrolledCourseId";

    public static IEnumerable<StudentView> GetAll()
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.Query<StudentView>(ViewQuery + " ORDER BY s.LastName, s.FirstName").ToList();
    }

    public static IEnumerable<StudentView> GetByCourse(int courseId)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.Query<StudentView>(
            ViewQuery + " WHERE s.EnrolledCourseId = @Id ORDER BY s.LastName, s.FirstName",
            new { Id = courseId }).ToList();
    }

    public static Student? GetById(int id)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.QueryFirstOrDefault<Student>(
            "SELECT * FROM Students WHERE Id = @Id", new { Id = id });
    }

    public static int Insert(Student student)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.ExecuteScalar<int>(@"
            INSERT INTO Students
                (FirstName, LastName, Address, Email, BirthDate, ContactNo,
                 Gender, RegisterDate, Status, EnrolledCourseId)
            VALUES
                (@FirstName, @LastName, @Address, @Email, @BirthDate, @ContactNo,
                 @Gender, @RegisterDate, @Status, @EnrolledCourseId);
            SELECT last_insert_rowid();", student);
    }

    public static void Update(Student student)
    {
        using var conn = DatabaseHelper.CreateConnection();
        conn.Execute(@"
            UPDATE Students SET
                FirstName = @FirstName, LastName = @LastName, Address = @Address,
                Email = @Email, BirthDate = @BirthDate, ContactNo = @ContactNo,
                Gender = @Gender, RegisterDate = @RegisterDate, Status = @Status,
                EnrolledCourseId = @EnrolledCourseId
            WHERE Id = @Id", student);
    }

    public static void Delete(int id)
    {
        using var conn = DatabaseHelper.CreateConnection();
        conn.Execute("DELETE FROM Students WHERE Id = @Id", new { Id = id });
    }

    public static void Transfer(int studentId, int? newCourseId)
    {
        using var conn = DatabaseHelper.CreateConnection();
        conn.Execute(
            "UPDATE Students SET EnrolledCourseId = @CourseId WHERE Id = @Id",
            new { Id = studentId, CourseId = newCourseId });
    }
}
