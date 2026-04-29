using System.Globalization;
using AfterSchool.Data;
using AfterSchool.Models;
using AfterSchool.Services;
using AfterSchool.UI;

namespace AfterSchool.Forms;

public class GradesControl : UserControl
{
    private readonly ComboBox _courseCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Button _addEditBtn = new();
    private readonly Button _clearBtn = new();
    private readonly Button _exportBtn = new();
    private readonly Button _transcriptBtn = new();
    private readonly Label _statsLabel = new();
    private readonly DataGridView _grid = new();

    private List<Course> _courses = new();
    private List<GradeView> _grades = new();

    public GradesControl()
    {
        BackColor = Theme.Background;
        BuildLayout();
        LoadCourses();
    }

    private void BuildLayout()
    {
        _addEditBtn.Text = Loc.T("grades.btn.addgrade");
        _clearBtn.Text = Loc.T("grades.btn.cleargrade");
        _exportBtn.Text = Loc.T("grades.btn.export");
        _transcriptBtn.Text = Loc.T("grades.btn.transcript");

        Theme.StyleButton(_addEditBtn, primary: true);
        Theme.StyleButton(_clearBtn, danger: true);
        Theme.StyleButton(_exportBtn);
        Theme.StyleButton(_transcriptBtn);

        _addEditBtn.Click += (_, _) => OpenGradeEditor();
        _clearBtn.Click += (_, _) => ClearGrade();
        _exportBtn.Click += (_, _) => ExportGrades();
        _transcriptBtn.Click += (_, _) => OpenTranscript();

        _courseCombo.Font = Theme.BodyFont;
        _courseCombo.Width = 300;
        _courseCombo.Height = 30;
        _courseCombo.SelectedIndexChanged += (_, _) => LoadGrades();

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

        // ── Toolbar ───────────────────────────────────────────────────────────
        var toolbar = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Theme.Surface };

        var courseLabel = new Label
        {
            Text = Loc.T("grades.lbl.course"),
            Font = new Font("Segoe UI Semibold", 9.5f),
            ForeColor = Theme.TextSecondary,
            AutoSize = true,
            Left = 0,
            Top = 18
        };
        _courseCombo.Left = courseLabel.PreferredWidth + 8;
        _courseCombo.Top = 12;

