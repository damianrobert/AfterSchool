using AfterSchool.Models;
using Dapper;

namespace AfterSchool.Data;

public static class NotificationRepository
{
    public static void Insert(Notification n)
    {
        using var conn = DatabaseHelper.CreateConnection();
        conn.Execute(@"
            INSERT INTO Notifications (UserId, Type, Message, IsRead, CreatedDate)
            VALUES (@UserId, @Type, @Message, 0, @CreatedDate)", n);
    }

    public static IEnumerable<Notification> GetForUser(int userId, int limit = 30)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.Query<Notification>(
            "SELECT * FROM Notifications WHERE UserId = @UserId ORDER BY CreatedDate DESC LIMIT @Limit",
            new { UserId = userId, Limit = limit }).ToList();
    }

    public static int GetUnreadCount(int userId)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM Notifications WHERE UserId = @UserId AND IsRead = 0",
            new { UserId = userId });
    }

    public static void MarkRead(int notificationId)
    {
        using var conn = DatabaseHelper.CreateConnection();
        conn.Execute("UPDATE Notifications SET IsRead = 1 WHERE Id = @Id", new { Id = notificationId });
    }

    public static void MarkAllRead(int userId)
    {
        using var conn = DatabaseHelper.CreateConnection();
        conn.Execute("UPDATE Notifications SET IsRead = 1 WHERE UserId = @UserId", new { UserId = userId });
    }
}
