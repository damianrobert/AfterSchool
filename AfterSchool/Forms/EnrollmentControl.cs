using AfterSchool.Data;
using AfterSchool.Models;
using AfterSchool.Services;
using AfterSchool.UI;

namespace AfterSchool.Forms;

public class EnrollmentControl : UserControl
{
    private readonly DataGridView _grid = new();
    private readonly TextBox _searchBox = new();
    private readonly ComboBox _courseFilter = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Button _newBtn = new();
    private readonly Button _editBtn = new();
    private readonly Button _transferBtn = new();
    private readonly Button _deleteBtn = new();
    private readonly Button _accountBtn = new();

    private List<StudentView> _students = new();

    public EnrollmentControl()
    {
        BackColor = Theme.Background;
        BuildLayout();
        LoadStudents();
    }

    private void BuildLayout()
    {
        _newBtn.Text      = Loc.T("enrollment.btn.new");
        _editBtn.Text     = Loc.T("common.edit");
        _transferBtn.Text = Loc.T("common.transfer");
        _deleteBtn.Text   = Loc.T("common.delete");
        _accountBtn.Text  = Loc.T("enrollment.btn.create_account");
        _accountBtn.Visible = Session.Current?.Role == "Administrator";

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

        // Left side: search + course filter in a docked fill panel
        var leftPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Theme.Surface,
            Padding = new Padding(0, 9, 0, 9)
        };

        _searchBox.PlaceholderText = Loc.T("enrollment.search");
        _searchBox.Width = 240;
        _searchBox.Height = 32;
        _searchBox.Margin = new Padding(0, 1, 8, 1);
        Theme.StyleTextBox(_searchBox);
        _searchBox.TextChanged += (_, _) => ApplyFilter();

        _courseFilter.Width = 200;
        _courseFilter.Height = 32;
        _courseFilter.Margin = new Padding(0, 1, 0, 1);
        _courseFilter.Font = Theme.BodyFont;
        _courseFilter.SelectedIndexChanged += (_, _) => ApplyFilter();

        leftPanel.Controls.Add(_searchBox);
        leftPanel.Controls.Add(_courseFilter);

        // Right side: action buttons
        Theme.StyleButton(_newBtn, primary: true);
        Theme.StyleButton(_editBtn);
        Theme.StyleButton(_transferBtn);
        Theme.StyleButton(_deleteBtn, danger: true);
        Theme.StyleButton(_accountBtn);

