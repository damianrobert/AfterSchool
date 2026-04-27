namespace AfterSchool.Models;

public class Course
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Teacher { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string GradingScale { get; set; } = "Numeric";

    public override string ToString() => Name;
}
