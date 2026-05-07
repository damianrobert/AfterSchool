using System.Diagnostics;
using System.Text.Json.Nodes;
using AfterSchool.Data;
using AfterSchool.Models;

namespace AfterSchool.Services;

/// <summary>
/// Extensible registry of actions Milo can perform via Gemini function calling.
/// To add a new action:
///   1. Add a JsonObject to GetFunctionDeclarations() describing the schema.
///   2. Add a case in ExecuteAsync() routing to an implementation method.
///   3. Implement the method (returns a plain-English result string fed back to Gemini).
/// </summary>
public static class MiloActions
{
    public static JsonArray GetFunctionDeclarations() => new()
    {
        new JsonObject
        {
            ["name"] = "add_schedule_slot",
            ["description"] = "Adds a time slot for a course to the weekly schedule.",
            ["parameters"] = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["course_name"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = "Name of the course to schedule (case-insensitive, partial match supported)."
                    },
                    ["day_of_week"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = "Day of the week: Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, or Sunday."
                    },
                    ["start_time"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = "Start time in 24-hour HH:mm format, e.g. '15:00'."
                    },
                    ["end_time"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = "End time in 24-hour HH:mm format, e.g. '16:00'. Defaults to 1 hour after start_time if omitted."
                    },
                    ["room"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = "Room identifier, e.g. '101' or 'Lab A'. Optional."
                    }
                },
                ["required"] = new JsonArray { "course_name", "day_of_week", "start_time" }
            }
        },
        new JsonObject
        {
            ["name"] = "export_students",
            ["description"] = "Exports all student records to a file and opens it.",
            ["parameters"] = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["format"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = "Output format: 'excel' (default) or 'pdf'."
                    }
                },
                ["required"] = new JsonArray()
            }
        },
        new JsonObject
        {
            ["name"] = "export_courses",
            ["description"] = "Exports the course catalog to a file and opens it.",
            ["parameters"] = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["format"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = "Output format: 'excel' (default) or 'pdf'."
                    }
                },
                ["required"] = new JsonArray()
            }
        },
        new JsonObject
        {
            ["name"] = "export_class_lists",
            ["description"] = "Exports class lists for every course (one sheet/page per course) to a file and opens it.",
            ["parameters"] = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["format"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = "Output format: 'excel' (default) or 'pdf'."
                    }
                },
                ["required"] = new JsonArray()
            }
        },
        new JsonObject
        {
            ["name"] = "export_class_list",
            ["description"] = "Exports the student roster for a specific course to a file and opens it.",
            ["parameters"] = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["course_name"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = "Name of the course (case-insensitive, partial match supported)."
                    },
                    ["format"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = "Output format: 'excel' (default) or 'pdf'."
                    }
                },
                ["required"] = new JsonArray { "course_name" }
            }
        },
        new JsonObject
        {
            ["name"] = "export_schedule",
            ["description"] = "Exports the full weekly schedule to a file and opens it.",
            ["parameters"] = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["format"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = "Output format: 'excel' (default) or 'pdf'."
                    }
                },
                ["required"] = new JsonArray()
            }
        },
        new JsonObject
        {
            ["name"] = "export_grade_sheet",
            ["description"] = "Exports the grade sheet for a specific course to a file and opens it.",
            ["parameters"] = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["course_name"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = "Name of the course (case-insensitive, partial match supported)."
                    },
                    ["format"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = "Output format: 'excel' (default) or 'pdf'."
                    }
                },
                ["required"] = new JsonArray { "course_name" }
            }
        },
        new JsonObject
        {
            ["name"] = "export_transcript",
            ["description"] = "Exports the grade transcript for a specific student to a file and opens it.",
            ["parameters"] = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["student_name"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = "Full name or partial name of the student (case-insensitive match on first name, last name, or full name)."
                    },
                    ["format"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = "Output format: 'excel' (default) or 'pdf'."
                    }
                },
                ["required"] = new JsonArray { "student_name" }
            }
        }
    };

    public static Task<string> ExecuteAsync(string functionName, JsonNode? args) =>
        functionName switch
        {
            "add_schedule_slot"  => AddScheduleSlotAsync(args),
            "export_students"    => ExportStudentsAsync(args),
            "export_courses"     => ExportCoursesAsync(args),
            "export_class_lists" => ExportClassListsAsync(args),
            "export_class_list"  => ExportClassListAsync(args),
            "export_schedule"    => ExportScheduleAsync(args),
            "export_grade_sheet" => ExportGradeSheetAsync(args),
            "export_transcript"  => ExportTranscriptAsync(args),
            _ => Task.FromResult($"Unknown action: {functionName}")
        };

    // ── Action implementations ────────────────────────────────────────────────

    private static Task<string> AddScheduleSlotAsync(JsonNode? args)
    {
        var courseName = args?["course_name"]?.GetValue<string>() ?? "";
        var day        = args?["day_of_week"]?.GetValue<string>() ?? "";
        var startRaw   = args?["start_time"]?.GetValue<string>() ?? "";
        var endRaw     = args?["end_time"]?.GetValue<string>() ?? "";
        var room       = args?["room"]?.GetValue<string>() ?? "";

        if (string.IsNullOrWhiteSpace(courseName))
            return Task.FromResult("Error: course_name is required.");
        if (string.IsNullOrWhiteSpace(day))
            return Task.FromResult("Error: day_of_week is required.");
        if (string.IsNullOrWhiteSpace(startRaw))
            return Task.FromResult("Error: start_time is required.");

        var validDays = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
        var normalizedDay = validDays.FirstOrDefault(d => d.Equals(day, StringComparison.OrdinalIgnoreCase));
        if (normalizedDay == null)
            return Task.FromResult($"Error: '{day}' is not a valid day. Use one of: {string.Join(", ", validDays)}");

        if (!TryParseTime(startRaw, out var startTs))
            return Task.FromResult($"Error: '{startRaw}' is not a valid start time. Use HH:mm (e.g. '15:00').");

        TimeSpan endTs;
        if (string.IsNullOrWhiteSpace(endRaw))
            endTs = startTs.Add(TimeSpan.FromMinutes(90));
        else if (!TryParseTime(endRaw, out endTs))
            return Task.FromResult($"Error: '{endRaw}' is not a valid end time. Use HH:mm (e.g. '16:00').");

        var startStr = FormatTime(startTs);
        var endStr   = FormatTime(endTs);

        var courses = CourseRepository.GetAll().ToList();
        var course = courses.FirstOrDefault(c => c.Name.Equals(courseName, StringComparison.OrdinalIgnoreCase))
                  ?? courses.FirstOrDefault(c => c.Name.Contains(courseName, StringComparison.OrdinalIgnoreCase));

        if (course == null)
        {
            var names = courses.Count > 0 ? string.Join(", ", courses.Select(c => c.Name)) : "none";
            return Task.FromResult($"Error: No course found matching '{courseName}'. Available courses: {names}");
        }

        if (!string.IsNullOrWhiteSpace(room))
        {
            var occupied = ScheduleRepository.GetOccupiedRooms(normalizedDay, startStr, endStr);
            if (occupied.Any(r => r.Equals(room, StringComparison.OrdinalIgnoreCase)))
                return Task.FromResult(
                    $"Error: Room '{room}' is already occupied on {normalizedDay} between {startStr} and {endStr}. " +
                    "Please choose a different room or time.");
        }

        ScheduleRepository.Insert(new Schedule
        {
            CourseId    = course.Id,
            DayOfWeek   = normalizedDay,
            StartTime   = startStr,
            EndTime     = endStr,
            Room        = room,
            AddedByMilo = true
        });

        var roomPart = string.IsNullOrWhiteSpace(room) ? "" : $" in room {room}";
        return Task.FromResult($"Success: Added '{course.Name}' on {normalizedDay} from {startStr} to {endStr}{roomPart}.");
    }

    private static Task<string> ExportStudentsAsync(JsonNode? args)
    {
        var path = IsPdf(args)
            ? PdfExportService.ExportAllStudents()
            : ExcelExportService.ExportAllStudents();
        OpenFile(path);
        return Task.FromResult($"Success: Exported all students to '{Path.GetFileName(path)}'.");
    }

    private static Task<string> ExportCoursesAsync(JsonNode? args)
    {
        var path = IsPdf(args)
            ? PdfExportService.ExportCourses()
            : ExcelExportService.ExportCourses();
        OpenFile(path);
        return Task.FromResult($"Success: Exported courses to '{Path.GetFileName(path)}'.");
    }

    private static Task<string> ExportClassListsAsync(JsonNode? args)
    {
        var path = IsPdf(args)
            ? PdfExportService.ExportClassLists()
            : ExcelExportService.ExportClassLists();
        OpenFile(path);
        return Task.FromResult($"Success: Exported all class lists to '{Path.GetFileName(path)}'.");
    }

    private static Task<string> ExportClassListAsync(JsonNode? args)
    {
        var courseName = args?["course_name"]?.GetValue<string>() ?? "";
        if (string.IsNullOrWhiteSpace(courseName))
            return Task.FromResult("Error: course_name is required.");

        var course = FindCourse(courseName);
        if (course == null)
        {
            var names = CourseRepository.GetAll().Select(c => c.Name).ToList();
            var available = names.Count > 0 ? string.Join(", ", names) : "none";
            return Task.FromResult($"Error: No course matching '{courseName}'. Available courses: {available}");
        }

        var path = IsPdf(args)
            ? PdfExportService.ExportClassList(course)
            : ExcelExportService.ExportClassList(course);
        OpenFile(path);
        return Task.FromResult($"Success: Exported class list for '{course.Name}' to '{Path.GetFileName(path)}'.");
    }

    private static Task<string> ExportScheduleAsync(JsonNode? args)
    {
        var path = IsPdf(args)
            ? PdfExportService.ExportSchedule()
            : ExcelExportService.ExportSchedule();
        OpenFile(path);
        return Task.FromResult($"Success: Exported schedule to '{Path.GetFileName(path)}'.");
    }

    private static Task<string> ExportGradeSheetAsync(JsonNode? args)
    {
        var courseName = args?["course_name"]?.GetValue<string>() ?? "";
        if (string.IsNullOrWhiteSpace(courseName))
            return Task.FromResult("Error: course_name is required.");

        var course = FindCourse(courseName);
        if (course == null)
        {
            var names = CourseRepository.GetAll().Select(c => c.Name).ToList();
            var available = names.Count > 0 ? string.Join(", ", names) : "none";
            return Task.FromResult($"Error: No course matching '{courseName}'. Available courses: {available}");
        }

        var path = IsPdf(args)
            ? PdfExportService.ExportGradeSheet(course)
            : ExcelExportService.ExportGradeSheet(course);
        OpenFile(path);
        return Task.FromResult($"Success: Exported grade sheet for '{course.Name}' to '{Path.GetFileName(path)}'.");
    }

    private static Task<string> ExportTranscriptAsync(JsonNode? args)
    {
        var studentName = args?["student_name"]?.GetValue<string>() ?? "";
        if (string.IsNullOrWhiteSpace(studentName))
            return Task.FromResult("Error: student_name is required.");

        var students = StudentRepository.GetAll().ToList();
        var student = students.FirstOrDefault(s =>
                $"{s.FirstName} {s.LastName}".Equals(studentName, StringComparison.OrdinalIgnoreCase))
            ?? students.FirstOrDefault(s =>
                $"{s.FirstName} {s.LastName}".Contains(studentName, StringComparison.OrdinalIgnoreCase)
                || s.FirstName.Contains(studentName, StringComparison.OrdinalIgnoreCase)
                || s.LastName.Contains(studentName, StringComparison.OrdinalIgnoreCase));

        if (student == null)
            return Task.FromResult($"Error: No student found matching '{studentName}'.");

        var grades = GradeRepository.GetByStudent(student.Id).ToList();
        var path = IsPdf(args)
            ? PdfExportService.ExportTranscript(student, grades)
            : ExcelExportService.ExportTranscript(student, grades);
        OpenFile(path);
        return Task.FromResult($"Success: Exported transcript for {student.FirstName} {student.LastName} to '{Path.GetFileName(path)}'.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool IsPdf(JsonNode? args) =>
        (args?["format"]?.GetValue<string>() ?? "").Trim().Equals("pdf", StringComparison.OrdinalIgnoreCase);

    private static Course? FindCourse(string name)
    {
        var courses = CourseRepository.GetAll().ToList();
        return courses.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?? courses.FirstOrDefault(c => c.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
    }

    private static void OpenFile(string path)
    {
        try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
        catch { /* silently ignore if the OS can't open the file */ }
    }

    private static bool TryParseTime(string input, out TimeSpan result)
    {
        if (TimeSpan.TryParse(input, out result)) return true;

        var s  = input.Trim().ToLowerInvariant().Replace(" ", "");
        bool pm = s.EndsWith("pm");
        bool am = s.EndsWith("am");
        if (!pm && !am) return false;

        s = s[..^2];
        if (!TimeSpan.TryParse(s.Contains(':') ? s : s + ":00", out result)) return false;

        if (pm && result.Hours < 12) result = result.Add(TimeSpan.FromHours(12));
        if (am && result.Hours == 12) result = result.Subtract(TimeSpan.FromHours(12));
        return true;
    }

    private static string FormatTime(TimeSpan t) => $"{t.Hours:D2}:{t.Minutes:D2}";
}
