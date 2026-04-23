using AfterSchool.Data;
using AfterSchool.Models;
using AfterSchool.UI;

namespace AfterSchool.Forms;

public class SignupForm : Form
{
    private readonly TextBox _fullName = new();
    private readonly TextBox _username = new();
    private readonly TextBox _password = new() { UseSystemPasswordChar = true };
    private readonly TextBox _confirm = new() { UseSystemPasswordChar = true };
    private readonly ComboBox _role = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label _error = new();
    private readonly bool _firstRun;

    public User? CreatedUser { get; private set; }

    public SignupForm(bool firstRun)
    {
        _firstRun = firstRun;
        Text = "AfterSchool — Create account";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Size = new Size(560, 620);
        BackColor = Theme.Surface;
        Font = Theme.BodyFont;

        BuildLayout();
    }

    private void BuildLayout()
    {
        var root = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Surface, Padding = new Padding(48, 40, 48, 24) };

        var heading = new Label
        {
            Text = _firstRun ? "Welcome — let's set up the first account" : "Create an account",
            Font = new Font("Segoe UI Semibold", 18f),
            ForeColor = Theme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 36
        };
        var sub = new Label
        {
            Text = _firstRun
                ? "No users exist yet. This account will be an administrator."
                : "Register to access the management system.",
            Font = Theme.BodyFont,
            ForeColor = Theme.TextSecondary,
            Dock = DockStyle.Top,
            Height = 28
        };

        foreach (var tb in new[] { _fullName, _username, _password, _confirm })
        {
            Theme.StyleTextBox(tb);
            tb.Font = new Font("Segoe UI", 10.5f);
        }

        _role.Items.AddRange(new object[] { "Staff", "Teacher", "Administrator" });
        _role.SelectedItem = _firstRun ? "Administrator" : "Staff";
        _role.Font = Theme.BodyFont;

        _error.Dock = DockStyle.Top;
        _error.Height = 26;
        _error.ForeColor = Theme.Danger;
        _error.Font = Theme.SmallFont;

        var createBtn = new Button { Text = _firstRun ? "Create account & sign in" : "Create account", Dock = DockStyle.Top, Height = 42 };
        Theme.StyleButton(createBtn, primary: true);
        createBtn.Font = new Font("Segoe UI Semibold", 10.5f);
        createBtn.Click += (_, _) => TrySignup();

        var switchPanel = new Panel { Dock = DockStyle.Top, Height = 36, BackColor = Theme.Surface, Padding = new Padding(0, 12, 0, 0) };
        var prompt = new Label
        {
            Text = "Already have an account?",
            Font = Theme.BodyFont,
            ForeColor = Theme.TextSecondary,
            AutoSize = true,
            Top = 12,
            Left = 0
        };
        var loginLink = new LinkLabel
        {
            Text = "Sign in",
            Font = new Font("Segoe UI Semibold", 10f),
            LinkColor = Theme.Primary,
            ActiveLinkColor = Theme.PrimaryHover,
            AutoSize = true,
            Top = 12,
            Left = 170
        };
        loginLink.LinkClicked += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };
        switchPanel.Controls.Add(loginLink);
        switchPanel.Controls.Add(prompt);

        root.Controls.Add(switchPanel);
        root.Controls.Add(Spacer(4));
        root.Controls.Add(createBtn);
        root.Controls.Add(Spacer(8));
        root.Controls.Add(_error);

        AddField(root, "Role", _role);
        AddField(root, "Confirm password", _confirm);
        AddField(root, "Password", _password);
        AddField(root, "Username", _username);
        AddField(root, "Full name", _fullName);

        root.Controls.Add(Spacer(16));
        root.Controls.Add(sub);
        root.Controls.Add(heading);

        Controls.Add(root);
        AcceptButton = createBtn;
    }

    private static void AddField(Panel container, string label, Control control)
    {
        control.Dock = DockStyle.Top;
        control.Height = 32;
        container.Controls.Add(control);
        container.Controls.Add(new Label
        {
            Text = label,
            Font = new Font("Segoe UI Semibold", 9.5f),
            ForeColor = Theme.TextSecondary,
            Dock = DockStyle.Top,
            Height = 22,
            TextAlign = ContentAlignment.BottomLeft
        });
        container.Controls.Add(Spacer(10));
    }

    private static Panel Spacer(int h) => new() { Dock = DockStyle.Top, Height = h, BackColor = Theme.Surface };

    private void TrySignup()
    {
        _error.Text = "";
        var fullName = _fullName.Text.Trim();
        var username = _username.Text.Trim();
        var password = _password.Text;
        var confirm = _confirm.Text;
        var role = _role.SelectedItem?.ToString() ?? "Staff";

        if (string.IsNullOrWhiteSpace(fullName))
        {
            _error.Text = "Full name is required.";
            _fullName.Focus();
            return;
        }
        if (username.Length < 3)
        {
            _error.Text = "Username must be at least 3 characters.";
            _username.Focus();
            return;
        }
        if (password.Length < 6)
        {
            _error.Text = "Password must be at least 6 characters.";
            _password.Focus();
            return;
        }
        if (password != confirm)
        {
            _error.Text = "Passwords do not match.";
            _confirm.SelectAll();
            _confirm.Focus();
            return;
        }
        if (UserRepository.UsernameExists(username))
        {
            _error.Text = "That username is already taken.";
            _username.Focus();
            return;
        }

        var user = new User
        {
            Username = username,
            FullName = fullName,
            Role = role,
            PasswordHash = PasswordHasher.Hash(password),
            CreatedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            IsActive = 1
        };

        try
        {
            user.Id = UserRepository.Insert(user);
        }
        catch (Exception ex)
        {
            _error.Text = $"Could not create account: {ex.Message}";
            return;
        }

        CreatedUser = user;
        DialogResult = DialogResult.OK;
        Close();
    }
}