        var buttonBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            Width = 500,
            Height = 56,
            BackColor = Theme.Surface,
            WrapContents = false
        };
        buttonBar.Controls.Add(_clearBtn);
        buttonBar.Controls.Add(_addEditBtn);
        buttonBar.Controls.Add(_exportBtn);
        buttonBar.Controls.Add(_transcriptBtn);

        toolbar.Controls.Add(courseLabel);
        toolbar.Controls.Add(_courseCombo);
        toolbar.Controls.Add(buttonBar);

        // ── Stats panel ───────────────────────────────────────────────────────
        var statsPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 40,
            BackColor = Theme.Background,
            Padding = new Padding(8, 0, 8, 0)
        };
        statsPanel.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawLine(pen, 0, statsPanel.Height - 1, statsPanel.Width, statsPanel.Height - 1);
        };

        _statsLabel.Dock = DockStyle.Fill;
        _statsLabel.Font = Theme.SmallFont;
        _statsLabel.ForeColor = Theme.TextSecondary;
        _statsLabel.TextAlign = ContentAlignment.MiddleLeft;
        _statsLabel.Text = "";
        statsPanel.Controls.Add(_statsLabel);

        // ── Grid ──────────────────────────────────────────────────────────────
        Theme.StyleGrid(_grid);
        _grid.Dock = DockStyle.Fill;
        _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) OpenGradeEditor(); };

        card.Controls.Add(_grid);
        card.Controls.Add(statsPanel);
        card.Controls.Add(toolbar);
        Controls.Add(card);
    }

    // ── Data loading ──────────────────────────────────────────────────────────

    private void LoadCourses()
    {
        _courses = CourseRepository.GetAll().ToList();
        _courseCombo.Items.Clear();
        foreach (var c in _courses)
            _courseCombo.Items.Add(c);

        if (_courses.Count > 0)
            _courseCombo.SelectedIndex = 0;
        else
            ShowEmpty();
    }

    private void LoadGrades()
    {
        var course = SelectedCourse();
        if (course == null) { ShowEmpty(); return; }

        _grades = GradeRepository.GetByCourse(course.Id).ToList();
        RefreshGrid(course);
        UpdateStats(course.GradingScale);
    }

    private void ShowEmpty()
    {
        _grades.Clear();
        _grid.DataSource = null;
        _statsLabel.Text = _courses.Count == 0
            ? Loc.T("grades.lbl.no_courses")
            : Loc.T("grades.lbl.select_course");
    }

    private void RefreshGrid(Course course)
    {
        // Sort and rank
        List<GradeView> sorted;
        if (course.GradingScale == "Numeric")
        {
            sorted = _grades
                .OrderByDescending(g => ParseNumeric(g.Score) ?? -1)
                .ThenBy(g => g.StudentLastName).ThenBy(g => g.StudentFirstName)
                .ToList();
        }
        else
        {
            sorted = _grades
                .OrderBy(g => g.StudentLastName).ThenBy(g => g.StudentFirstName)
                .ToList();
        }

        // Build display rows
        var rows = new List<object>();
        double? prevScore = null;
        int denseRank = 0;
        int rowNum = 0;

        foreach (var g in sorted)
        {
            rowNum++;
            string rankDisplay;

            if (string.IsNullOrEmpty(g.Score))
            {
                rankDisplay = Loc.T("grades.no_grade");
            }
            else if (course.GradingScale == "Numeric")
            {
                var score = ParseNumeric(g.Score);
                if (score.HasValue)
                {
                    if (!prevScore.HasValue || prevScore.Value != score.Value)
                    {
                        denseRank = rowNum;
                        prevScore = score.Value;
                    }
                    rankDisplay = denseRank.ToString();
                }
                else
                {
                    rankDisplay = Loc.T("grades.no_grade");
                }
            }
            else
            {
                rankDisplay = rowNum.ToString();
            }

            rows.Add(new
            {
                Rank = rankDisplay,
                Student = $"{g.StudentLastName}, {g.StudentFirstName}",
                Score = string.IsNullOrEmpty(g.Score) ? Loc.T("grades.no_grade") : g.Score,
                Notes = g.Notes,
                Date = g.GradedDate,
                GradedBy = g.GradedBy,
                // hidden
                _StudentId = g.StudentId,
                _HasGrade = g.Id > 0
            });
        }

        _grid.DataSource = rows;

        if (_grid.Columns["_StudentId"] is { } sid) sid.Visible = false;
        if (_grid.Columns["_HasGrade"] is { } hg)  hg.Visible = false;
        if (_grid.Columns["Rank"] is { } r)       { r.HeaderText = Loc.T("grades.col.rank"); r.Width = 50; r.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; }
        if (_grid.Columns["Student"] is { } s)       s.HeaderText = Loc.T("grades.col.student");
        if (_grid.Columns["Score"] is { } sc)        sc.HeaderText = Loc.T("grades.col.score");
        if (_grid.Columns["Notes"] is { } n)         n.HeaderText = Loc.T("grades.col.notes");
        if (_grid.Columns["Date"] is { } d)        { d.HeaderText = Loc.T("grades.col.date"); d.Width = 100; d.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; }
        if (_grid.Columns["GradedBy"] is { } gb)     gb.HeaderText = Loc.T("grades.col.gradedby");
    }

    private void UpdateStats(string scale)
    {
        var graded = _grades.Where(g => !string.IsNullOrEmpty(g.Score)).ToList();
        int total = _grades.Count;
        int gradedCount = graded.Count;

        if (gradedCount == 0)
        {
            _statsLabel.Text = $"{string.Format(Loc.T("grades.stats.graded_of"), 0, total)}  ·  {Loc.T("grades.stats.nograded")}";
            return;
        }

        var gradedStr = string.Format(Loc.T("grades.stats.graded_of"), gradedCount, total);

        if (scale == "Numeric")
        {
            var scores = graded
                .Select(g => ParseNumeric(g.Score))
                .Where(v => v.HasValue).Select(v => v!.Value).ToList();

            if (scores.Count > 0)
            {
                var avg = scores.Average();
                var high = scores.Max();
                var low = scores.Min();
                _statsLabel.Text = $"{gradedStr}  ·  " +
                    string.Format(Loc.T("grades.stats.numeric"), $"{avg:F1}", high, low);
            }
            else
            {
                _statsLabel.Text = gradedStr;
            }
        }
        else if (scale == "Letter")
        {
            var dist = new[] { "A", "B", "C", "D", "F" }
                .Select(l => new { l, cnt = graded.Count(g => g.Score == l) })
                .Where(x => x.cnt > 0)
                .Select(x => $"{x.l}: {x.cnt}");
            _statsLabel.Text = $"{gradedStr}  ·  {string.Join("  ", dist)}";
        }
        else // PassFail
        {
            var pass = graded.Count(g => g.Score == "Pass");
            var fail = graded.Count(g => g.Score == "Fail");
            var rate = gradedCount > 0 ? (int)Math.Round(pass * 100.0 / gradedCount) : 0;
            _statsLabel.Text = $"{gradedStr}  ·  {string.Format(Loc.T("grades.stats.passfail"), pass, fail, rate)}";
        }
    }

    // ── Actions ───────────────────────────────────────────────────────────────

    private void OpenGradeEditor()
    {
        var course = SelectedCourse();
        var studentId = SelectedStudentId();
        if (course == null || studentId == 0) return;

        var gradeView = _grades.FirstOrDefault(g => g.StudentId == studentId);
        if (gradeView == null) return;

        using var dlg = new GradeEditorDialog(gradeView, course);
        if (dlg.ShowDialog(this) == DialogResult.OK)
            LoadGrades();
    }

    private void ClearGrade()
    {
        var course = SelectedCourse();
        var studentId = SelectedStudentId();
        if (course == null || studentId == 0) return;

        var gradeView = _grades.FirstOrDefault(g => g.StudentId == studentId);
        if (gradeView == null || gradeView.Id == 0) return;

        var studentName = $"{gradeView.StudentLastName}, {gradeView.StudentFirstName}";
        var result = MessageBox.Show(
            string.Format(Loc.T("grades.delete.confirm"), studentName),
            Loc.T("grades.delete.title"),
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes) return;

        GradeRepository.Delete(studentId, course.Id);
        LoadGrades();
    }

    private void ExportGrades()
    {
        var course = SelectedCourse();
        if (course == null)
        {
            MessageBox.Show(Loc.T("grades.export.no_course"), Loc.T("grades.export.title"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        try
        {
            var file = ExcelExportService.ExportGradeSheet(course);
            var msg = string.Format(Loc.T("reports.export.success_msg"), file);
            var res = MessageBox.Show(msg, Loc.T("reports.export.success_title"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (res == DialogResult.Yes && File.Exists(file))
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(file) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(string.Format(Loc.T("reports.export.error_msg"), ex.Message),
                Loc.T("reports.export.error_title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OpenTranscript()
    {
        using var dlg = new TranscriptDialog();
        dlg.ShowDialog(this);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Course? SelectedCourse()
    {
        return _courseCombo.SelectedItem as Course;
    }

    private int SelectedStudentId()
    {
        if (_grid.CurrentRow == null) return 0;
        if (_grid.CurrentRow.Cells["_StudentId"]?.Value is int id) return id;
        return 0;
    }

    private static double? ParseNumeric(string s)
    {
        if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
            return v;
        return null;
    }
}

// ── Grade editor dialog ───────────────────────────────────────────────────────

internal sealed class GradeEditorDialog : Form
{
    private readonly GradeView _gradeView;
    private readonly Course _course;
    private Control? _scoreControl;
    private readonly TextBox _notes = new() { Multiline = true, ScrollBars = ScrollBars.Vertical };
    private readonly DateTimePicker _datePicker = new();
    private readonly TextBox _gradedBy = new();

    public GradeEditorDialog(GradeView gradeView, Course course)
    {
        _gradeView = gradeView;
        _course = course;

        var isNew = gradeView.Id == 0;
        Text = isNew ? Loc.T("grades.editor.title.new") : Loc.T("grades.editor.title.edit");
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;
        Size = new Size(480, 520);
        BackColor = Theme.Surface;
        Font = Theme.BodyFont;

        // ── Header ───────────────────────────────────────────────────────────
        var header = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Theme.Background, Padding = new Padding(20, 12, 20, 8) };
        header.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
        };
        var studentLbl = new Label
        {
            Text = $"{gradeView.StudentLastName}, {gradeView.StudentFirstName}",
            Font = Theme.HeadingFont, ForeColor = Theme.TextPrimary,
            Dock = DockStyle.Top, Height = 24, TextAlign = ContentAlignment.MiddleLeft
        };
        var courseLbl = new Label
        {
            Text = $"{_course.Name}  ·  {ScaleDisplayName(_course.GradingScale)}",
            Font = Theme.SmallFont, ForeColor = Theme.TextSecondary,
            Dock = DockStyle.Top, Height = 18, TextAlign = ContentAlignment.MiddleLeft
        };
        header.Controls.Add(courseLbl);
        header.Controls.Add(studentLbl);

        // ── Form ─────────────────────────────────────────────────────────────
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            Padding = new Padding(24, 16, 24, 16),
            BackColor = Theme.Surface,
            AutoScroll = true
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // Score
        layout.Controls.Add(FieldLabel(Loc.T("grades.editor.score")));
        _scoreControl = BuildScoreControl(_course.GradingScale, _gradeView.Score);
        _scoreControl.Dock = DockStyle.Top;
        layout.Controls.Add(_scoreControl);

        layout.Controls.Add(Spacer(10));

        // Notes
        layout.Controls.Add(FieldLabel(Loc.T("grades.editor.notes")));
        Theme.StyleTextBox(_notes);
        _notes.Text = _gradeView.Notes;
        _notes.Dock = DockStyle.Top; _notes.Height = 72;
        layout.Controls.Add(_notes);

        layout.Controls.Add(Spacer(10));

        // Date
        layout.Controls.Add(FieldLabel(Loc.T("grades.editor.date")));
        _datePicker.Font = Theme.BodyFont;
        _datePicker.Format = DateTimePickerFormat.Short;
        _datePicker.Dock = DockStyle.Top; _datePicker.Height = 30;
        if (DateTime.TryParseExact(_gradeView.GradedDate, "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            _datePicker.Value = dt;
        else
            _datePicker.Value = DateTime.Today;
        layout.Controls.Add(_datePicker);

        layout.Controls.Add(Spacer(10));

        // Graded by
        layout.Controls.Add(FieldLabel(Loc.T("grades.editor.gradedby")));
        Theme.StyleTextBox(_gradedBy);
        _gradedBy.Text = string.IsNullOrEmpty(_gradeView.GradedBy)
            ? Session.DisplayName : _gradeView.GradedBy;
        _gradedBy.Dock = DockStyle.Top; _gradedBy.Height = 30;
        layout.Controls.Add(_gradedBy);

        // ── Buttons ───────────────────────────────────────────────────────────
        var ok = new Button { Text = isNew ? Loc.T("common.save") : Loc.T("common.save"), DialogResult = DialogResult.None };
        var cancel = new Button { Text = Loc.T("common.cancel"), DialogResult = DialogResult.Cancel };
        Theme.StyleButton(ok, primary: true);
        Theme.StyleButton(cancel);
        ok.Click += (_, _) => Save();

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 56,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(24, 12, 24, 12),
            BackColor = Theme.Surface
        };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);

        Controls.Add(layout);
        Controls.Add(buttons);
        Controls.Add(header);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private Control BuildScoreControl(string scale, string existingScore)
    {
        if (scale == "Letter")
        {
            var cb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = Theme.BodyFont, Height = 30 };
            foreach (var l in new[] { "A", "B", "C", "D", "F" }) cb.Items.Add(l);
            var idx = cb.Items.IndexOf(existingScore);
            cb.SelectedIndex = idx >= 0 ? idx : 0;
            return cb;
        }
        if (scale == "PassFail")
        {
            var cb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = Theme.BodyFont, Height = 30 };
            cb.Items.Add("Pass"); cb.Items.Add("Fail");
            var idx = cb.Items.IndexOf(existingScore);
            cb.SelectedIndex = idx >= 0 ? idx : 0;
            return cb;
        }
        // Numeric
        var nud = new NumericUpDown
        {
            Minimum = 1, Maximum = 10, DecimalPlaces = 1, Increment = 0.5m,
            Font = Theme.BodyFont, Height = 30
        };
        if (decimal.TryParse(existingScore, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
            nud.Value = Math.Clamp(v, 1, 10);
        else
            nud.Value = 10;
        return nud;
    }

    private void Save()
    {
        var scoreStr = GetScoreString();
        if (string.IsNullOrWhiteSpace(scoreStr))
        {
            MessageBox.Show(this, Loc.T("grades.validation.score_required"),
                Loc.T("grades.validation.title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var grade = new Grade
        {
            StudentId = _gradeView.StudentId,
            CourseId = _gradeView.CourseId,
            Score = scoreStr,
            Notes = _notes.Text.Trim(),
            GradedDate = _datePicker.Value.ToString("yyyy-MM-dd"),
            GradedBy = _gradedBy.Text.Trim()
        };
        GradeRepository.Upsert(grade);
        DialogResult = DialogResult.OK;
        Close();
    }

    private string GetScoreString()
    {
        if (_scoreControl is NumericUpDown nud)
            return nud.Value.ToString(CultureInfo.InvariantCulture);
        if (_scoreControl is ComboBox cb && cb.SelectedItem is string s)
            return s;
        return "";
    }

    private static string ScaleDisplayName(string scale) => scale switch
    {
        "Letter"   => Loc.T("grades.scale.letter"),
        "PassFail" => Loc.T("grades.scale.passfail"),
        _          => Loc.T("grades.scale.numeric")
    };

    private static Label FieldLabel(string text) => new()
    {
        Text = text,
        Font = new Font("Segoe UI Semibold", 9.5f),
        ForeColor = Theme.TextSecondary,
        Dock = DockStyle.Top, Height = 22,
        TextAlign = ContentAlignment.BottomLeft
    };

    private static Panel Spacer(int h) => new() { Dock = DockStyle.Top, Height = h, BackColor = Theme.Surface };
}

// ── Transcript dialog ─────────────────────────────────────────────────────────

internal sealed class TranscriptDialog : Form
{
    private readonly ComboBox _studentCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Button _exportBtn = new();
    private readonly DataGridView _grid = new();
    private List<StudentView> _students = new();
    private List<GradeView> _grades = new();

    public TranscriptDialog()
    {
        Text = Loc.T("grades.tab.transcript");
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize = new Size(640, 440);
        Size = new Size(720, 520);
        BackColor = Theme.Background;
        Font = Theme.BodyFont;

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
        var toolbar = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Theme.Surface };

        var lbl = new Label
        {
            Text = Loc.T("grades.lbl.student"),
            Font = new Font("Segoe UI Semibold", 9.5f),
            ForeColor = Theme.TextSecondary,
            AutoSize = true, Left = 0, Top = 18
        };
        _studentCombo.Font = Theme.BodyFont;
        _studentCombo.Width = 300;
        _studentCombo.Left = lbl.PreferredWidth + 8;
        _studentCombo.Top = 12;
        _studentCombo.SelectedIndexChanged += (_, _) => LoadGrades();

        _exportBtn.Text = Loc.T("grades.btn.export_transcript");
        Theme.StyleButton(_exportBtn);
        _exportBtn.Click += (_, _) => ExportTranscript();

        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right, Width = 200, Height = 56,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Theme.Surface, WrapContents = false
        };
        btnPanel.Controls.Add(_exportBtn);

        toolbar.Controls.Add(lbl);
        toolbar.Controls.Add(_studentCombo);
        toolbar.Controls.Add(btnPanel);

        Theme.StyleGrid(_grid);
        _grid.Dock = DockStyle.Fill;

        card.Controls.Add(_grid);
        card.Controls.Add(toolbar);
        Controls.Add(card);

        LoadStudents();
    }

    private void LoadStudents()
    {
        _students = StudentRepository.GetAll().ToList();
        _studentCombo.Items.Clear();
        foreach (var s in _students)
            _studentCombo.Items.Add(new StudentItem(s.Id, $"{s.LastName}, {s.FirstName}"));
        if (_students.Count > 0) _studentCombo.SelectedIndex = 0;
    }

    private void LoadGrades()
    {
        if (_studentCombo.SelectedItem is not StudentItem item) return;
        _grades = GradeRepository.GetByStudent(item.Id).ToList();

        var rows = _grades.Select(g => new
        {
            Course = g.CourseName,
            Scale = ScaleLabel(g.GradingScale),
            Score = string.IsNullOrEmpty(g.Score) ? Loc.T("grades.no_grade") : g.Score,
            Notes = g.Notes,
            Date = g.GradedDate
        }).ToList();

        _grid.DataSource = rows;
        if (_grid.Columns["Course"] is { } c) c.HeaderText = Loc.T("grades.col.course");
        if (_grid.Columns["Scale"]  is { } sc) sc.HeaderText = Loc.T("grades.col.scale");
        if (_grid.Columns["Score"]  is { } s)  s.HeaderText  = Loc.T("grades.col.score");
        if (_grid.Columns["Notes"]  is { } n)  n.HeaderText  = Loc.T("grades.col.notes");
        if (_grid.Columns["Date"]   is { } d) { d.HeaderText = Loc.T("grades.col.date"); d.Width = 100; d.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; }
    }

    private void ExportTranscript()
    {
        if (_studentCombo.SelectedItem is not StudentItem item)
        {
            MessageBox.Show(Loc.T("grades.export.no_student"), Loc.T("grades.export.title"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var student = _students.FirstOrDefault(s => s.Id == item.Id);
        if (student == null) return;
        try
        {
            var file = ExcelExportService.ExportTranscript(student, _grades);
            var msg = string.Format(Loc.T("reports.export.success_msg"), file);
            var res = MessageBox.Show(msg, Loc.T("reports.export.success_title"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (res == DialogResult.Yes && File.Exists(file))
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(file) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(string.Format(Loc.T("reports.export.error_msg"), ex.Message),
                Loc.T("reports.export.error_title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string ScaleLabel(string scale) => scale switch
    {
        "Letter"   => Loc.T("grades.scale.letter"),
        "PassFail" => Loc.T("grades.scale.passfail"),
        _          => Loc.T("grades.scale.numeric")
    };

    private sealed record StudentItem(int Id, string Label)
    {
        public override string ToString() => Label;
    }
}
