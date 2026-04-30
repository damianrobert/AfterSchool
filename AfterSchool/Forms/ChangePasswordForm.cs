using AfterSchool.Data;
using AfterSchool.Services;
using AfterSchool.UI;

namespace AfterSchool.Forms;

public class ChangePasswordForm : Form
{
    private readonly TextBox _newPass    = new() { UseSystemPasswordChar = true };
    private readonly TextBox _confirmPass = new() { UseSystemPasswordChar = true };
    private readonly Label   _error      = new();

    public ChangePasswordForm()
    {
        Text = "AfterSchool — Set Password";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Size = new Size(500, 420);
        BackColor = Theme.Surface;
        Font = Theme.BodyFont;

        BuildLayout();
    }

    private void BuildLayout()
    {
        var content = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(48, 48, 48, 32),
            BackColor = Theme.Surface
        };

        var icon = new Label
        {
            Text = "🔒",
            Font = new Font("Segoe UI", 28f),
            Dock = DockStyle.Top,
            Height = 52,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var heading = new Label
        {
            Text = Loc.T("changepass.heading"),
            Font = new Font("Segoe UI Semibold", 20f),
            ForeColor = Theme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 40
        };

        var sub = new Label
        {
            Text = Loc.T("changepass.sub"),
            Font = Theme.BodyFont,
            ForeColor = Theme.TextSecondary,
            Dock = DockStyle.Top,
            Height = 48
        };

        foreach (var tb in new[] { _newPass, _confirmPass })
        {
            Theme.StyleTextBox(tb);
            tb.Font = new Font("Segoe UI", 11f);
            tb.Dock = DockStyle.Top;
            tb.Height = 34;
        }

        _error.Dock = DockStyle.Top;
        _error.Height = 26;
        _error.ForeColor = Theme.Danger;
        _error.Font = Theme.SmallFont;
        _error.TextAlign = ContentAlignment.MiddleLeft;

        var setBtn = new Button
        {
            Text = Loc.T("changepass.btn.set"),
            Dock = DockStyle.Top,
            Height = 44
        };
        Theme.StyleButton(setBtn, primary: true);
        setBtn.Font = new Font("Segoe UI Semibold", 11f);
        setBtn.Click += (_, _) => TrySetPassword();

        content.Controls.Add(Spacer(8));
        content.Controls.Add(setBtn);
        content.Controls.Add(Spacer(8));
        content.Controls.Add(_error);
        content.Controls.Add(_confirmPass);
        content.Controls.Add(FieldLabel(Loc.T("changepass.field.confirm")));
        content.Controls.Add(Spacer(10));
        content.Controls.Add(_newPass);
        content.Controls.Add(FieldLabel(Loc.T("changepass.field.new")));
        content.Controls.Add(Spacer(16));
        content.Controls.Add(sub);
        content.Controls.Add(heading);
        content.Controls.Add(icon);

        Controls.Add(content);
        AcceptButton = setBtn;
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

    private static Panel Spacer(int h) =>
        new() { Dock = DockStyle.Top, Height = h, BackColor = Theme.Surface };

    private void TrySetPassword()
    {
        _error.Text = "";
        var newPass  = _newPass.Text;
        var confirm  = _confirmPass.Text;

        if (newPass.Length < 6)
        {
            _error.Text = Loc.T("changepass.validation.length");
            return;
        }
        if (newPass != confirm)
        {
            _error.Text = Loc.T("changepass.validation.match");
            _confirmPass.Clear();
            _confirmPass.Focus();
            return;
        }

        var hash = PasswordHasher.Hash(newPass);
        UserRepository.UpdatePassword(Session.Current!.Id, hash);
        Session.Current.MustChangePassword = 0;
        Session.Current.PasswordHash = hash;

        DialogResult = DialogResult.OK;
        Close();
    }
}
