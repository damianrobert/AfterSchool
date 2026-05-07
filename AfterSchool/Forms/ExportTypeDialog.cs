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
        Size = new Size(420, 240);
        BackColor = Theme.Surface;
        Font = Theme.BodyFont;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Theme.Surface,
            Padding = new Padding(24, 20, 24, 16)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

        var title = new Label
        {
            Text = Loc.T("export.dialog.hint"),
            Font = Theme.BodyFont,
            ForeColor = Theme.TextSecondary,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var btnRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Theme.Surface,
            Padding = new Padding(0, 8, 0, 8)
        };

        var excelBtn = MakeFormatButton(Loc.T("export.dialog.excel"), ExportFormat.Excel, Theme.Primary);
        var pdfBtn = MakeFormatButton(Loc.T("export.dialog.pdf"), ExportFormat.Pdf, Color.FromArgb(220, 38, 38));

        excelBtn.Width = 158;
        pdfBtn.Width = 158;
        pdfBtn.Margin = new Padding(12, 0, 0, 0);

        btnRow.Controls.Add(excelBtn);
        btnRow.Controls.Add(pdfBtn);

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

        root.Controls.Add(title, 0, 0);
        root.Controls.Add(btnRow, 0, 1);
        root.Controls.Add(cancelLink, 0, 2);

        Controls.Add(root);
    }

    private Button MakeFormatButton(string label, ExportFormat format, Color color)
    {
        var btn = new Button
        {
            Text = label,
            Height = 48,
            FlatStyle = FlatStyle.Flat,
            BackColor = color,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 10f),
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.Click += (_, _) =>
        {
            ChosenFormat = format;
            DialogResult = DialogResult.OK;
            Close();
        };
        return btn;
    }
}
