using AfterSchool.Data;
using AfterSchool.Models;
using AfterSchool.Services;
using AfterSchool.UI;

namespace AfterSchool.Forms;

public class DirectChatControl : UserControl
{
    // ── Layout ────────────────────────────────────────────────────────────────
    private readonly Panel           _sidebar       = new();
    private readonly FlowLayoutPanel _convList      = new();
    private readonly TextBox         _searchBox     = new();
    private readonly Panel           _msgPanel      = new();
    private readonly TextBox         _inputBox      = new();
    private readonly Button          _sendBtn       = new();
    private readonly Button          _attachBtn     = new();
    private readonly FlowLayoutPanel _pendingBar    = new();
    private readonly Label           _headerLbl     = new();
    private readonly Label           _headerRoleLbl = new();
    private readonly Button          _deleteBtn     = new();

    // ── State ─────────────────────────────────────────────────────────────────
    private DirectConversationView?          _active;
    private readonly List<DirectMessageView> _messages      = new();
    private readonly List<Panel>             _bubbleRows    = new();
    private readonly List<string>            _pendingFiles  = new();
    private readonly Dictionary<int, string> _lastReadDates = new(); // convId -> last-read date
    private bool _sending;
    private bool _relayouting;
    private readonly System.Windows.Forms.Timer _pollTimer = new() { Interval = 10_000 };

