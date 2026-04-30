using AfterSchool.Data;
using AfterSchool.Models;
using AfterSchool.Services;
using AfterSchool.UI;

namespace AfterSchool.Forms;

public class StudentFilesControl : UserControl
{
    private readonly DataGridView _grid        = new();
    private readonly Button       _downloadBtn = new();
    private readonly Label        _emptyLabel  = new();

    private List<CourseFile> _files = new();

    public StudentFilesControl()
    {
        BackColor = Theme.Background;
        BuildLayout();
    }

    private void BuildLayout()
    {
        _downloadBtn.Text = Loc.T("student.files.btn.download");
        Theme.StyleButton(_downloadBtn, primary: true);
        _downloadBtn.Enabled = false;
        _downloadBtn.Click += (_, _) => DownloadSelected();

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

        var toolbar = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Theme.Surface };
        var btns = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Theme.Surface,
            Padding = new Padding(0, 6, 0, 6)
        };
        btns.Controls.Add(_downloadBtn);
        toolbar.Controls.Add(btns);

        Theme.StyleGrid(_grid);
        _grid.Dock = DockStyle.Fill;
        _grid.SelectionChanged += (_, _) => _downloadBtn.Enabled = _grid.CurrentRow != null;
        _grid.CellDoubleClick  += (_, e) => { if (e.RowIndex >= 0) DownloadSelected(); };

        _emptyLabel.Dock = DockStyle.Fill;
        _emptyLabel.Font = Theme.BodyFont;
        _emptyLabel.ForeColor = Theme.TextSecondary;
        _emptyLabel.TextAlign = ContentAlignment.MiddleCenter;

        card.Controls.Add(_emptyLabel);
        card.Controls.Add(_grid);
        card.Controls.Add(toolbar);

        Controls.Add(card);

        LoadFiles();
    }

    private void LoadFiles()
    {
        var studentId = Session.Current?.StudentId;
        var student   = studentId.HasValue ? StudentRepository.GetById(studentId.Value) : null;

        if (student?.EnrolledCourseId == null)
        {
            ShowEmpty(Loc.T("student.files.no_course"));
            return;
        }

        _files = CourseFileRepository.GetByCourse(student.EnrolledCourseId.Value).ToList();

        if (_files.Count == 0)
        {
            ShowEmpty(Loc.T("student.files.no_files"));
            return;
        }

        _emptyLabel.Visible = false;
        _grid.Visible = true;
        _grid.DataSource = _files.Select(f => new
        {
            Icon = (Image)FileIconHelper.Get(f.FileName),
            f.Id,
            Name = f.FileName,
            Size = CourseFileRepository.FormatSize(f.FileSize),
            Date = f.UploadedDate,
            By   = f.UploadedBy
        }).ToList();

        if (_grid.Columns["Icon"] is DataGridViewImageColumn imgCol)
        {
            imgCol.HeaderText   = "";
            imgCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            imgCol.Width        = 42;
            imgCol.ImageLayout  = DataGridViewImageCellLayout.Zoom;
        }
        if (_grid.Columns["Id"]   is { } ic) ic.Visible = false;
        if (_grid.Columns["Name"] is { } nc) nc.HeaderText = Loc.T("files.col.name");
        if (_grid.Columns["Size"] is { } sc) { sc.HeaderText = Loc.T("files.col.size"); sc.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; sc.Width = 90; }
        if (_grid.Columns["Date"] is { } dc) { dc.HeaderText = Loc.T("files.col.date"); dc.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; dc.Width = 110; }
        if (_grid.Columns["By"]   is { } bc) { bc.HeaderText = Loc.T("files.col.by");   bc.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; bc.Width = 130; }
    }

    private void ShowEmpty(string message)
    {
        _grid.Visible = false;
        _downloadBtn.Enabled = false;
        _emptyLabel.Text = message;
        _emptyLabel.Visible = true;
    }

    private void DownloadSelected()
    {
        if (_grid.CurrentRow == null) return;
        var id   = (int)_grid.CurrentRow.Cells["Id"].Value;
        var file = _files.FirstOrDefault(f => f.Id == id);
        if (file == null) return;

        using var dlg = new SaveFileDialog
        {
            Title    = Loc.T("files.download.title"),
            FileName = file.FileName,
            Filter   = Loc.T("files.upload.filter")
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            CourseFileRepository.Download(file.Id, dlg.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                string.Format(Loc.T("files.download.error"), ex.Message),
                Loc.T("student.files.btn.download"),
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
