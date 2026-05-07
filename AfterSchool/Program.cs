using AfterSchool.Data;
using AfterSchool.Forms;
using AfterSchool.Models;
using AfterSchool.Services;
using AfterSchool.UI;

namespace AfterSchool;

internal static class Program
{
    private const int SessionHours = 8;

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        AppSettings.Load();
        Loc.Init(AppSettings.Language);

        try
        {
            DatabaseHelper.Initialize();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to initialize database:\n\n{ex.Message}",
                "AfterSchool",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        User? restoredUser = TryRestoreSession();

        while (true)
        {
            if (restoredUser != null)
            {
                Session.Current = restoredUser;
                restoredUser = null;
            }
            else
            {
                using var login = new LoginForm();
                if (login.ShowDialog() != DialogResult.OK || login.AuthenticatedUser == null)
                    return;
                Session.Current = login.AuthenticatedUser;
                AppSettings.SaveSession(Session.Current.Id, DateTime.UtcNow.AddHours(SessionHours));
            }

            if (Session.Current!.MustChangePassword == 1)
            {
                using var changePass = new ChangePasswordForm();
                if (changePass.ShowDialog() != DialogResult.OK)
                {
                    AppSettings.ClearSession();
                    Session.Current = null;
                    continue;
                }
            }

            Session.SignOutRequested = false;

            if (Session.Current.Role == "Student")
                Application.Run(new StudentMainForm());
            else
                Application.Run(new MainForm());

            if (!Session.SignOutRequested)
                return; // Normal close — session stays valid for next launch

            AppSettings.ClearSession();
            Session.Current = null;
        }
    }

    private static User? TryRestoreSession()
    {
        int uid = AppSettings.SessionUserId;
        if (uid <= 0) return null;
        if (AppSettings.SessionExpiry <= DateTime.UtcNow) return null;
        return UserRepository.GetById(uid);
    }
}
