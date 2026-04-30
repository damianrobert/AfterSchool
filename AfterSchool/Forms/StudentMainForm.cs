using AfterSchool.Data;
using AfterSchool.Models;
using AfterSchool.Services;
using AfterSchool.UI;

namespace AfterSchool.Forms;

// ── Student portal shell ──────────────────────────────────────────────────────

public class StudentMainForm : Form
{
    private readonly Panel            _content    = new();
    private readonly Label            _titleLabel = new();
    private readonly List<NavButton>  _navButtons = new();
    private UserControl?              _currentView;
    private string                    _activeTitleKey = "nav.student.overview.title";

    public StudentMainForm()
    {
        Text = "AfterSchool — Student Portal";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1000, 660);
        Size = new Size(1200, 760);
        BackColor = Theme.Background;
        Font = Theme.BodyFont;
        Icon = SystemIcons.Application;

        BuildLayout();

        Loc.LanguageChanged += OnLanguageChanged;
        FormClosed += (_, _) => Loc.LanguageChanged -= OnLanguageChanged;
    }

    private void BuildLayout()
    {
        // ── Sidebar ──────────────────────────────────────────────────────────
        var sidebar = new Panel { Dock = DockStyle.Left, Width = 220, BackColor = Theme.Sidebar };

        var brand = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Color.FromArgb(15, 23, 42) };
        brand.Paint += (_, e) =>
        {
            using var titleFont = new Font("Segoe UI Semibold", 14f);
            using var subFont   = new Font("Segoe UI", 8.5f);
            e.Graphics.DrawString("AfterSchool",    titleFont, Brushes.White,                             new PointF(20, 14));
            e.Graphics.DrawString("Student Portal", subFont,   new SolidBrush(Color.FromArgb(148,163,184)), new PointF(21, 38));
        };

        var navStack = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = false,
            Padding = new Padding(10, 16, 10, 10),
            BackColor = Theme.Sidebar
        };

        var navDefs = new[]
        {
            ("nav.student.overview", "nav.student.overview.title", (Func<UserControl>)(() => new StudentOverviewControl())),
            ("nav.student.schedule", "nav.student.schedule.title", (Func<UserControl>)(() => new StudentScheduleControl())),
            ("nav.student.grades",   "nav.student.grades.title",   (Func<UserControl>)(() => new StudentGradesControl())),
            ("nav.student.assignments", "nav.student.assignments.title", (Func<UserControl>)(() => new StudentAssignmentsControl())),
            ("nav.student.files",      "nav.student.files.title",       (Func<UserControl>)(() => new StudentFilesControl())),
        };

        foreach (var (labelKey, titleKey, factory) in navDefs)
        {
            var btn = new NavButton(labelKey, titleKey, factory);
            btn.Clicked += OnNavClicked;
            _navButtons.Add(btn);
            navStack.Controls.Add(btn);
        }

        sidebar.Controls.Add(navStack);
        sidebar.Controls.Add(BuildFooter());
        sidebar.Controls.Add(brand);

        // ── Top bar ──────────────────────────────────────────────────────────
        var topBar = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Theme.Surface };
        topBar.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawLine(pen, 0, topBar.Height - 1, topBar.Width, topBar.Height - 1);
        };

        _titleLabel.Font = Theme.TitleFont;
        _titleLabel.ForeColor = Theme.TextPrimary;
        _titleLabel.Dock = DockStyle.Fill;
        _titleLabel.TextAlign = ContentAlignment.MiddleLeft;
        _titleLabel.Padding = new Padding(24, 0, 0, 0);
        _titleLabel.Text = Loc.T(_activeTitleKey);

        var userChip = new Label
        {
            Text = $"{Loc.T("main.signedin")} {Session.DisplayName}",
            Font = Theme.SmallFont,
            ForeColor = Theme.TextSecondary,
            Dock = DockStyle.Right,
            AutoSize = false,
            Width = 240,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 0, 16, 0)
        };

        topBar.Controls.Add(_titleLabel);
        topBar.Controls.Add(userChip);

        // ── Content area ─────────────────────────────────────────────────────
        _content.Dock = DockStyle.Fill;
        _content.BackColor = Theme.Background;
        _content.Padding = new Padding(24);

        Controls.Add(_content);
        Controls.Add(topBar);
        Controls.Add(sidebar);

        // Activate first nav button
        _navButtons[0].PerformClick();
    }

    private Panel BuildFooter()
    {
        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 80,
            BackColor = Color.FromArgb(15, 23, 42),
            Padding = new Padding(16, 12, 16, 12)
        };

        var name = new Label
        {
            Text = Session.DisplayName,
            Font = new Font("Segoe UI Semibold", 10f),
            ForeColor = Color.White,
            Dock = DockStyle.Top,
            Height = 22
        };
        var role = new Label
        {
            Text = "Student",
            Font = Theme.SmallFont,
            ForeColor = Color.FromArgb(148, 163, 184),
            Dock = DockStyle.Top,
            Height = 18
        };
        var signOut = new Button { Text = Loc.T("main.signout"), Dock = DockStyle.Bottom, Height = 30 };
        Theme.StyleButton(signOut);
        signOut.BackColor = Color.FromArgb(51, 65, 85);
        signOut.ForeColor = Color.FromArgb(203, 213, 225);
        signOut.FlatAppearance.BorderSize = 0;
        signOut.FlatAppearance.MouseOverBackColor = Color.FromArgb(71, 85, 105);
        signOut.Click += (_, _) =>
        {
            Session.SignOutRequested = true;
            Close();
        };

        footer.Controls.Add(signOut);
        footer.Controls.Add(role);
        footer.Controls.Add(name);
        return footer;
    }

    private void OnNavClicked(NavButton sender)
    {
        foreach (var b in _navButtons) b.IsActive = false;
        sender.IsActive = true;

        _activeTitleKey = sender.TitleKey;
        _titleLabel.Text = Loc.T(_activeTitleKey);

        SwapView(sender.CreateView());
    }

    private void SwapView(UserControl next)
    {
        _currentView?.Dispose();
        next.Dock = DockStyle.Fill;
        _content.Controls.Clear();
        _content.Controls.Add(next);
        _currentView = next;
    }

    private void OnLanguageChanged()
    {
        foreach (var b in _navButtons) b.UpdateLabel();
        _titleLabel.Text = Loc.T(_activeTitleKey);
        var active = _navButtons.FirstOrDefault(b => b.IsActive);
        if (active != null) SwapView(active.CreateView());
    }

    // ── NavButton ─────────────────────────────────────────────────────────────

    private sealed class NavButton : Panel
    {
        public string LabelKey  { get; }
        public string TitleKey  { get; }
        public Func<UserControl> CreateView { get; }

        public event Action<NavButton>? Clicked;

        private readonly Label _lbl = new();
        private bool _isActive;

        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; Refresh(); }
        }

        public NavButton(string labelKey, string titleKey, Func<UserControl> factory)
        {
            LabelKey   = labelKey;
            TitleKey   = titleKey;
            CreateView = factory;

            Width = 200;
            Height = 42;
            Cursor = Cursors.Hand;
            Margin = new Padding(0, 2, 0, 2);

            _lbl.Text = Loc.T(labelKey);
            _lbl.Font = new Font("Segoe UI", 10.5f);
            _lbl.ForeColor = Theme.SidebarText;
            _lbl.Dock = DockStyle.Fill;
            _lbl.TextAlign = ContentAlignment.MiddleLeft;
            _lbl.Padding = new Padding(14, 0, 0, 0);
            _lbl.MouseClick += (_, e) => OnMouseClick(e);
            Controls.Add(_lbl);

            Paint += (_, e) =>
            {
                if (_isActive)
                {
                    e.Graphics.FillRectangle(new SolidBrush(Theme.SidebarActive), ClientRectangle);
                    _lbl.ForeColor = Color.White;
                }
                else
                {
                    e.Graphics.FillRectangle(new SolidBrush(Theme.Sidebar), ClientRectangle);
                    _lbl.ForeColor = Theme.SidebarText;
                }
            };
        }

        public void UpdateLabel() => _lbl.Text = Loc.T(LabelKey);

        public void PerformClick() => Clicked?.Invoke(this);

        protected override void OnMouseClick(MouseEventArgs e) { base.OnMouseClick(e); Clicked?.Invoke(this); }
    }
}

