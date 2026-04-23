using AfterSchool.Data;
using AfterSchool.Models;
using AfterSchool.UI;

namespace AfterSchool.Forms;

public class SchedulePlannerControl : UserControl
{
    private static readonly string[] Days =
        { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

    private readonly TableLayoutPanel _grid = new();
    private readonly Button _newBtn = new() { Text = "Add Time Slot" };
    private readonly Button _editBtn = new() { Text = "Edit" };
    private readonly Button _deleteBtn = new() { Text = "Delete" };

    private readonly Dictionary<int, Panel> _dayColumns = new();
    private ScheduleView? _selected;
    private Panel? _selectedCard;

    public SchedulePlannerControl()
    {
        BackColor = Theme.Background;
        BuildLayout();
        Refresh();
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

        Theme.StyleButton(_newBtn, primary: true);
        Theme.StyleButton(_editBtn);
        Theme.StyleButton(_deleteBtn, danger: true);

        _newBtn.Click += (_, _) => OpenEditor(null);
        _editBtn.Click += (_, _) => { if (_selected != null) OpenEditor(_selected); };
        _deleteBtn.Click += (_, _) => DeleteSelected();

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            Width = 400,
            Height = 52,
            BackColor = Theme.Surface
        };
        buttons.Controls.Add(_deleteBtn);
        buttons.Controls.Add(_editBtn);
        buttons.Controls.Add(_newBtn);

        var hint = new Label
        {
            Text = "Click a slot to select. Double-click to edit.",
            Font = Theme.SmallFont,
            ForeColor = Theme.TextSecondary,
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Left,
            Width = 320,
            Top = 14
        };

        toolbar.Controls.Add(hint);
        toolbar.Controls.Add(buttons);

        _grid.Dock = DockStyle.Fill;
        _grid.ColumnCount = Days.Length;
        _grid.RowCount = 2;
        _grid.BackColor = Theme.Surface;
        _grid.Padding = new Padding(0, 8, 0, 0);

        for (int i = 0; i < Days.Length; i++)
            _grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / Days.Length));
        _grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        _grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        for (int i = 0; i < Days.Length; i++)
        {
            var header = new Label
            {
                Text = Days[i],
                Font = new Font("Segoe UI Semibold", 10.5f),
                ForeColor = Theme.TextPrimary,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                BackColor = Theme.Background,
                Margin = new Padding(2)
            };
            _grid.Controls.Add(header, i, 0);

            var column = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.Surface,
                AutoScroll = true,
                Margin = new Padding(2),
                Padding = new Padding(4)
            };
            column.Paint += (_, e) =>
            {
                using var pen = new Pen(Theme.Border);
                e.Graphics.DrawRectangle(pen, 0, 0, column.Width - 1, column.Height - 1);
            };
            _dayColumns[i] = column;
            _grid.Controls.Add(column, i, 1);
        }

        card.Controls.Add(_grid);
        card.Controls.Add(toolbar);

        Controls.Add(card);
    }

    public override void Refresh()
    {
        base.Refresh();
        ReloadSlots();
    }

    private void ReloadSlots()
    {
        _selected = null;
        _selectedCard = null;
        foreach (var col in _dayColumns.Values) col.Controls.Clear();

        var slots = ScheduleRepository.GetAll().ToList();

        for (int i = 0; i < Days.Length; i++)
        {
            var day = Days[i];
            var col = _dayColumns[i];
            var daySlots = slots
                .Where(s => string.Equals(s.DayOfWeek, day, StringComparison.OrdinalIgnoreCase))
                .OrderBy(s => s.StartTime)
                .ToList();

            foreach (var s in daySlots)
                col.Controls.Add(BuildSlotCard(s));
        }
    }

    private Panel BuildSlotCard(ScheduleView slot)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 78,
            Margin = new Padding(0, 0, 0, 8),
            BackColor = Color.FromArgb(239, 246, 255),
            Padding = new Padding(10, 8, 10, 8),
            Cursor = Cursors.Hand,
            Tag = slot
        };
        panel.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(147, 197, 253));
            e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
            using var bar = new SolidBrush(Theme.Primary);
            e.Graphics.FillRectangle(bar, 0, 0, 3, panel.Height);
        };

        var time = new Label
        {
            Text = $"{slot.StartTime} – {slot.EndTime}",
            Font = new Font("Segoe UI Semibold", 9.5f),
            ForeColor = Theme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 20
        };
        var course = new Label
        {
            Text = slot.CourseName,
            Font = Theme.BodyFont,
            ForeColor = Theme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 20
        };
        var meta = new Label
        {
            Text = string.IsNullOrWhiteSpace(slot.Room)
                ? slot.Teacher
                : $"{slot.Teacher} · {slot.Room}",
            Font = Theme.SmallFont,
            ForeColor = Theme.TextSecondary,
            Dock = DockStyle.Top,
            Height = 18
        };

        panel.Controls.Add(meta);
        panel.Controls.Add(course);
        panel.Controls.Add(time);

        panel.Click += (_, _) => Select(panel, slot);
        time.Click += (_, _) => Select(panel, slot);
        course.Click += (_, _) => Select(panel, slot);
        meta.Click += (_, _) => Select(panel, slot);

        panel.DoubleClick += (_, _) => OpenEditor(slot);
        time.DoubleClick += (_, _) => OpenEditor(slot);
        course.DoubleClick += (_, _) => OpenEditor(slot);
        meta.DoubleClick += (_, _) => OpenEditor(slot);

        return panel;
    }

    private void Select(Panel card, ScheduleView slot)
    {
        if (_selectedCard != null)
        {
            _selectedCard.BackColor = Color.FromArgb(239, 246, 255);
            _selectedCard.Invalidate();
        }
        _selectedCard = card;
        _selected = slot;
        card.BackColor = Color.FromArgb(191, 219, 254);
        card.Invalidate();
    }

    private void DeleteSelected()
    {
        if (_selected == null) return;
        var result = MessageBox.Show(
            $"Delete this slot?\n\n{_selected.CourseName}\n{_selected.DayOfWeek} {_selected.StartTime}–{_selected.EndTime}",
            "Confirm delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes) return;
        ScheduleRepository.Delete(_selected.Id);
        ReloadSlots();
    }

    private void OpenEditor(ScheduleView? existing)
    {
        using var dlg = new ScheduleEditorDialog(existing);
        if (dlg.ShowDialog(this) == DialogResult.OK)
            ReloadSlots();
    }
}

