using AfterSchool.Data;
using AfterSchool.Models;
using Dapper;

namespace AfterSchool.Services;

public static class NotificationService
{
    public static void Send(int userId, string type, string message)
    {
        try
        {
            NotificationRepository.Insert(new Notification
            {
                UserId = userId,
                Type = type,
                Message = message,
                CreatedDate = DateTime.UtcNow.ToString("O")
            });
        }
        catch { }
    }

    // Notify all student users enrolled in a course
    public static void NotifyEnrolledStudents(int courseId, string type, string message)
    {
        try
        {
            using var conn = DatabaseHelper.CreateConnection();
            var userIds = conn.Query<int>(@"
                SELECT u.Id FROM Users u
                JOIN StudentCourses sc ON sc.StudentId = u.StudentId
                WHERE sc.CourseId = @CourseId AND u.IsActive = 1 AND u.StudentId IS NOT NULL",
                new { CourseId = courseId }).ToList();
            foreach (var uid in userIds)
                Send(uid, type, message);
        }
        catch { }
    }

    // Notify the teacher user assigned to a course (matched by FullName)
    public static void NotifyTeacherOfCourse(int courseId, string type, string message)
    {
        try
        {
            var course = CourseRepository.GetById(courseId);
            if (course == null || string.IsNullOrWhiteSpace(course.Teacher)) return;
            var teacher = UserRepository.GetByRole("Teacher")
                .FirstOrDefault(u => string.Equals(u.FullName, course.Teacher, StringComparison.OrdinalIgnoreCase));
            if (teacher != null)
                Send(teacher.Id, type, message);
        }
        catch { }
    }

    // Notify the user account linked to a student record
    public static void NotifyStudentUser(int studentId, string type, string message)
    {
        try
        {
            using var conn = DatabaseHelper.CreateConnection();
            var userId = conn.QueryFirstOrDefault<int?>(
                "SELECT Id FROM Users WHERE StudentId = @StudentId AND IsActive = 1",
                new { StudentId = studentId });
            if (userId.HasValue)
                Send(userId.Value, type, message);
        }
        catch { }
    }
}
