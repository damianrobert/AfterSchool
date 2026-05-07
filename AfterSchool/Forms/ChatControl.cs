using AfterSchool.Data;
using AfterSchool.Models;
using AfterSchool.Services;
using AfterSchool.UI;

namespace AfterSchool.Forms;

public class ChatControl : UserControl
{
    // ── Layout ────────────────────────────────────────────────────────────────
    private readonly Panel              _sidebar        = new();
    private readonly FlowLayoutPanel    _convList       = new();
    private readonly Panel              _messagesScroll = new();
    private readonly Panel              _messagesInner  = new();
    private readonly TextBox            _inputBox       = new();
    private readonly Button             _sendBtn        = new();
    private readonly Label              _titleLbl       = new();
    private readonly Label              _typingLbl      = new();

    // ── State ─────────────────────────────────────────────────────────────────
    private ChatConversation? _active;
    private readonly List<ChatMessage> _messages = new();
    private bool _sending;
    private Panel? _typingBubble;

    public ChatControl()
    {
        BackColor = Theme.Background;
        BuildLayout();
        LoadConversationList();
        Loc.LanguageChanged += () => { LoadConversationList(); UpdateUiText(); };
    }

    // ── Layout builders ───────────────────────────────────────────────────────

    private void BuildLayout()
    {
        // ── Left sidebar (conversation list) ─────────────────────────────────
        _sidebar.Dock      = DockStyle.Left;
        _sidebar.Width     = 260;
        _sidebar.BackColor = Theme.Surface;
        _sidebar.Padding   = new Padding(0);

        var sidebarHeader = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 60,
            BackColor = Theme.Surface,
            Padding   = new Padding(16, 0, 16, 0)
        };
        sidebarHeader.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawLine(pen, 0, sidebarHeader.Height - 1, sidebarHeader.Width, sidebarHeader.Height - 1);
        };

        var miloLabel = new Label
        {
            Text      = "Milo",
            Font      = new Font("Segoe UI Semibold", 13f),
            ForeColor = Theme.TextPrimary,
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var newBtn = new Button
        {
            Text   = "+",
            Width  = 32,
            Height = 32,
            Dock   = DockStyle.Right,
            Font   = new Font("Segoe UI", 14f),
            Cursor = Cursors.Hand,
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.Primary,
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter
        };
        newBtn.FlatAppearance.BorderSize = 0;
        newBtn.FlatAppearance.MouseOverBackColor = Theme.PrimaryHover;
        newBtn.Margin = new Padding(0, 14, 0, 14);
        newBtn.Click += (_, _) => StartNewConversation();

        sidebarHeader.Controls.Add(miloLabel);
        sidebarHeader.Controls.Add(newBtn);

        _convList.Dock          = DockStyle.Fill;
        _convList.FlowDirection = FlowDirection.TopDown;
        _convList.WrapContents  = false;
        _convList.AutoScroll    = true;
        _convList.Padding       = new Padding(8, 8, 8, 8);
        _convList.BackColor     = Theme.Surface;

        var sidebarBorder = new Panel
        {
            Dock      = DockStyle.Right,
            Width     = 1,
            BackColor = Theme.Border
        };

        _sidebar.Controls.Add(_convList);
        _sidebar.Controls.Add(sidebarHeader);
        _sidebar.Controls.Add(sidebarBorder);

        // ── Right: chat area ──────────────────────────────────────────────────
        var chatArea = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background };

        // Top header
        var chatHeader = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 56,
            BackColor = Theme.Surface,
            Padding   = new Padding(20, 0, 16, 0)
        };
        chatHeader.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawLine(pen, 0, chatHeader.Height - 1, chatHeader.Width, chatHeader.Height - 1);
        };

        _titleLbl.Font      = new Font("Segoe UI Semibold", 11f);
        _titleLbl.ForeColor = Theme.TextPrimary;
        _titleLbl.Dock      = DockStyle.Fill;
        _titleLbl.TextAlign = ContentAlignment.MiddleLeft;
        _titleLbl.Text      = Loc.T("chat.no_conversation");

        var deleteBtn = new Button
        {
            Text      = Loc.T("chat.btn.delete"),
            Width     = 80,
            Dock      = DockStyle.Right,
            FlatStyle = FlatStyle.Flat,
            Cursor    = Cursors.Hand,
            Font      = Theme.SmallFont,
            BackColor = Theme.Surface,
            ForeColor = Theme.TextSecondary
        };
        deleteBtn.FlatAppearance.BorderSize = 0;
        deleteBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(254, 242, 242);
        deleteBtn.Click += (_, _) => DeleteActiveConversation();

        chatHeader.Controls.Add(_titleLbl);
        chatHeader.Controls.Add(deleteBtn);

        // Messages area
        _messagesScroll.Dock        = DockStyle.Fill;
        _messagesScroll.AutoScroll  = true;
        _messagesScroll.BackColor   = Color.FromArgb(248, 250, 252);
        _messagesScroll.Padding     = new Padding(0);
        _messagesScroll.SizeChanged += (_, _) => RelayoutMessages();

        _messagesInner.AutoSize     = false;
        _messagesInner.BackColor    = Color.Transparent;
        _messagesInner.Location     = new Point(0, 0);
        _messagesScroll.Controls.Add(_messagesInner);

        // Input bar
        var inputBar = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 72,
            BackColor = Theme.Surface,
            Padding   = new Padding(16, 12, 16, 12)
        };
        inputBar.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawLine(pen, 0, 0, inputBar.Width, 0);
        };

        _inputBox.Multiline    = false;
        _inputBox.Dock         = DockStyle.Fill;
        _inputBox.Font         = Theme.BodyFont;
        _inputBox.BorderStyle  = BorderStyle.FixedSingle;
        _inputBox.BackColor    = Theme.Background;
        _inputBox.ForeColor    = Theme.TextPrimary;
        _inputBox.PlaceholderText = Loc.T("chat.input.placeholder");
        _inputBox.KeyDown     += InputBox_KeyDown;

        _sendBtn.Text      = Loc.T("chat.btn.send");
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
        _sendBtn.Click    += (_, _) => _ = SendMessageAsync();

        inputBar.Controls.Add(_inputBox);
        inputBar.Controls.Add(_sendBtn);

        // Typing indicator (bottom of messages area, above input)
        _typingLbl.Dock      = DockStyle.Bottom;
        _typingLbl.Height    = 20;
        _typingLbl.Font      = new Font("Segoe UI", 8.5f, FontStyle.Italic);
        _typingLbl.ForeColor = Theme.TextSecondary;
        _typingLbl.Text      = "";
        _typingLbl.Padding   = new Padding(20, 0, 0, 0);
        _typingLbl.BackColor = Theme.Background;

        chatArea.Controls.Add(_messagesScroll);
        chatArea.Controls.Add(_typingLbl);
        chatArea.Controls.Add(chatHeader);

        Controls.Add(chatArea);
        Controls.Add(_sidebar);

        // bottom input bar added to chatArea AFTER fill panel
        chatArea.Controls.Add(inputBar);
        chatArea.Controls.SetChildIndex(inputBar, 0);
        chatArea.Controls.SetChildIndex(_messagesScroll, 1);
        chatArea.Controls.SetChildIndex(_typingLbl, 2);
        chatArea.Controls.SetChildIndex(chatHeader, 3);
    }

    private void UpdateUiText()
    {
        _titleLbl.Text = _active?.Title ?? Loc.T("chat.no_conversation");
        _inputBox.PlaceholderText = Loc.T("chat.input.placeholder");
        _sendBtn.Text = Loc.T("chat.btn.send");
    }

    // ── Conversation list ─────────────────────────────────────────────────────

    private void LoadConversationList()
    {
        _convList.Controls.Clear();
        var userId = UI.Session.Current?.Id ?? 0;
        var convs  = ChatRepository.GetForUser(userId);

        foreach (var c in convs)
            _convList.Controls.Add(BuildConvItem(c));

        if (!_convList.Controls.Contains(BuildEmptyHint()))
        {
            if (_convList.Controls.Count == 0)
            {
                var hint = new Label
                {
                    Text      = Loc.T("chat.no_history"),
                    Font      = Theme.SmallFont,
                    ForeColor = Theme.TextSecondary,
                    AutoSize  = false,
                    Width     = 230,
                    Height    = 36,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Tag       = "empty_hint"
                };
                _convList.Controls.Add(hint);
            }
        }
    }

    private static Label BuildEmptyHint() => new() { Tag = "empty_hint" };

    private Panel BuildConvItem(ChatConversation c)
    {
        var item = new Panel
        {
            Width     = 240,
            Height    = 56,
            Cursor    = Cursors.Hand,
            BackColor = _active?.Id == c.Id ? Color.FromArgb(219, 234, 254) : Theme.Surface,
            Margin    = new Padding(0, 0, 0, 2),
            Tag       = c
        };
        item.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(226, 232, 240));
            e.Graphics.DrawLine(pen, 8, item.Height - 1, item.Width - 8, item.Height - 1);
        };

        var title = new Label
        {
            Text      = c.Title,
            Font      = new Font("Segoe UI", 9.5f),
            ForeColor = Theme.TextPrimary,
            Bounds    = new Rectangle(12, 8, 216, 22),
            AutoEllipsis = true
        };
        var date = new Label
        {
            Text      = FormatDate(c.LastMessageDate),
            Font      = Theme.SmallFont,
            ForeColor = Theme.TextSecondary,
            Bounds    = new Rectangle(12, 30, 216, 18)
        };

        item.Controls.Add(title);
        item.Controls.Add(date);
        item.Click       += (_, _) => LoadConversation(c);
        title.Click      += (_, _) => LoadConversation(c);
        date.Click       += (_, _) => LoadConversation(c);

        return item;
    }

    private static string FormatDate(string iso)
    {
        if (DateTime.TryParse(iso, out var dt))
        {
            if (dt.Date == DateTime.Today)           return dt.ToString("HH:mm");
            if (dt.Date == DateTime.Today.AddDays(-1)) return "Yesterday";
            return dt.ToString("dd MMM");
        }
        return iso;
    }

    // ── Load / create conversation ────────────────────────────────────────────

    private void LoadConversation(ChatConversation c)
    {
        _active = c;
        _titleLbl.Text = c.Title;
        _messages.Clear();
        _bubbleRows.Clear();
        _messagesInner.Controls.Clear();

        var dbMessages = ChatRepository.GetMessages(c.Id).ToList();
        _messages.AddRange(dbMessages);

        foreach (var m in _messages)
            AddBubble(m.Content, m.Role == "user");

        ScrollToBottom();
        LoadConversationList(); // refresh selection highlight
        _inputBox.Focus();
    }

    private void StartNewConversation()
    {
        _active = null;
        _messages.Clear();
        _bubbleRows.Clear();
        _messagesInner.Controls.Clear();
        _titleLbl.Text = Loc.T("chat.no_conversation");
        LoadConversationList();
        _inputBox.Focus();
    }

    // ── Send message ──────────────────────────────────────────────────────────

    private void InputBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter && !e.Shift)
        {
            e.SuppressKeyPress = true;
            _ = SendMessageAsync();
        }
    }

    private async Task SendMessageAsync()
    {
        var text = _inputBox.Text.Trim();
        if (string.IsNullOrEmpty(text) || _sending) return;

        _sending = true;
        _sendBtn.Enabled = false;
        _inputBox.Text = "";

        // Create conversation on first message
        if (_active == null)
        {
            var title = text.Length > 50 ? text[..47] + "…" : text;
            var now = DateTime.UtcNow.ToString("O");
            var conv = new ChatConversation
            {
                UserId          = UI.Session.Current?.Id ?? 0,
                Title           = title,
                CreatedDate     = now,
                LastMessageDate = now
            };
            conv.Id = ChatRepository.InsertConversation(conv);
            _active = conv;
            _titleLbl.Text = conv.Title;
        }

        // Save and display user message
        var userMsg = new ChatMessage
        {
            ConversationId = _active.Id,
            Role           = "user",
            Content        = text,
            SentDate       = DateTime.UtcNow.ToString("O")
        };
        ChatRepository.InsertMessage(userMsg);
        _messages.Add(userMsg);
        AddBubble(text, isUser: true);

        // Show typing indicator
        ShowTypingIndicator();
        ScrollToBottom();

        try
        {
            var user    = UI.Session.Current;
            var context = await Task.Run(MiloService.BuildSchoolContext);
            var reply   = await MiloService.SendAsync(
                _messages.SkipLast(1).ToList(), // history without the just-sent message
                text,
                user?.FullName ?? user?.Username ?? "User",
                user?.Role ?? "Staff",
                context);

            HideTypingIndicator();

            var modelMsg = new ChatMessage
            {
                ConversationId = _active.Id,
                Role           = "model",
                Content        = reply,
                SentDate       = DateTime.UtcNow.ToString("O")
            };
            ChatRepository.InsertMessage(modelMsg);
            _messages.Add(modelMsg);
            AddBubble(reply, isUser: false);

            ChatRepository.TouchConversation(_active.Id, DateTime.UtcNow.ToString("O"));
            LoadConversationList();
        }
        catch (Exception ex)
        {
            HideTypingIndicator();
            AddBubble($"Error: {ex.Message}", isUser: false);
        }
        finally
        {
            _sending = false;
            _sendBtn.Enabled = true;
            ScrollToBottom();
            _inputBox.Focus();
        }
    }

    // ── Message bubbles ───────────────────────────────────────────────────────

    private readonly List<Panel> _bubbleRows = new();

    private void AddBubble(string content, bool isUser)
    {
        var row = new Panel { BackColor = Color.Transparent, AutoSize = false, Tag = isUser };

        if (!isUser)
        {
            row.Controls.Add(new Label
            {
                Text      = "Milo",
                Font      = new Font("Segoe UI Semibold", 8f),
                ForeColor = Theme.Primary,
                AutoSize  = true,
                Location  = new Point(0, 0),
                Tag       = "name"
            });
        }

        var bubble = new Panel { BackColor = isUser ? Theme.Primary : Theme.Surface, AutoSize = false, Tag = "bubble" };
        bubble.Paint += (_, e) =>
        {
            if (!isUser)
            {
                using var pen = new Pen(Theme.Border);
                e.Graphics.DrawRectangle(pen, 0, 0, bubble.Width - 1, bubble.Height - 1);
            }
        };

        // RichTextBox: read-only, no border, no scrollbar — user can select and copy text
        var rtb = new RichTextBox
        {
            Text        = content,
            ReadOnly    = true,
            BorderStyle = BorderStyle.None,
            BackColor   = isUser ? Theme.Primary : Theme.Surface,
            ForeColor   = isUser ? Color.White : Theme.TextPrimary,
            Font        = Theme.BodyFont,
            ScrollBars  = RichTextBoxScrollBars.None,
            WordWrap    = true,
            TabStop     = false,
            DetectUrls  = false,
            Cursor      = Cursors.IBeam,
            Tag         = "text"
        };

        bubble.Controls.Add(rtb);
        row.Controls.Add(bubble);
        _messagesInner.Controls.Add(row);
        _bubbleRows.Add(row);

        int cw = _messagesScroll.ClientSize.Width;
        PositionRow(row, cw);
        // Place row below all previous rows
        row.Location = new Point(0, _bubbleRows.SkipLast(1).Sum(r => r.Height + 4) + 8);
        _messagesInner.Size = new Size(Math.Max(_messagesInner.Width, cw), row.Bottom + 8);
    }

    private void PositionRow(Panel row, int containerWidth)
    {
        if (containerWidth < 100) containerWidth = 600;
        bool isUser   = (bool)(row.Tag ?? false);
        const int Pad = 16;
        const int PadH = 12; // bubble horizontal padding each side
        const int PadV = 10; // bubble vertical padding each side
        int maxBubble = (int)(containerWidth * 0.72);
        int maxTextW  = maxBubble - PadH * 2;

        var bubble = row.Controls.OfType<Panel>().FirstOrDefault(p => (string?)p.Tag == "bubble");
        var rtb    = bubble?.Controls.OfType<RichTextBox>().FirstOrDefault();
        var name   = row.Controls.OfType<Label>().FirstOrDefault(l => (string?)l.Tag == "name");

        if (rtb == null || bubble == null) return;

        // Measure text using TextRenderer — accurate pre-render, no handle required
        var measured = TextRenderer.MeasureText(
            rtb.Text.Length > 0 ? rtb.Text : " ",
            rtb.Font,
            new Size(maxTextW, int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);

        int textW  = Math.Min(measured.Width, maxTextW);
        // Add one extra line height as buffer for RichTextBox's internal padding
        int textH  = measured.Height + (int)rtb.Font.GetHeight();
        int bubbleW = Math.Max(textW + PadH * 2, 64);
        int nameH   = name != null ? 20 : 0;
        int bubbleH = textH + PadV * 2;
        int rowH    = nameH + bubbleH + 8;

        int bubbleX = isUser ? containerWidth - bubbleW - Pad : Pad;

        if (name != null)
        {
            name.Location = new Point(Pad, 0);
            name.Size     = name.PreferredSize;
        }

        bubble.Location = new Point(bubbleX, nameH);
        bubble.Size     = new Size(bubbleW, bubbleH);

        rtb.Location = new Point(PadH, PadV);
        rtb.Size     = new Size(textW, textH);

        row.Size = new Size(containerWidth, rowH);
    }

    private void RelayoutMessages()
    {
        int w = _messagesScroll.ClientSize.Width;
        int y = 8;
        foreach (var row in _bubbleRows)
        {
            PositionRow(row, w);
            row.Location = new Point(0, y);
            y += row.Height + 4;
        }
        _messagesInner.Size = new Size(w, y + 8);
    }

    private void ScrollToBottom()
    {
        _messagesScroll.AutoScrollPosition = new Point(0, _messagesInner.Height + 1000);
    }

    private void ShowTypingIndicator()
    {
        _typingLbl.Text = Loc.T("chat.typing");

        _typingBubble = new Panel
        {
            BackColor = Theme.Surface,
            Size      = new Size(80, 38),
            Tag       = false
        };
        _typingBubble.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, _typingBubble.Width - 1, _typingBubble.Height - 1);
        };

        var dots = new Label
        {
            Text      = "● ● ●",
            ForeColor = Theme.TextSecondary,
            Font      = new Font("Segoe UI", 8f),
            AutoSize  = true,
            Location  = new Point(12, 10)
        };
        _typingBubble.Controls.Add(dots);

        var row = new Panel
        {
            BackColor = Color.Transparent,
            Size      = new Size(_messagesScroll.ClientSize.Width, 46),
            Location  = new Point(0, _bubbleRows.Sum(r => r.Height + 4) + 8),
            Tag       = false
        };
        row.Controls.Add(_typingBubble);
        _typingBubble.Location = new Point(16, 0);
        _messagesInner.Controls.Add(row);
        _messagesInner.Size = new Size(_messagesScroll.ClientSize.Width, row.Bottom + 8);
    }

    private void HideTypingIndicator()
    {
        _typingLbl.Text = "";
        foreach (var c in _messagesInner.Controls.OfType<Panel>()
            .Where(p => p.Tag is bool b && !(bool)p.Tag && !_bubbleRows.Contains(p))
            .ToList())
        {
            _messagesInner.Controls.Remove(c);
            c.Dispose();
        }
    }

    // ── Delete conversation ───────────────────────────────────────────────────

    private void DeleteActiveConversation()
    {
        if (_active == null) return;
        var result = MessageBox.Show(
            Loc.T("chat.delete.confirm"),
            Loc.T("chat.delete.title"),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (result != DialogResult.Yes) return;

        ChatRepository.DeleteConversation(_active.Id);
        StartNewConversation();
    }
}
