using AfterSchool.Services;
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

    // Fields that need live text updates on language change
    private readonly Button _langBtn = new();
    private readonly Label _userChipLbl = new();
    private readonly Button _signOutBtn = new();
    private string _activeTitleKey = "nav.dashboard.title";
    private Bitmap? _langFlag;

    public MainForm()
    {
        Text = "AfterSchool Management System";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1100, 700);
        Size = new Size(1280, 800);
        WindowState = FormWindowState.Maximized;
        BackColor = Theme.Background;
        Font = Theme.BodyFont;
        Icon = AppLogo.CreateWindowIcon();

        _sidebar = new Panel { Dock = DockStyle.Left, Width = 240, BackColor = Theme.Sidebar };

        var brand = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = Color.FromArgb(15, 23, 42)
        };
        brand.Paint += (_, e) =>
            AppLogo.DrawHorizontalLogo(e.Graphics, 40, 14, 0, brand.Height);

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
        _sidebar.Controls.Add(BuildSidebarFooter());
        _sidebar.Controls.Add(brand);

        var topBar = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Theme.Surface };
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
            Text = Loc.T(_activeTitleKey)
        };

        topBar.Controls.Add(_titleLabel);
        topBar.Controls.Add(BuildUserChip());
        topBar.Controls.Add(BuildLangPanel());

        _content = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background, Padding = new Padding(24) };

        Controls.Add(_content);
        Controls.Add(topBar);
        Controls.Add(_sidebar);

        AddNavButton("nav.dashboard", "nav.dashboard.title", () => new DashboardControl());
        AddNavButton("nav.courses",   "nav.courses.title",   () => new CourseManagerControl());
        AddNavButton("nav.schedule",  "nav.schedule.title",  () => new SchedulePlannerControl());
        AddNavButton("nav.students",  "nav.students.title",  () => new EnrollmentControl());
        AddNavButton("nav.grades",       "nav.grades.title",      () => new GradesControl());
        AddNavButton("nav.assignments",  "nav.assignments.title", () => new AssignmentsControl());
        AddNavButton("nav.files",        "nav.files.title",       () => new CourseFilesControl());
        AddNavButton("nav.reports",   "nav.reports.title",   () => new ReportsControl());
        AddNavButton("nav.tts",       "nav.tts.title",       () => new TextToSpeechControl());
        AddNavButton("nav.chat",      "nav.chat.title",      () => new ChatControl());
        AddNavButton("nav.messages",  "nav.messages.title",  () => new DirectChatControl());

        Loc.LanguageChanged += OnLanguageChanged;
        Load += (_, _) => _navButtons[0].PerformClick();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Loc.LanguageChanged -= OnLanguageChanged;
            _langFlag?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void OnLanguageChanged()
    {
        // Update nav button labels
        foreach (var b in _navButtons) b.UpdateLabel();

        // Update top bar
        _titleLabel.Text = Loc.T(_activeTitleKey);
        _userChipLbl.Text = $"{Loc.T("main.signedin")}  {Session.DisplayName}";
        UpdateLangButton();

        // Update sidebar footer
        _signOutBtn.Text = Loc.T("main.signout");

        // Recreate the active view so it rebuilds with the new language
        _navButtons.FirstOrDefault(b => b.IsActive)?.PerformClick();
    }

    // ── Sidebar footer ────────────────────────────────────────────────────────

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

        _signOutBtn.Text = Loc.T("main.signout");
        _signOutBtn.Dock = DockStyle.Bottom;
        _signOutBtn.Height = 30;
        _signOutBtn.Cursor = Cursors.Hand;
        _signOutBtn.FlatStyle = FlatStyle.Flat;
        _signOutBtn.BackColor = Color.FromArgb(51, 65, 85);
        _signOutBtn.ForeColor = Color.White;
        _signOutBtn.Font = new Font("Segoe UI", 9.5f);
        _signOutBtn.FlatAppearance.BorderSize = 0;
        _signOutBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(71, 85, 105);
        _signOutBtn.Click += (_, _) => { Session.SignOutRequested = true; Close(); };

        footer.Controls.Add(_signOutBtn);
        footer.Controls.Add(role);
        footer.Controls.Add(name);
        return footer;
    }

    // ── Top-bar: user chip and language toggle (separate right-docked panels) ──

    private Panel BuildUserChip()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Right,
            Width = 260,
            BackColor = Theme.Surface,
            Padding = new Padding(0, 14, 28, 14)
        };

        _userChipLbl.Dock = DockStyle.Fill;
        _userChipLbl.TextAlign = ContentAlignment.MiddleRight;
        _userChipLbl.Font = Theme.BodyFont;
        _userChipLbl.ForeColor = Theme.TextPrimary;
        _userChipLbl.Text = $"{Loc.T("main.signedin")}  {Session.DisplayName}";

        panel.Controls.Add(_userChipLbl);
        return panel;
    }

    private Panel BuildLangPanel()
    {
        const int BtnW = 76, BtnH = 34, PanelW = 96;
        var panel = new Panel { Dock = DockStyle.Right, Width = PanelW, BackColor = Theme.Surface };

        UpdateLangButton();
        _langBtn.Width  = BtnW;
        _langBtn.Height = BtnH;
        _langBtn.Left   = (PanelW - BtnW) / 2;
        _langBtn.Top    = (70 - BtnH) / 2;
        _langBtn.Font   = new Font("Segoe UI Semibold", 9f);
        _langBtn.TextImageRelation = TextImageRelation.ImageBeforeText;
        _langBtn.TextAlign  = ContentAlignment.MiddleLeft;
        _langBtn.Padding    = new Padding(6, 0, 8, 0);
        _langBtn.Cursor     = Cursors.Hand;
        _langBtn.FlatStyle  = FlatStyle.Flat;
        _langBtn.BackColor  = Theme.Background;
        _langBtn.ForeColor  = Theme.TextPrimary;
        _langBtn.FlatAppearance.BorderSize           = 1;
        _langBtn.FlatAppearance.BorderColor          = Theme.Border;
        _langBtn.FlatAppearance.MouseOverBackColor   = Theme.Border;
        _langBtn.Click += (_, _) => Loc.SetLanguage(Loc.Current == "en" ? "ro" : "en");

        panel.Controls.Add(_langBtn);
        return panel;
    }

    private void UpdateLangButton()
    {
        var old = _langFlag;
        _langFlag = CreateFlagBitmap(Loc.Current);
        _langBtn.Text  = Loc.Current.ToUpper();
        _langBtn.Image = _langFlag;
        old?.Dispose();
    }

    private static Bitmap CreateFlagBitmap(string lang)
    {
        const int W = 26, H = 16;
        var bmp = new Bitmap(W, H);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        if (lang == "ro")
            DrawRomanianFlag(g, W, H);
        else
            DrawUnionJack(g, W, H);

        // Thin dark border
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
        using var border = new Pen(Color.FromArgb(90, 0, 0, 0));
        g.DrawRectangle(border, 0, 0, W - 1, H - 1);

        return bmp;
    }

    private static void DrawRomanianFlag(Graphics g, int W, int H)
    {
        int third = W / 3;
        using var b = new SolidBrush(Color.FromArgb(0, 43, 127));
        using var y = new SolidBrush(Color.FromArgb(252, 209, 22));
        using var r = new SolidBrush(Color.FromArgb(206, 17, 38));
        g.FillRectangle(b, 0,          0, third,          H);
        g.FillRectangle(y, third,      0, third,          H);
        g.FillRectangle(r, third * 2,  0, W - third * 2, H);
    }

    private static void DrawUnionJack(Graphics g, int W, int H)
    {
        var blue = Color.FromArgb(0, 36, 125);
        var white = Color.White;
        var red = Color.FromArgb(207, 20, 43);

        // Blue background
        g.FillRectangle(new SolidBrush(blue), 0, 0, W, H);

        // White diagonals (St Andrew's cross)
        float diagW = H / 3.5f;
        using (var wp = new Pen(white, diagW) { LineJoin = System.Drawing.Drawing2D.LineJoin.Miter })
        {
            g.DrawLine(wp, 0, 0, W, H);
            g.DrawLine(wp, W, 0, 0, H);
        }

        // Red diagonals (St Patrick's cross, simplified — no counterchange offset at this size)
        float redDiagW = diagW * 0.45f;
        using (var rp = new Pen(red, redDiagW) { LineJoin = System.Drawing.Drawing2D.LineJoin.Miter })
        {
            g.DrawLine(rp, 0, 0, W, H);
            g.DrawLine(rp, W, 0, 0, H);
        }

        // White cross
        float cw = H / 3f;
        g.FillRectangle(new SolidBrush(white), 0, (H - cw) / 2f, W, cw);
        g.FillRectangle(new SolidBrush(white), (W - cw) / 2f, 0, cw, H);

        // Red cross (narrower, centred)
        float rw = cw * 0.55f;
        g.FillRectangle(new SolidBrush(red), 0, (H - rw) / 2f, W, rw);
        g.FillRectangle(new SolidBrush(red), (W - rw) / 2f, 0, rw, H);
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    private void AddNavButton(string labelKey, string titleKey, Func<UserControl> factory)
    {
        var btn = new NavButton(labelKey, titleKey);
        btn.Click += (_, _) =>
        {
            foreach (var b in _navButtons) b.IsActive = false;
            btn.IsActive = true;
            _activeTitleKey = titleKey;
            _titleLabel.Text = Loc.T(titleKey);
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

    // ── NavButton ─────────────────────────────────────────────────────────────

    private sealed class NavButton : Button
    {
        public string LabelKey { get; }
        public string TitleKey { get; }
        private bool _active;

        public NavButton(string labelKey, string titleKey)
        {
            LabelKey = labelKey;
            TitleKey = titleKey;
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
            UpdateLabel();
        }

        /// <summary>Re-reads the translation for this button's label key.</summary>
        public void UpdateLabel() => Text = "  " + Loc.T(LabelKey);

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
