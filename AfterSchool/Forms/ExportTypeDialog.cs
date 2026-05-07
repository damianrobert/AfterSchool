using System.Drawing.Drawing2D;
using AfterSchool.Services;
using AfterSchool.UI;

namespace AfterSchool.Forms;

public enum ExportFormat { None, Excel, Pdf }

internal sealed class ExportTypeDialog : Form
{
    public ExportFormat ChosenFormat { get; private set; } = ExportFormat.None;

    public static ExportFormat Ask(IWin32Window? owner = null)
    {
        using var dlg = new ExportTypeDialog();
        dlg.ShowDialog(owner);
        return dlg.ChosenFormat;
    }

    public ExportTypeDialog()
    {
        Text = Loc.T("export.dialog.title");
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Size = new Size(440, 260);
        BackColor = Theme.Surface;
        Font = Theme.BodyFont;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Theme.Surface,
            Padding = new Padding(28, 22, 28, 18)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));

        var hint = new Label
        {
            Text = Loc.T("export.dialog.hint"),
            Font = Theme.BodyFont,
            ForeColor = Theme.TextSecondary,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var cardRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Theme.Surface,
            Padding = new Padding(0, 4, 0, 4)
        };

        var excelCard = MakeFormatCard(".xlsx", Color.FromArgb(33, 115, 70),
            Loc.T("export.dialog.excel"), ExportFormat.Excel);
        var pdfCard = MakeFormatCard(".pdf", Color.FromArgb(220, 38, 38),
            Loc.T("export.dialog.pdf"), ExportFormat.Pdf);

        pdfCard.Margin = new Padding(14, 0, 0, 0);

        cardRow.Controls.Add(excelCard);
        cardRow.Controls.Add(pdfCard);

        var cancelLink = new LinkLabel
        {
            Text = Loc.T("export.dialog.cancel"),
            Font = Theme.SmallFont,
            LinkColor = Theme.TextSecondary,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight
        };
        cancelLink.LinkClicked += (_, _) =>
        {
            ChosenFormat = ExportFormat.None;
            DialogResult = DialogResult.Cancel;
            Close();
        };

        root.Controls.Add(hint, 0, 0);
        root.Controls.Add(cardRow, 0, 1);
        root.Controls.Add(cancelLink, 0, 2);

        Controls.Add(root);
    }

    private Panel MakeFormatCard(string badgeText, Color badgeColor, string label, ExportFormat format)
    {
        var card = new Panel
        {
            Width = 168,
            Height = 86,
            BackColor = Color.White,
            Cursor = Cursors.Hand
        };

        card.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        var badge = new BadgeLabel(badgeColor)
        {
            Text = badgeText,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            AutoSize = false,
            Left = 14,
            Top = 14,
            Width = 46,
            Height = 22
        };

        var lbl = new Label
        {
            Text = label,
            Font = new Font("Segoe UI Semibold", 10.5f),
            ForeColor = Theme.TextPrimary,
            BackColor = Color.Transparent,
            AutoSize = false,
            Left = 14,
            Top = 46,
            Width = card.Width - 28,
            Height = 28,
            TextAlign = ContentAlignment.MiddleLeft
        };

        card.Controls.Add(badge);
        card.Controls.Add(lbl);

        void Click(object? s, EventArgs e)
        {
            ChosenFormat = format;
            DialogResult = DialogResult.OK;
            Close();
        }

        void Enter(object? s, EventArgs e)
        {
            card.BackColor = Color.FromArgb(248, 250, 252);
            card.Invalidate();
        }

        void Leave(object? s, EventArgs e)
        {
            card.BackColor = Color.White;
            card.Invalidate();
        }

        foreach (Control c in new Control[] { card, badge, lbl })
        {
            c.Click += Click;
            c.MouseEnter += Enter;
            c.MouseLeave += Leave;
        }

        return card;
    }

    private sealed class BadgeLabel : Label
    {
        private readonly Color _fill;

        public BadgeLabel(Color fill)
        {
            _fill = fill;
            BackColor = Color.Transparent;
            ForeColor = Color.White;
            TextAlign = ContentAlignment.MiddleCenter;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var brush = new SolidBrush(_fill);
            using var path = RoundedPath(rect, 5);
            e.Graphics.FillPath(brush, path);
            using var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            using var textBrush = new SolidBrush(Color.White);
            e.Graphics.DrawString(Text, Font, textBrush, RectangleF.FromLTRB(0, 0, Width, Height), sf);
        }

        private static GraphicsPath RoundedPath(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(r.Right - radius * 2, r.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(r.Right - radius * 2, r.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(r.X, r.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
