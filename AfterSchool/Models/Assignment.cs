namespace AfterSchool.Models;

public class Assignment
{
    public int    Id          { get; set; }
    public int    CourseId    { get; set; }
    public string Title       { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DueDate     { get; set; } = string.Empty;
    public string CreatedBy   { get; set; } = string.Empty;
    public string CreatedDate { get; set; } = string.Empty;
}

// Teacher/admin list row — includes submission count
public class AssignmentView : Assignment
{
    public int SubmissionCount { get; set; }
}

// Student list row — includes their own submission info
public class AssignmentStudentView : Assignment
{
    public int?    SubmissionId   { get; set; }
    public string? SubmissionFile { get; set; }
    public string? SubmittedDate  { get; set; }
    public string? StoredName     { get; set; }
    public long?   FileSize       { get; set; }

    public bool IsSubmitted => SubmissionId is > 0;
    public bool IsOverdue   => !IsSubmitted
                               && DateTime.TryParse(DueDate, out var d)
                               && d < DateTime.Today;
}

// Submission row used in the teacher's submissions dialog
public class AssignmentSubmissionView
{
    public int    Id            { get; set; }
    public int    AssignmentId  { get; set; }
    public int    StudentId     { get; set; }
    public string StudentName   { get; set; } = string.Empty;
    public string FileName      { get; set; } = string.Empty;
    public string StoredName    { get; set; } = string.Empty;
    public long   FileSize      { get; set; }
    public string SubmittedDate { get; set; } = string.Empty;
}
