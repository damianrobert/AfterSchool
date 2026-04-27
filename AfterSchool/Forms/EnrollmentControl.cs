using AfterSchool.Data;
using AfterSchool.Models;
using AfterSchool.UI;

namespace AfterSchool.Forms;

public class EnrollmentControl : UserControl
{
    private readonly DataGridView _grid = new();
    private readonly TextBox _searchBox = new();
    private readonly ComboBox _courseFilter = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Button _newBtn = new() { Text = "New Student" };
    private readonly Button _editBtn = new() { Text = "Edit" };
    private readonly Button _transferBtn = new() { Text = "Transfer" };
    private readonly Button _deleteBtn = new() { Text = "Delete" };

    private List<StudentView> _students = new();

    public EnrollmentControl()
    {
        BackColor = Theme.Background;
        BuildLayout();
        LoadStudents();
    }

    private void BuildLayout()
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

        var toolbar = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Theme.Surface };

        _searchBox.PlaceholderText = "Search name or email...";
        _searchBox.Width = 260;
        _searchBox.Left = 0;
        _searchBox.Top = 10;
        Theme.StyleTextBox(_searchBox);
        _searchBox.TextChanged += (_, _) => ApplyFilter();

        _courseFilter.Width = 220;
        _courseFilter.Left = 272;
        _courseFilter.Top = 10;
        _courseFilter.Font = Theme.BodyFont;
        _courseFilter.SelectedIndexChanged += (_, _) => ApplyFilter();

        Theme.StyleButton(_newBtn, primary: true);
        Theme.StyleButton(_editBtn);
        Theme.StyleButton(_transferBtn);
        Theme.StyleButton(_deleteBtn, danger: true);

        _newBtn.Click += (_, _) => OpenEditor(null);
        _editBtn.Click += (_, _) => EditSelected();
        _transferBtn.Click += (_, _) => TransferSelected();
        _deleteBtn.Click += (_, _) => DeleteSelected();

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            Width = 500,
            Height = 52,
            BackColor = Theme.Surface
        };
        buttons.Controls.Add(_deleteBtn);
        buttons.Controls.Add(_transferBtn);
        buttons.Controls.Add(_editBtn);
        buttons.Controls.Add(_newBtn);

        toolbar.Controls.Add(_searchBox);
        toolbar.Controls.Add(_courseFilter);
        toolbar.Controls.Add(buttons);

        Theme.StyleGrid(_grid);
        _grid.Dock = DockStyle.Fill;
        _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) EditSelected(); };

        card.Controls.Add(_grid);
        card.Controls.Add(toolbar);

        Controls.Add(card);
    }

    private void LoadStudents()
    {
        _students = StudentRepository.GetAll().ToList();

        var currentFilter = _courseFilter.SelectedItem as CourseFilterItem;
        _courseFilter.Items.Clear();
        _courseFilter.Items.Add(new CourseFilterItem(null, "All students"));
        _courseFilter.Items.Add(new CourseFilterItem(-1, "Unenrolled"));
        foreach (var c in CourseRepository.GetAll())
            _courseFilter.Items.Add(new CourseFilterItem(c.Id, c.Name));
        _courseFilter.SelectedIndex = 0;
        if (currentFilter != null)
        {
            for (int i = 0; i < _courseFilter.Items.Count; i++)
            {
                if (_courseFilter.Items[i] is CourseFilterItem item
                    && item.CourseId == currentFilter.CourseId)
                {
                    _courseFilter.SelectedIndex = i;
                    break;
                }
            }
        }

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var q = _searchBox.Text.Trim();
        var filter = _courseFilter.SelectedItem as CourseFilterItem;

        IEnumerable<StudentView> query = _students;
        if (!string.IsNullOrEmpty(q))
            query = query.Where(s =>
                s.FirstName.Contains(q, StringComparison.OrdinalIgnoreCase)
                || s.LastName.Contains(q, StringComparison.OrdinalIgnoreCase)
                || s.Email.Contains(q, StringComparison.OrdinalIgnoreCase));

        if (filter?.CourseId == -1)
            query = query.Where(s => s.EnrolledCourseId == null);
        else if (filter?.CourseId != null)
            query = query.Where(s => s.EnrolledCourseId == filter.CourseId);

        var rows = query.Select(s => new
        {
            s.Id,
            Name = $"{s.LastName}, {s.FirstName}",
            s.Email,
            s.ContactNo,
            Course = s.CourseName ?? "—",
            s.Status
        }).ToList();

        _grid.DataSource = rows;
        if (_grid.Columns["Id"] is { } idCol) idCol.Visible = false;
        if (_grid.Columns["ContactNo"] is { } c) c.HeaderText = "Contact";
    }

    private StudentView? SelectedStudent()
    {
        if (_grid.CurrentRow == null) return null;
        var id = (int)_grid.CurrentRow.Cells["Id"].Value;
        return _students.FirstOrDefault(s => s.Id == id);
    }

    private void EditSelected()
    {
        var s = SelectedStudent();
        if (s == null) return;
        OpenEditor(s);
    }

    private void DeleteSelected()
    {
        var s = SelectedStudent();
        if (s == null) return;
        var result = MessageBox.Show(
            $"Delete student \"{s.LastName}, {s.FirstName}\"?",
            "Confirm delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes) return;
        StudentRepository.Delete(s.Id);
        LoadStudents();
    }

    private void TransferSelected()
    {
        var s = SelectedStudent();
        if (s == null) return;
        using var dlg = new TransferDialog(s);
        if (dlg.ShowDialog(this) == DialogResult.OK) LoadStudents();
    }

    private void OpenEditor(StudentView? existing)
    {
        Student? model = existing == null ? null : StudentRepository.GetById(existing.Id);
        using var dlg = new StudentEditorDialog(model);
        if (dlg.ShowDialog(this) == DialogResult.OK) LoadStudents();
    }

    private sealed record CourseFilterItem(int? CourseId, string Label)
    {
        public override string ToString() => Label;
    }
}

