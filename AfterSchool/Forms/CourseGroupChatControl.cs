using AfterSchool.Data;
using AfterSchool.Models;
using AfterSchool.Services;
using AfterSchool.UI;

namespace AfterSchool.Forms;

public class CourseGroupChatControl : UserControl
{
    // ── Layout ────────────────────────────────────────────────────────────────
    private readonly Panel           _sidebar      = new();
    private readonly FlowLayoutPanel _courseList   = new();
    private readonly Panel           _msgPanel     = new();
    private readonly TextBox         _inputBox     = new();
    private readonly Button          _sendBtn      = new();
    private readonly Label           _headerLbl    = new();
    private readonly Label           _headerSubLbl = new();

    // ── State ─────────────────────────────────────────────────────────────────
    private Course?                          _activeCourse;
    private readonly List<CourseMessageView> _messages        = new();
    private readonly List<Panel>             _bubbleRows      = new();
    private readonly List<Course>            _accessibleCourses = new();
    private readonly Dictionary<int, int>    _currentMsgIds   = new(); // latest msg ID per course
    private readonly Dictionary<int, int>    _lastReadMsgIds  = new(); // last read msg ID per course
    private bool _sending;
    private bool _relayouting;
    private int  _lastMessageId;
    private readonly System.Windows.Forms.Timer _pollTimer = new() { Interval = 5_000 };

    // Teacher bubble colours
    private static readonly Color TeacherBubbleBg     = Color.FromArgb(240, 253, 244);
    private static readonly Color TeacherBubbleBorder = Color.FromArgb(134, 239, 172);
    private static readonly Color TeacherNameColor    = Color.FromArgb(22, 163, 74);

    public CourseGroupChatControl()
    {
        BackColor = Theme.Background;
        BuildLayout();
        LoadCourseList();
        _pollTimer.Tick   += (_, _) => PollNewMessages();
        Disposed          += (_, _) => _pollTimer.Dispose();
        Loc.LanguageChanged += () => { LoadCourseList(); UpdateUiText(); };
    }

    // ── Build Layout ──────────────────────────────────────────────────────────

