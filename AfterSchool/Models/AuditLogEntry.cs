namespace AfterSchool.Models;

public class AuditLogEntry
{
    public int    Id         { get; set; }
    public string Timestamp  { get; set; } = "";
    public int    UserId     { get; set; }
    public string UserName   { get; set; } = "";
    public string Action     { get; set; } = "";
    public string EntityType { get; set; } = "";
    public string EntityId   { get; set; } = "";
    public string Details    { get; set; } = "";
}