        _newBtn.Click      += (_, _) => OpenEditor(null);
        _editBtn.Click     += (_, _) => EditSelected();
        _transferBtn.Click += (_, _) => TransferSelected();
        _deleteBtn.Click   += (_, _) => DeleteSelected();
        _accountBtn.Click  += (_, _) => CreateAccountForSelected();

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Theme.Surface,
            Padding = new Padding(0, 8, 0, 8)
        };
        buttons.Controls.Add(_deleteBtn);
        buttons.Controls.Add(_transferBtn);
        buttons.Controls.Add(_editBtn);
        buttons.Controls.Add(_newBtn);
        buttons.Controls.Add(_accountBtn);

        // Add Fill panel first, Right panel last — WinForms docks last-added first
        toolbar.Controls.Add(leftPanel);
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
        _courseFilter.Items.Add(new CourseFilterItem(null, Loc.T("enrollment.filter.all")));
        _courseFilter.Items.Add(new CourseFilterItem(-1, Loc.T("enrollment.filter.unenrolled")));
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
        if (_grid.Columns["ContactNo"] is { } c) c.HeaderText = Loc.T("enrollment.col.contact");
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
            string.Format(Loc.T("enrollment.delete.confirm"), s.LastName, s.FirstName),
            Loc.T("enrollment.delete.title"),
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
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

    private void CreateAccountForSelected()
    {
        var s = SelectedStudent();
        if (s == null) return;

        if (UserRepository.HasStudentAccount(s.Id))
        {
            MessageBox.Show(this,
                Loc.T("enrollment.account.already_exists"),
                Loc.T("enrollment.account.dialog.title"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new CreateStudentAccountDialog(s);
        dlg.ShowDialog(this);
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

        Text = _isNew ? Loc.T("enrollment.editor.title.new") : Loc.T("enrollment.editor.title.edit");
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Size = new Size(620, 640);
        BackColor = Theme.Surface;
        Font = Theme.BodyFont;

        foreach (var tb in new[] { _firstName, _lastName, _email, _contact, _address })
            Theme.StyleTextBox(tb);

        // Gender and status values are stored in DB as English — not translated
        _gender.Items.AddRange(new object[] { "Female", "Male", "Other" });
        _status.Items.AddRange(new object[] { "Active", "Inactive", "Graduated" });

        var courses = new List<CourseChoice> { new(null, Loc.T("enrollment.not_enrolled")) };
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
        cursor.Add(Loc.T("enrollment.field.firstname"), _firstName);
        cursor.Add(Loc.T("enrollment.field.lastname"), _lastName);
        cursor.Add(Loc.T("enrollment.field.email"), _email);
        cursor.Add(Loc.T("enrollment.field.contact"), _contact);
        cursor.Add(Loc.T("enrollment.field.birthdate"), _birth);
        cursor.Add(Loc.T("enrollment.field.gender"), _gender);
        cursor.Add(Loc.T("enrollment.field.registerdate"), _register);
        cursor.Add(Loc.T("enrollment.field.status"), _status);
        cursor.Add(Loc.T("enrollment.field.address"), _address, colSpan: 2);
        cursor.Add(Loc.T("enrollment.field.course"), _course, colSpan: 2);

        var ok = new Button { Text = _isNew ? Loc.T("common.create") : Loc.T("common.save") };
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
            MessageBox.Show(this, Loc.T("enrollment.validation.name_required"), Loc.T("schedule.editor.validation.title"),
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
        Text = Loc.T("enrollment.transfer.title");
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Size = new Size(480, 260);
        BackColor = Theme.Surface;
        Font = Theme.BodyFont;

        var choices = new List<StudentEditorDialog.CourseChoice>
        {
            new(null, Loc.T("enrollment.unenroll"))
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
            Text = string.Format(Loc.T("enrollment.transfer.currently"),
                _student.CourseName ?? Loc.T("enrollment.transfer.unenrolled")),
            Font = Theme.BodyFont,
            ForeColor = Theme.TextSecondary,
            Dock = DockStyle.Top,
            Height = 22
        };
        var pickLbl = new Label
        {
            Text = Loc.T("enrollment.transfer.to"),
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

        var ok = new Button { Text = Loc.T("common.transfer") };
        var cancel = new Button { Text = Loc.T("common.cancel"), DialogResult = DialogResult.Cancel };
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

internal sealed class CreateStudentAccountDialog : Form
{
    private readonly StudentView _student;
    private readonly TextBox _username = new();
    private readonly TextBox _password = new() { UseSystemPasswordChar = true };
    private readonly TextBox _confirm  = new() { UseSystemPasswordChar = true };
    private readonly Label   _error    = new();

    public CreateStudentAccountDialog(StudentView student)
    {
        _student = student;

        Text = Loc.T("enrollment.account.dialog.title");
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Size = new Size(480, 460);
        BackColor = Theme.Surface;
        Font = Theme.BodyFont;

        BuildLayout();
    }

    private void BuildLayout()
    {
        var root = new Panel { Dock = DockStyle.Fill, Padding = new Padding(32, 28, 32, 20), BackColor = Theme.Surface };

        var heading = new Label
        {
            Text = $"{_student.LastName}, {_student.FirstName}",
            Font = new Font("Segoe UI Semibold", 16f),
            ForeColor = Theme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 36
        };
        var sub = new Label
        {
            Text = Loc.T("enrollment.account.note"),
            Font = Theme.BodyFont,
            ForeColor = Theme.TextSecondary,
            Dock = DockStyle.Top,
            Height = 40
        };

        // Pre-fill username from student name
        _username.Text = $"{_student.FirstName}.{_student.LastName}"
                          .ToLowerInvariant()
                          .Replace(" ", "");

        foreach (var tb in new[] { _username, _password, _confirm })
        {
            Theme.StyleTextBox(tb);
            tb.Font = new Font("Segoe UI", 10.5f);
            tb.Dock = DockStyle.Top;
            tb.Height = 32;
        }

        _error.Dock = DockStyle.Top;
        _error.Height = 26;
        _error.ForeColor = Theme.Danger;
        _error.Font = Theme.SmallFont;
        _error.TextAlign = ContentAlignment.MiddleLeft;

        var createBtn = new Button { Text = Loc.T("common.create"), Dock = DockStyle.Top, Height = 40 };
        var cancelBtn = new Button { Text = Loc.T("common.cancel"), DialogResult = DialogResult.Cancel, Dock = DockStyle.Bottom, Height = 36 };
        Theme.StyleButton(createBtn, primary: true);
        Theme.StyleButton(cancelBtn);
        createBtn.Font = new Font("Segoe UI Semibold", 10.5f);
        createBtn.Click += (_, _) => TryCreate();

        root.Controls.Add(Spacer(8));
        root.Controls.Add(cancelBtn);
        root.Controls.Add(Spacer(6));
        root.Controls.Add(createBtn);
        root.Controls.Add(Spacer(6));
        root.Controls.Add(_error);
        root.Controls.Add(_confirm);
        root.Controls.Add(FieldLabel(Loc.T("enrollment.account.field.confirm")));
        root.Controls.Add(Spacer(8));
        root.Controls.Add(_password);
        root.Controls.Add(FieldLabel(Loc.T("enrollment.account.field.password")));
        root.Controls.Add(Spacer(8));
        root.Controls.Add(_username);
        root.Controls.Add(FieldLabel(Loc.T("enrollment.account.field.username")));
        root.Controls.Add(Spacer(14));
        root.Controls.Add(sub);
        root.Controls.Add(heading);

        Controls.Add(root);
        AcceptButton = createBtn;
        CancelButton = cancelBtn;
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

    private void TryCreate()
    {
        _error.Text = "";
        var username = _username.Text.Trim();
        var password = _password.Text;
        var confirm  = _confirm.Text;

        if (username.Length < 3)
        {
            _error.Text = Loc.T("enrollment.account.validation.username");
            return;
        }
        if (password.Length < 6)
        {
            _error.Text = Loc.T("enrollment.account.validation.password");
            return;
        }
        if (password != confirm)
        {
            _error.Text = Loc.T("enrollment.account.validation.match");
            _confirm.Clear();
            _confirm.Focus();
            return;
        }
        if (UserRepository.UsernameExists(username))
        {
            _error.Text = Loc.T("enrollment.account.validation.taken");
            _username.Focus();
            return;
        }

        var user = new AfterSchool.Models.User
        {
            Username           = username,
            PasswordHash       = PasswordHasher.Hash(password),
            FullName           = $"{_student.FirstName} {_student.LastName}",
            Role               = "Student",
            CreatedDate        = DateTime.Today.ToString("yyyy-MM-dd"),
            IsActive           = 1,
            MustChangePassword = 1,
            StudentId          = _student.Id
        };
        UserRepository.Insert(user);

        MessageBox.Show(
            string.Format(Loc.T("enrollment.account.created"), username),
            Loc.T("enrollment.account.created.title"),
            MessageBoxButtons.OK, MessageBoxIcon.Information);

        DialogResult = DialogResult.OK;
        Close();
    }
}
