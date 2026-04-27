using AfterSchool.Data;
using AfterSchool.Models;
using AfterSchool.Services;
using AfterSchool.UI;

namespace AfterSchool.Forms;

public class CourseManagerControl : UserControl
{
    private readonly DataGridView _grid = new();
    private readonly TextBox _searchBox = new();
    private readonly Button _newBtn = new();
    private readonly Button _editBtn = new();
    private readonly Button _deleteBtn = new();

    private List<Course> _courses = new();

    public CourseManagerControl()
    {
        BackColor = Theme.Background;
        BuildLayout();
        LoadCourses();
    }

    private void BuildLayout()
    {
        _newBtn.Text = Loc.T("course.btn.new");
        _editBtn.Text = Loc.T("common.edit");
        _deleteBtn.Text = Loc.T("common.delete");

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

        var toolbar = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Theme.Surface };

        _searchBox.PlaceholderText = Loc.T("course.search");
        _searchBox.Width = 280;
        _searchBox.Left = 0;
        _searchBox.Top = 8;
        Theme.StyleTextBox(_searchBox);
        _searchBox.TextChanged += (_, _) => ApplyFilter();

        Theme.StyleButton(_newBtn, primary: true);
        Theme.StyleButton(_editBtn);
        Theme.StyleButton(_deleteBtn, danger: true);

        _newBtn.Click += (_, _) => OpenEditor(null);
        _editBtn.Click += (_, _) => EditSelected();
        _deleteBtn.Click += (_, _) => DeleteSelected();

        var buttonBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            Width = 380,
            Height = 52,
            BackColor = Theme.Surface,
            WrapContents = false
        };
        buttonBar.Controls.Add(_deleteBtn);
        buttonBar.Controls.Add(_editBtn);
        buttonBar.Controls.Add(_newBtn);

        toolbar.Controls.Add(_searchBox);
        toolbar.Controls.Add(buttonBar);

        Theme.StyleGrid(_grid);
        _grid.Dock = DockStyle.Fill;
        _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) EditSelected(); };

        card.Controls.Add(_grid);
        card.Controls.Add(toolbar);

        Controls.Add(card);
    }

    private void LoadCourses()
    {
        _courses = CourseRepository.GetAll().ToList();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var q = _searchBox.Text.Trim();
        var rows = _courses
            .Where(c => string.IsNullOrEmpty(q)
                       || c.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                       || c.Teacher.Contains(q, StringComparison.OrdinalIgnoreCase))
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Teacher,
                c.Capacity,
                Enrolled = CourseRepository.GetEnrolledCount(c.Id),
                c.Description
            })
            .ToList();

        _grid.DataSource = rows;
        if (_grid.Columns["Id"] is { } idCol) idCol.Visible = false;
        if (_grid.Columns["Name"] is { } n) n.HeaderText = Loc.T("course.col.name");
        if (_grid.Columns["Enrolled"] is { } en) en.HeaderText = Loc.T("course.col.enrolled");
    }

    private Course? SelectedCourse()
    {
        if (_grid.CurrentRow == null) return null;
        var id = (int)_grid.CurrentRow.Cells["Id"].Value;
        return _courses.FirstOrDefault(c => c.Id == id);
    }

    private void EditSelected()
    {
        var c = SelectedCourse();
        if (c == null) return;
        OpenEditor(c);
    }

    private void DeleteSelected()
    {
        var c = SelectedCourse();
        if (c == null) return;
        var enrolled = CourseRepository.GetEnrolledCount(c.Id);
        var warning = enrolled > 0
            ? string.Format(Loc.T("course.delete.enrolled_warning"), enrolled)
            : "";
        var result = MessageBox.Show(
            string.Format(Loc.T("course.delete.confirm"), c.Name, warning),
            Loc.T("course.delete.title"),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (result != DialogResult.Yes) return;

        CourseRepository.Delete(c.Id);
        LoadCourses();
    }

    private void OpenEditor(Course? existing)
    {
        using var dlg = new CourseEditorDialog(existing);
        if (dlg.ShowDialog(this) == DialogResult.OK)
            LoadCourses();
    }
}

internal sealed class CourseEditorDialog : Form
{
    private readonly Course _course;
    private readonly bool _isNew;
    private readonly TextBox _name = new();
    private readonly ComboBox _teacher = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label _teacherHint = new();
    private readonly NumericUpDown _capacity = new() { Minimum = 0, Maximum = 500, Value = 20 };
    private readonly ComboBox _gradingScale = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _description = new() { Multiline = true, ScrollBars = ScrollBars.Vertical };

    private static readonly (string Value, string LabelKey)[] ScaleOptions =
    {
        ("Numeric",  "grades.scale.numeric"),
        ("Letter",   "grades.scale.letter"),
        ("PassFail", "grades.scale.passfail"),
    };

