using AfterSchool.UI;

namespace AfterSchool.Forms;

public class MainForm : Form
{
    private readonly Panel _sidebar;
    private readonly Panel _content;
    private readonly Label _titleLabel;
    private readonly List<NavButton> _navButtons = new();
    private UserControl? _currentView;

    public MainForm()
    {
        Text = "AfterSchool Management System";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1100, 700);
        Size = new Size(1280, 800);
        BackColor = Theme.Background;
        Font = Theme.BodyFont;
        Icon = SystemIcons.Application;

        _sidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 240,
            BackColor = Theme.Sidebar
        };

        var brand = new Label
        {
            Text = "AfterSchool",
            Font = new Font("Segoe UI Semibold", 16f),
            ForeColor = Color.White,
            Dock = DockStyle.Top,
            Height = 70,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(24, 0, 0, 0),
            BackColor = Color.FromArgb(15, 23, 42)
        };
        _sidebar.Controls.Add(brand);

        var topBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = Theme.Surface
        };
        topBar.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawLine(pen, 0, topBar.Height - 1, topBar.Width, topBar.Height - 1);
        };

        _titleLabel = new Label
        {
            Font = Theme.TitleFont,
            ForeColor = Theme.TextPrimary,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(28, 0, 0, 0),
            Text = "Dashboard"
        };
        topBar.Controls.Add(_titleLabel);

        _content = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Background,
            Padding = new Padding(24)
        };

        Controls.Add(_content);
        Controls.Add(topBar);
        Controls.Add(_sidebar);

        AddNavButton("Courses", "Course Manager", () => new CourseManagerControl());
        AddNavButton("Schedule", "Weekly Schedule Planner", () => new SchedulePlannerControl());
        AddNavButton("Students", "Enrollment Center", () => new EnrollmentControl());
        AddNavButton("Reports", "Reports & Export", () => new ReportsControl());

        if (_navButtons.Count > 0)
            _navButtons[0].PerformClick();
    }

    private void AddNavButton(string label, string title, Func<UserControl> factory)
    {
        var btn = new NavButton(label);
        btn.Click += (_, _) =>
        {
            foreach (var b in _navButtons) b.IsActive = false;
            btn.IsActive = true;
            _titleLabel.Text = title;
            SwapView(factory());
        };
        _navButtons.Add(btn);

        var stack = _sidebar.Controls.OfType<FlowLayoutPanel>().FirstOrDefault();
        if (stack == null)
        {
            stack = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = false,
                Padding = new Padding(12, 16, 12, 12),
                BackColor = Theme.Sidebar
            };
            _sidebar.Controls.Add(stack);
        }
        stack.Controls.Add(btn);
    }

    private void SwapView(UserControl view)
    {
        _content.SuspendLayout();
        if (_currentView != null)
        {
            _content.Controls.Remove(_currentView);
            _currentView.Dispose();
        }
        view.Dock = DockStyle.Fill;
        _content.Controls.Add(view);
        _currentView = view;
        _content.ResumeLayout();
    }

    private sealed class NavButton : Button
    {
        private bool _active;

        public NavButton(string text)
        {
            Text = "  " + text;
            Width = 216;
            Height = 42;
            Margin = new Padding(0, 4, 0, 0);
            TextAlign = ContentAlignment.MiddleLeft;
            Font = new Font("Segoe UI", 10.5f);
            Cursor = Cursors.Hand;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            ForeColor = Theme.SidebarText;
            BackColor = Theme.Sidebar;
            FlatAppearance.MouseOverBackColor = Theme.SidebarHover;
        }

        public bool IsActive
        {
            get => _active;
            set
            {
                _active = value;
                if (value)
                {
                    BackColor = Theme.SidebarActive;
                    ForeColor = Color.White;
                    FlatAppearance.MouseOverBackColor = Theme.SidebarActive;
                }
                else
                {
                    BackColor = Theme.Sidebar;
                    ForeColor = Theme.SidebarText;
                    FlatAppearance.MouseOverBackColor = Theme.SidebarHover;
                }
            }
        }
    }
}
