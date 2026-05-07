using AfterSchool.Data;
using AfterSchool.Models;
using AfterSchool.Services;
using AfterSchool.UI;

namespace AfterSchool.Forms;

public class DashboardControl : UserControl
{
    private static readonly Color AccentBlue = Color.FromArgb(37, 99, 235);
    private static readonly Color AccentPurple = Color.FromArgb(124, 58, 237);
    private static readonly Color AccentAmber = Color.FromArgb(217, 119, 6);
    private static readonly Color AccentGreen = Color.FromArgb(22, 163, 74);

    public DashboardControl()
    {
        BackColor = Theme.Background;
        AutoScroll = true;
        BuildLayout();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        BeginInvoke(() => AutoScrollPosition = new Point(0, 0));
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Theme.Background,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddRow(root, BuildWelcome(), 78);
        AddRow(root, BuildStatsRow(), 136);
        AddRow(root, BuildMidRow(), 360);
        AddRow(root, BuildRecentRegistrations(), 300);

        Controls.Add(root);
    }

    private static void AddRow(TableLayoutPanel table, Control child, int height)
    {
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, height + 20));
        child.Dock = DockStyle.Fill;
        child.Margin = new Padding(0, 0, 0, 20);
        table.Controls.Add(child, 0, table.RowCount);
        table.RowCount++;
    }

    // ---------------- Welcome ----------------

    private Control BuildWelcome()
    {
        var panel = new Panel { BackColor = Theme.Background };

        var greeting = new Label
        {
            Text = string.Format(Loc.T("dashboard.greeting"), Session.DisplayName),
            Font = new Font("Segoe UI Semibold", 22f),
            ForeColor = Theme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 42,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var sub = new Label
        {
            Text = DateTime.Today.ToString("dddd, MMMM d, yyyy", Loc.Culture),
            Font = Theme.BodyFont,
            ForeColor = Theme.TextSecondary,
            Dock = DockStyle.Top,
            Height = 24,
            TextAlign = ContentAlignment.MiddleLeft
        };

        panel.Controls.Add(sub);
        panel.Controls.Add(greeting);
        return panel;
    }

    // ---------------- Stats ----------------

    private Control BuildStatsRow()
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = Theme.Background
        };
        for (int i = 0; i < 4; i++)
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var students = StudentRepository.GetAll().ToList();
        var activeStudents = students.Count(s =>
            string.Equals(s.Status, "Active", StringComparison.OrdinalIgnoreCase));
        var courses = CourseRepository.GetAll().ToList();
        var teachersCount = UserRepository.GetByRole("Teacher").Count();
        var today = DateTime.Today.DayOfWeek.ToString();
        var schedule = ScheduleRepository.GetAll().ToList();
        var todaysClasses = schedule.Count(s =>
            string.Equals(s.DayOfWeek, today, StringComparison.OrdinalIgnoreCase));
        var roomsCount = RoomRepository.GetAll().Count();

        var teachersSub = string.Format(
            teachersCount == 1
                ? Loc.T("dashboard.stat.teachers.sub")
                : Loc.T("dashboard.stat.teachers.sub.plural"),
            teachersCount);

        grid.Controls.Add(BuildStatCard(
            Loc.T("dashboard.stat.students"), students.Count.ToString(),
            string.Format(Loc.T("dashboard.stat.students.sub"), activeStudents),
            AccentBlue, marginRight: 16), 0, 0);
        grid.Controls.Add(BuildStatCard(
            Loc.T("dashboard.stat.courses"), courses.Count.ToString(),
            teachersSub,
            AccentPurple, marginRight: 16), 1, 0);
        grid.Controls.Add(BuildStatCard(
            Loc.T("dashboard.stat.todayclasses"), todaysClasses.ToString(),
            DateTime.Today.ToString("dddd", Loc.Culture),
            AccentAmber, marginRight: 16), 2, 0);
        grid.Controls.Add(BuildStatCard(
            Loc.T("dashboard.stat.classrooms"), roomsCount.ToString(),
            Loc.T("dashboard.stat.classrooms.sub"),
            AccentGreen, marginRight: 0), 3, 0);

        return grid;
    }

    private Panel BuildStatCard(string label, string value, string sub, Color accent, int marginRight)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Surface,
            Margin = new Padding(0, 0, marginRight, 0),
            Padding = new Padding(22, 18, 22, 18)
        };
        card.Paint += (_, e) =>
        {
            using var border = new Pen(Theme.Border);
            e.Graphics.DrawRectangle(border, 0, 0, card.Width - 1, card.Height - 1);
            using var bar = new SolidBrush(accent);
            e.Graphics.FillRectangle(bar, 0, 0, 4, card.Height);
        };

        var subLbl = new Label
        {
            Text = sub,
            Font = Theme.SmallFont,
            ForeColor = Theme.TextSecondary,
            Dock = DockStyle.Top,
            Height = 20,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var valueLbl = new Label
        {
            Text = value,
            Font = new Font("Segoe UI Semibold", 26f),
            ForeColor = Theme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 46,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var labelLbl = new Label
        {
            Text = label.ToUpperInvariant(),
            Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Regular),
            ForeColor = Theme.TextSecondary,
            Dock = DockStyle.Top,
            Height = 20,
            TextAlign = ContentAlignment.MiddleLeft
        };

        card.Controls.Add(subLbl);
        card.Controls.Add(valueLbl);
        card.Controls.Add(labelLbl);
        return card;
    }

    // ---------------- Mid row ----------------

    private Control BuildMidRow()
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Theme.Background
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var today = BuildTodayScheduleCard();
        today.Margin = new Padding(0, 0, 10, 0);
        grid.Controls.Add(today, 0, 0);

        var top = BuildTopCoursesCard();
        top.Margin = new Padding(10, 0, 0, 0);
        grid.Controls.Add(top, 1, 0);

        return grid;
    }

    private Panel BuildTodayScheduleCard()
    {
        var today = DateTime.Today.DayOfWeek.ToString();
        var slots = ScheduleRepository.GetAll()
            .Where(s => string.Equals(s.DayOfWeek, today, StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => s.StartTime)
            .ToList();

        var body = new Panel
        {
            BackColor = Theme.Surface,
            AutoScroll = true,
            Padding = new Padding(20, 4, 20, 16)
        };

        if (slots.Count == 0)
        {
            var emptyText = today == "Sunday"
                ? Loc.T("dashboard.schedule.nosunday")
                : string.Format(Loc.T("dashboard.schedule.noclasses"),
                    DateTime.Today.ToString("dddd", Loc.Culture));
            var empty = new Label
            {
                Text = emptyText,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = Theme.BodyFont,
                ForeColor = Theme.TextSecondary
            };
            body.Controls.Add(empty);
        }
        else
        {
            foreach (var slot in slots.AsEnumerable().Reverse())
                body.Controls.Add(BuildScheduleRow(slot));
        }

        var sub = string.Format(
            slots.Count == 1
                ? Loc.T("dashboard.card.todayschedule.sub")
                : Loc.T("dashboard.card.todayschedule.sub.plural"),
            slots.Count);
        return BuildCard(Loc.T("dashboard.card.todayschedule"), sub, body);
    }

    private Panel BuildScheduleRow(ScheduleView s)
    {
        var row = new Panel
        {
            Dock = DockStyle.Top,
            Height = 56,
            Margin = new Padding(0, 0, 0, 8),
            BackColor = Color.FromArgb(239, 246, 255),
            Padding = new Padding(14, 9, 14, 9)
        };
        row.Paint += (_, e) =>
        {
            using var bar = new SolidBrush(Theme.Primary);
            e.Graphics.FillRectangle(bar, 0, 0, 3, row.Height);
        };

        var timeLine = new Label
        {
            Text = $"{s.StartTime} – {s.EndTime}   ·   {s.CourseName}",
            Font = new Font("Segoe UI Semibold", 9.75f),
            ForeColor = Theme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 20
        };
        var meta = new Label
        {
            Text = string.IsNullOrWhiteSpace(s.Room)
                ? string.IsNullOrWhiteSpace(s.Teacher) ? "" : s.Teacher
                : string.IsNullOrWhiteSpace(s.Teacher) ? s.Room : $"{s.Teacher}  ·  {s.Room}",
            Font = Theme.SmallFont,
            ForeColor = Theme.TextSecondary,
            Dock = DockStyle.Top,
            Height = 18
        };

        row.Controls.Add(meta);
        row.Controls.Add(timeLine);
        return row;
    }

    private Panel BuildTopCoursesCard()
    {
        var courses = CourseRepository.GetAll()
            .Select(c => new { Course = c, Enrolled = CourseRepository.GetEnrolledCount(c.Id) })
            .OrderByDescending(x => x.Enrolled)
            .ThenBy(x => x.Course.Name)
            .Take(6)
            .ToList();

        var body = new Panel
        {
            BackColor = Theme.Surface,
            AutoScroll = true,
            Padding = new Padding(20, 6, 20, 16)
        };

        if (courses.Count == 0)
        {
            var empty = new Label
            {
                Text = Loc.T("dashboard.courses.empty"),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = Theme.BodyFont,
                ForeColor = Theme.TextSecondary
            };
            body.Controls.Add(empty);
        }
        else
        {
            foreach (var c in courses.AsEnumerable().Reverse())
                body.Controls.Add(BuildCourseBar(c.Course.Name, c.Enrolled, c.Course.Capacity));
        }

        var total = courses.Sum(c => c.Enrolled);
        var sub = string.Format(
            courses.Count == 1
                ? Loc.T("dashboard.card.topcourses.sub")
                : Loc.T("dashboard.card.topcourses.sub.plural"),
            total, courses.Count);
        return BuildCard(Loc.T("dashboard.card.topcourses"), sub, body);
    }

    private Panel BuildCourseBar(string name, int enrolled, int capacity)
    {
        var row = new Panel
        {
            Dock = DockStyle.Top,
            Height = 56,
            Margin = new Padding(0, 0, 0, 10),
            BackColor = Theme.Surface
        };

        var nameLbl = new Label
        {
            Text = name,
            Font = new Font("Segoe UI Semibold", 9.75f),
            ForeColor = Theme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 20,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var countText = capacity > 0
            ? string.Format(Loc.T("dashboard.course.enrolled"), enrolled, capacity)
            : string.Format(Loc.T("dashboard.course.enrolled.nolimit"), enrolled);
        var countLbl = new Label
        {
            Text = countText,
            Font = Theme.SmallFont,
            ForeColor = Theme.TextSecondary,
            Dock = DockStyle.Top,
            Height = 16,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var track = new Panel
        {
            Dock = DockStyle.Top,
            Height = 10,
            Margin = new Padding(0, 4, 0, 0),
            BackColor = Color.FromArgb(226, 232, 240)
        };
        track.Paint += (_, e) =>
        {
            if (enrolled <= 0) return;
            var ratio = capacity > 0 ? Math.Min(1.0, (double)enrolled / capacity) : 1.0;
            var filled = (int)(track.Width * ratio);
            var color = ratio >= 1.0 ? AccentAmber
                      : ratio >= 0.8 ? Theme.Primary
                      : AccentGreen;
            using var brush = new SolidBrush(color);
            e.Graphics.FillRectangle(brush, 0, 0, filled, track.Height);
        };

        row.Controls.Add(track);
        row.Controls.Add(countLbl);
        row.Controls.Add(nameLbl);
        return row;
    }

    // ---------------- Recent registrations ----------------

    private Control BuildRecentRegistrations()
    {
        var students = StudentRepository.GetAll()
            .OrderByDescending(s => s.RegisterDate)
            .ThenByDescending(s => s.Id)
            .Take(8)
            .ToList();

        var body = new Panel { BackColor = Theme.Surface, Padding = new Padding(20, 4, 20, 16) };

        if (students.Count == 0)
        {
            var empty = new Label
            {
                Text = Loc.T("dashboard.students.empty"),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = Theme.BodyFont,
                ForeColor = Theme.TextSecondary
            };
            body.Controls.Add(empty);
        }
        else
        {
            var grid = new DataGridView();
            Theme.StyleGrid(grid);
            grid.Dock = DockStyle.Fill;
            grid.TabStop = false;
            grid.DataSource = students.Select(s => new
            {
                Name = $"{s.LastName}, {s.FirstName}",
                s.Email,
                Course = s.CourseName ?? "—",
                Registered = s.RegisterDate,
                s.Status
            }).ToList();
            body.Controls.Add(grid);
        }

        var sub = string.Format(
            students.Count == 1
                ? Loc.T("dashboard.card.recentreg.sub")
                : Loc.T("dashboard.card.recentreg.sub.plural"),
            students.Count);
        return BuildCard(Loc.T("dashboard.card.recentreg"), sub, body);
    }

    // ---------------- Shared card shell ----------------

    private Panel BuildCard(string title, string subtitle, Control body)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Surface
        };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = Theme.Surface,
            Padding = new Padding(20, 16, 20, 0)
        };
        var sub = new Label
        {
            Text = subtitle,
            Font = Theme.SmallFont,
            ForeColor = Theme.TextSecondary,
            Dock = DockStyle.Top,
            Height = 16,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var titleLbl = new Label
        {
            Text = title,
            Font = new Font("Segoe UI Semibold", 12f),
            ForeColor = Theme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 24,
            TextAlign = ContentAlignment.MiddleLeft
        };
        header.Controls.Add(sub);
        header.Controls.Add(titleLbl);

        body.Dock = DockStyle.Fill;
        card.Controls.Add(body);
        card.Controls.Add(header);
        return card;
    }
}
