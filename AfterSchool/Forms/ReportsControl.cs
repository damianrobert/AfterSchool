using System.Diagnostics;
using AfterSchool.Data;
using AfterSchool.Models;
using AfterSchool.Services;
using AfterSchool.UI;

namespace AfterSchool.Forms;

public class ReportsControl : UserControl
{
    private readonly ComboBox _courseSelector = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly DataGridView _classList = new();
    private readonly Label _summary = new();

    public ReportsControl()
    {
        BackColor = Theme.Background;
        BuildLayout();
        LoadCourses();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Theme.Background
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 180));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        root.Controls.Add(BuildExportCard(), 0, 0);
        root.Controls.Add(BuildClassListCard(), 0, 1);

        Controls.Add(root);
    }

    private Panel BuildExportCard()
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Surface,
            Padding = new Padding(20),
            Margin = new Padding(0, 0, 0, 16)
        };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        var header = new Label
        {
            Text = Loc.T("reports.export.header"),
            Font = Theme.HeadingFont,
            ForeColor = Theme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 28
        };
        var hint = new Label
        {
            Text = Loc.T("reports.export.hint"),
            Font = Theme.SmallFont,
            ForeColor = Theme.TextSecondary,
            Dock = DockStyle.Top,
            Height = 20
        };

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = Theme.Surface,
            Padding = new Padding(0, 16, 0, 0)
        };

        AddExportButton(buttons, Loc.T("reports.btn.all_students"),
            () => SafeExport(ExcelExportService.ExportAllStudents, PdfExportService.ExportAllStudents));
        AddExportButton(buttons, Loc.T("reports.btn.courses"),
            () => SafeExport(ExcelExportService.ExportCourses, PdfExportService.ExportCourses));
        AddExportButton(buttons, Loc.T("reports.btn.class_lists"),
            () => SafeExport(ExcelExportService.ExportClassLists, PdfExportService.ExportClassLists));
        AddExportButton(buttons, Loc.T("reports.btn.schedule"),
            () => SafeExport(ExcelExportService.ExportSchedule, PdfExportService.ExportSchedule));

        card.Controls.Add(buttons);
        card.Controls.Add(hint);
        card.Controls.Add(header);
        return card;
    }

    private Panel BuildClassListCard()
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Surface,
            Padding = new Padding(20)
        };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        var header = new Label
        {
            Text = Loc.T("reports.classlist.header"),
            Font = Theme.HeadingFont,
            ForeColor = Theme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 28
        };

        var toolbar = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Theme.Surface };

        var lbl = new Label
        {
            Text = Loc.T("reports.classlist.course_label"),
            Font = new Font("Segoe UI Semibold", 9.5f),
            ForeColor = Theme.TextSecondary,
            Top = 4,
            Left = 0,
            Width = 80,
            Height = 20
        };
        _courseSelector.Top = 22;
        _courseSelector.Left = 0;
        _courseSelector.Width = 280;
        _courseSelector.Font = Theme.BodyFont;
        _courseSelector.SelectedIndexChanged += (_, _) => ReloadClassList();

        _summary.Font = Theme.BodyFont;
        _summary.ForeColor = Theme.TextSecondary;
        _summary.Left = 300;
        _summary.Top = 26;
        _summary.Width = 320;
        _summary.Height = 22;

        var exportBtn = new Button { Text = Loc.T("reports.btn.export_classlist") };
        Theme.StyleButton(exportBtn, primary: true);
        exportBtn.Top = 14;
        exportBtn.Left = 0;
        exportBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        exportBtn.Click += (_, _) =>
        {
            if (_courseSelector.SelectedItem is Course c)
                SafeExport(() => ExcelExportService.ExportClassList(c), () => PdfExportService.ExportClassList(c));
        };

        var btnPanel = new Panel { Dock = DockStyle.Right, Width = 220, Height = 52, BackColor = Theme.Surface };
        exportBtn.Dock = DockStyle.Right;
        btnPanel.Controls.Add(exportBtn);

        toolbar.Controls.Add(lbl);
        toolbar.Controls.Add(_courseSelector);
        toolbar.Controls.Add(_summary);
        toolbar.Controls.Add(btnPanel);

        Theme.StyleGrid(_classList);
        _classList.Dock = DockStyle.Fill;

        card.Controls.Add(_classList);
        card.Controls.Add(toolbar);
        card.Controls.Add(header);
        return card;
    }

    private void AddExportButton(FlowLayoutPanel container, string text, Action action)
    {
        var btn = new Button { Text = text };
        Theme.StyleButton(btn);
        btn.Width = 220;
        btn.Margin = new Padding(0, 0, 12, 8);
        btn.Click += (_, _) => action();
        container.Controls.Add(btn);
    }

    private void LoadCourses()
    {
        var courses = CourseRepository.GetAll().ToList();
        _courseSelector.DataSource = courses;
        _courseSelector.DisplayMember = nameof(Course.Name);
        _courseSelector.ValueMember = nameof(Course.Id);
        ReloadClassList();
    }

    private void ReloadClassList()
    {
        if (_courseSelector.SelectedItem is not Course course)
        {
            _classList.DataSource = null;
            _summary.Text = "";
            return;
        }

        var students = StudentRepository.GetByCourse(course.Id).ToList();
        _classList.DataSource = students.Select(s => new
        {
            s.Id,
            LastName = s.LastName,
            FirstName = s.FirstName,
            s.Email,
            s.ContactNo,
            s.Status
        }).ToList();

        if (_classList.Columns["Id"] is { } idCol) idCol.Visible = false;
        if (_classList.Columns["ContactNo"] is { } c) c.HeaderText = Loc.T("enrollment.col.contact");

        _summary.Text = string.Format(Loc.T("reports.classlist.summary"),
            students.Count, course.Capacity, course.Teacher);
    }

    private void SafeExport(Func<string> excelFn, Func<string> pdfFn)
    {
        var format = ExportTypeDialog.Ask(this);
        if (format == ExportFormat.None) return;

        var exportFn = format == ExportFormat.Excel ? excelFn : pdfFn;
        try
        {
            var path = exportFn();
            var result = MessageBox.Show(
                string.Format(Loc.T("reports.export.success_msg"), path),
                Loc.T("reports.export.success_title"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);
            if (result == DialogResult.Yes)
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                string.Format(Loc.T("reports.export.error_msg"), ex.Message),
                Loc.T("reports.export.error_title"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