    private void BuildLayout()
    {
        // ── Left sidebar ──────────────────────────────────────────────────────
        _sidebar.Dock      = DockStyle.Left;
        _sidebar.Width     = 260;
        _sidebar.BackColor = Theme.Surface;

        var sidebarHeader = new Panel
        {
            Dock = DockStyle.Top, Height = 60,
            BackColor = Theme.Surface, Padding = new Padding(16, 0, 16, 0)
        };
        sidebarHeader.Paint += (_, e) =>
        {
            using var p = new Pen(Theme.Border);
            e.Graphics.DrawLine(p, 0, sidebarHeader.Height - 1, sidebarHeader.Width, sidebarHeader.Height - 1);
        };
        sidebarHeader.Controls.Add(new Label
        {
            Text = Loc.T("nav.coursechat"), Font = new Font("Segoe UI Semibold", 13f),
            ForeColor = Theme.TextPrimary, Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        });

        _courseList.Dock          = DockStyle.Fill;
        _courseList.FlowDirection = FlowDirection.TopDown;
        _courseList.WrapContents  = false;
        _courseList.AutoScroll    = true;
        _courseList.Padding       = new Padding(8);
        _courseList.BackColor     = Theme.Surface;

        _sidebar.Controls.Add(_courseList);
        _sidebar.Controls.Add(sidebarHeader);
        _sidebar.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 1, BackColor = Theme.Border });

        // ── Right chat area ───────────────────────────────────────────────────
        var chatArea = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background };

        var chatHeader = new Panel
        {
            Dock = DockStyle.Top, Height = 56,
            BackColor = Theme.Surface, Padding = new Padding(20, 0, 16, 0)
        };
        chatHeader.Paint += (_, e) =>
        {
            using var p = new Pen(Theme.Border);
            e.Graphics.DrawLine(p, 0, chatHeader.Height - 1, chatHeader.Width, chatHeader.Height - 1);
        };

        var headerTextPanel = new Panel { Dock = DockStyle.Fill };
        _headerLbl.Font      = new Font("Segoe UI Semibold", 11f);
        _headerLbl.ForeColor = Theme.TextPrimary;
        _headerLbl.Dock      = DockStyle.Top;
        _headerLbl.Height    = 30;
        _headerLbl.TextAlign = ContentAlignment.BottomLeft;
        _headerLbl.Text      = Loc.T("coursechat.select");

        _headerSubLbl.Font      = Theme.SmallFont;
        _headerSubLbl.ForeColor = Theme.TextSecondary;
        _headerSubLbl.Dock      = DockStyle.Fill;
        _headerSubLbl.TextAlign = ContentAlignment.TopLeft;

        headerTextPanel.Controls.Add(_headerSubLbl);
        headerTextPanel.Controls.Add(_headerLbl);
        chatHeader.Controls.Add(headerTextPanel);

        // Messages panel
        _msgPanel.Dock        = DockStyle.Fill;
        _msgPanel.AutoScroll  = true;
        _msgPanel.BackColor   = Color.FromArgb(248, 250, 252);
        _msgPanel.SizeChanged += (_, _) => RelayoutMessages();

        // Input bar
        var inputBar = new Panel
        {
            Dock = DockStyle.Bottom, Height = 72,
            BackColor = Theme.Surface, Padding = new Padding(16, 12, 16, 12)
        };
        inputBar.Paint += (_, e) =>
        {
            using var p = new Pen(Theme.Border);
            e.Graphics.DrawLine(p, 0, 0, inputBar.Width, 0);
        };

        _inputBox.Multiline       = false;
        _inputBox.Dock            = DockStyle.Fill;
        _inputBox.Font            = Theme.BodyFont;
        _inputBox.BorderStyle     = BorderStyle.FixedSingle;
        _inputBox.BackColor       = Theme.Background;
        _inputBox.ForeColor       = Theme.TextPrimary;
        _inputBox.PlaceholderText = Loc.T("coursechat.input.placeholder");
        _inputBox.Enabled         = false;
        _inputBox.KeyDown        += InputBox_KeyDown;

        _sendBtn.Text      = Loc.T("coursechat.btn.send");
        _sendBtn.Width     = 80;
        _sendBtn.Dock      = DockStyle.Right;
        _sendBtn.Cursor    = Cursors.Hand;
        _sendBtn.FlatStyle = FlatStyle.Flat;
        _sendBtn.BackColor = Theme.Primary;
        _sendBtn.ForeColor = Color.White;
        _sendBtn.Font      = Theme.BodyFont;
        _sendBtn.FlatAppearance.BorderSize = 0;
        _sendBtn.FlatAppearance.MouseOverBackColor = Theme.PrimaryHover;
        _sendBtn.Margin    = new Padding(8, 0, 0, 0);
        _sendBtn.Enabled   = false;
        _sendBtn.Click    += (_, _) => SendMessage();

        inputBar.Controls.Add(_inputBox);
        inputBar.Controls.Add(_sendBtn);

        chatArea.Controls.Add(_msgPanel);
        chatArea.Controls.Add(inputBar);
        chatArea.Controls.Add(chatHeader);

        Controls.Add(chatArea);
        Controls.Add(_sidebar);
    }

    private void UpdateUiText()
    {
        _inputBox.PlaceholderText = Loc.T("coursechat.input.placeholder");
        _sendBtn.Text = Loc.T("coursechat.btn.send");
        if (_activeCourse == null) _headerLbl.Text = Loc.T("coursechat.select");
    }

    // ── Course list ───────────────────────────────────────────────────────────

    private void LoadCourseList()
    {
        _accessibleCourses.Clear();
        _accessibleCourses.AddRange(GetAccessibleCourses());

        // Batch-fetch latest message IDs for badge tracking
        var latestIds = CourseMessageRepository.GetLastMessageIds(_accessibleCourses.Select(c => c.Id));
        foreach (var c in _accessibleCourses)
        {
            var latestId = latestIds.GetValueOrDefault(c.Id, 0);
            _currentMsgIds[c.Id] = latestId;
            if (!_lastReadMsgIds.ContainsKey(c.Id))
                _lastReadMsgIds[c.Id] = latestId; // first time = treat as read
        }

        RenderCourseList();
    }

    private void RenderCourseList()
    {
        _courseList.Controls.Clear();
        foreach (var c in _accessibleCourses) _courseList.Controls.Add(BuildCourseItem(c));
        if (_courseList.Controls.Count == 0)
            _courseList.Controls.Add(new Label
            {
                Text = Loc.T("coursechat.no_courses"), Font = Theme.SmallFont,
                ForeColor = Theme.TextSecondary, AutoSize = false,
                Width = 230, Height = 36, TextAlign = ContentAlignment.MiddleCenter
            });
    }

    private List<Course> GetAccessibleCourses()
    {
        var session = Session.Current;
        if (session == null) return new List<Course>();
        if (session.Role == "Student" && session.StudentId.HasValue)
            return CourseMessageRepository.GetCoursesForStudent(session.StudentId.Value);
        if (session.Role == "Teacher")
            return CourseMessageRepository.GetCoursesForTeacher(session.FullName ?? "");
        return CourseRepository.GetAll().ToList();
    }

    private Panel BuildCourseItem(Course c)
    {
        bool hasUnread = _activeCourse?.Id != c.Id
                         && _currentMsgIds.TryGetValue(c.Id, out var cur)
                         && cur > _lastReadMsgIds.GetValueOrDefault(c.Id, 0);

        var item = new Panel
        {
            Width = 240, Height = 56, Cursor = Cursors.Hand,
            BackColor = _activeCourse?.Id == c.Id ? Color.FromArgb(219, 234, 254) : Theme.Surface,
            Margin = new Padding(0, 0, 0, 2), Tag = c
        };
        item.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawLine(pen, 8, item.Height - 1, item.Width - 8, item.Height - 1);
            if (hasUnread)
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(Color.FromArgb(220, 38, 38));
                e.Graphics.FillEllipse(brush, item.Width - 22, (item.Height - 10) / 2, 10, 10);
            }
        };
        var nameLbl = new Label
        {
            Text = c.Name, Font = new Font("Segoe UI", hasUnread ? 9.5f : 9.5f),
            ForeColor = hasUnread ? Theme.TextPrimary : Theme.TextPrimary,
            Bounds = new Rectangle(12, 8, 210, 22), AutoEllipsis = true
        };
        if (hasUnread) nameLbl.Font = new Font("Segoe UI Semibold", 9.5f);

        var teacherLbl = new Label
        {
            Text = c.Teacher.Length > 0 ? c.Teacher : "—", Font = Theme.SmallFont,
            ForeColor = Theme.TextSecondary, Bounds = new Rectangle(12, 30, 210, 18), AutoEllipsis = true
        };
        void OnClick(object? s, EventArgs e) => LoadCourse(c);
        item.Click += OnClick; nameLbl.Click += OnClick; teacherLbl.Click += OnClick;
        item.Controls.Add(nameLbl);
        item.Controls.Add(teacherLbl);
        return item;
    }

    // ── Load course chat ──────────────────────────────────────────────────────

    private void LoadCourse(Course c)
    {
        _pollTimer.Stop();
        _activeCourse = c;
        _headerLbl.Text    = c.Name;
        _headerSubLbl.Text = c.Teacher.Length > 0
            ? string.Format(Loc.T("coursechat.teacher_label"), c.Teacher)
            : "";
        _inputBox.Enabled = true;
        _sendBtn.Enabled  = true;
        ClearMessages();
        _messages.Clear();
        _messages.AddRange(CourseMessageRepository.GetMessages(c.Id, Session.Current?.Id ?? 0));
        _lastMessageId = _messages.Count > 0 ? _messages[^1].Id : 0;

        // Mark as read
        _lastReadMsgIds[c.Id] = _lastMessageId;
        _currentMsgIds[c.Id]  = _lastMessageId;

        foreach (var m in _messages) AddMessageRow(m);
        ScrollToBottom();
        RenderCourseList(); // refresh sidebar to clear badge
        _inputBox.Focus();
        _pollTimer.Start();
    }

    private void PollNewMessages()
    {
        if (_sending) return;

        // Check all courses for badge updates
        var latestIds = CourseMessageRepository.GetLastMessageIds(_accessibleCourses.Select(x => x.Id));
        bool listDirty = false;
        foreach (var (courseId, lastId) in latestIds)
        {
            _currentMsgIds[courseId] = lastId;
            if (courseId != _activeCourse?.Id && lastId > _lastReadMsgIds.GetValueOrDefault(courseId, 0))
                listDirty = true;
        }
        if (listDirty) RenderCourseList();

        // Fetch new messages for the active course
        if (_activeCourse == null) return;
        var all = CourseMessageRepository.GetMessages(_activeCourse.Id, Session.Current?.Id ?? 0);
        var newMsgs = all.Where(m => m.Id > _lastMessageId).ToList();
        if (newMsgs.Count == 0) return;
        foreach (var m in newMsgs) { _messages.Add(m); AddMessageRow(m); }
        _lastMessageId = _messages[^1].Id;
        // Keep active course marked as read
        _lastReadMsgIds[_activeCourse.Id] = _lastMessageId;
        _currentMsgIds[_activeCourse.Id]  = _lastMessageId;
        ScrollToBottom();
    }

    private void ClearMessages()
    {
        foreach (var r in _bubbleRows) { _msgPanel.Controls.Remove(r); r.Dispose(); }
        _bubbleRows.Clear();
        _msgPanel.AutoScrollMinSize  = Size.Empty;
        _msgPanel.AutoScrollPosition = Point.Empty;
    }

    // ── Send message ──────────────────────────────────────────────────────────

    private void InputBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter && !e.Shift)
        {
            e.SuppressKeyPress = true;
            SendMessage();
        }
    }

    private void SendMessage()
    {
        if (_activeCourse == null || _sending) return;
        var text = _inputBox.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        _sending = true;
        _sendBtn.Enabled  = false;
        _inputBox.Text    = "";
        _inputBox.Enabled = false;

        try
        {
            var session     = Session.Current;
            var senderName  = string.IsNullOrWhiteSpace(session?.FullName) ? session?.Username ?? "" : session.FullName;
            CourseMessageRepository.SendMessage(
                _activeCourse.Id, session?.Id ?? 0, text, senderName, _activeCourse.Name);

            var all     = CourseMessageRepository.GetMessages(_activeCourse.Id, session?.Id ?? 0);
            var newMsgs = all.Where(m => m.Id > _lastMessageId).ToList();
            foreach (var m in newMsgs) { _messages.Add(m); AddMessageRow(m); }
            if (_messages.Count > 0)
            {
                _lastMessageId = _messages[^1].Id;
                _lastReadMsgIds[_activeCourse.Id] = _lastMessageId;
                _currentMsgIds[_activeCourse.Id]  = _lastMessageId;
            }
            ScrollToBottom();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _sending = false;
            _sendBtn.Enabled  = true;
            _inputBox.Enabled = true;
            _inputBox.Focus();
        }
    }

    // ── Message rendering ─────────────────────────────────────────────────────

    private void AddMessageRow(CourseMessageView msg)
    {
        bool isMe      = msg.IsFromMe;
        bool isTeacher = !isMe && msg.SenderRole == "Teacher";

        var row = new Panel { BackColor = Color.Transparent, Tag = isMe };

        if (!isMe)
            row.Controls.Add(new Label
            {
                Text      = msg.SenderDisplayName,
                Font      = new Font("Segoe UI Semibold", 8f),
                ForeColor = isTeacher ? TeacherNameColor : Theme.Primary,
                AutoSize  = true, Location = Point.Empty, Tag = "name"
            });

        var bubbleBg = isMe ? Theme.Primary : isTeacher ? TeacherBubbleBg : Theme.Surface;
        var bubble   = new Panel { BackColor = bubbleBg, Tag = "bubble" };
        if (!isMe)
        {
            var borderColor = isTeacher ? TeacherBubbleBorder : Theme.Border;
            bubble.Paint += (_, e) =>
            {
                using var pen = new Pen(borderColor);
                e.Graphics.DrawRectangle(pen, 0, 0, bubble.Width - 1, bubble.Height - 1);
            };
        }

        var rtb = new RichTextBox
        {
            Text        = msg.Content, ReadOnly    = true,
            BorderStyle = BorderStyle.None,
            BackColor   = bubbleBg,
            ForeColor   = isMe ? Color.White : Theme.TextPrimary,
            Font        = Theme.BodyFont, ScrollBars = RichTextBoxScrollBars.None,
            WordWrap    = true, TabStop = false, DetectUrls = false,
            Cursor      = Cursors.IBeam, Tag = "text"
        };

        bubble.Controls.Add(rtb);
        row.Controls.Add(bubble);

        int cw = Math.Max(_msgPanel.ClientSize.Width, 200);
        MeasureRow(row, cw);
        row.Location = new Point(0, ContentBottom());
        _msgPanel.Controls.Add(row);
        _bubbleRows.Add(row);
        ExtendScrollRange();
    }

    private void MeasureRow(Panel row, int cw)
    {
        bool isMe      = (bool)(row.Tag ?? false);
        const int Pad  = 16;
        const int PadH = 12;
        const int PadV = 10;
        int maxW       = (int)(cw * 0.72);

        var nameLbl = row.Controls.OfType<Label>().FirstOrDefault(l => (string?)l.Tag == "name");
        var bubble  = row.Controls.OfType<Panel>().FirstOrDefault(p => (string?)p.Tag == "bubble");
        var rtb     = bubble?.Controls.OfType<RichTextBox>().FirstOrDefault();

        if (rtb == null || bubble == null) return;

        int nameH    = nameLbl != null ? 20 : 0;
        int maxTextW = maxW - PadH * 2;

        var measured = TextRenderer.MeasureText(
            string.IsNullOrEmpty(rtb.Text) ? " " : rtb.Text,
            rtb.Font, new Size(maxTextW, int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);

        int textW = Math.Min(measured.Width, maxTextW);
        int textH = measured.Height + (int)rtb.Font.GetHeight();
        int bW    = Math.Max(textW + PadH * 2, 64);
        int bH    = textH + PadV * 2;

        rtb.Location    = new Point(PadH, PadV);
        rtb.Size        = new Size(textW, textH);
        bubble.Size     = new Size(bW, bH);
        bubble.Location = new Point(isMe ? cw - bW - Pad : Pad, nameH);

        if (nameLbl != null) { nameLbl.Location = new Point(Pad, 0); nameLbl.Size = nameLbl.PreferredSize; }

        row.Size = new Size(cw, nameH + bH + 8);
    }

    private int ContentBottom() =>
        _bubbleRows.Count == 0 ? 8 : _bubbleRows[^1].Bottom + 4;

    private void ExtendScrollRange()
    {
        _msgPanel.AutoScrollMinSize = new Size(1, ContentBottom() + 8);
    }

    private void RelayoutMessages()
    {
        if (_relayouting) return;
        _relayouting = true;
        try
        {
            int cw = Math.Max(_msgPanel.ClientSize.Width, 200);
            int y  = 8;
            foreach (var row in _bubbleRows)
            {
                MeasureRow(row, cw);
                row.Location = new Point(0, y);
                y = row.Bottom + 4;
            }
            _msgPanel.AutoScrollMinSize = new Size(1, y + 8);
        }
        finally { _relayouting = false; }
    }

    private void ScrollToBottom()
    {
        _msgPanel.AutoScrollPosition = new Point(0, _msgPanel.AutoScrollMinSize.Height);
    }
}
