using AfterSchool.UI;

namespace AfterSchool.Forms;

public class ReportsControl : UserControl
{
    public ReportsControl()
    {
        BackColor = Theme.Background;
        var lbl = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Reports & Export coming up...",
            Font = Theme.HeadingFont,
            TextAlign = ContentAlignment.MiddleCenter
        };
        Controls.Add(lbl);
    }
}
