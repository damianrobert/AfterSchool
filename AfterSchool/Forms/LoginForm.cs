using AfterSchool.Data;
using AfterSchool.Models;
using AfterSchool.Services;
using AfterSchool.UI;

namespace AfterSchool.Forms;

public class LoginForm : Form
{
    private readonly TextBox _username = new();
    private readonly TextBox _password = new() { UseSystemPasswordChar = true };
    private readonly Label _error = new();

    public User? AuthenticatedUser { get; private set; }

    public LoginForm()
    {
        Text = "AfterSchool — Sign in";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Size = new Size(880, 560);
        BackColor = Theme.Surface;
        Font = Theme.BodyFont;
        Icon = AppLogo.CreateWindowIcon();

        BuildLayout();

        if (UserRepository.Count() == 0)
        {
            Shown += (_, _) => OpenSignup(firstRun: true);
        }
    }

    private void BuildLayout()
    {
        var hero = new Panel
        {
            Dock = DockStyle.Left,
            Width = 380,
            BackColor = Theme.Sidebar
        };
        hero.Paint += (_, e) =>
        {
            var g = e.Graphics;
            using var bg = new SolidBrush(Theme.Sidebar);
            g.FillRectangle(bg, hero.ClientRectangle);

            // Logo badge
            AppLogo.DrawBadge(g, 52, 40, 28);

            using var title = new Font("Segoe UI Semibold", 26f);
            using var titleBrush = new SolidBrush(Color.White);
            g.DrawString("AfterSchool", title, titleBrush, new PointF(40, 94));

            using var tag = new Font("Segoe UI", 11f);
            using var tagBrush = new SolidBrush(Color.FromArgb(148, 163, 184));
            g.DrawString("Management System", tag, tagBrush, new PointF(42, 138));

            var blurbY = 210;
            using var blurb = new Font("Segoe UI", 10.5f);
            using var blurbBrush = new SolidBrush(Color.FromArgb(203, 213, 225));
            string[] lines =
            {
                "• Manage courses and teachers",
                "• Plan the weekly schedule",
                "• Track student enrollments",
                "• Export reports to Excel"
            };
            foreach (var line in lines)
            {
                g.DrawString(line, blurb, blurbBrush, new PointF(40, blurbY));
                blurbY += 30;
            }
        };

        var right = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Surface, Padding = new Padding(56, 80, 56, 40) };

        var heading = new Label
        {
            Text = Loc.T("login.heading"),
            Font = new Font("Segoe UI Semibold", 22f),
            ForeColor = Theme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 42
        };
        var sub = new Label
        {
            Text = Loc.T("login.sub"),
            Font = Theme.BodyFont,
            ForeColor = Theme.TextSecondary,
            Dock = DockStyle.Top,
            Height = 30
        };

        Theme.StyleTextBox(_username);
        Theme.StyleTextBox(_password);
        _username.Font = new Font("Segoe UI", 11f);
        _password.Font = new Font("Segoe UI", 11f);

        var usernameLabel = FieldLabel(Loc.T("login.field.username"));
        _username.Dock = DockStyle.Top; _username.Height = 34;
        var passwordLabel = FieldLabel(Loc.T("login.field.password"));
        _password.Dock = DockStyle.Top; _password.Height = 34;

        _error.Dock = DockStyle.Top;
        _error.Height = 28;
        _error.ForeColor = Theme.Danger;
        _error.Font = Theme.SmallFont;
        _error.TextAlign = ContentAlignment.MiddleLeft;
        _error.Text = "";

        var loginBtn = new Button { Text = Loc.T("login.btn.signin"), Dock = DockStyle.Top, Height = 44 };
        Theme.StyleButton(loginBtn, primary: true);
        loginBtn.Font = new Font("Segoe UI Semibold", 11f);
        loginBtn.Click += (_, _) => TryLogin();

        var switchPanel = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Theme.Surface, Padding = new Padding(0, 14, 0, 0) };
        var prompt = new Label
        {
            Text = Loc.T("login.switch.prompt"),
            Font = Theme.BodyFont,
            ForeColor = Theme.TextSecondary,
            AutoSize = true,
            Top = 14,
            Left = 0
        };
        var signupLink = new LinkLabel
        {
            Text = Loc.T("login.switch.link"),
            Font = new Font("Segoe UI Semibold", 10f),
            LinkColor = Theme.Primary,
            ActiveLinkColor = Theme.PrimaryHover,
            AutoSize = true,
            Top = 14,
            Left = 170
        };
        signupLink.LinkClicked += (_, _) => OpenSignup(firstRun: false);
        switchPanel.Controls.Add(signupLink);
        switchPanel.Controls.Add(prompt);

        right.Controls.Add(switchPanel);
        right.Controls.Add(Spacer(8));
        right.Controls.Add(loginBtn);
        right.Controls.Add(Spacer(8));
        right.Controls.Add(_error);
        right.Controls.Add(_password);
        right.Controls.Add(passwordLabel);
        right.Controls.Add(Spacer(12));
        right.Controls.Add(_username);
        right.Controls.Add(usernameLabel);
        right.Controls.Add(Spacer(24));
        right.Controls.Add(sub);
        right.Controls.Add(heading);

        Controls.Add(right);
        Controls.Add(hero);

        AcceptButton = loginBtn;
    }

    private static Label FieldLabel(string text) => new()
    {
        Text = text,
        Font = new Font("Segoe UI Semibold", 9.5f),
        ForeColor = Theme.TextSecondary,
        Dock = DockStyle.Top,
        Height = 22,
        TextAlign = ContentAlignment.BottomLeft
    };

    private static Panel Spacer(int h) => new() { Dock = DockStyle.Top, Height = h, BackColor = Theme.Surface };

    private void TryLogin()
    {
        _error.Text = "";
        var username = _username.Text.Trim();
        var password = _password.Text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            _error.Text = Loc.T("login.validation.empty");
            return;
        }

        var user = UserRepository.Authenticate(username, password);
        if (user == null)
        {
            _error.Text = Loc.T("login.validation.invalid");
            _password.SelectAll();
            _password.Focus();
            return;
        }

        AuthenticatedUser = user;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void OpenSignup(bool firstRun)
    {
        using var signup = new SignupForm(firstRun);
        var result = signup.ShowDialog(this);
        if (result == DialogResult.OK && signup.CreatedUser != null)
        {
            AuthenticatedUser = signup.CreatedUser;
            DialogResult = DialogResult.OK;
            Close();
        }
        else if (firstRun && UserRepository.Count() == 0)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
