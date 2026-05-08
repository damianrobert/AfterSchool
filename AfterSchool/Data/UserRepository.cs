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

    public static User? GetById(int id)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.QueryFirstOrDefault<User>(
            "SELECT * FROM Users WHERE Id = @Id AND IsActive = 1",
            new { Id = id });
    }

    public static int Insert(User user)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.ExecuteScalar<int>(@"
            INSERT INTO Users
                (Username, PasswordHash, FullName, Role, CreatedDate, IsActive, MustChangePassword, StudentId)
            VALUES
                (@Username, @PasswordHash, @FullName, @Role, @CreatedDate, @IsActive, @MustChangePassword, @StudentId);
            SELECT last_insert_rowid();", user);
    }

    public static void UpdatePassword(int userId, string newPasswordHash)
    {
        using var conn = DatabaseHelper.CreateConnection();
        conn.Execute(
            "UPDATE Users SET PasswordHash = @Hash, MustChangePassword = 0 WHERE Id = @Id",
            new { Hash = newPasswordHash, Id = userId });
    }

    public static bool HasStudentAccount(int studentId)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM Users WHERE StudentId = @StudentId",
            new { StudentId = studentId }) > 0;
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

    public static IEnumerable<User> Search(string term, int excludeUserId)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.Query<User>(@"
            SELECT * FROM Users
            WHERE IsActive = 1
              AND Id != @ExcludeId
              AND (FullName LIKE @Term COLLATE NOCASE OR Username LIKE @Term COLLATE NOCASE)
            ORDER BY FullName, Username
            LIMIT 30",
            new { Term = $"%{term}%", ExcludeId = excludeUserId }).ToList();
    }

    public static User? Authenticate(string username, string password)
    {
        var user = GetByUsername(username);
        if (user == null || user.IsActive == 0) return null;
        return PasswordHasher.Verify(password, user.PasswordHash) ? user : null;
    }
}
