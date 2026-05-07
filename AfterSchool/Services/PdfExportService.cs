using System.Globalization;
using AfterSchool.Data;
using AfterSchool.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AfterSchool.Services;

public static class PdfExportService
{
    private static readonly string HeaderBg = "#1e293b";
    private static readonly string HeaderFg = "#ffffff";
    private static readonly string AltRowBg = "#f1f5f9";
    private static readonly string BorderColor = "#e2e8f0";

    static PdfExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static string ExportAllStudents()
    {
        var students = StudentRepository.GetAll().ToList();
        var file = UniqueName("AfterSchool_Students");

        var headers = new[] { "ID", "First name", "Last name", "Email", "Contact", "Gender", "Birth date", "Register date", "Status", "Course", "Address" };
        var colWidths = new float[] { 1, 2, 2, 3, 2, 1.5f, 2, 2, 1.5f, 2, 3 };
        var rows = students.Select(s => new string?[]
        {
            s.Id.ToString(), s.FirstName, s.LastName, s.Email, s.ContactNo,
            s.Gender, s.BirthDate, s.RegisterDate, s.Status, s.CourseName ?? "", s.Address
        }).ToList();

        Document.Create(c => c.Page(p =>
        {
            p.Size(PageSizes.A4.Landscape());
            SetupPage(p, "All Students");
            p.Content().Element(ct => WriteTable(ct, headers, rows, colWidths));
        })).GeneratePdf(file);

        return file;
    }

    public static string ExportCourses()
    {
        var courses = CourseRepository.GetAll().ToList();
        var file = UniqueName("AfterSchool_Courses");

        var headers = new[] { "ID", "Name", "Teacher", "Capacity", "Enrolled", "Description" };
        var colWidths = new float[] { 1, 3, 3, 1.5f, 1.5f, 5 };
        var rows = courses.Select(c => new string?[]
        {
            c.Id.ToString(), c.Name, c.Teacher,
            c.Capacity.ToString(), CourseRepository.GetEnrolledCount(c.Id).ToString(),
            c.Description
        }).ToList();

        Document.Create(c => c.Page(p =>
        {
            p.Size(PageSizes.A4);
            SetupPage(p, "Courses");
            p.Content().Element(ct => WriteTable(ct, headers, rows, colWidths));
        })).GeneratePdf(file);

        return file;
    }

    public static string ExportClassLists()
    {
        var courses = CourseRepository.GetAll().ToList();
        var file = UniqueName("AfterSchool_ClassLists");

        Document.Create(container =>
        {
            // Overview page
            container.Page(p =>
            {
                p.Size(PageSizes.A4);
                SetupPage(p, "Class Lists — Overview");
                var overviewRows = courses.Select(c => new string?[]
                {
                    c.Name, c.Teacher, c.Capacity.ToString(),
                    CourseRepository.GetEnrolledCount(c.Id).ToString()
                }).ToList();
                p.Content().Element(ct =>
                    WriteTable(ct, new[] { "Course", "Teacher", "Capacity", "Enrolled" },
                        overviewRows, new float[] { 3, 3, 1.5f, 1.5f }));
            });

            // One page per course
            foreach (var course in courses)
            {
                container.Page(p =>
                {
                    p.Size(PageSizes.A4);
                    SetupPage(p, $"Class List — {course.Name}");

                    var students = StudentRepository.GetByCourse(course.Id).ToList();
                    var rows = students.Select((s, i) => new string?[]
                    {
                        (i + 1).ToString(), s.LastName, s.FirstName, s.Email, s.ContactNo
                    }).ToList();

                    p.Content().Column(col =>
                    {
                        col.Item().PaddingBottom(6).Text(t =>
                        {
                            t.Span("Teacher: ").Bold();
                            t.Span(course.Teacher);
                            t.Span("     ");
                            t.Span("Capacity: ").Bold();
                            t.Span(course.Capacity.ToString());
                        });
                        col.Item().Element(ct =>
                            WriteTable(ct, new[] { "#", "Last name", "First name", "Email", "Contact" },
                                rows, new float[] { 0.5f, 2, 2, 3, 2 }));
                    });
                });
            }
        }).GeneratePdf(file);

        return file;
    }

    public static string ExportClassList(Course course)
    {
        var file = UniqueName($"ClassList_{course.Name}");
        var students = StudentRepository.GetByCourse(course.Id).ToList();
        var rows = students.Select((s, i) => new string?[]
        {
            (i + 1).ToString(), s.LastName, s.FirstName, s.Email, s.ContactNo
        }).ToList();

        Document.Create(c => c.Page(p =>
        {
            p.Size(PageSizes.A4);
            SetupPage(p, $"Class List — {course.Name}");
            p.Content().Column(col =>
            {
                col.Item().PaddingBottom(6).Text(t =>
                {
                    t.Span("Teacher: ").Bold();
                    t.Span(course.Teacher);
                    t.Span("     ");
                    t.Span("Capacity: ").Bold();
                    t.Span(course.Capacity.ToString());
                });
                col.Item().Element(ct =>
                    WriteTable(ct, new[] { "#", "Last name", "First name", "Email", "Contact" },
                        rows, new float[] { 0.5f, 2, 2, 3, 2 }));
            });
        })).GeneratePdf(file);

        return file;
    }

    public static string ExportSchedule()
    {
        var slots = ScheduleRepository.GetAll().ToList();
        var file = UniqueName("AfterSchool_Schedule");

        var headers = new[] { "Day", "Start", "End", "Course", "Teacher", "Room" };
        var colWidths = new float[] { 2, 1.5f, 1.5f, 3, 3, 2 };
        var rows = slots.Select(s => new string?[]
        {
            s.DayOfWeek, s.StartTime, s.EndTime, s.CourseName, s.Teacher, s.Room
        }).ToList();

        Document.Create(c => c.Page(p =>
        {
            p.Size(PageSizes.A4);
            SetupPage(p, "Weekly Schedule");
            p.Content().Element(ct => WriteTable(ct, headers, rows, colWidths));
        })).GeneratePdf(file);

        return file;
    }

