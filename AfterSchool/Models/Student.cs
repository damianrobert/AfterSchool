namespace AfterSchool.Models;

public class Student
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string BirthDate { get; set; } = string.Empty;
    public string ContactNo { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string RegisterDate { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public int? EnrolledCourseId { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}

public class StudentView
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string BirthDate { get; set; } = string.Empty;
    public string ContactNo { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string RegisterDate { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? CourseName { get; set; }    // comma-separated list from GROUP_CONCAT
    public string? CourseIdList { get; set; }  // comma-separated course IDs from GROUP_CONCAT

    public List<int> GetCourseIds() =>
        CourseIdList?
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(int.Parse)
            .ToList()
        ?? new List<int>();
}
