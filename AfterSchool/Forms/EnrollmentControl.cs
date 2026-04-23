using AfterSchool.UI;

namespace AfterSchool.Forms;

public class EnrollmentControl : UserControl
{
    public EnrollmentControl()
    {
        BackColor = Theme.Background;
        var lbl = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Enrollment Center coming up...",
            Font = Theme.HeadingFont,
            TextAlign = ContentAlignment.MiddleCenter
        };
        Controls.Add(lbl);
    }
}