    public DirectChatControl()
    {
        BackColor = Theme.Background;
        BuildLayout();
        LoadConversationList();
        _pollTimer.Tick   += (_, _) => PollConversationList();
        Disposed          += (_, _) => _pollTimer.Dispose();
        _pollTimer.Start();
        Loc.LanguageChanged += () => { LoadConversationList(); UpdateUiText(); };
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
            Text = Loc.T("nav.messages"), Font = new Font("Segoe UI Semibold", 13f),
            ForeColor = Theme.TextPrimary, Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        });

        var searchPanel = new Panel
        {
            Dock = DockStyle.Top, Height = 48,
            BackColor = Theme.Surface, Padding = new Padding(10, 8, 10, 8)
        };
        searchPanel.Paint += (_, e) =>
        {
            using var p = new Pen(Theme.Border);
            e.Graphics.DrawLine(p, 0, searchPanel.Height - 1, searchPanel.Width, searchPanel.Height - 1);
        };
        _searchBox.Dock         = DockStyle.Fill;
        _searchBox.Font         = Theme.BodyFont;
        _searchBox.BorderStyle  = BorderStyle.FixedSingle;
        _searchBox.BackColor    = Theme.Background;
        _searchBox.ForeColor    = Theme.TextPrimary;
        _searchBox.PlaceholderText = Loc.T("messages.search.placeholder");
        _searchBox.TextChanged  += (_, _) => OnSearchChanged();
        searchPanel.Controls.Add(_searchBox);

        _convList.Dock          = DockStyle.Fill;
        _convList.FlowDirection = FlowDirection.TopDown;
        _convList.WrapContents  = false;
        _convList.AutoScroll    = true;
        _convList.Padding       = new Padding(8);
        _convList.BackColor     = Theme.Surface;

        _sidebar.Controls.Add(_convList);
        _sidebar.Controls.Add(searchPanel);
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
        _headerLbl.Text      = Loc.T("messages.no_conversation");

        _headerRoleLbl.Font      = Theme.SmallFont;
        _headerRoleLbl.ForeColor = Theme.TextSecondary;
        _headerRoleLbl.Dock      = DockStyle.Fill;
        _headerRoleLbl.TextAlign = ContentAlignment.TopLeft;

        headerTextPanel.Controls.Add(_headerRoleLbl);
        headerTextPanel.Controls.Add(_headerLbl);

        _deleteBtn.Text      = Loc.T("messages.btn.delete");
        _deleteBtn.Width     = 80;
        _deleteBtn.Dock      = DockStyle.Right;
        _deleteBtn.FlatStyle = FlatStyle.Flat;
        _deleteBtn.Cursor    = Cursors.Hand;
        _deleteBtn.Font      = Theme.SmallFont;
        _deleteBtn.BackColor = Theme.Surface;
        _deleteBtn.ForeColor = Theme.TextSecondary;
        _deleteBtn.FlatAppearance.BorderSize = 0;
        _deleteBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(254, 242, 242);
        _deleteBtn.Enabled   = false;
        _deleteBtn.Click    += (_, _) => DeleteActiveConversation();

        chatHeader.Controls.Add(headerTextPanel);
        chatHeader.Controls.Add(_deleteBtn);

        // Messages panel
        _msgPanel.Dock       = DockStyle.Fill;
        _msgPanel.AutoScroll = true;
        _msgPanel.BackColor  = Color.FromArgb(248, 250, 252);
        _msgPanel.SizeChanged += (_, _) => RelayoutMessages();

        // Pending attachments bar (hidden until files are queued)
        _pendingBar.Dock          = DockStyle.Bottom;
        _pendingBar.FlowDirection = FlowDirection.LeftToRight;
        _pendingBar.WrapContents  = true;
        _pendingBar.AutoSize      = true;
        _pendingBar.AutoSizeMode  = AutoSizeMode.GrowAndShrink;
        _pendingBar.BackColor     = Theme.Surface;
        _pendingBar.Padding       = new Padding(12, 8, 12, 0);
        _pendingBar.Visible       = false;
        _pendingBar.Paint += (_, e) =>
        {
            using var p = new Pen(Theme.Border);
            e.Graphics.DrawLine(p, 0, 0, _pendingBar.Width, 0);
        };

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

        _attachBtn.Text      = "📎";
        _attachBtn.Width     = 40;
        _attachBtn.Dock      = DockStyle.Left;
        _attachBtn.Cursor    = Cursors.Hand;
        _attachBtn.FlatStyle = FlatStyle.Flat;
        _attachBtn.BackColor = Theme.Background;
        _attachBtn.ForeColor = Theme.TextPrimary;
        _attachBtn.Font      = new Font("Segoe UI", 14f);
        _attachBtn.FlatAppearance.BorderSize = 0;
        _attachBtn.FlatAppearance.MouseOverBackColor = Theme.Border;
        _attachBtn.Margin    = new Padding(0, 0, 8, 0);
        _attachBtn.Enabled   = false;
        _attachBtn.Click    += (_, _) => PickAttachment();

        _inputBox.Multiline       = false;
        _inputBox.Dock            = DockStyle.Fill;
        _inputBox.Font            = Theme.BodyFont;
        _inputBox.BorderStyle     = BorderStyle.FixedSingle;
        _inputBox.BackColor       = Theme.Background;
        _inputBox.ForeColor       = Theme.TextPrimary;
        _inputBox.PlaceholderText = Loc.T("messages.input.placeholder");
        _inputBox.Enabled         = false;
        _inputBox.KeyDown        += InputBox_KeyDown;

        _sendBtn.Text      = Loc.T("messages.btn.send");
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
        inputBar.Controls.Add(_attachBtn);

        chatArea.Controls.Add(_msgPanel);
        chatArea.Controls.Add(_pendingBar);
        chatArea.Controls.Add(inputBar);
        chatArea.Controls.Add(chatHeader);

        Controls.Add(chatArea);
        Controls.Add(_sidebar);
    }

    private void UpdateUiText()
    {
        _searchBox.PlaceholderText = Loc.T("messages.search.placeholder");
        _inputBox.PlaceholderText  = Loc.T("messages.input.placeholder");
        _sendBtn.Text   = Loc.T("messages.btn.send");
        _deleteBtn.Text = Loc.T("messages.btn.delete");
        if (_active == null)
            _headerLbl.Text = Loc.T("messages.no_conversation");
    }

    private void SetInputEnabled(bool enabled)
    {
        _inputBox.Enabled  = enabled;
        _sendBtn.Enabled   = enabled;
        _attachBtn.Enabled = enabled;
        _deleteBtn.Enabled = enabled;
    }

    // ── Conversation list ─────────────────────────────────────────────────────

    private void LoadConversationList()
    {
        _convList.Controls.Clear();
        var convs = DirectMessageRepository.GetConversations(Session.Current?.Id ?? 0);
        foreach (var c in convs)
        {
            if (!_lastReadDates.ContainsKey(c.Id))
                _lastReadDates[c.Id] = c.LastMessageDate; // first time = treat as read
            _convList.Controls.Add(BuildConvItem(c));
        }
        if (_convList.Controls.Count == 0)
            _convList.Controls.Add(new Label
            {
                Text = Loc.T("messages.no_history"), Font = Theme.SmallFont,
                ForeColor = Theme.TextSecondary, AutoSize = false,
                Width = 230, Height = 36, TextAlign = ContentAlignment.MiddleCenter
            });
    }

    private void PollConversationList()
    {
        if (!string.IsNullOrEmpty(_searchBox.Text)) return;
        LoadConversationList();
    }

    private Panel BuildConvItem(DirectConversationView c)
    {
        bool hasUnread = c.Id != _active?.Id
                         && _lastReadDates.TryGetValue(c.Id, out var readDate)
                         && string.Compare(c.LastMessageDate, readDate, StringComparison.Ordinal) > 0;

        var item = new Panel
        {
            Width = 240, Height = 56, Cursor = Cursors.Hand,
            BackColor = _active?.Id == c.Id ? Color.FromArgb(219, 234, 254) : Theme.Surface,
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
            Text = c.DisplayName,
            Font = hasUnread ? new Font("Segoe UI Semibold", 9.5f) : new Font("Segoe UI", 9.5f),
            ForeColor = Theme.TextPrimary,
            Bounds = new Rectangle(12, 8, 210, 22), AutoEllipsis = true
        };
        var preview = new Label
        {
            Text = c.LastPreview, Font = Theme.SmallFont,
            ForeColor = hasUnread ? Theme.TextPrimary : Theme.TextSecondary,
            Bounds = new Rectangle(12, 30, 175, 18), AutoEllipsis = true
        };
        var dateLbl = new Label
        {
            Text = FormatDate(c.LastMessageDate), Font = Theme.SmallFont,
            ForeColor = Theme.TextSecondary, TextAlign = ContentAlignment.MiddleRight,
            Bounds = new Rectangle(175, 30, 57, 18)
        };
        void OnClick(object? s, EventArgs e) => LoadConversation(c);
        item.Click   += OnClick; nameLbl.Click  += OnClick;
        preview.Click += OnClick; dateLbl.Click += OnClick;
        item.Controls.Add(nameLbl);
        item.Controls.Add(preview);
        item.Controls.Add(dateLbl);
        return item;
    }

    private static string FormatDate(string iso)
    {
        if (!DateTime.TryParse(iso, out var dt)) return iso;
        if (dt.Date == DateTime.Today)             return dt.ToString("HH:mm");
        if (dt.Date == DateTime.Today.AddDays(-1)) return "Yesterday";
        return dt.ToString("dd MMM");
    }

    // ── User search ───────────────────────────────────────────────────────────

    private void OnSearchChanged()
    {
        var term = _searchBox.Text.Trim();
        if (string.IsNullOrEmpty(term)) { LoadConversationList(); return; }
        ShowSearchResults(term);
    }

    private void ShowSearchResults(string term)
    {
        _convList.Controls.Clear();
        var users = UserRepository.Search(term, Session.Current?.Id ?? 0);
        foreach (var u in users) _convList.Controls.Add(BuildUserItem(u));
        if (_convList.Controls.Count == 0)
            _convList.Controls.Add(new Label
            {
                Text = Loc.T("messages.search.no_results"), Font = Theme.SmallFont,
                ForeColor = Theme.TextSecondary, AutoSize = false,
                Width = 230, Height = 36, TextAlign = ContentAlignment.MiddleCenter
            });
    }

    private Panel BuildUserItem(User u)
    {
        var display = string.IsNullOrWhiteSpace(u.FullName) ? u.Username : u.FullName;
        var item = new Panel
        {
            Width = 240, Height = 48, Cursor = Cursors.Hand,
            BackColor = Theme.Surface, Margin = new Padding(0, 0, 0, 2)
        };
        item.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawLine(pen, 8, item.Height - 1, item.Width - 8, item.Height - 1);
        };
        var nameLbl = new Label
        {
            Text = display, Font = new Font("Segoe UI", 9.5f), ForeColor = Theme.TextPrimary,
            Bounds = new Rectangle(12, 8, 216, 20), AutoEllipsis = true
        };
        var roleLbl = new Label
        {
            Text = u.Role, Font = Theme.SmallFont, ForeColor = Theme.TextSecondary,
            Bounds = new Rectangle(12, 28, 216, 14)
        };

        void OnClick(object? s, EventArgs e)
        {
            _searchBox.Text = "";
            var convId = DirectMessageRepository.GetOrCreateConversation(Session.Current?.Id ?? 0, u.Id);
            var convView = new DirectConversationView
            {
                Id = convId, OtherUserId = u.Id,
                OtherUserFullName = u.FullName, OtherUserUsername = u.Username,
                OtherUserRole = u.Role
            };
            LoadConversation(convView);
            LoadConversationList();
        }
        item.Click += OnClick; nameLbl.Click += OnClick; roleLbl.Click += OnClick;
        item.Controls.Add(nameLbl);
        item.Controls.Add(roleLbl);
        return item;
    }

    // ── Load conversation ─────────────────────────────────────────────────────

    private void LoadConversation(DirectConversationView c)
    {
        _active = c;
        _lastReadDates[c.Id] = c.LastMessageDate; // mark as read before rebuilding list
        _headerLbl.Text     = c.DisplayName;
        _headerRoleLbl.Text = c.OtherUserRole;
        SetInputEnabled(true);
        ClearMessages();
        _messages.Clear();
        _messages.AddRange(DirectMessageRepository.GetMessages(c.Id, Session.Current?.Id ?? 0));
        foreach (var m in _messages) AddMessageRow(m);
        ScrollToBottom();
        LoadConversationList(); // refresh sidebar to remove badge
        _inputBox.Focus();
    }

    private void ClearMessages()
    {
        foreach (var r in _bubbleRows) { _msgPanel.Controls.Remove(r); r.Dispose(); }
        _bubbleRows.Clear();
        _msgPanel.AutoScrollMinSize  = Size.Empty;
        _msgPanel.AutoScrollPosition = Point.Empty;
    }

    // ── Attach files ──────────────────────────────────────────────────────────

    private void PickAttachment()
    {
        using var dlg = new OpenFileDialog
        {
            Title     = Loc.T("messages.upload.title"),
            Filter    = Loc.T("messages.upload.filter"),
            Multiselect = true
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        foreach (var f in dlg.FileNames)
            if (!_pendingFiles.Contains(f)) _pendingFiles.Add(f);
        RefreshPendingBar();
    }

    private void RefreshPendingBar()
    {
        _pendingBar.Controls.Clear();
        foreach (var path in _pendingFiles) _pendingBar.Controls.Add(BuildPendingChip(path));
        _pendingBar.Visible = _pendingFiles.Count > 0;
    }

    private Panel BuildPendingChip(string path)
    {
        var chip = new Panel { Height = 28, Width = 170, Margin = new Padding(0, 0, 8, 6) };
        chip.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, chip.Width - 1, chip.Height - 1);
        };
        var lbl = new Label
        {
            Text = Path.GetFileName(path), Font = Theme.SmallFont, ForeColor = Theme.TextPrimary,
            Bounds = new Rectangle(6, 6, 130, 16), AutoEllipsis = true
        };
        var removeBtn = new Button
        {
            Text = "×", Bounds = new Rectangle(147, 3, 20, 22),
            Font = new Font("Segoe UI", 9f), FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent, ForeColor = Theme.TextSecondary, Cursor = Cursors.Hand
        };
        removeBtn.FlatAppearance.BorderSize = 0;
        removeBtn.Click += (_, _) => { _pendingFiles.Remove(path); RefreshPendingBar(); };
        chip.Controls.Add(lbl);
        chip.Controls.Add(removeBtn);
        return chip;
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
        if (_active == null || _sending) return;
        var text  = _inputBox.Text.Trim();
        if (string.IsNullOrEmpty(text) && _pendingFiles.Count == 0) return;

        _sending = true;
        SetInputEnabled(false);
        _inputBox.Text = "";

        var filesToSend = _pendingFiles.ToList();
        _pendingFiles.Clear();
        RefreshPendingBar();

        try
        {
            var msgId = DirectMessageRepository.SendMessage(
                _active.Id, Session.Current?.Id ?? 0, text);

            foreach (var f in filesToSend)
            {
                try { DirectMessageRepository.UploadAttachment(msgId, f); }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format(Loc.T("messages.upload.error"), ex.Message),
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            // Fetch the freshly saved message (includes DB-assigned attachment rows)
            var fresh = DirectMessageRepository.GetMessages(_active.Id, Session.Current?.Id ?? 0)
                            .FirstOrDefault(m => m.Id == msgId);
            if (fresh != null)
            {
                _messages.Add(fresh);
                AddMessageRow(fresh);
            }

            LoadConversationList();
            ScrollToBottom();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _sending = false;
            SetInputEnabled(true);
            _inputBox.Focus();
        }
    }

    // ── Message rendering ─────────────────────────────────────────────────────

    private void AddMessageRow(DirectMessageView msg)
    {
        bool isMe = msg.IsFromMe;
        var row = new Panel { BackColor = Color.Transparent, Tag = isMe };

        if (!isMe)
            row.Controls.Add(new Label
            {
                Text = msg.SenderDisplayName,
                Font = new Font("Segoe UI Semibold", 8f),
                ForeColor = Theme.Primary, AutoSize = true, Location = Point.Empty, Tag = "name"
            });

        if (!string.IsNullOrEmpty(msg.Content))
        {
            var bubble = new Panel { BackColor = isMe ? Theme.Primary : Theme.Surface, Tag = "bubble" };
            bubble.Paint += (_, e) =>
            {
                if (!isMe)
                {
                    using var pen = new Pen(Theme.Border);
                    e.Graphics.DrawRectangle(pen, 0, 0, bubble.Width - 1, bubble.Height - 1);
                }
            };
            var rtb = new RichTextBox
            {
                Text        = msg.Content, ReadOnly    = true,
                BorderStyle = BorderStyle.None,
                BackColor   = isMe ? Theme.Primary : Theme.Surface,
                ForeColor   = isMe ? Color.White   : Theme.TextPrimary,
                Font        = Theme.BodyFont, ScrollBars = RichTextBoxScrollBars.None,
                WordWrap    = true, TabStop = false, DetectUrls = false,
                Cursor      = Cursors.IBeam, Tag = "text"
            };
            bubble.Controls.Add(rtb);
            row.Controls.Add(bubble);
        }

        foreach (var att in msg.Attachments)
            row.Controls.Add(BuildAttachCard(att, isMe));

        int cw = Math.Max(_msgPanel.ClientSize.Width, 200);
        MeasureRow(row, cw);
        row.Location = new Point(0, ContentBottom());
        _msgPanel.Controls.Add(row);
        _bubbleRows.Add(row);
        ExtendScrollRange();
    }

    private Panel BuildAttachCard(DirectMessageAttachment att, bool isMe)
    {
        var card = new Panel
        {
            BackColor = isMe ? Color.FromArgb(29, 78, 216) : Theme.Surface,
            Tag       = "attach"
        };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(isMe ? Color.FromArgb(147, 197, 253) : Theme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };
        card.Controls.Add(new Label
        {
            Text = "📄", Font = new Font("Segoe UI", 11f), AutoSize = false, Tag = "icon"
        });
        card.Controls.Add(new Label
        {
            Text = att.FileName, Font = Theme.SmallFont,
            ForeColor = isMe ? Color.White : Theme.TextPrimary,
            AutoEllipsis = true, Tag = "filename"
        });
        card.Controls.Add(new Label
        {
            Text = DirectMessageRepository.FormatSize(att.FileSize), Font = Theme.SmallFont,
            ForeColor = isMe ? Color.FromArgb(186, 230, 253) : Theme.TextSecondary,
            Tag = "size"
        });
        var dlBtn = new Button
        {
            Text = "⬇", FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = isMe ? Color.White : Theme.Primary,
            Cursor = Cursors.Hand, Font = new Font("Segoe UI", 10f), Tag = "dl"
        };
        dlBtn.FlatAppearance.BorderSize = 0;
        dlBtn.FlatAppearance.MouseOverBackColor = Color.Transparent;
        dlBtn.Click += (_, _) => DownloadAttachment(att);
        card.Controls.Add(dlBtn);
        return card;
    }

    private void DownloadAttachment(DirectMessageAttachment att)
    {
        using var dlg = new SaveFileDialog
        {
            Title    = Loc.T("messages.download.title"),
            FileName = att.FileName,
            Filter   = "All files (*.*)|*.*"
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;
        try { DirectMessageRepository.DownloadAttachment(att.Id, dlg.FileName); }
        catch (Exception ex)
        {
            MessageBox.Show(string.Format(Loc.T("messages.download.error"), ex.Message),
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ── Bubble layout & sizing ────────────────────────────────────────────────

    private void MeasureRow(Panel row, int cw)
    {
        bool isMe       = (bool)(row.Tag ?? false);
        const int Pad   = 16;
        const int PadH  = 12;
        const int PadV  = 10;
        const int AttH  = 44;
        const int Gap   = 6;
        int maxW        = (int)(cw * 0.72);

        var nameLbl = row.Controls.OfType<Label>().FirstOrDefault(l => (string?)l.Tag == "name");
        var bubble  = row.Controls.OfType<Panel>().FirstOrDefault(p => (string?)p.Tag == "bubble");
        var rtb     = bubble?.Controls.OfType<RichTextBox>().FirstOrDefault();
        var attaches = row.Controls.OfType<Panel>().Where(p => (string?)p.Tag == "attach").ToList();

        int nameH = nameLbl != null ? 20 : 0;
        int y     = nameH;

        if (nameLbl != null)
        {
            nameLbl.Location = new Point(Pad, 0);
            nameLbl.Size     = nameLbl.PreferredSize;
        }

        if (bubble != null && rtb != null)
        {
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
            bubble.Location = new Point(isMe ? cw - bW - Pad : Pad, y);
            y += bH + Gap;
        }

        foreach (var card in attaches)
        {
            int cardW = Math.Min(maxW, cw - 2 * Pad);
            SizeAttachCard(card, cardW, AttH);
            card.Size     = new Size(cardW, AttH);
            card.Location = new Point(isMe ? cw - cardW - Pad : Pad, y);
            y += AttH + Gap;
        }

        row.Size = new Size(cw, Math.Max(y + 4, nameH + 8));
    }

    private static void SizeAttachCard(Panel card, int cardW, int cardH)
    {
        const int IconW  = 26;
        const int DlBtnW = 30;
        const int LeftM  = 8;
        const int RightM = 6;

        int nameW = cardW - IconW - LeftM * 2 - DlBtnW - RightM;

        var icon    = card.Controls.OfType<Label>().FirstOrDefault(l => (string?)l.Tag == "icon");
        var nameLbl = card.Controls.OfType<Label>().FirstOrDefault(l => (string?)l.Tag == "filename");
        var sizeLbl = card.Controls.OfType<Label>().FirstOrDefault(l => (string?)l.Tag == "size");
        var dlBtn   = card.Controls.OfType<Button>().FirstOrDefault();

        if (icon    != null) icon.Bounds    = new Rectangle(LeftM, (cardH - 20) / 2, 20, 20);
        if (nameLbl != null) nameLbl.Bounds = new Rectangle(LeftM + IconW, 8,  nameW, 15);
        if (sizeLbl != null) sizeLbl.Bounds = new Rectangle(LeftM + IconW, 24, nameW, 13);
        if (dlBtn   != null) dlBtn.Bounds   = new Rectangle(cardW - DlBtnW - RightM, (cardH - 28) / 2, DlBtnW, 28);
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

    // ── Delete conversation ───────────────────────────────────────────────────

    private void DeleteActiveConversation()
    {
        if (_active == null) return;
        if (MessageBox.Show(Loc.T("messages.delete.confirm"), Loc.T("messages.delete.title"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        DirectMessageRepository.DeleteConversation(_active.Id);
        _active = null;
        _headerLbl.Text     = Loc.T("messages.no_conversation");
        _headerRoleLbl.Text = "";
        SetInputEnabled(false);
        ClearMessages();
        _messages.Clear();
        LoadConversationList();
    }
}
