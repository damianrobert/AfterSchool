using AfterSchool.Data;
using AfterSchool.Models;
using AfterSchool.Services;
using AfterSchool.UI;

namespace AfterSchool.Forms;

public class StudentAssignmentsControl : UserControl
{
    private readonly DataGridView _grid        = new();
    private readonly Button       _submitBtn   = new();
    private readonly Button       _downloadBtn = new();
    private readonly Label        _descLabel   = new();
    private readonly Label        _emptyLabel  = new();
    private readonly Button       _todoBtn     = new();
    private readonly Button       _doneBtn     = new();

    private List<AssignmentStudentView> _assignments = new();
    private bool _showDone;
    private int? _studentId;

    public StudentAssignmentsControl()
    {
        BackColor = Theme.Background;
        _studentId = Session.Current?.StudentId;
        BuildLayout();
        LoadAssignments();
    }

    private void BuildLayout()
    {
        _submitBtn.Text   = Loc.T("student.assignments.btn.submit");
        _downloadBtn.Text = Loc.T("student.assignments.btn.download");
        Theme.StyleButton(_submitBtn, primary: true);
        Theme.StyleButton(_downloadBtn);
        _submitBtn.Enabled   = false;
        _downloadBtn.Enabled = false;

        _submitBtn.Click   += (_, _) => SubmitFile();
        _downloadBtn.Click += (_, _) => DownloadMySubmission();

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

        // Toolbar
        var toolbar = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Theme.Surface };

        // Toggle buttons (left side)
        var togglePanel = BuildToggle();
        togglePanel.Dock = DockStyle.Left;
        togglePanel.Width = _todoBtn.Width + _doneBtn.Width + 2; // 2 = border + divider
        toolbar.Controls.Add(togglePanel);

        // Action buttons (right side)
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
        btns.Controls.Add(_submitBtn);
        toolbar.Controls.Add(btns);

