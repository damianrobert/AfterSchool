using AfterSchool.Models;
using Dapper;

namespace AfterSchool.Data;

public static class AssignmentRepository
{
    public static string StorageDir =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assignment_files");

    private static void EnsureDir() => Directory.CreateDirectory(StorageDir);

    // ── Assignments ──────────────────────────────────────────────────────────

    public static IEnumerable<AssignmentView> GetByCourse(int courseId)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.Query<AssignmentView>(@"
            SELECT a.*,
                   COUNT(s.Id) AS SubmissionCount
            FROM Assignments a
            LEFT JOIN AssignmentSubmissions s ON s.AssignmentId = a.Id
            WHERE a.CourseId = @CourseId
            GROUP BY a.Id
            ORDER BY a.DueDate, a.Title",
            new { CourseId = courseId }).ToList();
    }

    public static Assignment? GetById(int id)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.QueryFirstOrDefault<Assignment>(
            "SELECT * FROM Assignments WHERE Id = @Id", new { Id = id });
    }

    public static int Insert(Assignment a)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.ExecuteScalar<int>(@"
            INSERT INTO Assignments (CourseId, Title, Description, DueDate, CreatedBy, CreatedDate)
            VALUES (@CourseId, @Title, @Description, @DueDate, @CreatedBy, @CreatedDate);
            SELECT last_insert_rowid();", a);
    }

    public static void Update(Assignment a)
    {
        using var conn = DatabaseHelper.CreateConnection();
        conn.Execute(@"
            UPDATE Assignments
            SET Title = @Title, Description = @Description, DueDate = @DueDate
            WHERE Id = @Id", a);
    }

    public static void Delete(int id)
    {
        // Submission files on disk are not auto-cleaned here;
        // acceptable since the assignment folder is scoped.
        using var conn = DatabaseHelper.CreateConnection();
        conn.Execute("DELETE FROM Assignments WHERE Id = @Id", new { Id = id });
    }

    // ── Submissions ──────────────────────────────────────────────────────────

    // All submissions for a given assignment (teacher view)
    public static IEnumerable<AssignmentSubmissionView> GetSubmissions(int assignmentId)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.Query<AssignmentSubmissionView>(@"
            SELECT s.Id, s.AssignmentId, s.StudentId,
                   st.FirstName || ' ' || st.LastName AS StudentName,
                   s.FileName, s.StoredName, s.FileSize, s.SubmittedDate
            FROM AssignmentSubmissions s
            JOIN Students st ON st.Id = s.StudentId
            WHERE s.AssignmentId = @AssignmentId
            ORDER BY st.LastName, st.FirstName",
            new { AssignmentId = assignmentId }).ToList();
    }

    // All assignments for a course with the student's own submission status
    public static IEnumerable<AssignmentStudentView> GetForStudent(int courseId, int studentId)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.Query<AssignmentStudentView>(@"
            SELECT a.*,
                   s.Id           AS SubmissionId,
                   s.FileName     AS SubmissionFile,
                   s.SubmittedDate,
                   s.StoredName,
                   s.FileSize
            FROM Assignments a
            LEFT JOIN AssignmentSubmissions s
                   ON s.AssignmentId = a.Id AND s.StudentId = @StudentId
            WHERE a.CourseId = @CourseId
            ORDER BY a.DueDate, a.Title",
            new { CourseId = courseId, StudentId = studentId }).ToList();
    }

    public static AssignmentStudentView? GetForStudentById(int assignmentId, int studentId)
    {
        using var conn = DatabaseHelper.CreateConnection();
        return conn.QueryFirstOrDefault<AssignmentStudentView>(@"
            SELECT a.*,
                   s.Id           AS SubmissionId,
                   s.FileName     AS SubmissionFile,
                   s.SubmittedDate,
                   s.StoredName,
                   s.FileSize
            FROM Assignments a
            LEFT JOIN AssignmentSubmissions s
                   ON s.AssignmentId = a.Id AND s.StudentId = @StudentId
            WHERE a.Id = @AssignmentId",
            new { AssignmentId = assignmentId, StudentId = studentId });
    }

    // Submit or re-submit. Replaces previous file on disk and updates DB.
    public static void Submit(int assignmentId, int studentId, string sourceFilePath)
    {
        EnsureDir();

        // Get existing submission to clean up old file after successful copy
        using var conn = DatabaseHelper.CreateConnection();
        var existing = conn.QueryFirstOrDefault<(string StoredName, int Id)>(
            "SELECT StoredName, Id FROM AssignmentSubmissions WHERE AssignmentId = @A AND StudentId = @S",
            new { A = assignmentId, S = studentId });

        var originalName = Path.GetFileName(sourceFilePath);
        var ext          = Path.GetExtension(sourceFilePath);
        var storedName   = $"a{assignmentId}_s{studentId}_{Guid.NewGuid():N}{ext}";
        var destPath     = Path.Combine(StorageDir, storedName);
        File.Copy(sourceFilePath, destPath, overwrite: false);
        var fileSize = new FileInfo(destPath).Length;
        var today    = DateTime.Today.ToString("yyyy-MM-dd");

        if (existing.Id > 0)
        {
            conn.Execute(@"
                UPDATE AssignmentSubmissions
                SET FileName = @FileName, StoredName = @StoredName,
                    FileSize = @FileSize, SubmittedDate = @SubmittedDate
                WHERE AssignmentId = @AssignmentId AND StudentId = @StudentId",
                new { FileName = originalName, StoredName = storedName, FileSize = fileSize,
                      SubmittedDate = today, AssignmentId = assignmentId, StudentId = studentId });

            var oldPath = Path.Combine(StorageDir, existing.StoredName);
            if (File.Exists(oldPath)) File.Delete(oldPath);
        }
        else
        {
            conn.Execute(@"
                INSERT INTO AssignmentSubmissions
                    (AssignmentId, StudentId, FileName, StoredName, FileSize, SubmittedDate)
                VALUES (@AssignmentId, @StudentId, @FileName, @StoredName, @FileSize, @SubmittedDate)",
                new { AssignmentId = assignmentId, StudentId = studentId,
                      FileName = originalName, StoredName = storedName,
                      FileSize = fileSize, SubmittedDate = today });
        }
    }

    public static void DownloadSubmission(string storedName, string destFilePath)
    {
        var src = Path.Combine(StorageDir, storedName);
        File.Copy(src, destFilePath, overwrite: true);
    }
}