// ── Student Overview ──────────────────────────────────────────────────────────

internal class StudentOverviewControl : UserControl
{
    public StudentOverviewControl()
    {
        BackColor = Theme.Background;
        BuildLayout();
    }

    private void BuildLayout()
    {
        var studentId = Session.Current?.StudentId;
        var student   = studentId.HasValue ? StudentRepository.GetById(studentId.Value) : null;
        Course? course = null;
        if (student?.EnrolledCourseId.HasValue == true)
            course = CourseRepository.GetById(student.EnrolledCourseId!.Value);

        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Theme.Background };

        var stack = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Dock = DockStyle.Top,
            BackColor = Theme.Background,
            Padding = new Padding(0, 0, 0, 24)
        };

        // Greeting card
        var greeting = Card(580, 100);
        var greetLbl = new Label
        {
            Text = string.Format(Loc.T("student.overview.greeting"),
                string.IsNullOrWhiteSpace(Session.Current?.FullName)
                    ? Session.Current?.Username
                    : Session.Current?.FullName),
            Font = new Font("Segoe UI Semibold", 22f),
            ForeColor = Theme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 46
        };
        var greetSub = new Label
        {
            Text = course != null
                ? $"{Loc.T("student.overview.enrolled_in")}: {course.Name}"
                : Loc.T("student.overview.not_enrolled"),
            Font = Theme.BodyFont,
            ForeColor = Theme.TextSecondary,
            Dock = DockStyle.Top,
            Height = 24
        };
        greeting.Controls.Add(greetSub);
        greeting.Controls.Add(greetLbl);
        stack.Controls.Add(greeting);

        // Course card
        if (course != null)
        {
            var courseCard = Card(580, 120);
            courseCard.Controls.Add(InfoRow(Loc.T("student.overview.teacher"), course.Teacher.Length > 0 ? course.Teacher : "—"));
            if (!string.IsNullOrWhiteSpace(course.Description))
                courseCard.Controls.Add(InfoRow("Description", course.Description));
            courseCard.Controls.Add(SectionHeader(course.Name));
            stack.Controls.Add(courseCard);

            // Today's schedule
            var today = DateTime.Today.DayOfWeek.ToString();
            var slots = ScheduleRepository.GetForCourse(course.Id)
                            .Where(s => s.DayOfWeek.Equals(today, StringComparison.OrdinalIgnoreCase))
                            .ToList();

            var todayCard = Card(580, slots.Count > 0 ? 60 + slots.Count * 36 : 80);
            todayCard.Controls.Add(SectionHeader(Loc.T("student.overview.today")));
            if (slots.Count == 0)
            {
                todayCard.Controls.Add(InfoRow("", Loc.T("student.overview.no_today")));
            }
            else
            {
                foreach (var s in slots)
                    todayCard.Controls.Add(InfoRow($"{s.StartTime}–{s.EndTime}", s.Room));
            }
            stack.Controls.Add(todayCard);
        }

        scroll.Controls.Add(stack);
        Controls.Add(scroll);
    }

    private static Panel Card(int width, int height)
    {
        var p = new Panel
        {
            Width = width,
            Height = height,
            BackColor = Theme.Surface,
            Padding = new Padding(20, 14, 20, 14),
            Margin = new Padding(0, 0, 0, 16)
        };
        p.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        };
        return p;
    }

    private static Label SectionHeader(string text) => new()
    {
        Text = text,
        Font = new Font("Segoe UI Semibold", 12f),
        ForeColor = Theme.TextPrimary,
        Dock = DockStyle.Top,
        Height = 30
    };

    private static Label InfoRow(string label, string value) => new()
    {
        Text = label.Length > 0 ? $"{label}:  {value}" : value,
        Font = Theme.BodyFont,
        ForeColor = Theme.TextSecondary,
        Dock = DockStyle.Top,
        Height = 24
    };
}

