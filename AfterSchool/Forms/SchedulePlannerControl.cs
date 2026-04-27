using AfterSchool.Data;
using AfterSchool.Models;
using AfterSchool.Services;
using AfterSchool.UI;

namespace AfterSchool.Forms;

public class SchedulePlannerControl : UserControl
{
    internal const int SlotDurationMinutes = 90;

    private static readonly string[] Days =
        { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

    private readonly TableLayoutPanel _grid = new();
    private readonly Button _newBtn = new();
    private readonly Button _editBtn = new();
    private readonly Button _deleteBtn = new();
    private readonly Button _roomsBtn = new();

    private readonly Dictionary<int, Panel> _dayColumns = new();
    private ScheduleView? _selected;
    private Panel? _selectedCard;

    public SchedulePlannerControl()
    {
        BackColor = Theme.Background;
        BuildLayout();
        ReloadSlots();
    }

    private void BuildLayout()
    {
        _newBtn.Text = Loc.T("schedule.btn.add");
        _editBtn.Text = Loc.T("common.edit");
        _deleteBtn.Text = Loc.T("common.delete");
        _roomsBtn.Text = Loc.T("schedule.btn.manage_rooms");

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
        Theme.StyleButton(_roomsBtn);

        _newBtn.Click += (_, _) => OpenEditor(null);
        _editBtn.Click += (_, _) => { if (_selected != null) OpenEditor(_selected); };
        _deleteBtn.Click += (_, _) => DeleteSelected();
        _roomsBtn.Click += (_, _) =>
        {
            using var dlg = new RoomsDialog();
            dlg.ShowDialog(this);
        };

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            Width = 560,
            Height = 52,
            BackColor = Theme.Surface
        };
        buttons.Controls.Add(_deleteBtn);
        buttons.Controls.Add(_editBtn);
        buttons.Controls.Add(_newBtn);
        buttons.Controls.Add(_roomsBtn);

        var hint = new Label
        {
            Text = Loc.T("schedule.hint"),
            Font = Theme.SmallFont,
            ForeColor = Theme.TextSecondary,
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Left,
            Width = 420,
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
                Text = Loc.T($"days.{Days[i].ToLower()}"),
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
            string.Format(Loc.T("schedule.delete.confirm"),
                _selected.CourseName, _selected.DayOfWeek,
                _selected.StartTime, _selected.EndTime),
            Loc.T("schedule.delete.title"),
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
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

    private static readonly string[] StartTimes = GenerateStartTimes();

    private readonly Schedule _slot;
    private readonly bool _isNew;
    private readonly ComboBox _course = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _day = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _startTime = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label _endTimeLabel = new();
    private readonly ComboBox _room = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label _hint = new();

    private bool _populating;

    // Wraps a day so the ComboBox shows a translated label but stores the English value.
    private sealed record DayItem(string Value, string Label)
    {
        public override string ToString() => Label;
    }

    public ScheduleEditorDialog(ScheduleView? existing)
    {
        _isNew = existing == null;
        _slot = existing == null
            ? new Schedule { DayOfWeek = "Monday", StartTime = "09:00", EndTime = "10:30" }
            : new Schedule
            {
                Id = existing.Id,
                CourseId = existing.CourseId,
                DayOfWeek = existing.DayOfWeek,
                StartTime = existing.StartTime,
                EndTime = existing.EndTime,
                Room = existing.Room
            };

        Text = _isNew ? Loc.T("schedule.editor.title.new") : Loc.T("schedule.editor.title.edit");
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Size = new Size(520, 500);
        BackColor = Theme.Surface;
        Font = Theme.BodyFont;

        BuildLayout();
        PopulateCourses();
        PopulateDays();
        PopulateStartTimes();
        RefreshRooms();
        UpdateEndTimeLabel();
    }

    private void BuildLayout()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            Padding = new Padding(24),
            BackColor = Theme.Surface
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _course.Font = Theme.BodyFont;
        _day.Font = Theme.BodyFont;
        _startTime.Font = Theme.BodyFont;
        _room.Font = Theme.BodyFont;

        _endTimeLabel.Font = new Font("Segoe UI Semibold", 10f);
        _endTimeLabel.ForeColor = Theme.TextPrimary;
        _endTimeLabel.Dock = DockStyle.Top;
        _endTimeLabel.Height = 26;

        _hint.Font = Theme.SmallFont;
        _hint.ForeColor = Theme.TextSecondary;
        _hint.Dock = DockStyle.Top;
        _hint.Height = 18;

        _course.SelectedIndexChanged += (_, _) => { if (!_populating) SyncState(); };
        _day.SelectedIndexChanged += (_, _) =>
        {
            if (_populating) return;
            RefreshRooms();
        };
        _startTime.SelectedIndexChanged += (_, _) =>
        {
            if (_populating) return;
            UpdateEndTimeLabel();
            RefreshRooms();
        };

        layout.Controls.Add(FieldLabel(Loc.T("schedule.editor.course")));
        _course.Dock = DockStyle.Top; _course.Height = 30;
        layout.Controls.Add(_course);

        layout.Controls.Add(Spacer(12));
        layout.Controls.Add(FieldLabel(Loc.T("schedule.editor.day")));
        _day.Dock = DockStyle.Top; _day.Height = 30;
        layout.Controls.Add(_day);

        layout.Controls.Add(Spacer(12));

        var times = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 62,
            ColumnCount = 2,
            BackColor = Theme.Surface
        };
        times.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        times.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        var startCol = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Surface };
        _startTime.Dock = DockStyle.Bottom; _startTime.Height = 30;
        startCol.Controls.Add(_startTime);
        startCol.Controls.Add(FieldLabel(Loc.T("schedule.editor.starttime")));

