using System.Globalization;
using AfterSchool.Data;
using AfterSchool.Models;
using ClosedXML.Excel;

namespace AfterSchool.Services;

public static class ExcelExportService
{
    public static string DefaultExportDirectory()
    {
        var downloads = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads");
        if (Directory.Exists(downloads)) return downloads;
        return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

    public static string ExportAllStudents()
    {
        var students = StudentRepository.GetAll().ToList();
        var file = UniqueName("AfterSchool_Students");

        using var wb = new XLWorkbook();
        var sheet = wb.AddWorksheet("Students");
        var headers = new[]
        {
            "ID", "First name", "Last name", "Email", "Contact",
            "Gender", "Birth date", "Register date", "Status",
            "Enrolled course", "Address"
        };
        WriteHeader(sheet, headers);

        for (int i = 0; i < students.Count; i++)
        {
            var s = students[i];
            int r = i + 2;
            sheet.Cell(r, 1).Value = s.Id;
            sheet.Cell(r, 2).Value = s.FirstName;
            sheet.Cell(r, 3).Value = s.LastName;
            sheet.Cell(r, 4).Value = s.Email;
            sheet.Cell(r, 5).Value = s.ContactNo;
            sheet.Cell(r, 6).Value = s.Gender;
            sheet.Cell(r, 7).Value = s.BirthDate;
            sheet.Cell(r, 8).Value = s.RegisterDate;
            sheet.Cell(r, 9).Value = s.Status;
            sheet.Cell(r, 10).Value = s.CourseName ?? "";
            sheet.Cell(r, 11).Value = s.Address;
        }
        sheet.Columns().AdjustToContents();
        wb.SaveAs(file);
        return file;
    }

    public static string ExportCourses()
    {
        var courses = CourseRepository.GetAll().ToList();
        var file = UniqueName("AfterSchool_Courses");

        using var wb = new XLWorkbook();
        var sheet = wb.AddWorksheet("Courses");
        WriteHeader(sheet, new[] { "ID", "Name", "Teacher", "Capacity", "Enrolled", "Description" });

        for (int i = 0; i < courses.Count; i++)
        {
            var c = courses[i];
            int r = i + 2;
            sheet.Cell(r, 1).Value = c.Id;
            sheet.Cell(r, 2).Value = c.Name;
            sheet.Cell(r, 3).Value = c.Teacher;
            sheet.Cell(r, 4).Value = c.Capacity;
            sheet.Cell(r, 5).Value = CourseRepository.GetEnrolledCount(c.Id);
            sheet.Cell(r, 6).Value = c.Description;
        }
        sheet.Columns().AdjustToContents();
        wb.SaveAs(file);
        return file;
    }

    public static string ExportClassLists()
    {
        var courses = CourseRepository.GetAll().ToList();
        var file = UniqueName("AfterSchool_ClassLists");

        using var wb = new XLWorkbook();

        var overview = wb.AddWorksheet("Overview");
        WriteHeader(overview, new[] { "Course", "Teacher", "Capacity", "Enrolled" });
        for (int i = 0; i < courses.Count; i++)
        {
            var c = courses[i];
            int r = i + 2;
            overview.Cell(r, 1).Value = c.Name;
            overview.Cell(r, 2).Value = c.Teacher;
            overview.Cell(r, 3).Value = c.Capacity;
            overview.Cell(r, 4).Value = CourseRepository.GetEnrolledCount(c.Id);
        }
        overview.Columns().AdjustToContents();

        foreach (var course in courses)
        {
            var safeName = SanitizeSheetName(course.Name);
            var sheet = wb.AddWorksheet(safeName);

            sheet.Cell(1, 1).Value = $"Class list — {course.Name}";
            sheet.Cell(1, 1).Style.Font.FontSize = 14;
            sheet.Cell(1, 1).Style.Font.Bold = true;
            sheet.Range(1, 1, 1, 5).Merge();

            sheet.Cell(2, 1).Value = $"Teacher: {course.Teacher}";
            sheet.Cell(2, 5).Value = $"Capacity: {course.Capacity}";

            WriteHeader(sheet, new[] { "#", "Last name", "First name", "Email", "Contact" }, row: 4);
            var students = StudentRepository.GetByCourse(course.Id).ToList();
            for (int i = 0; i < students.Count; i++)
            {
                var s = students[i];
                int r = i + 5;
                sheet.Cell(r, 1).Value = i + 1;
                sheet.Cell(r, 2).Value = s.LastName;
                sheet.Cell(r, 3).Value = s.FirstName;
                sheet.Cell(r, 4).Value = s.Email;
                sheet.Cell(r, 5).Value = s.ContactNo;
            }
            sheet.Columns().AdjustToContents();
        }

        wb.SaveAs(file);
        return file;
    }

    public static string ExportClassList(Course course)
    {
        var file = UniqueName($"ClassList_{course.Name}");
        using var wb = new XLWorkbook();
        var sheet = wb.AddWorksheet(SanitizeSheetName(course.Name));

        sheet.Cell(1, 1).Value = $"Class list — {course.Name}";
        sheet.Cell(1, 1).Style.Font.FontSize = 14;
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Range(1, 1, 1, 5).Merge();

        sheet.Cell(2, 1).Value = $"Teacher: {course.Teacher}";
        sheet.Cell(2, 5).Value = $"Capacity: {course.Capacity}";

        WriteHeader(sheet, new[] { "#", "Last name", "First name", "Email", "Contact" }, row: 4);
        var students = StudentRepository.GetByCourse(course.Id).ToList();
        for (int i = 0; i < students.Count; i++)
        {
            var s = students[i];
            int r = i + 5;
            sheet.Cell(r, 1).Value = i + 1;
            sheet.Cell(r, 2).Value = s.LastName;
            sheet.Cell(r, 3).Value = s.FirstName;
            sheet.Cell(r, 4).Value = s.Email;
            sheet.Cell(r, 5).Value = s.ContactNo;
        }
        sheet.Columns().AdjustToContents();
        wb.SaveAs(file);
        return file;
    }

    public static string ExportSchedule()
    {
        var slots = ScheduleRepository.GetAll().ToList();
        var file = UniqueName("AfterSchool_Schedule");

        using var wb = new XLWorkbook();
        var sheet = wb.AddWorksheet("Schedule");
        WriteHeader(sheet, new[] { "Day", "Start", "End", "Course", "Teacher", "Room" });

        for (int i = 0; i < slots.Count; i++)
        {
            var s = slots[i];
            int r = i + 2;
            sheet.Cell(r, 1).Value = s.DayOfWeek;
            sheet.Cell(r, 2).Value = s.StartTime;
            sheet.Cell(r, 3).Value = s.EndTime;
            sheet.Cell(r, 4).Value = s.CourseName;
            sheet.Cell(r, 5).Value = s.Teacher;
            sheet.Cell(r, 6).Value = s.Room;
        }
        sheet.Columns().AdjustToContents();
        wb.SaveAs(file);
        return file;
    }

    public static string ExportGradeSheet(Course course)
    {
        var grades = GradeRepository.GetByCourse(course.Id).ToList();
        var file = UniqueName($"GradeSheet_{course.Name}");

        using var wb = new XLWorkbook();
        var sheet = wb.AddWorksheet(SanitizeSheetName(course.Name));

        sheet.Cell(1, 1).Value = $"Grade Sheet — {course.Name}";
        sheet.Cell(1, 1).Style.Font.FontSize = 14;
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Range(1, 1, 1, 6).Merge();

        sheet.Cell(2, 1).Value = $"Teacher: {course.Teacher}";
        sheet.Cell(2, 3).Value = $"Scale: {course.GradingScale}";
        sheet.Cell(2, 5).Value = $"Exported: {DateTime.Today:yyyy-MM-dd}";

        WriteHeader(sheet, new[] { "#", "Last name", "First name", "Score", "Notes", "Date" }, row: 4);

        IEnumerable<GradeView> sorted = course.GradingScale == "Numeric"
            ? grades.OrderByDescending(g =>
                double.TryParse(g.Score, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : -1)
              .ThenBy(g => g.StudentLastName).ThenBy(g => g.StudentFirstName)
            : grades.OrderBy(g => g.StudentLastName).ThenBy(g => g.StudentFirstName);

        int i = 0;
        foreach (var g in sorted)
        {
            int r = i + 5;
            sheet.Cell(r, 1).Value = i + 1;
            sheet.Cell(r, 2).Value = g.StudentLastName;
            sheet.Cell(r, 3).Value = g.StudentFirstName;
            sheet.Cell(r, 4).Value = string.IsNullOrEmpty(g.Score) ? "—" : g.Score;
            sheet.Cell(r, 5).Value = g.Notes;
            sheet.Cell(r, 6).Value = g.GradedDate;
            i++;
        }

        // Stats row
        var graded = grades.Where(g => !string.IsNullOrEmpty(g.Score)).ToList();
        if (graded.Count > 0 && course.GradingScale == "Numeric")
        {
            var scores = graded.Select(g =>
                double.TryParse(g.Score, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : (double?)null)
                .Where(v => v.HasValue).Select(v => v!.Value).ToList();
            if (scores.Count > 0)
            {
                int statsRow = i + 6;
                sheet.Cell(statsRow, 3).Value = "Average:";
                sheet.Cell(statsRow, 4).Value = $"{scores.Average():F1}";
                sheet.Cell(statsRow, 3).Style.Font.Bold = true;
            }
        }

        sheet.Columns().AdjustToContents();
        wb.SaveAs(file);
        return file;
    }

    public static string ExportTranscript(StudentView student, IEnumerable<GradeView> grades)
    {
        var gradeList = grades.ToList();
        var file = UniqueName($"Transcript_{student.LastName}_{student.FirstName}");

        using var wb = new XLWorkbook();
        var sheet = wb.AddWorksheet("Transcript");

        var name = $"{student.FirstName} {student.LastName}";
        sheet.Cell(1, 1).Value = $"Transcript — {name}";
        sheet.Cell(1, 1).Style.Font.FontSize = 14;
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Range(1, 1, 1, 5).Merge();

        sheet.Cell(2, 1).Value = $"Exported: {DateTime.Today:yyyy-MM-dd}";

        WriteHeader(sheet, new[] { "Course", "Scale", "Score", "Notes", "Date" }, row: 4);

        for (int i = 0; i < gradeList.Count; i++)
        {
            var g = gradeList[i];
            int r = i + 5;
            sheet.Cell(r, 1).Value = g.CourseName;
            sheet.Cell(r, 2).Value = g.GradingScale;
            sheet.Cell(r, 3).Value = string.IsNullOrEmpty(g.Score) ? "—" : g.Score;
            sheet.Cell(r, 4).Value = g.Notes;
            sheet.Cell(r, 5).Value = g.GradedDate;
        }

        sheet.Columns().AdjustToContents();
        wb.SaveAs(file);
        return file;
    }

    private static void WriteHeader(IXLWorksheet sheet, string[] headers, int row = 1)
    {
        for (int i = 0; i < headers.Length; i++)
            sheet.Cell(row, i + 1).Value = headers[i];
        var range = sheet.Range(row, 1, row, headers.Length);
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = XLColor.FromArgb(30, 41, 59);
        range.Style.Font.FontColor = XLColor.White;
    }

    private static string UniqueName(string baseName)
    {
        var dir = DefaultExportDirectory();
        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var clean = string.Concat(baseName.Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
        return Path.Combine(dir, $"{clean}_{stamp}.xlsx");
    }

    private static string SanitizeSheetName(string name)
    {
        var invalid = new[] { '\\', '/', '?', '*', '[', ']', ':' };
        var clean = string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c));
        if (clean.Length > 31) clean = clean[..31];
        return string.IsNullOrWhiteSpace(clean) ? "Sheet" : clean;
    }
}
