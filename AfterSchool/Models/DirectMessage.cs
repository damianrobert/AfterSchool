namespace AfterSchool.Models;

public class DirectMessage
{
    public int    Id             { get; set; }
    public int    ConversationId { get; set; }
    public int    SenderId       { get; set; }
    public string Content        { get; set; } = "";
    public string SentDate       { get; set; } = "";
}

public class DirectMessageView : DirectMessage
{
    public string SenderFullName { get; set; } = "";
    public string SenderUsername { get; set; } = "";
    public bool   IsFromMe       { get; set; }
    public List<DirectMessageAttachment> Attachments { get; set; } = new();

    public string SenderDisplayName => string.IsNullOrWhiteSpace(SenderFullName)
        ? SenderUsername
        : SenderFullName;
}