// ── Student Schedule ──────────────────────────────────────────────────────────

internal class StudentScheduleControl : UserControl
{
    public StudentScheduleControl()
    {
        BackColor = Theme.Background;
        BuildLayout();
    }

    private void BuildLayout()
    {
        var studentId = Session.Current?.StudentId;
        var student   = studentId.HasValue ? StudentRepository.GetById(studentId.Value) : null;

        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Surface,
            Padding = new Padding(20)
        };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        if (student?.EnrolledCourseId == null)
        {
            card.Controls.Add(CenterLabel(Loc.T("student.schedule.no_course")));
            Controls.Add(card);
            return;
        }

        var slots = ScheduleRepository.GetForCourse(student.EnrolledCourseId.Value).ToList();

        if (slots.Count == 0)
        {
            card.Controls.Add(CenterLabel(Loc.T("student.schedule.no_slots")));
            Controls.Add(card);
            return;
        }

        var courseName = CourseRepository.GetById(student.EnrolledCourseId.Value)?.Name ?? "—";
        var dayOrder = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };

        var rows = slots
            .OrderBy(s => Array.IndexOf(dayOrder, s.DayOfWeek))
            .ThenBy(s => s.StartTime)
            .Select(s => new
            {
                Course = courseName,
                Day    = Loc.T($"days.{s.DayOfWeek.ToLower()}"),
                Start  = s.StartTime,
                End    = s.EndTime,
                Room   = s.Room
            }).ToList();

        var grid = new DataGridView { Dock = DockStyle.Fill };
        Theme.StyleGrid(grid);
        grid.DataSource = rows;

        if (grid.Columns["Course"] is { } co) co.HeaderText = Loc.T("student.schedule.col.course");
        if (grid.Columns["Day"]    is { } dc) dc.HeaderText = Loc.T("student.schedule.col.day");
        if (grid.Columns["Start"]  is { } sc) sc.HeaderText = Loc.T("student.schedule.col.start");
        if (grid.Columns["End"]    is { } ec) ec.HeaderText = Loc.T("student.schedule.col.end");
        if (grid.Columns["Room"]   is { } rc) rc.HeaderText = Loc.T("student.schedule.col.room");

        card.Controls.Add(grid);
        Controls.Add(card);
    }

    private static Label CenterLabel(string text) => new()
    {
        Text = text,
        Font = Theme.BodyFont,
        ForeColor = Theme.TextSecondary,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleCenter
    };
}