    public CourseEditorDialog(Course? existing)
    {
        _course = existing ?? new Course();
        _isNew = existing == null;
        Text = _isNew ? Loc.T("course.editor.title.new") : Loc.T("course.editor.title.edit");
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Size = new Size(520, 600);
        BackColor = Theme.Surface;
        Font = Theme.BodyFont;

        Theme.StyleTextBox(_name);
        Theme.StyleTextBox(_description);
        _teacher.Font = Theme.BodyFont;
        _capacity.Font = Theme.BodyFont;
        _gradingScale.Font = Theme.BodyFont;

        _name.Text = _course.Name;
        _capacity.Value = Math.Clamp(_course.Capacity, 0, 500);
        _description.Text = _course.Description;

        PopulateTeachers();
        PopulateGradingScale();

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            Padding = new Padding(24),
            BackColor = Theme.Surface
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        layout.Controls.Add(Label(Loc.T("course.field.name")));
        _name.Dock = DockStyle.Top; _name.Height = 30;
        layout.Controls.Add(_name);

        layout.Controls.Add(Spacer(12));

        layout.Controls.Add(Label(Loc.T("course.field.teacher")));
        _teacher.Dock = DockStyle.Top; _teacher.Height = 30;
        layout.Controls.Add(_teacher);

        _teacherHint.Dock = DockStyle.Top;
        _teacherHint.Height = 18;
        _teacherHint.Font = Theme.SmallFont;
        _teacherHint.ForeColor = Theme.TextSecondary;
        layout.Controls.Add(_teacherHint);

        layout.Controls.Add(Spacer(12));

        layout.Controls.Add(Label(Loc.T("course.field.capacity")));
        _capacity.Dock = DockStyle.Top; _capacity.Height = 30;
        layout.Controls.Add(_capacity);

        layout.Controls.Add(Spacer(12));

        layout.Controls.Add(Label(Loc.T("course.field.gradingscale")));
        _gradingScale.Dock = DockStyle.Top; _gradingScale.Height = 30;
        layout.Controls.Add(_gradingScale);

        layout.Controls.Add(Spacer(12));

        layout.Controls.Add(Label(Loc.T("course.field.description")));
        _description.Dock = DockStyle.Top; _description.Height = 120;
        layout.Controls.Add(_description);

        var ok = new Button { Text = _isNew ? Loc.T("common.create") : Loc.T("common.save"), DialogResult = DialogResult.None };
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
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private static Label Label(string text) => new()
    {
        Text = text,
        Font = new Font("Segoe UI Semibold", 9.5f),
        ForeColor = Theme.TextSecondary,
        Dock = DockStyle.Top,
        Height = 22,
        TextAlign = ContentAlignment.BottomLeft
    };

    private static Panel Spacer(int height) => new() { Dock = DockStyle.Top, Height = height, BackColor = Theme.Surface };

    private void PopulateGradingScale()
    {
        _gradingScale.Items.Clear();
        foreach (var (value, labelKey) in ScaleOptions)
            _gradingScale.Items.Add(new ScaleChoice(value, Loc.T(labelKey)));

        var idx = Array.FindIndex(ScaleOptions, o => o.Value == _course.GradingScale);
        _gradingScale.SelectedIndex = Math.Max(0, idx);
    }

    private void PopulateTeachers()
    {
        var teachers = UserRepository.GetByRole("Teacher").ToList();
        var choices = new List<TeacherChoice> { new(null, Loc.T("course.teacher.unassigned")) };
        choices.AddRange(teachers.Select(t => new TeacherChoice(t.Id, UserRepository.DisplayNameOf(t))));

        _teacher.Items.Clear();
        foreach (var c in choices) _teacher.Items.Add(c);

        if (teachers.Count == 0)
        {
            _teacher.SelectedIndex = 0;
            _teacher.Enabled = false;
            _teacherHint.Text = Loc.T("course.teacher.none");
            _teacherHint.ForeColor = Theme.Danger;
            return;
        }

        if (string.IsNullOrWhiteSpace(_course.Teacher))
        {
            _teacher.SelectedIndex = 0;
            return;
        }

        var matchIndex = choices.FindIndex(c =>
            c.UserId != null &&
            string.Equals(c.Label, _course.Teacher, StringComparison.OrdinalIgnoreCase));

        _teacher.SelectedIndex = matchIndex >= 0 ? matchIndex : 0;
        if (matchIndex < 0)
            _teacherHint.Text = string.Format(Loc.T("course.teacher.missing"), _course.Teacher);
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(_name.Text))
        {
            MessageBox.Show(this, Loc.T("course.validation.name_required"),
                Loc.T("course.validation.title"),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var choice = _teacher.SelectedItem as TeacherChoice;
        var scaleChoice = _gradingScale.SelectedItem as ScaleChoice;

        _course.Name = _name.Text.Trim();
        _course.Teacher = choice?.UserId == null ? "" : choice.Label;
        _course.Capacity = (int)_capacity.Value;
        _course.GradingScale = scaleChoice?.Value ?? "Numeric";
        _course.Description = _description.Text.Trim();

        if (_isNew) CourseRepository.Insert(_course);
        else CourseRepository.Update(_course);

        DialogResult = DialogResult.OK;
        Close();
    }

    private sealed record TeacherChoice(int? UserId, string Label)
    {
        public override string ToString() => Label;
    }

    private sealed record ScaleChoice(string Value, string Label)
    {
        public override string ToString() => Label;
    }
}
