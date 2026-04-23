using AfterSchool.Models;
using Dapper;

namespace AfterSchool.Data;

public static class RoomRepository
{
    private static readonly string[] Defaults =
    {
        "Room 101", "Room 102", "Room 103",
        "Room 201", "Room 202", "Room 203",
        "Hall A", "Hall B", "Lab 1", "Lab 2"
    };

    public static IEnumerable<Room> GetAll()
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.Query<Room>("SELECT * FROM Rooms ORDER BY Name").ToList();
    }

    public static int Insert(string name)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.ExecuteScalar<int>(@"
            INSERT INTO Rooms (Name) VALUES (@Name);
            SELECT last_insert_rowid();", new { Name = name });
    }

    public static void Delete(int id)
    {
        using var conn = DatabaseHelper.CreateConnection();
        conn.Execute("DELETE FROM Rooms WHERE Id = @Id", new { Id = id });
    }

    public static bool Exists(string name)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM Rooms WHERE Name = @Name COLLATE NOCASE",
            new { Name = name }) > 0;
    }

    public static int UsageCount(int roomId)
    {
        using var conn = DatabaseHelper.CreateConnection();
        var name = conn.ExecuteScalar<string?>(
            "SELECT Name FROM Rooms WHERE Id = @Id", new { Id = roomId });
        if (string.IsNullOrEmpty(name)) return 0;
        return conn.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM Schedule WHERE Room = @Name COLLATE NOCASE",
            new { Name = name });
    }

    public static void SeedIfEmpty()
    {
        using var conn = DatabaseHelper.CreateConnection();
        var count = conn.ExecuteScalar<long>("SELECT COUNT(*) FROM Rooms");
        if (count > 0) return;
        foreach (var name in Defaults)
            conn.Execute("INSERT INTO Rooms (Name) VALUES (@Name)", new { Name = name });
    }
}
