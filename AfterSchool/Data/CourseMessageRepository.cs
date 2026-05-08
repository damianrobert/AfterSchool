using AfterSchool.Models;
using Dapper;

namespace AfterSchool.Data;

public static class CourseMessageRepository
{
    public static List<CourseMessageView> GetMessages(int courseId, int currentUserId)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.Query<CourseMessageView>(@"
            SELECT cm.Id, cm.CourseId, cm.SenderId, cm.Content, cm.SentDate,
                   u.FullName AS SenderFullName, u.Role AS SenderRole,
                   CASE WHEN cm.SenderId = @UserId THEN 1 ELSE 0 END AS IsFromMe
            FROM CourseMessages cm
            JOIN Users u ON u.Id = cm.SenderId
            WHERE cm.CourseId = @CourseId
            ORDER BY cm.SentDate",
            new { CourseId = courseId, UserId = currentUserId }).ToList();
    }

    public static void SendMessage(int courseId, int senderId, string content, string senderName, string courseName)
    {
        using var conn = DatabaseHelper.CreateConnection();
        conn.Execute(@"
            INSERT INTO CourseMessages (CourseId, SenderId, Content, SentDate)
            VALUES (@CourseId, @SenderId, @Content, @SentDate)",
            new { CourseId = courseId, SenderId = senderId, Content = content,
                  SentDate = DateTime.UtcNow.ToString("O") });

        NotifyMembers(conn, courseId, senderId, senderName, courseName);
    }

    public static List<Course> GetCoursesForStudent(int studentId)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.Query<Course>(@"
            SELECT c.* FROM Courses c
            JOIN StudentCourses sc ON sc.CourseId = c.Id
            WHERE sc.StudentId = @StudentId
            ORDER BY c.Name",
            new { StudentId = studentId }).ToList();
    }

    public static Dictionary<int, int> GetLastReadMsgIds(int userId)
    {
        using var conn = DatabaseHelper.CreateConnection();
        var rows = conn.Query(
            "SELECT CourseId, LastMsgId FROM CourseLastRead WHERE UserId = @UserId",
            new { UserId = userId });
        return rows.ToDictionary(r => (int)r.CourseId, r => (int)r.LastMsgId);
    }

    public static void MarkCourseRead(int courseId, int userId, int lastMsgId)
    {
        using var conn = DatabaseHelper.CreateConnection();
        conn.Execute(@"
            INSERT INTO CourseLastRead (CourseId, UserId, LastMsgId)
            VALUES (@CourseId, @UserId, @LastMsgId)
            ON CONFLICT(CourseId, UserId) DO UPDATE SET LastMsgId = @LastMsgId",
            new { CourseId = courseId, UserId = userId, LastMsgId = lastMsgId });
    }

    public static Dictionary<int, int> GetLastMessageIds(IEnumerable<int> courseIds)
    {
        var ids = courseIds.ToList();
        if (ids.Count == 0) return new();
        using var conn = DatabaseHelper.CreateConnection();
        var rows = conn.Query(
            $"SELECT CourseId, MAX(Id) AS LastId FROM CourseMessages WHERE CourseId IN ({string.Join(",", ids)}) GROUP BY CourseId");
        return rows.ToDictionary(r => (int)r.CourseId, r => (int)r.LastId);
    }

    public static List<Course> GetCoursesForTeacher(string teacherName)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.Query<Course>(
            "SELECT * FROM Courses WHERE Teacher = @Teacher ORDER BY Name",
            new { Teacher = teacherName }).ToList();
    }

    private static void NotifyMembers(
        System.Data.IDbConnection conn, int courseId, int senderId,
        string senderName, string courseName)
    {
        // Collect all user IDs for enrolled students
        var studentUserIds = conn.Query<int>(@"
            SELECT u.Id FROM Users u
            JOIN Students s ON s.Id = u.StudentId
            JOIN StudentCourses sc ON sc.StudentId = s.Id
            WHERE sc.CourseId = @CourseId AND u.IsActive = 1",
            new { CourseId = courseId }).ToList();

        // Also find the teacher user by matching FullName to Course.Teacher
        var teacherName = conn.ExecuteScalar<string>(
            "SELECT Teacher FROM Courses WHERE Id = @Id", new { Id = courseId }) ?? "";
        if (!string.IsNullOrWhiteSpace(teacherName))
        {
            var teacherId = conn.ExecuteScalar<int?>(
                "SELECT Id FROM Users WHERE FullName = @Name AND IsActive = 1 LIMIT 1",
                new { Name = teacherName });
            if (teacherId.HasValue && !studentUserIds.Contains(teacherId.Value))
                studentUserIds.Add(teacherId.Value);
        }

        var now = DateTime.UtcNow.ToString("O");
        var message = $"New message in {courseName} from {senderName}";
        foreach (var uid in studentUserIds.Where(id => id != senderId))
        {
            conn.Execute(@"
                INSERT INTO Notifications (UserId, Type, Message, IsRead, CreatedDate)
                VALUES (@UserId, 'group_message', @Message, 0, @CreatedDate)",
                new { UserId = uid, Message = message, CreatedDate = now });
        }
    }
}
