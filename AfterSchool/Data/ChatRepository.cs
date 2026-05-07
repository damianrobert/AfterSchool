using AfterSchool.Models;
using Dapper;

namespace AfterSchool.Data;

public static class ChatRepository
{
    public static IEnumerable<ChatConversation> GetForUser(int userId)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.Query<ChatConversation>(
            "SELECT * FROM ChatConversations WHERE UserId = @userId ORDER BY LastMessageDate DESC",
            new { userId }).ToList();
    }

    public static ChatConversation? GetById(int id)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.QueryFirstOrDefault<ChatConversation>(
            "SELECT * FROM ChatConversations WHERE Id = @id", new { id });
    }

    public static int InsertConversation(ChatConversation c)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.ExecuteScalar<int>(@"
            INSERT INTO ChatConversations (UserId, Title, CreatedDate, LastMessageDate)
            VALUES (@UserId, @Title, @CreatedDate, @LastMessageDate);
            SELECT last_insert_rowid();", c);
    }

    public static void UpdateTitle(int id, string title)
    {
        using var conn = DatabaseHelper.CreateConnection();
        conn.Execute("UPDATE ChatConversations SET Title = @title WHERE Id = @id", new { id, title });
    }

    public static void TouchConversation(int id, string date)
    {
        using var conn = DatabaseHelper.CreateConnection();
        conn.Execute("UPDATE ChatConversations SET LastMessageDate = @date WHERE Id = @id", new { id, date });
    }

    public static void DeleteConversation(int id)
    {
        using var conn = DatabaseHelper.CreateConnection();
        conn.Execute("DELETE FROM ChatConversations WHERE Id = @id", new { id });
    }

    public static IEnumerable<ChatMessage> GetMessages(int conversationId)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.Query<ChatMessage>(
            "SELECT * FROM ChatMessages WHERE ConversationId = @conversationId ORDER BY Id",
            new { conversationId }).ToList();
    }

    public static void InsertMessage(ChatMessage m)
    {
        using var conn = DatabaseHelper.CreateConnection();
        conn.Execute(@"
            INSERT INTO ChatMessages (ConversationId, Role, Content, SentDate)
            VALUES (@ConversationId, @Role, @Content, @SentDate)", m);
    }
}
