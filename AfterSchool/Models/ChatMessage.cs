namespace AfterSchool.Models;

public class ChatMessage
{
    public int    Id             { get; set; }
    public int    ConversationId { get; set; }
    public string Role           { get; set; } = "user"; // "user" | "model"
    public string Content        { get; set; } = "";
    public string SentDate       { get; set; } = "";
}
