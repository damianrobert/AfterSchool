using AfterSchool.UI;

namespace AfterSchool.Forms;

public class MainForm : Form
{
    private readonly Panel _sidebar;
    private readonly FlowLayoutPanel _navStack;
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

        var footer = BuildSidebarFooter();

        _navStack = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = false,
            Padding = new Padding(12, 16, 12, 12),
            BackColor = Theme.Sidebar
        };

        _sidebar.Controls.Add(_navStack);
        _sidebar.Controls.Add(footer);
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

        var userChip = BuildUserChip();

        topBar.Controls.Add(_titleLabel);
        topBar.Controls.Add(userChip);

        _content = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Background,
            Padding = new Padding(24)
        };

        Controls.Add(_content);
        Controls.Add(topBar);
        Controls.Add(_sidebar);

        AddNavButton("Dashboard", "Dashboard", () => new DashboardControl());
        AddNavButton("Courses", "Course Manager", () => new CourseManagerControl());
        AddNavButton("Schedule", "Weekly Schedule Planner", () => new SchedulePlannerControl());
        AddNavButton("Students", "Enrollment Center", () => new EnrollmentControl());
        AddNavButton("Reports", "Reports & Export", () => new ReportsControl());
        AddNavButton("Text to Speech", "Text to Speech", () => new TextToSpeechControl());

        Load += (_, _) => _navButtons[0].PerformClick();
    }

    private Panel BuildSidebarFooter()
    {
        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 92,
            BackColor = Color.FromArgb(15, 23, 42),
            Padding = new Padding(16, 12, 16, 12)
        };

        var name = new Label
        {
            Text = Session.DisplayName,
            Font = new Font("Segoe UI Semibold", 10f),
            ForeColor = Color.White,
            Dock = DockStyle.Top,
            Height = 20,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var role = new Label
        {
            Text = Session.Current?.Role ?? "",
            Font = Theme.SmallFont,
            ForeColor = Color.FromArgb(148, 163, 184),
            Dock = DockStyle.Top,
            Height = 18,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var logout = new Button
        {
            Text = "Sign out",
            Dock = DockStyle.Bottom,
            Height = 30,
            Cursor = Cursors.Hand,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(51, 65, 85),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5f)
        };
        logout.FlatAppearance.BorderSize = 0;
        logout.FlatAppearance.MouseOverBackColor = Color.FromArgb(71, 85, 105);
        logout.Click += (_, _) =>
        {
            Session.SignOutRequested = true;
            Close();
        };

        footer.Controls.Add(logout);
        footer.Controls.Add(role);
        footer.Controls.Add(name);
        return footer;
    }

    private Panel BuildUserChip()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Right,
            Width = 260,
            BackColor = Theme.Surface,
            Padding = new Padding(0, 14, 28, 14)
        };

        var label = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            Font = Theme.BodyFont,
            ForeColor = Theme.TextPrimary,
            Text = $"Signed in as  {Session.DisplayName}"
        };
        panel.Controls.Add(label);
        return panel;
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
        _navStack.Controls.Add(btn);
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
