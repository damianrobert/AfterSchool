using AfterSchool.Data;
using AfterSchool.Models;
using AfterSchool.Services;
using AfterSchool.UI;

namespace AfterSchool.Forms;

public class StudentFilesControl : UserControl
{
    private readonly Panel           _cardsView      = new();
    private readonly Panel           _filesView      = new();
    private readonly FlowLayoutPanel _courseFlow     = new();
    private readonly DataGridView    _grid           = new();
    private readonly Button          _downloadBtn    = new();
    private readonly Button          _backBtn        = new();
    private readonly Label           _courseTitleLbl = new();
    private readonly Label           _emptyLabel     = new();

    private List<CourseFile> _files = new();

    public StudentFilesControl()
    {
        BackColor = Theme.Background;
        BuildLayout();
    }

    private void BuildLayout()
    {
        BuildCardsView();
        BuildFilesView();

        _cardsView.Dock = DockStyle.Fill;
        _filesView.Dock = DockStyle.Fill;

        Controls.Add(_filesView);
        Controls.Add(_cardsView);

        _filesView.Visible = false;

        LoadCourseCards();
    }

    // ── Cards view ────────────────────────────────────────────────────────────

    private void BuildCardsView()
    {
        _cardsView.BackColor = Theme.Background;

        var heading = new Label
        {
            Text      = Loc.T("student.files.heading"),
            Font      = new Font("Segoe UI Semibold", 14f),
            ForeColor = Theme.TextPrimary,
            Dock      = DockStyle.Top,
            Height    = 42,
            Padding   = new Padding(2, 8, 0, 0)
        };
        var sub = new Label
        {
            Text      = Loc.T("student.files.subheading"),
            Font      = Theme.BodyFont,
            ForeColor = Theme.TextSecondary,
            Dock      = DockStyle.Top,
            Height    = 26
        };

        _courseFlow.Dock          = DockStyle.Fill;
        _courseFlow.FlowDirection = FlowDirection.LeftToRight;
        _courseFlow.WrapContents  = true;
        _courseFlow.AutoScroll    = true;
        _courseFlow.BackColor     = Theme.Background;
        _courseFlow.Padding       = new Padding(0, 20, 0, 0);

        _cardsView.Controls.Add(_courseFlow);
        _cardsView.Controls.Add(sub);
        _cardsView.Controls.Add(heading);
    }

    private void LoadCourseCards()
    {
        _courseFlow.Controls.Clear();

        var studentId  = Session.Current?.StudentId;
        var courseIds  = studentId.HasValue
            ? StudentRepository.GetEnrolledCourseIds(studentId.Value)
            : new List<int>();

        if (courseIds.Count == 0)
        {
            _courseFlow.Controls.Add(EmptyHint(Loc.T("student.files.no_course")));
            return;
        }

        foreach (var courseId in courseIds)
        {
            var course = CourseRepository.GetById(courseId);
            if (course == null) continue;
            var fileCount = CourseFileRepository.GetByCourse(course.Id).Count();
            _courseFlow.Controls.Add(BuildCourseCard(course, fileCount));
        }
    }

    private Panel BuildCourseCard(Course course, int fileCount)
    {
        var card = new Panel
        {
            Width     = 240,
            Height    = 148,
            BackColor = Theme.Surface,
            Cursor    = Cursors.Hand,
            Margin    = new Padding(0, 0, 20, 20),
            Padding   = new Padding(20, 14, 20, 14)
        };

        var accent = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 4,
            BackColor = Theme.Primary
        };
        var nameLbl = new Label
        {
            Text      = course.Name,
            Font      = new Font("Segoe UI Semibold", 12.5f),
            ForeColor = Theme.TextPrimary,
            Dock      = DockStyle.Top,
            Height    = 34
        };
        var teacherLbl = new Label
        {
            Text      = string.IsNullOrWhiteSpace(course.Teacher) ? "—" : course.Teacher,
            Font      = Theme.SmallFont,
            ForeColor = Theme.TextSecondary,
            Dock      = DockStyle.Top,
            Height    = 22
        };
        var countText = fileCount == 1
            ? Loc.T("student.files.count.one")
            : string.Format(Loc.T("student.files.count.many"), fileCount);
        var fileLbl = new Label
        {
            Text      = countText,
            Font      = new Font("Segoe UI", 9f),
            ForeColor = Theme.Primary,
            Dock      = DockStyle.Bottom,
            Height    = 22,
            TextAlign = ContentAlignment.BottomLeft
        };

        card.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        var hoverBg = Color.FromArgb(239, 246, 255);

