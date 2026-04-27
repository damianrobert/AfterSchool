using System.Drawing;

namespace AfterSchool.UI;

public static class Theme
{
    public static readonly Color Sidebar = Color.FromArgb(30, 41, 59);
    public static readonly Color SidebarHover = Color.FromArgb(51, 65, 85);
    public static readonly Color SidebarActive = Color.FromArgb(37, 99, 235);
    public static readonly Color SidebarText = Color.FromArgb(226, 232, 240);

    public static readonly Color Background = Color.FromArgb(248, 250, 252);
    public static readonly Color Surface = Color.White;
    public static readonly Color Border = Color.FromArgb(226, 232, 240);

    public static readonly Color TextPrimary = Color.FromArgb(15, 23, 42);
    public static readonly Color TextSecondary = Color.FromArgb(100, 116, 139);

    public static readonly Color Primary = Color.FromArgb(37, 99, 235);
    public static readonly Color PrimaryHover = Color.FromArgb(29, 78, 216);
    public static readonly Color Danger = Color.FromArgb(220, 38, 38);
    public static readonly Color DangerHover = Color.FromArgb(185, 28, 28);
    public static readonly Color Success = Color.FromArgb(22, 163, 74);

    public static readonly Font TitleFont = new("Segoe UI Semibold", 18f);
    public static readonly Font HeadingFont = new("Segoe UI Semibold", 12f);
    public static readonly Font BodyFont = new("Segoe UI", 10f);
    public static readonly Font SmallFont = new("Segoe UI", 9f);

    public static void StyleButton(Button b, bool primary = false, bool danger = false)
    {
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 0;
        b.Font = BodyFont;
        b.Cursor = Cursors.Hand;
        b.AutoSize = true;
        b.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        b.MinimumSize = new Size(0, 36);
        b.MaximumSize = new Size(0, 36);
        b.Padding = new Padding(12, 0, 12, 0);

        if (primary)
        {
            b.BackColor = Primary;
            b.ForeColor = Color.White;
            b.FlatAppearance.MouseOverBackColor = PrimaryHover;
        }
        else if (danger)
        {
            b.BackColor = Danger;
            b.ForeColor = Color.White;
            b.FlatAppearance.MouseOverBackColor = DangerHover;
        }
        else
        {
            b.BackColor = Surface;
            b.ForeColor = TextPrimary;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = Border;
            b.FlatAppearance.MouseOverBackColor = Background;
        }
    }

    public static void StyleTextBox(TextBox t)
    {
        t.BorderStyle = BorderStyle.FixedSingle;
        t.Font = BodyFont;
        t.BackColor = Surface;
        t.ForeColor = TextPrimary;
    }

    public static void StyleGrid(DataGridView g)
    {
        g.BackgroundColor = Surface;
        g.BorderStyle = BorderStyle.None;
        g.GridColor = Border;
        g.RowHeadersVisible = false;
        g.AllowUserToAddRows = false;
        g.AllowUserToDeleteRows = false;
        g.AllowUserToResizeRows = false;
        g.ReadOnly = true;
        g.MultiSelect = false;
        g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        g.EnableHeadersVisualStyles = false;
        g.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Background,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI Semibold", 9.5f),
            Padding = new Padding(8, 6, 8, 6),
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            SelectionBackColor = Background,
            SelectionForeColor = TextPrimary
        };
        g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        g.ColumnHeadersHeight = 36;
        g.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Surface,
            ForeColor = TextPrimary,
            SelectionBackColor = Color.FromArgb(219, 234, 254),
            SelectionForeColor = TextPrimary,
            Font = BodyFont,
            Padding = new Padding(8, 4, 8, 4)
        };
        g.RowTemplate.Height = 34;
        g.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Background,
            ForeColor = TextPrimary,
            SelectionBackColor = Color.FromArgb(219, 234, 254),
            SelectionForeColor = TextPrimary,
            Font = BodyFont,
            Padding = new Padding(8, 4, 8, 4)
        };
    }
}
