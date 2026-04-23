using AfterSchool.Models;

namespace AfterSchool.UI;

public static class Session
{
    public static User? Current { get; set; }

    public static bool SignOutRequested { get; set; }

    public static string DisplayName =>
        Current == null
            ? ""
            : string.IsNullOrWhiteSpace(Current.FullName) ? Current.Username : Current.FullName;
}
