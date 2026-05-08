namespace AfterSchool.Models;

public class DirectConversation
{
    public int    Id              { get; set; }
    public int    User1Id         { get; set; }
    public int    User2Id         { get; set; }
    public string CreatedDate     { get; set; } = "";
    public string LastMessageDate { get; set; } = "";
}

public class DirectConversationView
{
    public int    Id                 { get; set; }
    public int    OtherUserId        { get; set; }
    public string OtherUserFullName  { get; set; } = "";
    public string OtherUserUsername  { get; set; } = "";
    public string OtherUserRole      { get; set; } = "";
    public string LastMessageDate    { get; set; } = "";
    public string LastPreview        { get; set; } = "";

    public string DisplayName => string.IsNullOrWhiteSpace(OtherUserFullName)
        ? OtherUserUsername
        : OtherUserFullName;
}