// ── Student Grades ────────────────────────────────────────────────────────────

internal class StudentGradesControl : UserControl
{
    public StudentGradesControl()
    {
        BackColor = Theme.Background;
        BuildLayout();
    }

    private void BuildLayout()
    {
        var studentId = Session.Current?.StudentId;

        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Surface,
            Padding = new Padding(20)
        };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        if (!studentId.HasValue)
        {
            card.Controls.Add(CenterLabel(Loc.T("student.grades.no_course")));
            Controls.Add(card);
            return;
        }

        var grades = GradeRepository.GetByStudent(studentId.Value).ToList();

        if (grades.Count == 0)
        {
            card.Controls.Add(CenterLabel(Loc.T("student.grades.no_grades")));
            Controls.Add(card);
            return;
        }

        var rows = grades.Select(g => new
        {
            Course    = g.CourseName,
            Score     = g.Score,
            Notes     = g.Notes,
            Date      = g.GradedDate,
            GradedBy  = g.GradedBy
        }).ToList();

        var grid = new DataGridView { Dock = DockStyle.Fill };
        Theme.StyleGrid(grid);
        grid.DataSource = rows;

        if (grid.Columns["Course"]   is { } cc) cc.HeaderText = Loc.T("student.grades.col.course");
        if (grid.Columns["Score"]    is { } sc) sc.HeaderText = Loc.T("student.grades.col.score");
        if (grid.Columns["Notes"]    is { } nc) nc.HeaderText = Loc.T("student.grades.col.notes");
        if (grid.Columns["Date"]     is { } dc) dc.HeaderText = Loc.T("student.grades.col.date");
        if (grid.Columns["GradedBy"] is { } gc) gc.HeaderText = Loc.T("student.grades.col.gradedby");

        card.Controls.Add(grid);
        Controls.Add(card);
    }

    private static Label CenterLabel(string text) => new()
    {
        Text = text,
        Font = Theme.BodyFont,
        ForeColor = Theme.TextSecondary,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleCenter
    };
}
