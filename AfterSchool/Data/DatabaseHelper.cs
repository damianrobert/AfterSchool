using Microsoft.Data.Sqlite;

namespace AfterSchool.Data;

public static class DatabaseHelper
{
    private const string DbFileName = "afterschool.db";

    private static string ExeDirectory =>
        Path.GetDirectoryName(Environment.ProcessPath) ?? AppDomain.CurrentDomain.BaseDirectory;

    public static string DatabasePath =>
        Path.Combine(ExeDirectory, DbFileName);

    public static string ConnectionString =>
        new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            ForeignKeys = true
        }.ToString();

    public static SqliteConnection CreateConnection()
    {
        var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        using var pragma = conn.CreateCommand();
        pragma.CommandText = "PRAGMA foreign_keys = ON;";
        pragma.ExecuteNonQuery();
        return conn;
    }

    public static void Initialize()
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Users (
                Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                Username     TEXT    NOT NULL UNIQUE COLLATE NOCASE,
                PasswordHash TEXT    NOT NULL,
                FullName     TEXT    NOT NULL DEFAULT '',
                Role         TEXT    NOT NULL DEFAULT 'Staff',
                CreatedDate  TEXT    NOT NULL DEFAULT '',
                IsActive     INTEGER NOT NULL DEFAULT 1
            );

            CREATE TABLE IF NOT EXISTS Courses (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                Name        TEXT    NOT NULL,
                Teacher     TEXT    NOT NULL DEFAULT '',
                Description TEXT    NOT NULL DEFAULT '',
                Capacity    INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS Schedule (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                CourseId  INTEGER NOT NULL,
                DayOfWeek TEXT    NOT NULL,
                StartTime TEXT    NOT NULL,
                EndTime   TEXT    NOT NULL,
                Room      TEXT    NOT NULL DEFAULT '',
                FOREIGN KEY (CourseId) REFERENCES Courses(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS Students (
                Id               INTEGER PRIMARY KEY AUTOINCREMENT,
                FirstName        TEXT    NOT NULL,
                LastName         TEXT    NOT NULL,
                Address          TEXT    NOT NULL DEFAULT '',
                Email            TEXT    NOT NULL DEFAULT '',
                BirthDate        TEXT    NOT NULL DEFAULT '',
                ContactNo        TEXT    NOT NULL DEFAULT '',
                Gender           TEXT    NOT NULL DEFAULT '',
                RegisterDate     TEXT    NOT NULL DEFAULT '',
                Status           TEXT    NOT NULL DEFAULT 'Active',
                EnrolledCourseId INTEGER,
                FOREIGN KEY (EnrolledCourseId) REFERENCES Courses(Id) ON DELETE SET NULL
            );

            CREATE TABLE IF NOT EXISTS Rooms (
                Id   INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT    NOT NULL UNIQUE COLLATE NOCASE
            );

            CREATE TABLE IF NOT EXISTS Grades (
                Id         INTEGER PRIMARY KEY AUTOINCREMENT,
                StudentId  INTEGER NOT NULL,
                CourseId   INTEGER NOT NULL,
                Score      TEXT    NOT NULL DEFAULT '',
                Notes      TEXT    NOT NULL DEFAULT '',
                GradedDate TEXT    NOT NULL DEFAULT '',
                GradedBy   TEXT    NOT NULL DEFAULT '',
                UNIQUE(StudentId, CourseId),
                FOREIGN KEY (StudentId) REFERENCES Students(Id) ON DELETE CASCADE,
                FOREIGN KEY (CourseId)  REFERENCES Courses(Id)  ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS CourseFiles (
                Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                CourseId     INTEGER NOT NULL,
                FileName     TEXT    NOT NULL,
                StoredName   TEXT    NOT NULL,
                FileSize     INTEGER NOT NULL DEFAULT 0,
                UploadedBy   TEXT    NOT NULL DEFAULT '',
                UploadedDate TEXT    NOT NULL DEFAULT '',
                FOREIGN KEY (CourseId) REFERENCES Courses(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS Assignments (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                CourseId    INTEGER NOT NULL,
                Title       TEXT    NOT NULL,
                Description TEXT    NOT NULL DEFAULT '',
                DueDate     TEXT    NOT NULL DEFAULT '',
                CreatedBy   TEXT    NOT NULL DEFAULT '',
                CreatedDate TEXT    NOT NULL DEFAULT '',
                FOREIGN KEY (CourseId) REFERENCES Courses(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS AssignmentSubmissions (
                Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                AssignmentId  INTEGER NOT NULL,
                StudentId     INTEGER NOT NULL,
                FileName      TEXT    NOT NULL,
                StoredName    TEXT    NOT NULL,
                FileSize      INTEGER NOT NULL DEFAULT 0,
                SubmittedDate TEXT    NOT NULL DEFAULT '',
                UNIQUE(AssignmentId, StudentId),
                FOREIGN KEY (AssignmentId) REFERENCES Assignments(Id) ON DELETE CASCADE,
                FOREIGN KEY (StudentId)    REFERENCES Students(Id)    ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS StudentCourses (
                StudentId INTEGER NOT NULL,
                CourseId  INTEGER NOT NULL,
                PRIMARY KEY (StudentId, CourseId),
                FOREIGN KEY (StudentId) REFERENCES Students(Id) ON DELETE CASCADE,
                FOREIGN KEY (CourseId)  REFERENCES Courses(Id)  ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS IX_Schedule_CourseId              ON Schedule(CourseId);
            CREATE INDEX IF NOT EXISTS IX_Students_EnrolledCourseId      ON Students(EnrolledCourseId);
            CREATE INDEX IF NOT EXISTS IX_StudentCourses_StudentId       ON StudentCourses(StudentId);
            CREATE INDEX IF NOT EXISTS IX_StudentCourses_CourseId        ON StudentCourses(CourseId);
            CREATE INDEX IF NOT EXISTS IX_Grades_CourseId                ON Grades(CourseId);
            CREATE INDEX IF NOT EXISTS IX_Grades_StudentId               ON Grades(StudentId);
            CREATE INDEX IF NOT EXISTS IX_CourseFiles_CourseId           ON CourseFiles(CourseId);
            CREATE INDEX IF NOT EXISTS IX_Assignments_CourseId           ON Assignments(CourseId);
            CREATE INDEX IF NOT EXISTS IX_AssignmentSubmissions_AsgId    ON AssignmentSubmissions(AssignmentId);
            CREATE INDEX IF NOT EXISTS IX_AssignmentSubmissions_StdId    ON AssignmentSubmissions(StudentId);

            CREATE TABLE IF NOT EXISTS ChatConversations (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId          INTEGER NOT NULL,
                Title           TEXT    NOT NULL DEFAULT 'New Chat',
                CreatedDate     TEXT    NOT NULL DEFAULT '',
                LastMessageDate TEXT    NOT NULL DEFAULT ''
            );

            CREATE TABLE IF NOT EXISTS ChatMessages (
                Id             INTEGER PRIMARY KEY AUTOINCREMENT,
                ConversationId INTEGER NOT NULL,
                Role           TEXT    NOT NULL DEFAULT 'user',
                Content        TEXT    NOT NULL DEFAULT '',
                SentDate       TEXT    NOT NULL DEFAULT '',
                FOREIGN KEY (ConversationId) REFERENCES ChatConversations(Id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS IX_ChatConversations_UserId   ON ChatConversations(UserId);
            CREATE INDEX IF NOT EXISTS IX_ChatMessages_ConvId        ON ChatMessages(ConversationId);

            CREATE TABLE IF NOT EXISTS DirectConversations (
                Id              INTEGER PRIMARY KEY AUTOINCREMENT,
                User1Id         INTEGER NOT NULL,
                User2Id         INTEGER NOT NULL,
                CreatedDate     TEXT    NOT NULL DEFAULT '',
                LastMessageDate TEXT    NOT NULL DEFAULT '',
                UNIQUE(User1Id, User2Id),
                FOREIGN KEY (User1Id) REFERENCES Users(Id) ON DELETE CASCADE,
                FOREIGN KEY (User2Id) REFERENCES Users(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS DirectMessages (
                Id             INTEGER PRIMARY KEY AUTOINCREMENT,
                ConversationId INTEGER NOT NULL,
                SenderId       INTEGER NOT NULL,
                Content        TEXT    NOT NULL DEFAULT '',
                SentDate       TEXT    NOT NULL DEFAULT '',
                FOREIGN KEY (ConversationId) REFERENCES DirectConversations(Id) ON DELETE CASCADE,
                FOREIGN KEY (SenderId)       REFERENCES Users(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS DirectMessageAttachments (
                Id         INTEGER PRIMARY KEY AUTOINCREMENT,
                MessageId  INTEGER NOT NULL,
                FileName   TEXT    NOT NULL,
                StoredName TEXT    NOT NULL,
                FileSize   INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (MessageId) REFERENCES DirectMessages(Id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS IX_DirectConv_User1Id       ON DirectConversations(User1Id);
            CREATE INDEX IF NOT EXISTS IX_DirectConv_User2Id       ON DirectConversations(User2Id);
            CREATE INDEX IF NOT EXISTS IX_DirectMessages_ConvId    ON DirectMessages(ConversationId);
            CREATE INDEX IF NOT EXISTS IX_DirectMessages_SenderId  ON DirectMessages(SenderId);
            CREATE INDEX IF NOT EXISTS IX_DirectAttach_MessageId   ON DirectMessageAttachments(MessageId);

            CREATE TABLE IF NOT EXISTS Notifications (
                Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId      INTEGER NOT NULL,
                Type        TEXT    NOT NULL DEFAULT '',
                Message     TEXT    NOT NULL DEFAULT '',
                IsRead      INTEGER NOT NULL DEFAULT 0,
                CreatedDate TEXT    NOT NULL DEFAULT '',
                FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS IX_Notifications_UserId ON Notifications(UserId);

            CREATE TABLE IF NOT EXISTS CourseMessages (
                Id         INTEGER PRIMARY KEY AUTOINCREMENT,
                CourseId   INTEGER NOT NULL,
                SenderId   INTEGER NOT NULL,
                Content    TEXT    NOT NULL DEFAULT '',
                SentDate   TEXT    NOT NULL DEFAULT '',
                FOREIGN KEY (CourseId) REFERENCES Courses(Id) ON DELETE CASCADE,
                FOREIGN KEY (SenderId) REFERENCES Users(Id)   ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS IX_CourseMessages_CourseId ON CourseMessages(CourseId);
            CREATE INDEX IF NOT EXISTS IX_CourseMessages_SenderId  ON CourseMessages(SenderId);

            CREATE TABLE IF NOT EXISTS DirectConversationReads (
                ConversationId INTEGER NOT NULL,
                UserId         INTEGER NOT NULL,
                LastReadDate   TEXT    NOT NULL DEFAULT '',
                PRIMARY KEY (ConversationId, UserId),
                FOREIGN KEY (ConversationId) REFERENCES DirectConversations(Id) ON DELETE CASCADE,
                FOREIGN KEY (UserId)         REFERENCES Users(Id)               ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS CourseLastRead (
                CourseId  INTEGER NOT NULL,
                UserId    INTEGER NOT NULL,
                LastMsgId INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY (CourseId, UserId)
            );
        ";
        cmd.ExecuteNonQuery();

        MigrateSchema(conn);
        RoomRepository.SeedIfEmpty();
    }

    private static void MigrateSchema(SqliteConnection conn)
    {
        TryAlter(conn, "ALTER TABLE Courses ADD COLUMN GradingScale TEXT NOT NULL DEFAULT 'Numeric';");
        TryAlter(conn, "ALTER TABLE Users ADD COLUMN MustChangePassword INTEGER NOT NULL DEFAULT 0;");
        TryAlter(conn, "ALTER TABLE Users ADD COLUMN StudentId INTEGER REFERENCES Students(Id) ON DELETE SET NULL;");
        TryAlter(conn, "ALTER TABLE Schedule ADD COLUMN AddedByMilo INTEGER NOT NULL DEFAULT 0;");
        RecreateSubmissionsWithoutUnique(conn);
        MigrateEnrollmentsToJunctionTable(conn);
    }

    private static void MigrateEnrollmentsToJunctionTable(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR IGNORE INTO StudentCourses (StudentId, CourseId)
            SELECT Id, EnrolledCourseId FROM Students WHERE EnrolledCourseId IS NOT NULL";
        cmd.ExecuteNonQuery();
    }

    // SQLite can't DROP CONSTRAINT, so we recreate the table without UNIQUE(AssignmentId, StudentId)
    private static void RecreateSubmissionsWithoutUnique(SqliteConnection conn)
    {
        using var check = conn.CreateCommand();
        check.CommandText = "SELECT sql FROM sqlite_master WHERE type='table' AND name='AssignmentSubmissions'";
        var tableSql = (string?)check.ExecuteScalar();
        if (tableSql == null || !tableSql.Contains("UNIQUE")) return;

        void Exec(string sql) { using var c = conn.CreateCommand(); c.CommandText = sql; c.ExecuteNonQuery(); }

        Exec("PRAGMA foreign_keys = OFF");
        Exec(@"CREATE TABLE AssignmentSubmissions_new (
            Id            INTEGER PRIMARY KEY AUTOINCREMENT,
            AssignmentId  INTEGER NOT NULL,
            StudentId     INTEGER NOT NULL,
            FileName      TEXT    NOT NULL,
            StoredName    TEXT    NOT NULL,
            FileSize      INTEGER NOT NULL DEFAULT 0,
            SubmittedDate TEXT    NOT NULL DEFAULT '',
            FOREIGN KEY (AssignmentId) REFERENCES Assignments(Id) ON DELETE CASCADE,
            FOREIGN KEY (StudentId)    REFERENCES Students(Id)    ON DELETE CASCADE
        )");
        Exec("INSERT INTO AssignmentSubmissions_new SELECT * FROM AssignmentSubmissions");
        Exec("DROP TABLE AssignmentSubmissions");
        Exec("ALTER TABLE AssignmentSubmissions_new RENAME TO AssignmentSubmissions");
        Exec("CREATE INDEX IF NOT EXISTS IX_AssignmentSubmissions_AsgId ON AssignmentSubmissions(AssignmentId)");
        Exec("CREATE INDEX IF NOT EXISTS IX_AssignmentSubmissions_StdId ON AssignmentSubmissions(StudentId)");
        Exec("PRAGMA foreign_keys = ON");
    }

    private static void TryAlter(SqliteConnection conn, string sql)
    {
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }
        catch { /* column already exists */ }
    }
}
