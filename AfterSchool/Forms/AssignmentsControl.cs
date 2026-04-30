using AfterSchool.Data;
using AfterSchool.Models;
using AfterSchool.Services;
using AfterSchool.UI;

namespace AfterSchool.Forms;

// ── Teacher / Admin: manage assignments per course ────────────────────────────

public class AssignmentsControl : UserControl
{
    private readonly ListBox      _courseList    = new();
    private readonly DataGridView _grid          = new();
    private readonly Button       _newBtn        = new();
    private readonly Button       _editBtn       = new();
    private readonly Button       _deleteBtn     = new();
    private readonly Button       _submissionsBtn = new();
    private readonly Label        _emptyLabel    = new();

    private List<Course>         _courses     = new();
    private List<AssignmentView> _assignments = new();

    public AssignmentsControl()
    {
        BackColor = Theme.Background;
        BuildLayout();
        LoadCourses();
    }

    private void BuildLayout()
    {
        _newBtn.Text         = Loc.T("assignments.btn.new");
        _editBtn.Text        = Loc.T("assignments.btn.edit");
        _deleteBtn.Text      = Loc.T("assignments.btn.delete");
        _submissionsBtn.Text = Loc.T("assignments.btn.submissions");

        Theme.StyleButton(_newBtn, primary: true);
        Theme.StyleButton(_editBtn);
        Theme.StyleButton(_deleteBtn, danger: true);
        Theme.StyleButton(_submissionsBtn);

        _newBtn.Click         += (_, _) => OpenEditor(null);
        _editBtn.Click        += (_, _) => EditSelected();
        _deleteBtn.Click      += (_, _) => DeleteSelected();
        _submissionsBtn.Click += (_, _) => ViewSubmissions();

        // ── Left: course list ────────────────────────────────────────────────
        var leftPanel = new Panel
        {
            Dock = DockStyle.Left,
            Width = 240,
            BackColor = Theme.Surface
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
        _courseList.SelectedIndexChanged += (_, _) => LoadAssignments();

        leftPanel.Controls.Add(_courseList);
        leftPanel.Controls.Add(courseHeader);

        // ── Right: toolbar + grid ────────────────────────────────────────────
        var rightPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Surface,
            Padding = new Padding(20)
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
        btns.Controls.Add(_editBtn);
        btns.Controls.Add(_submissionsBtn);
        btns.Controls.Add(_newBtn);
        toolbar.Controls.Add(btns);

        Theme.StyleGrid(_grid);
        _grid.Dock = DockStyle.Fill;
        _grid.SelectionChanged += (_, _) => UpdateButtons();
        _grid.CellDoubleClick  += (_, e) => { if (e.RowIndex >= 0) EditSelected(); };

        _emptyLabel.Dock = DockStyle.Fill;
        _emptyLabel.Text = Loc.T("assignments.select_course");
        _emptyLabel.Font = Theme.BodyFont;
        _emptyLabel.ForeColor = Theme.TextSecondary;
        _emptyLabel.TextAlign = ContentAlignment.MiddleCenter;

        rightPanel.Controls.Add(_emptyLabel);
        rightPanel.Controls.Add(_grid);
        rightPanel.Controls.Add(toolbar);

        var gap = new Panel { Dock = DockStyle.Left, Width = 12, BackColor = Theme.Background };

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
        e.Graphics.DrawString(_courses[e.Index].Name, Theme.BodyFont,
            new SolidBrush(fg), new PointF(e.Bounds.X + 12, e.Bounds.Y + 7));
    }

    private void LoadCourses()
    {
        _courses = CourseRepository.GetAll().ToList();
        _courseList.Items.Clear();
        foreach (var c in _courses) _courseList.Items.Add(c.Name);
        ShowEmpty(Loc.T("assignments.select_course"));
        UpdateButtons();
    }

    private void LoadAssignments()
    {
        var course = SelectedCourse();
        if (course == null) { ShowEmpty(Loc.T("assignments.select_course")); UpdateButtons(); return; }

        _assignments = AssignmentRepository.GetByCourse(course.Id).ToList();

        if (_assignments.Count == 0)
        {
            ShowEmpty(Loc.T("assignments.no_assignments"));
            UpdateButtons();
            return;
        }

        _emptyLabel.Visible = false;
        _grid.Visible = true;
        _grid.DataSource = _assignments.Select(a => new
        {
            a.Id,
            Title   = a.Title,
            Due     = a.DueDate,
            Submitted = $"{a.SubmissionCount} submitted"
        }).ToList();

        if (_grid.Columns["Id"]        is { } ic) ic.Visible = false;
        if (_grid.Columns["Title"]     is { } tc) tc.HeaderText = Loc.T("assignments.col.title");
        if (_grid.Columns["Due"]       is { } dc) { dc.HeaderText = Loc.T("assignments.col.duedate"); dc.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; dc.Width = 110; }
        if (_grid.Columns["Submitted"] is { } sc) { sc.HeaderText = Loc.T("assignments.col.submissions"); sc.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; sc.Width = 120; }

        UpdateButtons();
    }

    private void ShowEmpty(string msg)
    {
        _grid.Visible = false;
        _emptyLabel.Text = msg;
        _emptyLabel.Visible = true;
    }

    private void UpdateButtons()
    {
        var hasCourse     = SelectedCourse() != null;
        var hasAssignment = SelectedAssignment() != null;
        _newBtn.Enabled         = hasCourse;
        _editBtn.Enabled        = hasAssignment;
        _deleteBtn.Enabled      = hasAssignment;
        _submissionsBtn.Enabled = hasAssignment;
    }

    private Course? SelectedCourse()
    {
        var i = _courseList.SelectedIndex;
        return i >= 0 && i < _courses.Count ? _courses[i] : null;
    }

    private AssignmentView? SelectedAssignment()
    {
        if (_grid.CurrentRow == null || !_grid.Visible) return null;
        var id = (int)_grid.CurrentRow.Cells["Id"].Value;
        return _assignments.FirstOrDefault(a => a.Id == id);
    }

    private void OpenEditor(AssignmentView? existing)
    {
        var course = SelectedCourse();
        if (course == null) return;
        var model = existing == null ? null : AssignmentRepository.GetById(existing.Id);
        using var dlg = new AssignmentEditorDialog(course.Id, model);
        if (dlg.ShowDialog(this) == DialogResult.OK) LoadAssignments();
    }

    private void EditSelected()
    {
        var a = SelectedAssignment();
        if (a != null) OpenEditor(a);
    }

    private void DeleteSelected()
    {
        var a = SelectedAssignment();
        if (a == null) return;
        var result = MessageBox.Show(this,
            string.Format(Loc.T("assignments.delete.confirm"), a.Title),
            Loc.T("assignments.delete.title"),
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes) return;
        AssignmentRepository.Delete(a.Id);
        LoadAssignments();
    }

    private void ViewSubmissions()
    {
        var a = SelectedAssignment();
        if (a == null) return;
        using var dlg = new AssignmentSubmissionsDialog(a);
        dlg.ShowDialog(this);
    }
}

// ── Assignment editor dialog ──────────────────────────────────────────────────

internal sealed class AssignmentEditorDialog : Form
{
    private readonly int      _courseId;
    private readonly Assignment? _existing;
    private readonly TextBox  _title  = new();
    private readonly RichTextBox _desc = new();
    private readonly DateTimePicker _due = new() { Format = DateTimePickerFormat.Short };
    private readonly Label    _error  = new();