        void OnLeave(object? _, EventArgs __)
        {
            var pos = card.PointToClient(Cursor.Position);
            if (!card.ClientRectangle.Contains(pos))
                card.BackColor = Theme.Surface;
        }

        void Register(Control c)
        {
            c.MouseEnter += (_, _) => card.BackColor = hoverBg;
            c.MouseLeave += OnLeave;
            c.Click       += (_, _) => ShowCourseFiles(course);
        }

        Register(card);
        Register(accent);
        Register(nameLbl);
        Register(teacherLbl);
        Register(fileLbl);

        card.Controls.Add(fileLbl);
        card.Controls.Add(teacherLbl);
        card.Controls.Add(nameLbl);
        card.Controls.Add(accent);

        return card;
    }

    // ── Files view ────────────────────────────────────────────────────────────

    private void BuildFilesView()
    {
        _filesView.BackColor = Theme.Background;

        var topBar = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 52,
            BackColor = Theme.Background,
            Padding   = new Padding(0, 8, 0, 4)
        };

        _backBtn.Text = Loc.T("student.files.back");
        Theme.StyleButton(_backBtn);
        _backBtn.Click += (_, _) => ShowCardsView();

        _courseTitleLbl.Font      = new Font("Segoe UI Semibold", 13f);
        _courseTitleLbl.ForeColor = Theme.TextPrimary;
        _courseTitleLbl.Dock      = DockStyle.Fill;
        _courseTitleLbl.TextAlign = ContentAlignment.MiddleLeft;
        _courseTitleLbl.Padding   = new Padding(12, 0, 0, 0);

        topBar.Controls.Add(_courseTitleLbl);
        topBar.Controls.Add(_backBtn);

        var card = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Theme.Surface,
            Padding   = new Padding(20)
        };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        _downloadBtn.Text = Loc.T("student.files.btn.download");
        Theme.StyleButton(_downloadBtn, primary: true);
        _downloadBtn.Enabled = false;
        _downloadBtn.Click  += (_, _) => DownloadSelected();

        var toolbar = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Theme.Surface };
        var btns = new FlowLayoutPanel
        {
            Dock          = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents  = false,
            AutoSize      = true,
            AutoSizeMode  = AutoSizeMode.GrowAndShrink,
            BackColor     = Theme.Surface,
            Padding       = new Padding(0, 6, 0, 6)
        };
        btns.Controls.Add(_downloadBtn);
        toolbar.Controls.Add(btns);

        Theme.StyleGrid(_grid);
        _grid.Dock             = DockStyle.Fill;
        _grid.SelectionChanged += (_, _) => _downloadBtn.Enabled = _grid.CurrentRow != null;
        _grid.CellDoubleClick  += (_, e) => { if (e.RowIndex >= 0) DownloadSelected(); };

        _emptyLabel.Dock      = DockStyle.Fill;
        _emptyLabel.Font      = Theme.BodyFont;
        _emptyLabel.ForeColor = Theme.TextSecondary;
        _emptyLabel.TextAlign = ContentAlignment.MiddleCenter;

        card.Controls.Add(_emptyLabel);
        card.Controls.Add(_grid);
        card.Controls.Add(toolbar);

        _filesView.Controls.Add(card);
        _filesView.Controls.Add(topBar);
    }

    private void ShowCourseFiles(Course course)
    {
        _courseTitleLbl.Text = course.Name;
        _downloadBtn.Enabled = false;

        _grid.DataSource = null;
        _grid.Columns.Clear();

        _files = CourseFileRepository.GetByCourse(course.Id).ToList();

        if (_files.Count == 0)
        {
            ShowEmpty(Loc.T("student.files.no_files"));
        }
        else
        {
            _emptyLabel.Visible = false;
            _grid.Visible       = true;
            _grid.DataSource    = _files.Select(f => new
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

        _cardsView.Visible = false;
        _filesView.Visible = true;
        _filesView.BringToFront();
    }

    private void ShowCardsView()
    {
        _filesView.Visible = false;
        _cardsView.Visible = true;
        _cardsView.BringToFront();
    }

    private void ShowEmpty(string message)
    {
        _grid.Visible        = false;
        _downloadBtn.Enabled = false;
        _emptyLabel.Text     = message;
        _emptyLabel.Visible  = true;
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

    private static Label EmptyHint(string message) => new()
    {
        Text      = message,
        Font      = Theme.BodyFont,
        ForeColor = Theme.TextSecondary,
        AutoSize  = true,
        Padding   = new Padding(4, 0, 0, 0)
    };
}