    public static string ExportGradeSheet(Course course)
    {
        var grades = GradeRepository.GetByCourse(course.Id).ToList();
        var file = UniqueName($"GradeSheet_{course.Name}");

        IEnumerable<GradeView> sorted = course.GradingScale == "Numeric"
            ? grades.OrderByDescending(g =>
                double.TryParse(g.Score, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : -1)
              .ThenBy(g => g.StudentLastName).ThenBy(g => g.StudentFirstName)
            : grades.OrderBy(g => g.StudentLastName).ThenBy(g => g.StudentFirstName);

        var sortedList = sorted.ToList();
        var rows = sortedList.Select((g, i) => new string?[]
        {
            (i + 1).ToString(), g.StudentLastName, g.StudentFirstName,
            string.IsNullOrEmpty(g.Score) ? "—" : g.Score,
            g.Notes, g.GradedDate
        }).ToList();

        // Average row
        string? avgText = null;
        if (course.GradingScale == "Numeric")
        {
            var scores = grades
                .Where(g => !string.IsNullOrEmpty(g.Score))
                .Select(g => double.TryParse(g.Score, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : (double?)null)
                .Where(v => v.HasValue).Select(v => v!.Value).ToList();
            if (scores.Count > 0)
                avgText = $"Average: {scores.Average():F1}";
        }

        Document.Create(c => c.Page(p =>
        {
            p.Size(PageSizes.A4);
            SetupPage(p, $"Grade Sheet — {course.Name}");
            p.Content().Column(col =>
            {
                col.Item().PaddingBottom(6).Text(t =>
                {
                    t.Span("Teacher: ").Bold();
                    t.Span(course.Teacher);
                    t.Span("     ");
                    t.Span("Scale: ").Bold();
                    t.Span(course.GradingScale);
                    t.Span("     ");
                    t.Span("Exported: ").Bold();
                    t.Span(DateTime.Today.ToString("yyyy-MM-dd"));
                });

                col.Item().Element(ct =>
                    WriteTable(ct, new[] { "#", "Last name", "First name", "Score", "Notes", "Date" },
                        rows, new float[] { 0.5f, 2, 2, 1.5f, 3, 2 }));

                if (avgText != null)
                    col.Item().PaddingTop(6).AlignRight().Text(avgText).Bold().FontSize(10);
            });
        })).GeneratePdf(file);

        return file;
    }

    public static string ExportTranscript(StudentView student, IEnumerable<GradeView> grades)
    {
        var gradeList = grades.ToList();
        var file = UniqueName($"Transcript_{student.LastName}_{student.FirstName}");

        var rows = gradeList.Select(g => new string?[]
        {
            g.CourseName, g.GradingScale,
            string.IsNullOrEmpty(g.Score) ? "—" : g.Score,
            g.Notes, g.GradedDate
        }).ToList();

        Document.Create(c => c.Page(p =>
        {
            p.Size(PageSizes.A4);
            SetupPage(p, $"Transcript — {student.FirstName} {student.LastName}");
            p.Content().Column(col =>
            {
                col.Item().PaddingBottom(6).Text(t =>
                {
                    t.Span("Exported: ").Bold();
                    t.Span(DateTime.Today.ToString("yyyy-MM-dd"));
                });
                col.Item().Element(ct =>
                    WriteTable(ct, new[] { "Course", "Scale", "Score", "Notes", "Date" },
                        rows, new float[] { 3, 2, 1.5f, 3, 2 }));
            });
        })).GeneratePdf(file);

        return file;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void SetupPage(PageDescriptor p, string title)
    {
        p.Margin(1.5f, Unit.Centimetre);
        p.DefaultTextStyle(x => x.FontSize(10).FontFamily("Segoe UI"));

        p.Header().Column(col =>
        {
            col.Item().Text(title).Bold().FontSize(16);
            col.Item().PaddingTop(2).LineHorizontal(1).LineColor(BorderColor);
            col.Item().Height(8);
        });

        p.Footer().AlignCenter().Text(x =>
        {
            x.Span("Page ").FontSize(8).FontColor("#64748b");
            x.CurrentPageNumber().FontSize(8).FontColor("#64748b");
            x.Span(" of ").FontSize(8).FontColor("#64748b");
            x.TotalPages().FontSize(8).FontColor("#64748b");
        });
    }

    private static void WriteTable(IContainer container, string[] headers, List<string?[]> rows, float[] colWidths)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                foreach (var w in colWidths)
                    cols.RelativeColumn(w);
            });

            table.Header(hdr =>
            {
                foreach (var h in headers)
                    hdr.Cell().Background(HeaderBg).Padding(6)
                        .Text(t => t.Span(h).Bold().FontColor(HeaderFg).FontSize(9));
            });

            bool alt = false;
            foreach (var row in rows)
            {
                var bg = alt ? AltRowBg : "#ffffff";
                alt = !alt;
                foreach (var cell in row)
                    table.Cell().Background(bg)
                        .BorderBottom(0.5f).BorderColor(BorderColor)
                        .Padding(5)
                        .Text(cell ?? "").FontSize(9);
            }
        });
    }

    private static string UniqueName(string baseName)
    {
        var dir = ExcelExportService.DefaultExportDirectory();
        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var clean = string.Concat(baseName.Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
        return Path.Combine(dir, $"{clean}_{stamp}.pdf");
    }
}