        var endCol = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Surface };
        endCol.Controls.Add(_endTimeLabel);
        endCol.Controls.Add(FieldLabel(Loc.T("schedule.editor.endtime")));

        times.Controls.Add(startCol, 0, 0);
        times.Controls.Add(endCol, 1, 0);
        layout.Controls.Add(times);

        layout.Controls.Add(Spacer(12));
        layout.Controls.Add(FieldLabel(Loc.T("schedule.editor.room")));
        _room.Dock = DockStyle.Top; _room.Height = 30;
        layout.Controls.Add(_room);

        layout.Controls.Add(_hint);

        var ok = new Button { Text = _isNew ? Loc.T("common.add") : Loc.T("common.save") };
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

    private void PopulateCourses()
    {
        _populating = true;
        _course.Items.Clear();
        var courses = CourseRepository.GetAll().ToList();
        foreach (var c in courses) _course.Items.Add(c);

        if (courses.Count == 0)
        {
            _hint.Text = Loc.T("schedule.editor.nocourses");
            _hint.ForeColor = Theme.Danger;
        }
        else
        {
            var match = courses.FirstOrDefault(c => c.Id == _slot.CourseId) ?? courses[0];
            _course.SelectedItem = match;
        }
        _populating = false;
    }

    private void PopulateDays()
    {
        _populating = true;
        _day.Items.Clear();
        var items = Days.Select(d => new DayItem(d, Loc.T($"days.{d.ToLower()}"))).ToArray();
        foreach (var item in items) _day.Items.Add(item);
        _day.SelectedItem = items.FirstOrDefault(d => d.Value == _slot.DayOfWeek) ?? items[0];
        _populating = false;
    }

    private void PopulateStartTimes()
    {
        _populating = true;
        _startTime.Items.Clear();
        foreach (var t in StartTimes) _startTime.Items.Add(t);

        var current = _slot.StartTime;
        int idx = Array.IndexOf(StartTimes, current);
        if (idx < 0 && !string.IsNullOrEmpty(current))
        {
            _startTime.Items.Insert(0, current);
            idx = 0;
        }
        _startTime.SelectedIndex = Math.Max(0, idx);
        _populating = false;
    }

    private void UpdateEndTimeLabel()
    {
        _endTimeLabel.Text = ComputeEndTime(CurrentStartTime());
    }

    private void SyncState()
    {
        RefreshRooms();
    }

    private void RefreshRooms()
    {
        var previouslySelected = (_room.SelectedItem as Room)?.Name
                                  ?? _room.SelectedItem?.ToString()
                                  ?? _slot.Room;

        var allRooms = RoomRepository.GetAll().ToList();
        var day = CurrentDay();
        var start = CurrentStartTime();
        var end = ComputeEndTime(start);
        var occupied = string.IsNullOrEmpty(day) || string.IsNullOrEmpty(start)
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(
                ScheduleRepository.GetOccupiedRooms(day, start, end, _isNew ? null : _slot.Id),
                StringComparer.OrdinalIgnoreCase);

        _populating = true;
        _room.Items.Clear();

        var available = allRooms.Where(r => !occupied.Contains(r.Name)).ToList();
        if (available.Count == 0)
        {
            _room.Items.Add(Loc.T("schedule.rooms.noavailable"));
            _room.SelectedIndex = 0;
            _room.Enabled = false;
        }
        else
        {
            _room.Enabled = true;
            foreach (var r in available) _room.Items.Add(r);

            Room? match = null;
            if (!string.IsNullOrWhiteSpace(previouslySelected))
                match = available.FirstOrDefault(r =>
                    string.Equals(r.Name, previouslySelected, StringComparison.OrdinalIgnoreCase));
            _room.SelectedItem = match ?? available[0];
        }

        _populating = false;

        if (allRooms.Count == 0)
        {
            _hint.Text = Loc.T("schedule.rooms.noexist");
            _hint.ForeColor = Theme.Danger;
        }
        else if (occupied.Count > 0 && available.Count == 0)
        {
            _hint.Text = string.Format(Loc.T("schedule.rooms.allbooked"), allRooms.Count);
            _hint.ForeColor = Theme.Danger;
        }
        else if (occupied.Count > 0)
        {
            _hint.Text = string.Format(Loc.T("schedule.rooms.booked"), occupied.Count);
            _hint.ForeColor = Theme.TextSecondary;
        }
        else
        {
            _hint.Text = string.Format(Loc.T("schedule.rooms.available"), available.Count);
            _hint.ForeColor = Theme.TextSecondary;
        }
    }

    private string CurrentStartTime() => _startTime.SelectedItem?.ToString() ?? _slot.StartTime;
    private string CurrentDay() => (_day.SelectedItem as DayItem)?.Value ?? _slot.DayOfWeek;

    private static string ComputeEndTime(string startTime)
    {
        if (!TimeSpan.TryParse(startTime, out var t)) return "";
        var end = t + TimeSpan.FromMinutes(SchedulePlannerControl.SlotDurationMinutes);
        return $"{(int)end.TotalHours:D2}:{end.Minutes:D2}";
    }

    private static string[] GenerateStartTimes()
    {
        var list = new List<string>();
        for (int mins = 7 * 60; mins <= 19 * 60 + 30; mins += 30)
            list.Add($"{mins / 60:D2}:{mins % 60:D2}");
        return list.ToArray();
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

    private void Save()
    {
        if (_course.SelectedItem is not Course course)
        {
            MessageBox.Show(this, Loc.T("schedule.editor.validation.nocourse"),
                Loc.T("schedule.editor.validation.title"),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (_room.SelectedItem is not Room room)
        {
            MessageBox.Show(this, Loc.T("schedule.editor.validation.noroom"),
                Loc.T("schedule.editor.validation.title"),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var start = CurrentStartTime();
        var end = ComputeEndTime(start);
        if (string.IsNullOrEmpty(end))
        {
            MessageBox.Show(this, Loc.T("schedule.editor.validation.notime"),
                Loc.T("schedule.editor.validation.title"),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var day = CurrentDay();
        var conflicting = ScheduleRepository
            .GetOccupiedRooms(day, start, end, _isNew ? null : _slot.Id)
            .Any(n => string.Equals(n, room.Name, StringComparison.OrdinalIgnoreCase));
        if (conflicting)
        {
            MessageBox.Show(this,
                string.Format(Loc.T("schedule.editor.conflict"), room.Name, day, start, end),
                Loc.T("schedule.editor.conflict.title"),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            RefreshRooms();
            return;
        }

        _slot.CourseId = course.Id;
        _slot.DayOfWeek = day;
        _slot.StartTime = start;
        _slot.EndTime = end;
        _slot.Room = room.Name;

        if (_isNew) ScheduleRepository.Insert(_slot);
        else ScheduleRepository.Update(_slot);

        DialogResult = DialogResult.OK;
        Close();
    }
}

internal sealed class RoomsDialog : Form
{
    private readonly ListBox _list = new() { Font = new Font("Segoe UI", 10f) };
    private readonly TextBox _newName = new();
    private readonly Button _addBtn = new();
    private readonly Button _removeBtn = new();
    private readonly Label _status = new();

    public RoomsDialog()
    {
        Text = Loc.T("rooms.title");
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Size = new Size(440, 480);
        BackColor = Theme.Surface;
        Font = Theme.BodyFont;

        BuildLayout();
        Reload();
    }

    private void BuildLayout()
    {
        var header = new Label
        {
            Text = Loc.T("rooms.header"),
            Font = new Font("Segoe UI Semibold", 14f),
            ForeColor = Theme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 32
        };
        var sub = new Label
        {
            Text = Loc.T("rooms.sub"),
            Font = Theme.SmallFont,
            ForeColor = Theme.TextSecondary,
            Dock = DockStyle.Top,
            Height = 22
        };

        Theme.StyleTextBox(_newName);
        _newName.Width = 220;
        _newName.PlaceholderText = Loc.T("rooms.placeholder");

        _addBtn.Text = Loc.T("common.add");
        _removeBtn.Text = Loc.T("common.remove");
        Theme.StyleButton(_addBtn, primary: true);
        Theme.StyleButton(_removeBtn, danger: true);
        _addBtn.Click += (_, _) => AddRoom();
        _removeBtn.Click += (_, _) => RemoveSelected();

        _list.BorderStyle = BorderStyle.FixedSingle;
        _list.IntegralHeight = false;
        _list.Dock = DockStyle.Fill;
        _list.Margin = new Padding(0, 8, 0, 8);

        _status.Font = Theme.SmallFont;
        _status.ForeColor = Theme.TextSecondary;
        _status.Dock = DockStyle.Top;
        _status.Height = 22;

        var addRow = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Theme.Surface };
        _newName.Dock = DockStyle.Left;
        _newName.Height = 30;
        _addBtn.Dock = DockStyle.Right;
        _addBtn.Width = 90;
        _newName.Top = 6;
        addRow.Controls.Add(_newName);
        addRow.Controls.Add(_addBtn);

        var bottom = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = Theme.Surface };
        _removeBtn.Dock = DockStyle.Right;
        _removeBtn.Width = 110;
        var close = new Button
        {
            Text = Loc.T("common.close"),
            DialogResult = DialogResult.OK,
            Dock = DockStyle.Right,
            Width = 100
        };
        Theme.StyleButton(close);
        bottom.Controls.Add(close);
        bottom.Controls.Add(_removeBtn);

        var inner = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = Theme.Surface };
        inner.Controls.Add(_list);
        inner.Controls.Add(_status);
        inner.Controls.Add(addRow);
        inner.Controls.Add(sub);
        inner.Controls.Add(header);

        Controls.Add(inner);
        Controls.Add(bottom);
    }

    private void Reload()
    {
        var selected = _list.SelectedItem as Room;
        _list.Items.Clear();
        var rooms = RoomRepository.GetAll().ToList();
        foreach (var r in rooms) _list.Items.Add(r);
        if (selected != null)
        {
            var match = rooms.FirstOrDefault(r => r.Id == selected.Id);
            if (match != null) _list.SelectedItem = match;
        }
        _status.Text = string.Format(Loc.T("rooms.count"), rooms.Count);
        _status.ForeColor = Theme.TextSecondary;
    }

    private void AddRoom()
    {
        var name = _newName.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            _status.Text = Loc.T("rooms.enter_name");
            _status.ForeColor = Theme.Danger;
            return;
        }
        if (RoomRepository.Exists(name))
        {
            _status.Text = Loc.T("rooms.already_exists");
            _status.ForeColor = Theme.Danger;
            return;
        }
        RoomRepository.Insert(name);
        _newName.Text = "";
        Reload();
    }

    private void RemoveSelected()
    {
        if (_list.SelectedItem is not Room room)
        {
            _status.Text = Loc.T("rooms.select_first");
            _status.ForeColor = Theme.Danger;
            return;
        }
        var uses = RoomRepository.UsageCount(room.Id);
        if (uses > 0)
        {
            _status.Text = string.Format(Loc.T("rooms.in_use"), room.Name, uses);
            _status.ForeColor = Theme.Danger;
            return;
        }
        var confirm = MessageBox.Show(this,
            string.Format(Loc.T("rooms.confirm_remove"), room.Name),
            Loc.T("rooms.confirm_title"),
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes) return;
        RoomRepository.Delete(room.Id);
        Reload();
    }
}
