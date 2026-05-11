using AfterSchool.Data;
using AfterSchool.Services;
using AfterSchool.UI;
using ClosedXML.Excel;
using Microsoft.Data.Sqlite;

namespace AfterSchool.Forms;

public class AdminControl : UserControl
{
    private DataGridView _grid = null!;
    private ComboBox     _actionFilter = null!;
    private TextBox      _userSearch   = null!;
    private DateTimePicker _dateFrom   = null!;
    private DateTimePicker _dateTo     = null!;
    private Label        _statusLabel  = null!;

    public AdminControl()
    {
        BackColor = Theme.Background;
        BuildLayout();
        LoadGrid();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 1,
            RowCount    = 2,
            BackColor   = Theme.Background,
            Padding     = Padding.Empty,
            Margin      = Padding.Empty
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 130));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        root.Controls.Add(BuildBackupCard(), 0, 0);
        root.Controls.Add(BuildAuditCard(),  0, 1);
        Controls.Add(root);
    }

    // ── Backup & Restore card ─────────────────────────────────────────────────

    private Panel BuildBackupCard()
    {
        var card = CreateCard(margin: new Padding(0, 0, 0, 12));

        var title = new Label
        {
            Text      = Loc.T("admin.backup.title"),
            Font      = new Font("Segoe UI Semibold", 13f),
            ForeColor = Theme.TextPrimary,
            Dock      = DockStyle.Top,
            Height    = 32,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var btnRow = new FlowLayoutPanel
        {
            Dock           = DockStyle.Top,
            Height         = 44,
            FlowDirection  = FlowDirection.LeftToRight,
            WrapContents   = false,
            BackColor      = Color.Transparent,
            Padding        = new Padding(0, 4, 0, 0)
        };

        var backupBtn = new Button { Text = Loc.T("admin.backup.btn_create") };
        Theme.StyleButton(backupBtn, primary: true);
        backupBtn.Margin = new Padding(0, 0, 10, 0);
        backupBtn.Click += (_, _) => CreateBackup();

        var restoreBtn = new Button { Text = Loc.T("admin.backup.btn_restore") };
        Theme.StyleButton(restoreBtn);
        restoreBtn.Click += (_, _) => RestoreBackup();

        btnRow.Controls.Add(backupBtn);
        btnRow.Controls.Add(restoreBtn);

        _statusLabel = new Label
        {
            Text      = "",
            Font      = Theme.SmallFont,
            ForeColor = Theme.TextSecondary,
            Dock      = DockStyle.Top,
            Height    = 22,
            TextAlign = ContentAlignment.MiddleLeft
        };

        card.Controls.Add(_statusLabel);
        card.Controls.Add(btnRow);
        card.Controls.Add(title);
        return card;
    }

    // ── Audit log card ────────────────────────────────────────────────────────

    private Panel BuildAuditCard()
    {
        var card = CreateCard(margin: Padding.Empty);

        // Header row: title + buttons
        var header = new TableLayoutPanel
        {
            Dock        = DockStyle.Top,
            Height      = 44,
            ColumnCount = 3,
            BackColor   = Color.Transparent,
            Padding     = Padding.Empty,
            Margin      = Padding.Empty
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var titleLbl = new Label
        {
            Text      = Loc.T("admin.audit.title"),
            Font      = new Font("Segoe UI Semibold", 13f),
            ForeColor = Theme.TextPrimary,
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var exportBtn = new Button { Text = Loc.T("common.export"), Margin = new Padding(0, 6, 8, 6) };
        Theme.StyleButton(exportBtn);
        exportBtn.Click += (_, _) => ExportAuditLog();

        var refreshBtn = new Button { Text = Loc.T("common.refresh"), Margin = new Padding(0, 6, 0, 6) };
        Theme.StyleButton(refreshBtn, primary: true);
        refreshBtn.Click += (_, _) => LoadGrid();

        header.Controls.Add(titleLbl,   0, 0);
        header.Controls.Add(exportBtn,  1, 0);
        header.Controls.Add(refreshBtn, 2, 0);

        // Filter bar
        var filterBar = BuildFilterBar();

        // Grid
        _grid = new DataGridView { Dock = DockStyle.Fill };
        Theme.StyleGrid(_grid);
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = Loc.T("admin.audit.col.time"),    Name = "Time",       FillWeight = 16 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = Loc.T("admin.audit.col.user"),    Name = "User",       FillWeight = 14 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = Loc.T("admin.audit.col.action"),  Name = "Action",     FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = Loc.T("admin.audit.col.entity"),  Name = "EntityType", FillWeight = 14 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = Loc.T("admin.audit.col.id"),      Name = "EntityId",   FillWeight = 8  });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = Loc.T("admin.audit.col.details"), Name = "Details",    FillWeight = 36 });

        card.Controls.Add(_grid);
        card.Controls.Add(filterBar);
        card.Controls.Add(header);
        return card;
    }

    private Panel BuildFilterBar()
    {
        var bar = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 46,
            BackColor = Color.Transparent
        };

        var fromLbl = new Label { Text = Loc.T("admin.audit.filter.from"), Left = 0, Top = 14, AutoSize = true, ForeColor = Theme.TextSecondary, Font = Theme.SmallFont };

        _dateFrom = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Value  = DateTime.Today.AddDays(-30),
            Width  = 110,
            Left   = fromLbl.Left + 34,
            Top    = 9
        };
        _dateFrom.ValueChanged += (_, _) => LoadGrid();

        var toLbl = new Label { Text = Loc.T("admin.audit.filter.to"), Left = _dateFrom.Left + 118, Top = 14, AutoSize = true, ForeColor = Theme.TextSecondary, Font = Theme.SmallFont };

        _dateTo = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Value  = DateTime.Today.AddDays(1),
            Width  = 110,
            Left   = toLbl.Left + 22,
            Top    = 9
        };
        _dateTo.ValueChanged += (_, _) => LoadGrid();

        _actionFilter = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width         = 130,
            Left          = _dateTo.Left + 118,
            Top           = 9
        };
        _actionFilter.Items.Add(Loc.T("admin.audit.filter.all_actions"));
        _actionFilter.Items.AddRange(new[] { "Create", "Update", "Delete", "Login", "Backup", "Restore" });
        _actionFilter.SelectedIndex = 0;
        _actionFilter.SelectedIndexChanged += (_, _) => LoadGrid();

        _userSearch = new TextBox
        {
            PlaceholderText = Loc.T("admin.audit.filter.user"),
            Width           = 160,
            Left            = _actionFilter.Left + 138,
            Top             = 9
        };
        Theme.StyleTextBox(_userSearch);
        _userSearch.TextChanged += (_, _) => LoadGrid();

        bar.Controls.AddRange(new Control[] { fromLbl, _dateFrom, toLbl, _dateTo, _actionFilter, _userSearch });
        return bar;
    }

    // ── Data loading ──────────────────────────────────────────────────────────

    private void LoadGrid()
    {
        var filterAction = _actionFilter?.SelectedIndex > 0
            ? _actionFilter.SelectedItem?.ToString()
            : null;
        var filterUser = string.IsNullOrWhiteSpace(_userSearch?.Text) ? null : _userSearch.Text.Trim();
        var from = _dateFrom?.Value ?? DateTime.Today.AddDays(-30);
        var to   = _dateTo?.Value   ?? DateTime.Today.AddDays(1);

        var entries = AuditLogRepository.GetFiltered(filterAction, filterUser, from, to);

        _grid.Rows.Clear();
        foreach (var e in entries)
        {
            var ts = DateTime.TryParse(e.Timestamp, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt)
                ? dt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
                : e.Timestamp;
            _grid.Rows.Add(ts, e.UserName, e.Action, e.EntityType, e.EntityId, e.Details);
        }
    }

    // ── Backup ────────────────────────────────────────────────────────────────

    private void CreateBackup()
    {
        var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        if (!Directory.Exists(downloads))
            downloads = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        var ts         = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
        var backupPath = Path.Combine(downloads, $"afterschool_backup_{ts}.db").Replace("'", "''");

        try
        {
            using var conn = DatabaseHelper.CreateConnection();
            using var cmd  = conn.CreateCommand();
            cmd.CommandText = $"VACUUM INTO '{backupPath}';";
            cmd.ExecuteNonQuery();

            AuditService.Log("Backup", "Database", "", $"Saved to: {backupPath}");
            _statusLabel.Text      = string.Format(Loc.T("admin.backup.status_ok"), Path.GetFileName(backupPath));
            _statusLabel.ForeColor = Theme.Success;
            LoadGrid();

            MessageBox.Show(
                string.Format(Loc.T("admin.backup.success_msg"), backupPath),
                Loc.T("admin.backup.title"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            _statusLabel.Text      = Loc.T("admin.backup.status_err");
            _statusLabel.ForeColor = Theme.Danger;
            MessageBox.Show(ex.Message, Loc.T("admin.backup.err_title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ── Restore ───────────────────────────────────────────────────────────────

    private void RestoreBackup()
    {
        using var dlg = new OpenFileDialog
        {
            Title     = Loc.T("admin.restore.dialog_title"),
            Filter    = "SQLite Database (*.db)|*.db|All files (*.*)|*.*",
            Multiselect = false
        };
        if (dlg.ShowDialog() != DialogResult.OK) return;

        var result = MessageBox.Show(
            Loc.T("admin.restore.confirm_msg"),
            Loc.T("admin.restore.confirm_title"),
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes) return;

        try
        {
            // Validate that the chosen file is a SQLite database
            using (var testConn = new SqliteConnection($"Data Source={dlg.FileName}"))
            {
                testConn.Open();
                using var check = testConn.CreateCommand();
                check.CommandText = "SELECT COUNT(*) FROM sqlite_master;";
                check.ExecuteScalar();
            }

            File.Copy(dlg.FileName, DatabaseHelper.DatabasePath, overwrite: true);
            AuditService.Log("Restore", "Database", "", $"Restored from: {dlg.FileName}");

            MessageBox.Show(
                Loc.T("admin.restore.success_msg"),
                Loc.T("admin.restore.title"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            LoadGrid();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, Loc.T("admin.restore.err_title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ── Export ────────────────────────────────────────────────────────────────

    private void ExportAuditLog()
    {
        var filterAction = _actionFilter?.SelectedIndex > 0 ? _actionFilter.SelectedItem?.ToString() : null;
        var filterUser   = string.IsNullOrWhiteSpace(_userSearch?.Text) ? null : _userSearch.Text.Trim();
        var from = _dateFrom?.Value ?? DateTime.Today.AddDays(-30);
        var to   = _dateTo?.Value   ?? DateTime.Today.AddDays(1);

        var entries = AuditLogRepository.GetFiltered(filterAction, filterUser, from, to);

        var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        if (!Directory.Exists(downloads))
            downloads = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var path = Path.Combine(downloads, $"audit_log_{DateTime.Now:yyyy-MM-dd_HHmmss}.xlsx");

        try
        {
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Audit Log");

            ws.Cell(1, 1).Value = Loc.T("admin.audit.col.time");
            ws.Cell(1, 2).Value = Loc.T("admin.audit.col.user");
            ws.Cell(1, 3).Value = Loc.T("admin.audit.col.action");
            ws.Cell(1, 4).Value = Loc.T("admin.audit.col.entity");
            ws.Cell(1, 5).Value = Loc.T("admin.audit.col.id");
            ws.Cell(1, 6).Value = Loc.T("admin.audit.col.details");
            ws.Row(1).Style.Font.Bold = true;

            int row = 2;
            foreach (var e in entries)
            {
                var ts = DateTime.TryParse(e.Timestamp, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt)
                    ? dt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") : e.Timestamp;
                ws.Cell(row, 1).Value = ts;
                ws.Cell(row, 2).Value = e.UserName;
                ws.Cell(row, 3).Value = e.Action;
                ws.Cell(row, 4).Value = e.EntityType;
                ws.Cell(row, 5).Value = e.EntityId;
                ws.Cell(row, 6).Value = e.Details;
                row++;
            }
            ws.Columns().AdjustToContents();
            wb.SaveAs(path);

            MessageBox.Show(
                string.Format(Loc.T("admin.audit.export_success"), path),
                Loc.T("common.export"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Panel CreateCard(Padding margin)
    {
        var card = new Panel
        {
            BackColor = Theme.Surface,
            Dock      = DockStyle.Fill,
            Margin    = margin,
            Padding   = new Padding(20, 14, 20, 14)
        };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };
        return card;
    }
}
