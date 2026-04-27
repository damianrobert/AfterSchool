namespace AfterSchool.Models;

public class Grade
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public string Score { get; set; } = "";
    public string Notes { get; set; } = "";
    public string GradedDate { get; set; } = "";
    public string GradedBy { get; set; } = "";
}

public class GradeView
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentFirstName { get; set; } = "";
    public string StudentLastName { get; set; } = "";
    public int CourseId { get; set; }
    public string CourseName { get; set; } = "";
    public string GradingScale { get; set; } = "Numeric";
    public string Score { get; set; } = "";
    public string Notes { get; set; } = "";
    public string GradedDate { get; set; } = "";
    public string GradedBy { get; set; } = "";
}
