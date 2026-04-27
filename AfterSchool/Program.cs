using AfterSchool.Data;
using AfterSchool.Forms;
using AfterSchool.Services;
using AfterSchool.UI;

namespace AfterSchool;

internal static class Program
{
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

        while (true)
        {
            using (var login = new LoginForm())
            {
                if (login.ShowDialog() != DialogResult.OK || login.AuthenticatedUser == null)
                    return;
                Session.Current = login.AuthenticatedUser;
            }

            Session.SignOutRequested = false;
            Application.Run(new MainForm());

            if (!Session.SignOutRequested)
                return;

            Session.Current = null;
        }
    }
}
