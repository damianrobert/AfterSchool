namespace AfterSchool.Models;

public class ChatConversation
{
    public int    Id              { get; set; }
    public int    UserId          { get; set; }
    public string Title           { get; set; } = "New Chat";
    public string CreatedDate     { get; set; } = "";
    public string LastMessageDate { get; set; } = "";
}
