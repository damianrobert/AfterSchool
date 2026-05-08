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

    // Notification bell
    private readonly Button _bellBtn  = new();
    private readonly Label  _badgeLbl = new();
    private NotificationPanel? _notifPanel;
    private readonly System.Windows.Forms.Timer _notifTimer = new() { Interval = 30_000 };

    public StudentMainForm()
    {
        Text = "AfterSchool — Student Portal";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1000, 660);
        Size = new Size(1200, 760);
        WindowState = FormWindowState.Maximized;
        BackColor = Theme.Background;
        Font = Theme.BodyFont;
        Icon = AppLogo.CreateWindowIcon();

        BuildLayout();

        Loc.LanguageChanged += OnLanguageChanged;
        FormClosed += (_, _) =>
        {
            Loc.LanguageChanged -= OnLanguageChanged;
            _notifTimer.Dispose();
        };
    }

    private void BuildLayout()
    {
        // ── Sidebar ──────────────────────────────────────────────────────────
        var sidebar = new Panel { Dock = DockStyle.Left, Width = 220, BackColor = Theme.Sidebar };

        var brand = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Color.FromArgb(15, 23, 42) };
        brand.Paint += (_, e) =>
        {
            AppLogo.DrawHorizontalLogo(e.Graphics, 36, 14, 0, brand.Height);
            using var subFont = new Font("Segoe UI", 8.5f);
            using var subBrush = new SolidBrush(Color.FromArgb(148, 163, 184));
            e.Graphics.DrawString("Student Portal", subFont, subBrush, new PointF(64, 42));
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
            ("nav.chat",               "nav.chat.title",                (Func<UserControl>)(() => new ChatControl())),
            ("nav.messages",           "nav.messages.title",            (Func<UserControl>)(() => new DirectChatControl())),
            ("nav.coursechat",         "nav.coursechat.title",          (Func<UserControl>)(() => new CourseGroupChatControl())),
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
        topBar.Controls.Add(BuildBellPanel());

        // ── Content area ─────────────────────────────────────────────────────
        _content.Dock = DockStyle.Fill;
        _content.BackColor = Theme.Background;
        _content.Padding = new Padding(24);

        Controls.Add(_content);
        Controls.Add(topBar);
        Controls.Add(sidebar);

        // Activate first nav button
        _navButtons[0].PerformClick();

        // Init notifications after layout is built
        Load += (_, _) => InitNotifications();
    }

    // ── Notifications ─────────────────────────────────────────────────────────

    private Panel BuildBellPanel()
    {
        const int PanelW = 52;
        var panel = new Panel { Dock = DockStyle.Right, Width = PanelW, BackColor = Theme.Surface };

        _bellBtn.Width  = 40;
        _bellBtn.Height = 34;
        _bellBtn.Left   = (PanelW - 40) / 2;
        _bellBtn.Top    = (64 - 34) / 2;
        _bellBtn.Text   = "🔔";
        _bellBtn.Font   = new Font("Segoe UI Emoji", 14f);
        _bellBtn.Cursor = Cursors.Hand;
        _bellBtn.FlatStyle = FlatStyle.Flat;
        _bellBtn.BackColor = Theme.Background;
        _bellBtn.ForeColor = Theme.TextPrimary;
        _bellBtn.FlatAppearance.BorderSize = 0;
        _bellBtn.FlatAppearance.MouseOverBackColor = Theme.Border;
        _bellBtn.Click += (_, _) => ToggleNotifPanel();

        _badgeLbl.Size      = new Size(18, 14);
        _badgeLbl.Location  = new Point(_bellBtn.Right - 10, _bellBtn.Top - 2);
        _badgeLbl.Font      = new Font("Segoe UI Semibold", 7f);
        _badgeLbl.BackColor = Color.FromArgb(239, 68, 68);
        _badgeLbl.ForeColor = Color.White;
        _badgeLbl.TextAlign = ContentAlignment.MiddleCenter;
        _badgeLbl.Visible   = false;

        panel.Controls.Add(_bellBtn);
        panel.Controls.Add(_badgeLbl);
        _badgeLbl.BringToFront();
        return panel;
    }

    private void InitNotifications()
    {
        _notifPanel = new NotificationPanel(Session.Current?.Id ?? 0, RefreshBadge);
        Controls.Add(_notifPanel);
        _notifPanel.BringToFront();

        _notifTimer.Tick += (_, _) => RefreshBadge();
        _notifTimer.Start();
        RefreshBadge();
    }

    private void ToggleNotifPanel()
    {
        if (_notifPanel == null) return;
        if (_notifPanel.Visible)
        {
            _notifPanel.Visible = false;
            return;
        }

        _notifPanel.Reload();
        PositionNotifPanel();
        _notifPanel.Visible = true;
        _notifPanel.BringToFront();
    }

    private void PositionNotifPanel()
    {
        if (_notifPanel == null) return;
        var screenPt = _bellBtn.PointToScreen(new Point(0, _bellBtn.Height));
        var formPt   = PointToClient(screenPt);
        var x = Math.Max(0, formPt.X - _notifPanel.Width + _bellBtn.Width);
        _notifPanel.Location = new Point(x, formPt.Y);
    }

    private void RefreshBadge()
    {
        var count = Data.NotificationRepository.GetUnreadCount(Session.Current?.Id ?? 0);
        if (count == 0)
        {
            _badgeLbl.Visible = false;
        }
        else
        {
            _badgeLbl.Text    = count > 9 ? "9+" : count.ToString();
            _badgeLbl.Visible = true;
        }
    }

    // ── Footer ────────────────────────────────────────────────────────────────

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
        var courseIds = studentId.HasValue
            ? StudentRepository.GetEnrolledCourseIds(studentId.Value)
            : new List<int>();
        var courses = courseIds
            .Select(id => CourseRepository.GetById(id))
            .Where(c => c != null)
            .Cast<Course>()
            .ToList();

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
        var enrolledText = courses.Count > 0
            ? $"{Loc.T("student.overview.enrolled_in")}: {string.Join(", ", courses.Select(c => c.Name))}"
            : Loc.T("student.overview.not_enrolled");
        var greetSub = new Label
        {
            Text = enrolledText,
            Font = Theme.BodyFont,
            ForeColor = Theme.TextSecondary,
            Dock = DockStyle.Top,
            Height = 24
        };
        greeting.Controls.Add(greetSub);
        greeting.Controls.Add(greetLbl);
        stack.Controls.Add(greeting);

        // Course cards
        foreach (var course in courses)
        {
            var courseCard = Card(580, 120);
            courseCard.Controls.Add(InfoRow(Loc.T("student.overview.teacher"), course.Teacher.Length > 0 ? course.Teacher : "—"));
            if (!string.IsNullOrWhiteSpace(course.Description))
                courseCard.Controls.Add(InfoRow("Description", course.Description));
            courseCard.Controls.Add(SectionHeader(course.Name));
            stack.Controls.Add(courseCard);
        }

        // Today's schedule (across all enrolled courses)
        if (courses.Count > 0)
        {
            var today = DateTime.Today.DayOfWeek.ToString();
            var todaySlots = courses
                .SelectMany(c => ScheduleRepository.GetForCourse(c.Id)
                    .Where(s => s.DayOfWeek.Equals(today, StringComparison.OrdinalIgnoreCase))
                    .Select(s => (slot: s, courseName: c.Name)))
                .OrderBy(x => x.slot.StartTime)
                .ToList();

            var todayCard = Card(580, todaySlots.Count > 0 ? 60 + todaySlots.Count * 36 : 80);
            todayCard.Controls.Add(SectionHeader(Loc.T("student.overview.today")));
            if (todaySlots.Count == 0)
            {
                todayCard.Controls.Add(InfoRow("", Loc.T("student.overview.no_today")));
            }
            else
            {
                foreach (var (s, courseName) in todaySlots)
                    todayCard.Controls.Add(InfoRow($"{s.StartTime}–{s.EndTime}", $"{courseName} · {s.Room}"));
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
        var courseIds = studentId.HasValue
            ? StudentRepository.GetEnrolledCourseIds(studentId.Value)
            : new List<int>();

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

        if (courseIds.Count == 0)
        {
            card.Controls.Add(CenterLabel(Loc.T("student.schedule.no_course")));
            Controls.Add(card);
            return;
        }

        var dayOrder = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
        var rows = courseIds
            .SelectMany(cid =>
            {
                var cName = CourseRepository.GetById(cid)?.Name ?? "—";
                return ScheduleRepository.GetForCourse(cid)
                    .Select(s => new
                    {
                        Course = cName,
                        Day    = Loc.T($"days.{s.DayOfWeek.ToLower()}"),
                        DayKey = s.DayOfWeek,
                        Start  = s.StartTime,
                        End    = s.EndTime,
                        Room   = s.Room
                    });
            })
            .OrderBy(r => Array.IndexOf(dayOrder, r.DayKey))
            .ThenBy(r => r.Start)
            .Select(r => new { r.Course, r.Day, r.Start, r.End, r.Room })
            .ToList();

        if (rows.Count == 0)
        {
            card.Controls.Add(CenterLabel(Loc.T("student.schedule.no_slots")));
            Controls.Add(card);
            return;
        }

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