    public AssignmentEditorDialog(int courseId, Assignment? existing)
    {
        _courseId = courseId;
        _existing = existing;

        Text = existing == null
            ? Loc.T("assignments.editor.title.new")
            : Loc.T("assignments.editor.title.edit");
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Size = new Size(520, 420);
        BackColor = Theme.Surface;
        Font = Theme.BodyFont;

        Theme.StyleTextBox(_title);
        _title.Font = new Font("Segoe UI", 11f);
        _title.Text = existing?.Title ?? "";

        _desc.BackColor = Theme.Surface;
        _desc.ForeColor = Theme.TextPrimary;
        _desc.Font = Theme.BodyFont;
        _desc.BorderStyle = BorderStyle.FixedSingle;
        _desc.Text = existing?.Description ?? "";

        _due.Value = DateTime.TryParse(existing?.DueDate, out var d) ? d : DateTime.Today.AddDays(7);

        _error.ForeColor = Theme.Danger;
        _error.Font = Theme.SmallFont;
        _error.Height = 22;
        _error.Dock = DockStyle.Top;
        _error.TextAlign = ContentAlignment.MiddleLeft;

        var root = new Panel { Dock = DockStyle.Fill, Padding = new Padding(28, 24, 28, 16), BackColor = Theme.Surface };

        var ok     = new Button { Text = existing == null ? Loc.T("common.create") : Loc.T("common.save") };
        var cancel = new Button { Text = Loc.T("common.cancel"), DialogResult = DialogResult.Cancel };
        Theme.StyleButton(ok, primary: true);
        Theme.StyleButton(cancel);
        ok.Click += (_, _) => Save();

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 0, 8),
            BackColor = Theme.Surface
        };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);

        root.Controls.Add(Spacer(6));
        root.Controls.Add(_error);
        root.Controls.Add(_due);
        root.Controls.Add(FieldLabel(Loc.T("assignments.editor.field.duedate")));
        root.Controls.Add(Spacer(8));
        root.Controls.Add(_desc);
        root.Controls.Add(FieldLabel(Loc.T("assignments.editor.field.description")));
        root.Controls.Add(Spacer(8));
        root.Controls.Add(_title);
        root.Controls.Add(FieldLabel(Loc.T("assignments.editor.field.title")));

        _desc.Dock = DockStyle.Top;
        _desc.Height = 90;
        _title.Dock = DockStyle.Top;
        _title.Height = 32;
        _due.Dock = DockStyle.Top;
        _due.Height = 30;

        Controls.Add(root);
        Controls.Add(buttons);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private static Label FieldLabel(string text) => new()
    {
        Text = text,
        Font = new Font("Segoe UI Semibold", 9.5f),
        ForeColor = Theme.TextSecondary,
        Dock = DockStyle.Top,
        Height = 22,
        TextAlign = ContentAlignment.BottomLeft
    };

    private static Panel Spacer(int h) =>
        new() { Dock = DockStyle.Top, Height = h, BackColor = Theme.Surface };

    private void Save()
    {
        _error.Text = "";
        if (string.IsNullOrWhiteSpace(_title.Text))
        {
            _error.Text = Loc.T("assignments.editor.validation.required");
            return;
        }

        var a = _existing ?? new Assignment
        {
            CourseId    = _courseId,
            CreatedBy   = Session.Current?.Username ?? "",
            CreatedDate = DateTime.Today.ToString("yyyy-MM-dd")
        };
        a.Title       = _title.Text.Trim();
        a.Description = _desc.Text.Trim();
        a.DueDate     = _due.Value.ToString("yyyy-MM-dd");

        if (_existing == null) AssignmentRepository.Insert(a);
        else                   AssignmentRepository.Update(a);

        DialogResult = DialogResult.OK;
        Close();
    }
}

