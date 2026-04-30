namespace AfterSchool.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "Staff";
    public string CreatedDate { get; set; } = string.Empty;
    public int IsActive { get; set; } = 1;
    public int MustChangePassword { get; set; } = 0;
    public int? StudentId { get; set; }
}
