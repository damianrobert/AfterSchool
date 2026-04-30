namespace AfterSchool.Models;

public class CourseFile
{
    public int    Id           { get; set; }
    public int    CourseId     { get; set; }
    public string FileName     { get; set; } = string.Empty;  // original name shown to users
    public string StoredName   { get; set; } = string.Empty;  // UUID-based name on disk
    public long   FileSize     { get; set; }
    public string UploadedBy   { get; set; } = string.Empty;
    public string UploadedDate { get; set; } = string.Empty;
}
