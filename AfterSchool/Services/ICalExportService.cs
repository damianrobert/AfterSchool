using AfterSchool.Data;

namespace AfterSchool.Services;

public static class ICalExportService
{
    private static readonly Dictionary<string, string> DayToByday = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Monday"]    = "MO",
        ["Tuesday"]   = "TU",
        ["Wednesday"] = "WE",
        ["Thursday"]  = "TH",
        ["Friday"]    = "FR",
        ["Saturday"]  = "SA",
        ["Sunday"]    = "SU",
    };

    private static readonly Dictionary<string, DayOfWeek> DayToEnum = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Monday"]    = DayOfWeek.Monday,
        ["Tuesday"]   = DayOfWeek.Tuesday,
        ["Wednesday"] = DayOfWeek.Wednesday,
        ["Thursday"]  = DayOfWeek.Thursday,
        ["Friday"]    = DayOfWeek.Friday,
        ["Saturday"]  = DayOfWeek.Saturday,
        ["Sunday"]    = DayOfWeek.Sunday,
    };

    public static string ExportSchedule()
    {
        var slots = ScheduleRepository.GetAll().ToList();
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("BEGIN:VCALENDAR");
        sb.AppendLine("VERSION:2.0");
        sb.AppendLine("PRODID:-//AfterSchool//AfterSchool Management//EN");
        sb.AppendLine("CALSCALE:GREGORIAN");
        sb.AppendLine("METHOD:PUBLISH");

        var today = DateTime.Today;

        foreach (var slot in slots)
        {
            if (!DayToEnum.TryGetValue(slot.DayOfWeek, out var dow)) continue;
            if (!TimeSpan.TryParse(slot.StartTime, out var startTs)) continue;
            if (!TimeSpan.TryParse(slot.EndTime, out var endTs)) continue;

            int daysUntil = ((int)dow - (int)today.DayOfWeek + 7) % 7;
            var firstDate = today.AddDays(daysUntil);
            var dtStart   = firstDate + startTs;
            var dtEnd     = firstDate + endTs;
            var byday     = DayToByday[slot.DayOfWeek];

            var descParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(slot.Teacher)) descParts.Add($"Teacher: {slot.Teacher}");
            if (!string.IsNullOrWhiteSpace(slot.Room))    descParts.Add($"Room: {slot.Room}");

            sb.AppendLine("BEGIN:VEVENT");
            sb.AppendLine($"UID:slot-{slot.Id}@afterschool");
            sb.AppendLine($"DTSTART:{dtStart:yyyyMMdd'T'HHmmss}");
            sb.AppendLine($"DTEND:{dtEnd:yyyyMMdd'T'HHmmss}");
            sb.AppendLine($"RRULE:FREQ=WEEKLY;BYDAY={byday}");
            sb.AppendLine($"SUMMARY:{EscapeIcal(slot.CourseName)}");
            if (descParts.Count > 0)
                sb.AppendLine($"DESCRIPTION:{EscapeIcal(string.Join("\\n", descParts))}");
            if (!string.IsNullOrWhiteSpace(slot.Room))
                sb.AppendLine($"LOCATION:{EscapeIcal(slot.Room)}");
            sb.AppendLine("END:VEVENT");
        }

        sb.AppendLine("END:VCALENDAR");

        var file = UniqueIcsName("AfterSchool_Schedule");
        File.WriteAllText(file, sb.ToString(), new System.Text.UTF8Encoding(false));
        return file;
    }

    private static string EscapeIcal(string s) =>
        s.Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,").Replace("\n", "\\n");

    private static string UniqueIcsName(string baseName)
    {
        var dir   = ExcelExportService.DefaultExportDirectory();
        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var clean = string.Concat(baseName.Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
        return Path.Combine(dir, $"{clean}_{stamp}.ics");
    }
}
