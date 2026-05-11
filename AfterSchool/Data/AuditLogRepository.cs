using AfterSchool.Models;
using Dapper;

namespace AfterSchool.Data;

public static class AuditLogRepository
{
    public static void Insert(int userId, string userName, string action, string entityType, string entityId = "", string details = "")
    {
        try
        {
            using var conn = DatabaseHelper.CreateConnection();
            conn.Execute(@"
                INSERT INTO AuditLog (Timestamp, UserId, UserName, Action, EntityType, EntityId, Details)
                VALUES (@Timestamp, @UserId, @UserName, @Action, @EntityType, @EntityId, @Details)",
                new
                {
                    Timestamp  = DateTime.UtcNow.ToString("o"),
                    UserId     = userId,
                    UserName   = userName,
                    Action     = action,
                    EntityType = entityType,
                    EntityId   = entityId,
                    Details    = details
                });
        }
        catch { }
    }

    public static List<AuditLogEntry> GetFiltered(
        string? filterAction = null,
        string? filterUser   = null,
        DateTime? from       = null,
        DateTime? to         = null,
        int limit            = 1000)
    {
        var fromStr = (from ?? DateTime.Today.AddDays(-30)).ToUniversalTime().ToString("o");
        var toStr   = (to   ?? DateTime.Today.AddDays(1)).ToUniversalTime().ToString("o");

        var sql = @"
            SELECT * FROM AuditLog
            WHERE Timestamp >= @From
              AND Timestamp <= @To
              AND (@Action IS NULL OR Action = @Action)
              AND (@User   IS NULL OR UserName LIKE '%' || @User || '%')
            ORDER BY Timestamp DESC
            LIMIT @Limit";

        using var conn = DatabaseHelper.CreateConnection();
        return conn.Query<AuditLogEntry>(sql, new
        {
            From   = fromStr,
            To     = toStr,
            Action = filterAction,
            User   = filterUser,
            Limit  = limit
        }).ToList();
    }
}