// ── Submissions viewer dialog (teacher/admin) ─────────────────────────────────

internal sealed class AssignmentSubmissionsDialog : Form
{
    private readonly AssignmentView                _assignment;
    private readonly DataGridView                  _grid  = new();
    private readonly Button                        _downloadBtn = new();
    private List<AssignmentSubmissionView>         _subs  = new();

    public AssignmentSubmissionsDialog(AssignmentView assignment)
    {
        _assignment = assignment;
        Text = string.Format(Loc.T("assignments.submissions.title"), assignment.Title);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        Size = new Size(700, 480);
        BackColor = Theme.Surface;
        Font = Theme.BodyFont;

        BuildLayout();
        LoadSubmissions();
    }

    private void BuildLayout()
    {
        _downloadBtn.Text = Loc.T("assignments.submissions.btn.download");
        Theme.StyleButton(_downloadBtn, primary: true);
        _downloadBtn.Enabled = false;
        _downloadBtn.Click += (_, _) => DownloadSelected();

        var card = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            BackColor = Theme.Surface
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

        card.Controls.Add(_grid);
        card.Controls.Add(toolbar);
        Controls.Add(card);
    }

    private void LoadSubmissions()
    {
        _subs = AssignmentRepository.GetSubmissions(_assignment.Id).ToList();

        if (_subs.Count == 0)
        {
            _grid.Visible = false;
            var lbl = new Label
            {
                Text = Loc.T("assignments.submissions.no_submissions"),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = Theme.BodyFont,
                ForeColor = Theme.TextSecondary
            };
            Controls.Add(lbl);
            return;
        }

        _grid.DataSource = _subs.Select(s => new
        {
            s.Id,
            StoredName = s.StoredName,
            Icon     = (Image)FileIconHelper.Get(s.FileName),
            Student  = s.StudentName,
            File     = s.FileName,
            Size     = CourseFileRepository.FormatSize(s.FileSize),
            Date     = s.SubmittedDate
        }).ToList();

        if (_grid.Columns["Id"]         is { } ic) ic.Visible = false;
        if (_grid.Columns["StoredName"] is { } sn) sn.Visible = false;
        if (_grid.Columns["Icon"]    is DataGridViewImageColumn imgCol)
        {
            imgCol.HeaderText   = "";
            imgCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            imgCol.Width        = 42;
            imgCol.ImageLayout  = DataGridViewImageCellLayout.Zoom;
        }
        if (_grid.Columns["Student"] is { } sc) sc.HeaderText = Loc.T("assignments.submissions.col.student");
        if (_grid.Columns["File"]    is { } fc) fc.HeaderText = Loc.T("assignments.submissions.col.file");
        if (_grid.Columns["Size"]    is { } szc) { szc.HeaderText = Loc.T("assignments.submissions.col.size"); szc.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; szc.Width = 90; }
        if (_grid.Columns["Date"]    is { } dc) { dc.HeaderText = Loc.T("assignments.submissions.col.date");   dc.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; dc.Width = 110; }
    }

    private void DownloadSelected()
    {
        if (_grid.CurrentRow == null) return;
        var storedName = (string)_grid.CurrentRow.Cells["StoredName"].Value;
        var fileName   = (string)_grid.CurrentRow.Cells["File"].Value;

        using var dlg = new SaveFileDialog
        {
            Title    = Loc.T("assignments.download.title"),
            FileName = fileName,
            Filter   = Loc.T("assignments.upload.filter")
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try { AssignmentRepository.DownloadSubmission(storedName, dlg.FileName); }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                string.Format(Loc.T("assignments.download.error"), ex.Message),
                Loc.T("assignments.submissions.btn.download"),
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
