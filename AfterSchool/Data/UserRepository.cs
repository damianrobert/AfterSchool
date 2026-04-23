using AfterSchool.Models;
using Dapper;

namespace AfterSchool.Data;

public static class UserRepository
{
    public static int Count()
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.ExecuteScalar<int>("SELECT COUNT(*) FROM Users");
    }

    public static bool UsernameExists(string username)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM Users WHERE Username = @Username COLLATE NOCASE",
            new { Username = username }) > 0;
    }

    public static User? GetByUsername(string username)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.QueryFirstOrDefault<User>(
            "SELECT * FROM Users WHERE Username = @Username COLLATE NOCASE",
            new { Username = username });
    }

    public static int Insert(User user)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.ExecuteScalar<int>(@"
            INSERT INTO Users (Username, PasswordHash, FullName, Role, CreatedDate, IsActive)
            VALUES (@Username, @PasswordHash, @FullName, @Role, @CreatedDate, @IsActive);
            SELECT last_insert_rowid();", user);
    }

    public static IEnumerable<User> GetByRole(string role, bool activeOnly = true)
    {
        using var conn = DatabaseHelper.CreateConnection();
        var sql = activeOnly
            ? "SELECT * FROM Users WHERE Role = @Role COLLATE NOCASE AND IsActive = 1 ORDER BY FullName, Username"
            : "SELECT * FROM Users WHERE Role = @Role COLLATE NOCASE ORDER BY FullName, Username";
        return conn.Query<User>(sql, new { Role = role }).ToList();
    }

    public static string DisplayNameOf(User user) =>
        string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName;

    public static User? Authenticate(string username, string password)
    {
        var user = GetByUsername(username);
        if (user == null || user.IsActive == 0) return null;
        return PasswordHasher.Verify(password, user.PasswordHash) ? user : null;
    }
}
