using AfterSchool.Data;
using AfterSchool.UI;

namespace AfterSchool.Services;

public static class AuditService
{
    public static void Log(string action, string entityType, string entityId = "", string details = "")
    {
        var user     = Session.Current;
        var userId   = user?.Id ?? 0;
        var userName = user != null
            ? (string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName)
            : "System";

        AuditLogRepository.Insert(userId, userName, action, entityType, entityId, details);
    }
}
