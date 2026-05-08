namespace AfterSchool.Models;

public class DirectMessageAttachment
{
    public int    Id         { get; set; }
    public int    MessageId  { get; set; }
    public string FileName   { get; set; } = "";
    public string StoredName { get; set; } = "";
    public long   FileSize   { get; set; }
}