internal sealed class ScheduleEditorDialog : Form
{
    private static readonly string[] Days =
        { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

    private readonly Schedule _slot;
    private readonly bool _isNew;
    private readonly ComboBox _course = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _day = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly DateTimePicker _start = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm", ShowUpDown = true };
    private readonly DateTimePicker _end = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm", ShowUpDown = true };
    private readonly TextBox _room = new();

    public ScheduleEditorDialog(ScheduleView? existing)
    {
        _isNew = existing == null;
        _slot = existing == null
            ? new Schedule { DayOfWeek = "Monday", StartTime = "09:00", EndTime = "10:00" }
            : new Schedule
            {
                Id = existing.Id,
                CourseId = existing.CourseId,
                DayOfWeek = existing.DayOfWeek,
                StartTime = existing.StartTime,
                EndTime = existing.EndTime,
                Room = existing.Room
            };

        Text = _isNew ? "Add Time Slot" : "Edit Time Slot";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Size = new Size(500, 440);
        BackColor = Theme.Surface;
        Font = Theme.BodyFont;

        var courses = CourseRepository.GetAll().ToList();
        _course.DataSource = courses;
        _course.DisplayMember = nameof(Course.Name);
        _course.ValueMember = nameof(Course.Id);
        if (courses.Count > 0)
        {
            var match = courses.FirstOrDefault(c => c.Id == _slot.CourseId) ?? courses[0];
            _course.SelectedItem = match;
        }

        _day.Items.AddRange(Days);
        _day.SelectedItem = Days.Contains(_slot.DayOfWeek) ? _slot.DayOfWeek : Days[0];

        _start.Value = ParseTime(_slot.StartTime, new TimeSpan(9, 0, 0));
        _end.Value = ParseTime(_slot.EndTime, new TimeSpan(10, 0, 0));

        Theme.StyleTextBox(_room);
        _room.Text = _slot.Room;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            Padding = new Padding(24),
            BackColor = Theme.Surface
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        layout.Controls.Add(FieldLabel("Course"));
        _course.Dock = DockStyle.Top; _course.Height = 30; _course.Font = Theme.BodyFont;
        layout.Controls.Add(_course);

        layout.Controls.Add(Spacer(12));
        layout.Controls.Add(FieldLabel("Day"));
        _day.Dock = DockStyle.Top; _day.Height = 30; _day.Font = Theme.BodyFont;
        layout.Controls.Add(_day);

        layout.Controls.Add(Spacer(12));

        var times = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 60,
            ColumnCount = 2,
            BackColor = Theme.Surface
        };
        times.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        times.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        var startCol = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Surface };
        var endCol = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Surface };
        _start.Dock = DockStyle.Bottom; _start.Font = Theme.BodyFont;
        _end.Dock = DockStyle.Bottom; _end.Font = Theme.BodyFont;
        startCol.Controls.Add(_start);
        startCol.Controls.Add(FieldLabel("Start time"));
        endCol.Controls.Add(_end);
        endCol.Controls.Add(FieldLabel("End time"));
        times.Controls.Add(startCol, 0, 0);
        times.Controls.Add(endCol, 1, 0);
        layout.Controls.Add(times);

        layout.Controls.Add(Spacer(12));
        layout.Controls.Add(FieldLabel("Room"));
        _room.Dock = DockStyle.Top; _room.Height = 30;
        layout.Controls.Add(_room);

        var ok = new Button { Text = _isNew ? "Add" : "Save" };
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

    private static Label FieldLabel(string text) => new()
    {
        Text = text,
        Font = new Font("Segoe UI Semibold", 9.5f),
        ForeColor = Theme.TextSecondary,
        Dock = DockStyle.Top,
        Height = 22,
        TextAlign = ContentAlignment.BottomLeft
    };

    private static Panel Spacer(int h) => new() { Dock = DockStyle.Top, Height = h, BackColor = Theme.Surface };

    private static DateTime ParseTime(string s, TimeSpan fallback)
    {
        var today = DateTime.Today;
        return TimeSpan.TryParse(s, out var t) ? today + t : today + fallback;
    }

    private void Save()
    {
        if (_course.SelectedValue is not int courseId || courseId == 0)
        {
            MessageBox.Show(this, "No course available — create a course first.", "Validation",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (_end.Value <= _start.Value)
        {
            MessageBox.Show(this, "End time must be after start time.", "Validation",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _slot.CourseId = courseId;
        _slot.DayOfWeek = (string)_day.SelectedItem!;
        _slot.StartTime = _start.Value.ToString("HH:mm");
        _slot.EndTime = _end.Value.ToString("HH:mm");
        _slot.Room = _room.Text.Trim();

        if (_isNew) ScheduleRepository.Insert(_slot);
        else ScheduleRepository.Update(_slot);

        DialogResult = DialogResult.OK;
        Close();
    }
}