internal sealed class StudentEditorDialog : Form
{
    private readonly Student _student;
    private readonly bool _isNew;
    private readonly TextBox _firstName = new();
    private readonly TextBox _lastName = new();
    private readonly TextBox _email = new();
    private readonly TextBox _contact = new();
    private readonly TextBox _address = new();
    private readonly DateTimePicker _birth = new() { Format = DateTimePickerFormat.Short };
    private readonly ComboBox _gender = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _status = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _course = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly DateTimePicker _register = new() { Format = DateTimePickerFormat.Short };

    public StudentEditorDialog(Student? existing)
    {
        _isNew = existing == null;
        _student = existing ?? new Student
        {
            RegisterDate = DateTime.Today.ToString("yyyy-MM-dd"),
            Status = "Active"
        };

        Text = _isNew ? "New Student" : "Edit Student";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Size = new Size(620, 640);
        BackColor = Theme.Surface;
        Font = Theme.BodyFont;

        foreach (var tb in new[] { _firstName, _lastName, _email, _contact, _address })
            Theme.StyleTextBox(tb);

        _gender.Items.AddRange(new object[] { "Female", "Male", "Other" });
        _status.Items.AddRange(new object[] { "Active", "Inactive", "Graduated" });

        var courses = new List<CourseChoice> { new(null, "— Not enrolled —") };
        courses.AddRange(CourseRepository.GetAll().Select(c => new CourseChoice(c.Id, c.Name)));
        foreach (var c in courses) _course.Items.Add(c);

        _firstName.Text = _student.FirstName;
        _lastName.Text = _student.LastName;
        _email.Text = _student.Email;
        _contact.Text = _student.ContactNo;
        _address.Text = _student.Address;
        _gender.SelectedItem = string.IsNullOrEmpty(_student.Gender) ? "Female" : _student.Gender;
        _status.SelectedItem = string.IsNullOrEmpty(_student.Status) ? "Active" : _student.Status;
        _birth.Value = ParseDate(_student.BirthDate, DateTime.Today.AddYears(-10));
        _register.Value = ParseDate(_student.RegisterDate, DateTime.Today);
        var courseIdx = courses.FindIndex(c => c.CourseId == _student.EnrolledCourseId);
        _course.SelectedIndex = courseIdx >= 0 ? courseIdx : 0;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Padding = new Padding(24),
            BackColor = Theme.Surface
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        var cursor = new FieldCursor(layout);
        cursor.Add("First name", _firstName);
        cursor.Add("Last name", _lastName);
        cursor.Add("Email", _email);
        cursor.Add("Contact", _contact);
        cursor.Add("Birth date", _birth);
        cursor.Add("Gender", _gender);
        cursor.Add("Register date", _register);
        cursor.Add("Status", _status);
        cursor.Add("Address", _address, colSpan: 2);
        cursor.Add("Enrolled course", _course, colSpan: 2);

        var ok = new Button { Text = _isNew ? "Create" : "Save" };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };
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

    private sealed class FieldCursor
    {
        private readonly TableLayoutPanel _layout;
        private int _col;
        private int _row;

        public FieldCursor(TableLayoutPanel layout) { _layout = layout; }

