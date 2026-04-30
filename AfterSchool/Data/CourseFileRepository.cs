using AfterSchool.Models;
using Dapper;

namespace AfterSchool.Data;

public static class CourseFileRepository
{
    public static string StorageDir =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "course_files");

    private static void EnsureDir() => Directory.CreateDirectory(StorageDir);

    public static IEnumerable<CourseFile> GetByCourse(int courseId)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.Query<CourseFile>(
            "SELECT * FROM CourseFiles WHERE CourseId = @CourseId ORDER BY UploadedDate DESC, FileName",
            new { CourseId = courseId }).ToList();
    }

    public static CourseFile? GetById(int id)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.QueryFirstOrDefault<CourseFile>(
            "SELECT * FROM CourseFiles WHERE Id = @Id", new { Id = id });
    }

    // Copies sourceFilePath into storage and inserts a DB record. Returns new Id.
    public static int Upload(int courseId, string sourceFilePath, string uploadedBy)
    {
        EnsureDir();

        var originalName = Path.GetFileName(sourceFilePath);
        var ext          = Path.GetExtension(sourceFilePath);
        var storedName   = $"{courseId}_{Guid.NewGuid():N}{ext}";
        var destPath     = Path.Combine(StorageDir, storedName);

        File.Copy(sourceFilePath, destPath, overwrite: false);

        var file = new CourseFile
        {
            CourseId     = courseId,
            FileName     = originalName,
            StoredName   = storedName,
            FileSize     = new FileInfo(destPath).Length,
            UploadedBy   = uploadedBy,
            UploadedDate = DateTime.Today.ToString("yyyy-MM-dd")
        };

        using var conn = DatabaseHelper.CreateConnection();
        return conn.ExecuteScalar<int>(@"
            INSERT INTO CourseFiles (CourseId, FileName, StoredName, FileSize, UploadedBy, UploadedDate)
            VALUES (@CourseId, @FileName, @StoredName, @FileSize, @UploadedBy, @UploadedDate);
            SELECT last_insert_rowid();", file);
    }

    // Copies the stored file to destFilePath.
    public static void Download(int fileId, string destFilePath)
    {
        var file = GetById(fileId) ?? throw new FileNotFoundException("File record not found.");
        var src  = Path.Combine(StorageDir, file.StoredName);
        File.Copy(src, destFilePath, overwrite: true);
    }

    public static void Delete(int fileId)
    {
        var file = GetById(fileId);
        if (file == null) return;

        var path = Path.Combine(StorageDir, file.StoredName);
        if (File.Exists(path)) File.Delete(path);

        using var conn = DatabaseHelper.CreateConnection();
        conn.Execute("DELETE FROM CourseFiles WHERE Id = @Id", new { Id = fileId });
    }

    public static string FormatSize(long bytes) =>
        bytes < 1024            ? $"{bytes} B" :
        bytes < 1024 * 1024     ? $"{bytes / 1024.0:F1} KB" :
                                  $"{bytes / (1024.0 * 1024):F1} MB";
}
