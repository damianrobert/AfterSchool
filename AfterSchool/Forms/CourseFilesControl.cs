using AfterSchool.Data;
using AfterSchool.Models;
using AfterSchool.Services;
using AfterSchool.UI;

namespace AfterSchool.Forms;

public class CourseFilesControl : UserControl
{
    private readonly ListBox      _courseList = new();
    private readonly DataGridView _grid       = new();
    private readonly Button       _uploadBtn  = new();
    private readonly Button       _downloadBtn = new();
    private readonly Button       _deleteBtn  = new();
    private readonly Label        _emptyLabel = new();

    private List<Course>     _courses = new();
    private List<CourseFile> _files   = new();

    public CourseFilesControl()
    {
        BackColor = Theme.Background;
        BuildLayout();
        LoadCourses();
    }

    private void BuildLayout()
    {
        _uploadBtn.Text   = Loc.T("files.btn.upload");
        _downloadBtn.Text = Loc.T("files.btn.download");
        _deleteBtn.Text   = Loc.T("common.delete");
        Theme.StyleButton(_uploadBtn, primary: true);
        Theme.StyleButton(_downloadBtn);
        Theme.StyleButton(_deleteBtn, danger: true);

        _uploadBtn.Click   += (_, _) => UploadFile();
        _downloadBtn.Click += (_, _) => DownloadSelected();
        _deleteBtn.Click   += (_, _) => DeleteSelected();

        // ── Left: course list ────────────────────────────────────────────────
        var leftPanel = new Panel
        {
            Dock = DockStyle.Left,
            Width = 240,
            BackColor = Theme.Surface,
            Padding = new Padding(0)
        };
        leftPanel.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, leftPanel.Width - 1, leftPanel.Height - 1);
        };

        var courseHeader = new Label
        {
            Text = Loc.T("nav.courses"),
            Font = new Font("Segoe UI Semibold", 10f),
            ForeColor = Theme.TextSecondary,
            Dock = DockStyle.Top,
            Height = 36,
            Padding = new Padding(12, 10, 0, 0),
            BackColor = Theme.Background
        };

        _courseList.Dock = DockStyle.Fill;
        _courseList.BorderStyle = BorderStyle.None;
        _courseList.Font = Theme.BodyFont;
        _courseList.BackColor = Theme.Surface;
        _courseList.ForeColor = Theme.TextPrimary;
        _courseList.ItemHeight = 30;
        _courseList.DrawMode = DrawMode.OwnerDrawFixed;
        _courseList.DrawItem += DrawCourseItem;
        _courseList.SelectedIndexChanged += (_, _) => LoadFiles();

        leftPanel.Controls.Add(_courseList);
        leftPanel.Controls.Add(courseHeader);

        // ── Right: toolbar + grid ────────────────────────────────────────────
        var rightPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Surface,
            Padding = new Padding(20),
            Margin = new Padding(8, 0, 0, 0)
        };
        rightPanel.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, rightPanel.Width - 1, rightPanel.Height - 1);
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
        btns.Controls.Add(_deleteBtn);
        btns.Controls.Add(_downloadBtn);
        btns.Controls.Add(_uploadBtn);
        toolbar.Controls.Add(btns);

        Theme.StyleGrid(_grid);
        _grid.Dock = DockStyle.Fill;
        _grid.SelectionChanged += (_, _) => UpdateButtons();
        _grid.CellDoubleClick  += (_, e) => { if (e.RowIndex >= 0) DownloadSelected(); };

        _emptyLabel.Dock = DockStyle.Fill;
        _emptyLabel.Text = Loc.T("files.select_course");
        _emptyLabel.Font = Theme.BodyFont;
        _emptyLabel.ForeColor = Theme.TextSecondary;
        _emptyLabel.TextAlign = ContentAlignment.MiddleCenter;
        _emptyLabel.Visible = true;

        rightPanel.Controls.Add(_emptyLabel);
        rightPanel.Controls.Add(_grid);
        rightPanel.Controls.Add(toolbar);

        // Spacer between panels
        var gap = new Panel { Dock = DockStyle.Left, Width = 12, BackColor = Theme.Background };

        // Add in reverse dock order (Fill last)
        Controls.Add(rightPanel);
        Controls.Add(gap);
        Controls.Add(leftPanel);
    }

    private void DrawCourseItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _courses.Count) return;
        e.DrawBackground();

        var isSelected = (e.State & DrawItemState.Selected) != 0;
        var bg = isSelected ? Theme.SidebarActive : Theme.Surface;
        var fg = isSelected ? Color.White : Theme.TextPrimary;

        e.Graphics.FillRectangle(new SolidBrush(bg), e.Bounds);
        e.Graphics.DrawString(
            _courses[e.Index].Name,
            Theme.BodyFont,
            new SolidBrush(fg),
            new PointF(e.Bounds.X + 12, e.Bounds.Y + 7));
    }

    private void LoadCourses()
    {
        _courses = CourseRepository.GetAll().ToList();
        _courseList.Items.Clear();
        foreach (var c in _courses)
            _courseList.Items.Add(c.Name);

        _grid.Visible = false;
        _emptyLabel.Text = Loc.T("files.select_course");
        _emptyLabel.Visible = true;
        UpdateButtons();
    }

    private void LoadFiles()
    {
        var course = SelectedCourse();
        if (course == null)
        {
            _files.Clear();
            _grid.Visible = false;
            _emptyLabel.Text = Loc.T("files.select_course");
            _emptyLabel.Visible = true;
            UpdateButtons();
            return;
        }

        _files = CourseFileRepository.GetByCourse(course.Id).ToList();

        if (_files.Count == 0)
        {
            _grid.Visible = false;
            _emptyLabel.Text = Loc.T("files.no_files");
            _emptyLabel.Visible = true;
        }
        else
        {
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
                imgCol.HeaderText  = "";
                imgCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                imgCol.Width       = 42;
                imgCol.ImageLayout = DataGridViewImageCellLayout.Zoom;
            }
            if (_grid.Columns["Id"]   is { } ic) ic.Visible = false;
            if (_grid.Columns["Name"] is { } nc) nc.HeaderText = Loc.T("files.col.name");
            if (_grid.Columns["Size"] is { } sc) { sc.HeaderText = Loc.T("files.col.size"); sc.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; sc.Width = 90; }
            if (_grid.Columns["Date"] is { } dc) { dc.HeaderText = Loc.T("files.col.date"); dc.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; dc.Width = 110; }
            if (_grid.Columns["By"]   is { } bc) { bc.HeaderText = Loc.T("files.col.by");   bc.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; bc.Width = 130; }
        }

        UpdateButtons();
    }

    private void UpdateButtons()
    {
        var hasCourse = SelectedCourse() != null;
        _uploadBtn.Enabled   = hasCourse;
        _downloadBtn.Enabled = hasCourse && SelectedFile() != null;
        _deleteBtn.Enabled   = hasCourse && SelectedFile() != null;
    }

    private Course? SelectedCourse()
    {
        var i = _courseList.SelectedIndex;
        return i >= 0 && i < _courses.Count ? _courses[i] : null;
    }

    private CourseFile? SelectedFile()
    {
        if (_grid.CurrentRow == null || !_grid.Visible) return null;
        var id = (int)_grid.CurrentRow.Cells["Id"].Value;
        return _files.FirstOrDefault(f => f.Id == id);
    }

    private void UploadFile()
    {
        var course = SelectedCourse();
        if (course == null) return;

        using var dlg = new OpenFileDialog
        {
            Title  = Loc.T("files.upload.title"),
            Filter = Loc.T("files.upload.filter"),
            Multiselect = false
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            CourseFileRepository.Upload(course.Id, dlg.FileName, Session.Current?.Username ?? "");
            LoadFiles();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                string.Format(Loc.T("files.upload.error"), ex.Message),
                Loc.T("files.btn.upload"),
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void DownloadSelected()
    {
        var file = SelectedFile();
        if (file == null) return;

        using var dlg = new SaveFileDialog
        {
            Title      = Loc.T("files.download.title"),
            FileName   = file.FileName,
            Filter     = Loc.T("files.upload.filter")
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
                Loc.T("files.btn.download"),
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void DeleteSelected()
    {
        var file = SelectedFile();
        if (file == null) return;

        var result = MessageBox.Show(this,
            string.Format(Loc.T("files.delete.confirm"), file.FileName),
            Loc.T("files.delete.title"),
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes) return;

        try
        {
            CourseFileRepository.Delete(file.Id);
            LoadFiles();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, Loc.T("files.delete.title"),
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
