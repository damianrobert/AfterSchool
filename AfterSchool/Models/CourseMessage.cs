namespace AfterSchool.Models;

public class CourseMessage
{
    public int    Id       { get; set; }
    public int    CourseId { get; set; }
    public int    SenderId { get; set; }
    public string Content  { get; set; } = "";
    public string SentDate { get; set; } = "";
}

public class CourseMessageView : CourseMessage
{
    public string SenderFullName { get; set; } = "";
    public string SenderRole     { get; set; } = "";
    public bool   IsFromMe       { get; set; }

    public string SenderDisplayName =>
        string.IsNullOrWhiteSpace(SenderFullName) ? SenderRole : SenderFullName;
}