        // Description panel at bottom
        var descPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 80,
            BackColor = Theme.Background,
            Padding = new Padding(12, 8, 12, 8),
            Visible = false
        };
        descPanel.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawLine(pen, 0, 0, descPanel.Width, 0);
        };

        var descHeader = new Label
        {
            Text = Loc.T("student.assignments.description"),
            Font = new Font("Segoe UI Semibold", 9.5f),
            ForeColor = Theme.TextSecondary,
            Dock = DockStyle.Top,
            Height = 20
        };
        _descLabel.Dock = DockStyle.Fill;
        _descLabel.Font = Theme.BodyFont;
        _descLabel.ForeColor = Theme.TextPrimary;
        descPanel.Controls.Add(_descLabel);
        descPanel.Controls.Add(descHeader);

        // Grid
        Theme.StyleGrid(_grid);
        _grid.Dock = DockStyle.Fill;
        _grid.SelectionChanged += (_, _) => OnSelectionChanged(descPanel);
        _grid.CellDoubleClick  += (_, e) => { if (e.RowIndex >= 0) SubmitFile(); };

        // CellFormatting: look up by ID to be filter-safe
        _grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (_grid.Columns[e.ColumnIndex].Name != "Status") return;
            if (_grid.Rows[e.RowIndex].Cells["Id"].Value is not int id) return;
            var a = _assignments.FirstOrDefault(x => x.Id == id);
            if (a == null || e.CellStyle == null) return;
            e.CellStyle.ForeColor = a.IsSubmitted ? Theme.Success
                                  : a.IsOverdue   ? Theme.Danger
                                  :                 Theme.TextSecondary;
            e.CellStyle.Font = new Font("Segoe UI Semibold", 9.5f);
        };

        // Empty label
        _emptyLabel.Dock = DockStyle.Fill;
        _emptyLabel.Font = Theme.BodyFont;
        _emptyLabel.ForeColor = Theme.TextSecondary;
        _emptyLabel.TextAlign = ContentAlignment.MiddleCenter;

        card.Controls.Add(descPanel);
        card.Controls.Add(_emptyLabel);
        card.Controls.Add(_grid);
        card.Controls.Add(toolbar);

        Controls.Add(card);
    }

    private Panel BuildToggle()
    {
        _todoBtn.Text = Loc.T("student.assignments.filter.todo");
        _doneBtn.Text = Loc.T("student.assignments.filter.done");

        foreach (var b in new[] { _todoBtn, _doneBtn })
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.Font = Theme.BodyFont;
            b.Cursor = Cursors.Hand;
            b.Dock = DockStyle.Left;
            b.Height = 30;
            b.Padding = new Padding(14, 0, 14, 0);
            b.AutoSize = true;
            b.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        }

        // Container with a thin border that acts as the segmented control outline
        var container = new Panel
        {
            BackColor = Theme.Border,
            Padding = new Padding(1),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Height = 32
        };
        // Vertical divider between the two buttons (1px gap via BackColor)
        var inner = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Theme.Border // shows as 1px gap between buttons
        };

        _todoBtn.Click += (_, _) => { _showDone = false; RefreshToggle(); ApplyFilter(); };
        _doneBtn.Click += (_, _) => { _showDone = true;  RefreshToggle(); ApplyFilter(); };

        inner.Controls.Add(_todoBtn);
        inner.Controls.Add(_doneBtn);
        container.Controls.Add(inner);

        RefreshToggle();

        // Vertically center the container inside the toolbar
        var wrapper = new Panel { BackColor = Theme.Surface, Padding = new Padding(0, 8, 0, 8) };
        wrapper.Controls.Add(container);
        container.Top = 0;

        return wrapper;
    }

    private void RefreshToggle()
    {
        // Active tab: primary fill; inactive: plain surface
        _todoBtn.BackColor = !_showDone ? Theme.Primary  : Theme.Surface;
        _todoBtn.ForeColor = !_showDone ? Color.White     : Theme.TextSecondary;
        _todoBtn.FlatAppearance.MouseOverBackColor = !_showDone ? Theme.PrimaryHover : Theme.Background;

        _doneBtn.BackColor = _showDone  ? Theme.Primary  : Theme.Surface;
        _doneBtn.ForeColor = _showDone  ? Color.White     : Theme.TextSecondary;
        _doneBtn.FlatAppearance.MouseOverBackColor = _showDone ? Theme.PrimaryHover : Theme.Background;
    }

    private void LoadAssignments()
    {
        var student = _studentId.HasValue ? StudentRepository.GetById(_studentId.Value) : null;

        var courseIds = student != null
            ? StudentRepository.GetEnrolledCourseIds(student.Id)
            : new List<int>();

        if (courseIds.Count == 0)
        {
            _assignments = new List<AssignmentStudentView>();
            ShowEmpty(Loc.T("student.assignments.no_course"));
            return;
        }

        _assignments = courseIds
            .SelectMany(cid => AssignmentRepository.GetForStudent(cid, student!.Id))
            .ToList();

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var visible = _showDone
            ? _assignments.Where(a => a.IsSubmitted).ToList()
            : _assignments.Where(a => !a.IsSubmitted).ToList();

        if (_assignments.Count == 0)
        {
            ShowEmpty(Loc.T("student.assignments.no_assignments"));
            return;
        }

        if (visible.Count == 0)
        {
            ShowEmpty(_showDone
                ? Loc.T("student.assignments.empty.done")
                : Loc.T("student.assignments.empty.todo"));
            return;
        }

        _emptyLabel.Visible = false;
        _grid.Visible = true;

        _grid.DataSource = visible.Select(a => new
        {
            a.Id,
            Title  = a.Title,
            Due    = a.DueDate,
            Status = a.IsSubmitted
                       ? Loc.T("student.assignments.status.submitted")
                       : a.IsOverdue
                           ? Loc.T("student.assignments.status.overdue")
                           : Loc.T("student.assignments.status.not_submitted")
        }).ToList();

        if (_grid.Columns["Id"]     is { } ic) ic.Visible = false;
        if (_grid.Columns["Title"]  is { } tc) tc.HeaderText = Loc.T("student.assignments.col.title");
        if (_grid.Columns["Due"]    is { } dc) { dc.HeaderText = Loc.T("student.assignments.col.duedate"); dc.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; dc.Width = 110; }
        if (_grid.Columns["Status"] is { } sc) { sc.HeaderText = Loc.T("student.assignments.col.status");  sc.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; sc.Width = 140; }
    }

    private void ShowEmpty(string msg)
    {
        _grid.Visible = false;
        _emptyLabel.Text = msg;
        _emptyLabel.Visible = true;
        _submitBtn.Enabled = false;
        _downloadBtn.Enabled = false;
    }

    private void OnSelectionChanged(Panel descPanel)
    {
        var a = SelectedAssignment();
        if (a == null)
        {
            _submitBtn.Enabled   = false;
            _downloadBtn.Enabled = false;
            descPanel.Visible    = false;
            return;
        }

        _submitBtn.Text      = a.IsSubmitted
            ? Loc.T("student.assignments.btn.resubmit")
            : Loc.T("student.assignments.btn.submit");
        _submitBtn.Enabled   = true;
        _downloadBtn.Enabled = a.IsSubmitted;
        descPanel.Visible    = true;

        _descLabel.Text = string.IsNullOrWhiteSpace(a.Description)
            ? Loc.T("student.assignments.no_description")
            : a.Description;
    }

    private AssignmentStudentView? SelectedAssignment()
    {
        if (_grid.CurrentRow == null || !_grid.Visible) return null;
        var id = (int)_grid.CurrentRow.Cells["Id"].Value;
        return _assignments.FirstOrDefault(a => a.Id == id);
    }

    private void SubmitFile()
    {
        var a = SelectedAssignment();
        if (a == null || !_studentId.HasValue) return;

        using var dlg = new OpenFileDialog
        {
            Title  = Loc.T("assignments.upload.title"),
            Filter = Loc.T("assignments.upload.filter"),
            Multiselect = false
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            AssignmentRepository.Submit(a.Id, _studentId.Value, dlg.FileName);

            // Notify the course teacher about the submission
            var assignment = Data.AssignmentRepository.GetById(a.Id);
            if (assignment != null)
            {
                var student = Data.StudentRepository.GetById(_studentId.Value);
                var studentName = student != null ? $"{student.FirstName} {student.LastName}" : "";
                Services.NotificationService.NotifyTeacherOfCourse(assignment.CourseId, "submission",
                    string.Format(Services.Loc.T("notifications.msg.submission"), studentName, a.Title));
            }

            LoadAssignments();
            for (int i = 0; i < _grid.Rows.Count; i++)
            {
                if ((int)_grid.Rows[i].Cells["Id"].Value == a.Id)
                {
                    _grid.CurrentCell = _grid.Rows[i].Cells["Title"];
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                string.Format(Loc.T("assignments.upload.error"), ex.Message),
                Loc.T("student.assignments.btn.submit"),
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void DownloadMySubmission()
    {
        var a = SelectedAssignment();
        if (a?.StoredName == null) return;

        using var dlg = new SaveFileDialog
        {
            Title    = Loc.T("assignments.download.title"),
            FileName = a.SubmissionFile ?? "submission",
            Filter   = Loc.T("assignments.upload.filter")
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try { AssignmentRepository.DownloadSubmission(a.StoredName, dlg.FileName); }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                string.Format(Loc.T("assignments.download.error"), ex.Message),
                Loc.T("student.assignments.btn.download"),
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
