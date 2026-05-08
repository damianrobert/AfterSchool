using AfterSchool.Data;
using AfterSchool.Models;
using AfterSchool.Services;
using AfterSchool.UI;

namespace AfterSchool.Forms;

// Drop-down notification panel. Add to the Form's Controls, position below the bell button.
public sealed class NotificationPanel : Panel
{
    private readonly int _userId;
    private readonly Action _onCountChanged;
    private readonly FlowLayoutPanel _list;
    private readonly Panel _scrollHost;

    public NotificationPanel(int userId, Action onCountChanged)
    {
        _userId = userId;
        _onCountChanged = onCountChanged;

        Width = 340;
        Height = 160;
        BackColor = Theme.Surface;
        Visible = false;
        BorderStyle = BorderStyle.None;

        Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(180, 200, 220));
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        };

        // Header row
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 48,
            BackColor = Theme.Surface,
            Padding = new Padding(16, 0, 8, 0)
        };
        header.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
        };

        var title = new Label
        {
            Text = Loc.T("notifications.title"),
            Font = new Font("Segoe UI Semibold", 11f),
            ForeColor = Theme.TextPrimary,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var markAllBtn = new Button
        {
            Text = Loc.T("notifications.markallread"),
            Dock = DockStyle.Right,
            Width = 110,
            Font = Theme.SmallFont,
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.Surface,
            ForeColor = Theme.TextSecondary,
            Cursor = Cursors.Hand
        };
        markAllBtn.FlatAppearance.BorderSize = 0;
        markAllBtn.FlatAppearance.MouseOverBackColor = Theme.Background;
        markAllBtn.Click += (_, _) =>
        {
            NotificationRepository.MarkAllRead(_userId);
            Reload();
            _onCountChanged();
        };

        header.Controls.Add(title);
        header.Controls.Add(markAllBtn);

        // Scrollable notification list
        _scrollHost = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Theme.Background
        };

        _list = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Theme.Background,
            Padding = new Padding(0, 4, 0, 4)
        };

        _scrollHost.Controls.Add(_list);
        Controls.Add(_scrollHost);
        Controls.Add(header);
    }

    public void Reload()
    {
        _list.Controls.Clear();
        var notifs = NotificationRepository.GetForUser(_userId, 30).ToList();

        if (notifs.Count == 0)
        {
            _list.Controls.Add(new Label
            {
                Text = Loc.T("notifications.empty"),
                Font = Theme.SmallFont,
                ForeColor = Theme.TextSecondary,
                AutoSize = false,
                Width = 330,
                Height = 64,
                TextAlign = ContentAlignment.MiddleCenter
            });
            Height = 120;
            return;
        }

        foreach (var n in notifs)
            _list.Controls.Add(BuildItem(n));

        Height = Math.Min(420, 48 + notifs.Count * 62 + 12);
    }

    private Panel BuildItem(Notification n)
    {
        var item = new Panel
        {
            Width = 330,
            Height = 58,
            BackColor = n.IsRead ? Theme.Surface : Color.FromArgb(239, 246, 255),
            Cursor = Cursors.Hand,
            Margin = new Padding(4, 0, 4, 2)
        };
        item.Paint += (_, e) =>
        {
            if (!n.IsRead)
            {
                using var b = new SolidBrush(Theme.Primary);
                e.Graphics.FillRectangle(b, 0, 0, 3, item.Height);
            }
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawLine(pen, 0, item.Height - 1, item.Width, item.Height - 1);
        };

        var iconLbl = new Label
        {
            Text = TypeIcon(n.Type),
            Font = new Font("Segoe UI Emoji", 14f),
            AutoSize = false,
            Width = 36,
            Height = 58,
            Location = new Point(10, 0),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var msgLbl = new Label
        {
            Text = n.Message,
            Font = n.IsRead ? Theme.SmallFont : new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = n.IsRead ? Theme.TextSecondary : Theme.TextPrimary,
            AutoSize = false,
            Width = 230,
            Height = 36,
            Location = new Point(50, 4),
            AutoEllipsis = true
        };

        var timeLbl = new Label
        {
            Text = FormatRelative(n.CreatedDate),
            Font = new Font("Segoe UI", 7.5f),
            ForeColor = Theme.TextSecondary,
            AutoSize = false,
            Width = 56,
            Height = 14,
            Location = new Point(270, 6),
            TextAlign = ContentAlignment.TopRight
        };

        void OnClick(object? s, EventArgs e)
        {
            if (n.IsRead) return;
            NotificationRepository.MarkRead(n.Id);
            n.IsRead = true;
            item.BackColor = Theme.Surface;
            msgLbl.Font = Theme.SmallFont;
            msgLbl.ForeColor = Theme.TextSecondary;
            item.Invalidate();
            _onCountChanged();
        }

        item.Click   += OnClick;
        iconLbl.Click += OnClick;
        msgLbl.Click  += OnClick;
        timeLbl.Click += OnClick;

        item.Controls.Add(iconLbl);
        item.Controls.Add(msgLbl);
        item.Controls.Add(timeLbl);
        return item;
    }

    private static string TypeIcon(string type) => type switch
    {
        "message"    => "💬",
        "grade"      => "📝",
        "assignment" => "📋",
        "submission" => "📤",
        "enrollment" => "🎓",
        "schedule"   => "📅",
        _            => "🔔"
    };

    private static string FormatRelative(string iso)
    {
        if (!DateTime.TryParse(iso, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
            return "";
        var diff = DateTime.UtcNow - dt;
        if (diff.TotalMinutes < 1) return "now";
        if (diff.TotalHours < 1)   return $"{(int)diff.TotalMinutes}m";
        if (diff.TotalDays < 1)    return $"{(int)diff.TotalHours}h";
        return dt.ToLocalTime().ToString("MMM d");
    }
}