        public void Add(string label, Control control, int colSpan = 1)
        {
            var cell = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Surface, Height = 64, Margin = new Padding(0, 0, 12, 8) };
            var lbl = new Label
            {
                Text = label,
                Font = new Font("Segoe UI Semibold", 9.5f),
                ForeColor = Theme.TextSecondary,
                Dock = DockStyle.Top,
                Height = 22
            };
            control.Dock = DockStyle.Top;
            control.Height = 30;
            control.Font = Theme.BodyFont;
            cell.Controls.Add(control);
            cell.Controls.Add(lbl);

            while (_layout.RowStyles.Count <= _row)
                _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));

            _layout.Controls.Add(cell, _col, _row);
            if (colSpan > 1) _layout.SetColumnSpan(cell, colSpan);

            _col += colSpan;
            if (_col >= _layout.ColumnCount) { _col = 0; _row++; }
        }
    }

    private static DateTime ParseDate(string s, DateTime fallback) =>
        DateTime.TryParse(s, out var d) ? d : fallback;

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(_firstName.Text) || string.IsNullOrWhiteSpace(_lastName.Text))
        {
            MessageBox.Show(this, "First and last name are required.", "Validation",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _student.FirstName = _firstName.Text.Trim();
        _student.LastName = _lastName.Text.Trim();
        _student.Email = _email.Text.Trim();
        _student.ContactNo = _contact.Text.Trim();
        _student.Address = _address.Text.Trim();
        _student.BirthDate = _birth.Value.ToString("yyyy-MM-dd");
        _student.RegisterDate = _register.Value.ToString("yyyy-MM-dd");
        _student.Gender = _gender.SelectedItem?.ToString() ?? "";
        _student.Status = _status.SelectedItem?.ToString() ?? "Active";

        var selectedCourse = _course.SelectedItem as CourseChoice;
        _student.EnrolledCourseId = selectedCourse?.CourseId;

        if (_isNew) StudentRepository.Insert(_student);
        else StudentRepository.Update(_student);

        DialogResult = DialogResult.OK;
        Close();
    }

    internal sealed record CourseChoice(int? CourseId, string Label)
    {
        public override string ToString() => Label;
    }
}

internal sealed class TransferDialog : Form
{
    private readonly StudentView _student;
    private readonly ComboBox _course = new() { DropDownStyle = ComboBoxStyle.DropDownList };

    public TransferDialog(StudentView student)
    {
        _student = student;
        Text = "Transfer Student";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Size = new Size(480, 260);
        BackColor = Theme.Surface;
        Font = Theme.BodyFont;

        var choices = new List<StudentEditorDialog.CourseChoice>
        {
            new(null, "— Unenroll —")
        };
        choices.AddRange(CourseRepository.GetAll()
            .Select(c => new StudentEditorDialog.CourseChoice(c.Id, c.Name)));
        foreach (var c in choices) _course.Items.Add(c);
        var idx = choices.FindIndex(c => c.CourseId == student.EnrolledCourseId);
        _course.SelectedIndex = idx >= 0 ? idx : 0;
        _course.Font = Theme.BodyFont;

        var header = new Label
        {
            Text = $"{_student.LastName}, {_student.FirstName}",
            Font = new Font("Segoe UI Semibold", 13f),
            ForeColor = Theme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 30
        };
        var currentLbl = new Label
        {
            Text = $"Currently: {_student.CourseName ?? "unenrolled"}",
            Font = Theme.BodyFont,
            ForeColor = Theme.TextSecondary,
            Dock = DockStyle.Top,
            Height = 22
        };
        var pickLbl = new Label
        {
            Text = "Transfer to",
            Font = new Font("Segoe UI Semibold", 9.5f),
            ForeColor = Theme.TextSecondary,
            Dock = DockStyle.Top,
            Height = 22,
            Padding = new Padding(0, 12, 0, 0)
        };
        _course.Dock = DockStyle.Top;
        _course.Height = 30;

        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24), BackColor = Theme.Surface };
        body.Controls.Add(_course);
        body.Controls.Add(pickLbl);
        body.Controls.Add(currentLbl);
        body.Controls.Add(header);

        var ok = new Button { Text = "Transfer" };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };
        Theme.StyleButton(ok, primary: true);
        Theme.StyleButton(cancel);
        ok.Click += (_, _) =>
        {
            var choice = _course.SelectedItem as StudentEditorDialog.CourseChoice;
            StudentRepository.Transfer(_student.Id, choice?.CourseId);
            DialogResult = DialogResult.OK;
            Close();
        };

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

        Controls.Add(body);
        Controls.Add(buttons);
        AcceptButton = ok;
        CancelButton = cancel;
    }
}
