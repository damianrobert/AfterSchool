using AfterSchool.UI;

namespace AfterSchool.Forms;

public class SchedulePlannerControl : UserControl
{
    public SchedulePlannerControl()
    {
        BackColor = Theme.Background;
        var lbl = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Schedule Planner coming up...",
            Font = Theme.HeadingFont,
            TextAlign = ContentAlignment.MiddleCenter
        };
        Controls.Add(lbl);
    }
}
