using AfterSchool.Models;
using Dapper;

namespace AfterSchool.Data;

public static class DirectMessageRepository
{
    public static string StorageDir =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "direct_chat_files");

    private static void EnsureDir() => Directory.CreateDirectory(StorageDir);

    public static IEnumerable<DirectConversationView> GetConversations(int myUserId)
    {
        using var conn = DatabaseHelper.CreateConnection();
        var rows = conn.Query<DirectConversationView>(@"
            SELECT
                dc.Id,
                CASE WHEN dc.User1Id = @MyId THEN dc.User2Id ELSE dc.User1Id END AS OtherUserId,
                CASE WHEN dc.User1Id = @MyId
                     THEN CASE WHEN u2.FullName != '' THEN u2.FullName ELSE u2.Username END
                     ELSE CASE WHEN u1.FullName != '' THEN u1.FullName ELSE u1.Username END
                END AS OtherUserFullName,
                CASE WHEN dc.User1Id = @MyId THEN u2.Username ELSE u1.Username END AS OtherUserUsername,
                CASE WHEN dc.User1Id = @MyId THEN u2.Role ELSE u1.Role END AS OtherUserRole,
                dc.LastMessageDate,
                (SELECT
                    CASE WHEN dm.Content != '' THEN dm.Content
                         WHEN (SELECT COUNT(*) FROM DirectMessageAttachments WHERE MessageId = dm.Id) > 0
                              THEN '[file]'
                         ELSE ''
                    END
                 FROM DirectMessages dm
                 WHERE dm.ConversationId = dc.Id
                 ORDER BY dm.SentDate DESC LIMIT 1) AS LastPreview
            FROM DirectConversations dc
            JOIN Users u1 ON u1.Id = dc.User1Id
            JOIN Users u2 ON u2.Id = dc.User2Id
            WHERE dc.User1Id = @MyId OR dc.User2Id = @MyId
            ORDER BY dc.LastMessageDate DESC",
            new { MyId = myUserId }).ToList();
        return rows;
    }

    public static int GetOrCreateConversation(int userA, int userB)
    {
        int u1 = Math.Min(userA, userB);
        int u2 = Math.Max(userA, userB);
        using var conn = DatabaseHelper.CreateConnection();

        var existing = conn.QueryFirstOrDefault<int?>(
            "SELECT Id FROM DirectConversations WHERE User1Id = @U1 AND User2Id = @U2",
            new { U1 = u1, U2 = u2 });
        if (existing.HasValue) return existing.Value;

        var now = DateTime.UtcNow.ToString("O");
        return conn.ExecuteScalar<int>(@"
            INSERT INTO DirectConversations (User1Id, User2Id, CreatedDate, LastMessageDate)
            VALUES (@U1, @U2, @Now, @Now);
            SELECT last_insert_rowid();",
            new { U1 = u1, U2 = u2, Now = now });
    }

    public static IEnumerable<DirectMessageView> GetMessages(int conversationId, int myUserId)
    {
        using var conn = DatabaseHelper.CreateConnection();
        var messages = conn.Query<DirectMessageView>(@"
            SELECT dm.*, u.FullName AS SenderFullName, u.Username AS SenderUsername
            FROM DirectMessages dm
            JOIN Users u ON u.Id = dm.SenderId
            WHERE dm.ConversationId = @ConvId
            ORDER BY dm.SentDate ASC",
            new { ConvId = conversationId }).ToList();

        foreach (var msg in messages)
        {
            msg.IsFromMe = msg.SenderId == myUserId;
            msg.Attachments = conn.Query<DirectMessageAttachment>(
                "SELECT * FROM DirectMessageAttachments WHERE MessageId = @MsgId ORDER BY Id",
                new { MsgId = msg.Id }).ToList();
        }
        return messages;
    }

    public static int SendMessage(int conversationId, int senderId, string content)
    {
        var now = DateTime.UtcNow.ToString("O");
        using var conn = DatabaseHelper.CreateConnection();
        var msgId = conn.ExecuteScalar<int>(@"
            INSERT INTO DirectMessages (ConversationId, SenderId, Content, SentDate)
            VALUES (@ConvId, @SenderId, @Content, @Now);
            SELECT last_insert_rowid();",
            new { ConvId = conversationId, SenderId = senderId, Content = content, Now = now });
        conn.Execute(
            "UPDATE DirectConversations SET LastMessageDate = @Now WHERE Id = @Id",
            new { Now = now, Id = conversationId });

        // Notify the recipient
        var recipientId = conn.QueryFirstOrDefault<int?>(
            "SELECT CASE WHEN User1Id = @SenderId THEN User2Id ELSE User1Id END FROM DirectConversations WHERE Id = @ConvId",
            new { SenderId = senderId, ConvId = conversationId });
        if (recipientId.HasValue)
        {
            var sender = conn.QueryFirstOrDefault<string>(
                "SELECT CASE WHEN FullName != '' THEN FullName ELSE Username END FROM Users WHERE Id = @Id",
                new { Id = senderId }) ?? "";
            Services.NotificationService.Send(recipientId.Value, "message",
                string.Format(Services.Loc.T("notifications.msg.message"), sender));
        }

        return msgId;
    }

    public static void UploadAttachment(int messageId, string sourceFilePath)
    {
        EnsureDir();
        var ext        = Path.GetExtension(sourceFilePath);
        var storedName = $"{messageId}_{Guid.NewGuid():N}{ext}";
        var destPath   = Path.Combine(StorageDir, storedName);
        File.Copy(sourceFilePath, destPath, overwrite: false);

        using var conn = DatabaseHelper.CreateConnection();
        conn.Execute(@"
            INSERT INTO DirectMessageAttachments (MessageId, FileName, StoredName, FileSize)
            VALUES (@MessageId, @FileName, @StoredName, @FileSize)",
            new
            {
                MessageId  = messageId,
                FileName   = Path.GetFileName(sourceFilePath),
                StoredName = storedName,
                FileSize   = new FileInfo(destPath).Length
            });
    }

    public static void DownloadAttachment(int attachmentId, string destPath)
    {
        using var conn = DatabaseHelper.CreateConnection();
        var att = conn.QueryFirstOrDefault<DirectMessageAttachment>(
            "SELECT * FROM DirectMessageAttachments WHERE Id = @Id",
            new { Id = attachmentId })
            ?? throw new FileNotFoundException("Attachment not found.");
        var src = Path.Combine(StorageDir, att.StoredName);
        File.Copy(src, destPath, overwrite: true);
    }

    public static void DeleteConversation(int conversationId)
    {
        using var conn = DatabaseHelper.CreateConnection();
        var storedNames = conn.Query<string>(@"
            SELECT dma.StoredName FROM DirectMessageAttachments dma
            JOIN DirectMessages dm ON dm.Id = dma.MessageId
            WHERE dm.ConversationId = @Id",
            new { Id = conversationId }).ToList();

        conn.Execute("DELETE FROM DirectConversations WHERE Id = @Id", new { Id = conversationId });

        foreach (var name in storedNames)
        {
            var path = Path.Combine(StorageDir, name);
            if (File.Exists(path)) File.Delete(path);
        }
    }

    public static Dictionary<int, string> GetLastReadDates(int userId)
    {
        using var conn = DatabaseHelper.CreateConnection();
        var rows = conn.Query(
            "SELECT ConversationId, LastReadDate FROM DirectConversationReads WHERE UserId = @UserId",
            new { UserId = userId });
        return rows.ToDictionary(r => (int)r.ConversationId, r => (string)r.LastReadDate);
    }

    public static void MarkConversationRead(int conversationId, int userId, string date)
    {
        using var conn = DatabaseHelper.CreateConnection();
        conn.Execute(@"
            INSERT INTO DirectConversationReads (ConversationId, UserId, LastReadDate)
            VALUES (@ConvId, @UserId, @Date)
            ON CONFLICT(ConversationId, UserId) DO UPDATE SET LastReadDate = @Date",
            new { ConvId = conversationId, UserId = userId, Date = date });
    }

    public static string FormatSize(long bytes) =>
        bytes < 1024           ? $"{bytes} B"
        : bytes < 1024 * 1024  ? $"{bytes / 1024.0:F1} KB"
                                : $"{bytes / (1024.0 * 1024):F1} MB";
}
