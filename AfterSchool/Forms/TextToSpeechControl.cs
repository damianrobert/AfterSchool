using System.Speech.Synthesis;
using AfterSchool.Services;
using AfterSchool.UI;

namespace AfterSchool.Forms;

public class TextToSpeechControl : UserControl
{
    private readonly SpeechSynthesizer _synth = new();

    private readonly TextBox _textBox = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, AcceptsReturn = true };
    private readonly Button _playBtn = new();
    private readonly Button _pauseBtn = new();
    private readonly Button _resumeBtn = new();
    private readonly Button _stopBtn = new();
    private readonly Button _loadBtn = new();
    private readonly Button _saveBtn = new();
    private readonly TrackBar _rateBar = new() { Minimum = -10, Maximum = 10, Value = 0, TickFrequency = 2 };
    private readonly TrackBar _volBar = new() { Minimum = 0, Maximum = 100, Value = 100, TickFrequency = 10 };
    private readonly Label _rateValLbl = new() { Text = "0" };
    private readonly Label _volValLbl = new() { Text = "100" };
    private readonly Label _statusLbl = new();

    public TextToSpeechControl()
    {
        BackColor = Theme.Background;
        BuildLayout();

        _synth.Rate = 0;
        _synth.Volume = 100;
        _synth.SpeakCompleted += OnSpeakCompleted;

        SetIdleState();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _synth.SpeakCompleted -= OnSpeakCompleted;
            if (_synth.State == SynthesizerState.Paused)
                _synth.Resume();
            _synth.SpeakAsyncCancelAll();
            _synth.Dispose();
        }
        base.Dispose(disposing);
    }

    private void OnSpeakCompleted(object? sender, SpeakCompletedEventArgs e)
    {
        if (!IsDisposed && IsHandleCreated)
            BeginInvoke(() =>
            {
                SetStatus(Loc.T("tts.status.idle"), Theme.TextSecondary);
                SetIdleState();
            });
    }

    private void BuildLayout()
    {
        _playBtn.Text = Loc.T("tts.btn.play");
        _pauseBtn.Text = Loc.T("tts.btn.pause");
        _resumeBtn.Text = Loc.T("tts.btn.resume");
        _stopBtn.Text = Loc.T("tts.btn.stop");
        _loadBtn.Text = Loc.T("tts.btn.load");
        _saveBtn.Text = Loc.T("tts.btn.save");
        _statusLbl.Text = Loc.T("tts.status.idle");

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

        Theme.StyleButton(_playBtn, primary: true);
        Theme.StyleButton(_pauseBtn);
        Theme.StyleButton(_resumeBtn);
        Theme.StyleButton(_stopBtn, danger: true);
        Theme.StyleButton(_loadBtn);
        Theme.StyleButton(_saveBtn);

        _playBtn.Click += (_, _) => Speak();
        _pauseBtn.Click += (_, _) => Pause();
        _resumeBtn.Click += (_, _) => Resume();
        _stopBtn.Click += (_, _) => Stop();
        _loadBtn.Click += (_, _) => LoadFile();
        _saveBtn.Click += (_, _) => SaveWav();

        _statusLbl.Font = Theme.SmallFont;
        _statusLbl.ForeColor = Theme.TextSecondary;
        _statusLbl.TextAlign = ContentAlignment.MiddleLeft;
        _statusLbl.Width = 130;
        _statusLbl.Height = 36;

        var btnRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 52,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Theme.Surface,
            Padding = new Padding(0, 8, 0, 8)
        };
        btnRow.Controls.Add(_playBtn);
        btnRow.Controls.Add(_pauseBtn);
        btnRow.Controls.Add(_resumeBtn);
        btnRow.Controls.Add(_stopBtn);
        btnRow.Controls.Add(new Panel { Width = 16, Height = 36, BackColor = Theme.Surface });
        btnRow.Controls.Add(_loadBtn);
        btnRow.Controls.Add(_saveBtn);
        btnRow.Controls.Add(new Panel { Width = 16, Height = 36, BackColor = Theme.Surface });
        btnRow.Controls.Add(_statusLbl);

        _rateValLbl.Font = new Font("Segoe UI Semibold", 9f);
        _rateValLbl.ForeColor = Theme.TextPrimary;
        _rateValLbl.Width = 28;
        _rateValLbl.Height = 18;
        _rateValLbl.TextAlign = ContentAlignment.MiddleLeft;

        _volValLbl.Font = new Font("Segoe UI Semibold", 9f);
        _volValLbl.ForeColor = Theme.TextPrimary;
        _volValLbl.Width = 36;
        _volValLbl.Height = 18;
        _volValLbl.TextAlign = ContentAlignment.MiddleLeft;

        _rateBar.TickStyle = TickStyle.BottomRight;
        _volBar.TickStyle = TickStyle.BottomRight;

        _rateBar.ValueChanged += (_, _) =>
        {
            _rateValLbl.Text = _rateBar.Value.ToString();
            _synth.Rate = _rateBar.Value;
        };
        _volBar.ValueChanged += (_, _) =>
        {
            _volValLbl.Text = _volBar.Value.ToString();
            _synth.Volume = _volBar.Value;
        };

        var sliderRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 58,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Theme.Surface,
            Padding = new Padding(0, 2, 0, 4)
        };
        sliderRow.Controls.Add(MakeSliderGroup(Loc.T("tts.slider.speed"), _rateBar, _rateValLbl, barWidth: 180));
        sliderRow.Controls.Add(new Panel { Width = 32, Height = 52, BackColor = Theme.Surface });
        sliderRow.Controls.Add(MakeSliderGroup(Loc.T("tts.slider.volume"), _volBar, _volValLbl, barWidth: 200));

        Theme.StyleTextBox(_textBox);
        _textBox.Dock = DockStyle.Fill;
        _textBox.PlaceholderText = Loc.T("tts.placeholder");
        _textBox.Font = new Font("Segoe UI", 11f);

        card.Controls.Add(_textBox);
        card.Controls.Add(sliderRow);
        card.Controls.Add(btnRow);

        Controls.Add(card);
    }

    private static Panel MakeSliderGroup(string label, TrackBar bar, Label valueLbl, int barWidth)
    {
        var grp = new Panel { Width = barWidth + 80, Height = 52, BackColor = Theme.Surface };

        var lbl = new Label
        {
            Text = label,
            Font = new Font("Segoe UI Semibold", 9f),
            ForeColor = Theme.TextSecondary,
            AutoSize = true,
            Left = 0,
            Top = 0
        };

        valueLbl.Left = lbl.Width + 6;
        valueLbl.Top = 0;

        bar.Left = 0;
        bar.Top = 20;
        bar.Width = barWidth;
        bar.Height = 30;

        grp.Controls.Add(lbl);
        grp.Controls.Add(valueLbl);
        grp.Controls.Add(bar);
        return grp;
    }

    // ── Playback actions ──────────────────────────────────────────────────────

    private void Speak()
    {
        var text = _textBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            MessageBox.Show(this, Loc.T("tts.speak.empty.msg"), Loc.T("tts.speak.empty.title"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        try
        {
            _synth.SpeakAsync(text);
            SetStatus(Loc.T("tts.status.speaking"), Theme.Primary);
            _playBtn.Enabled = false;
            _pauseBtn.Enabled = true;
            _resumeBtn.Enabled = false;
            _stopBtn.Enabled = true;
            _loadBtn.Enabled = false;
            _saveBtn.Enabled = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, string.Format(Loc.T("tts.error.playback"), ex.Message),
                Loc.T("tts.error.title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void Pause()
    {
        try
        {
            _synth.Pause();
            SetStatus(Loc.T("tts.status.paused"), Theme.TextSecondary);
            _pauseBtn.Enabled = false;
            _resumeBtn.Enabled = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, string.Format(Loc.T("tts.error.pause"), ex.Message),
                Loc.T("tts.error.title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void Resume()
    {
        try
        {
            _synth.Resume();
            SetStatus(Loc.T("tts.status.speaking"), Theme.Primary);
            _pauseBtn.Enabled = true;
            _resumeBtn.Enabled = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, string.Format(Loc.T("tts.error.resume"), ex.Message),
                Loc.T("tts.error.title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void Stop()
    {
        try
        {
            if (_synth.State == SynthesizerState.Paused)
                _synth.Resume();
            _synth.SpeakAsyncCancelAll();
            SetStatus(Loc.T("tts.status.idle"), Theme.TextSecondary);
            SetIdleState();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, string.Format(Loc.T("tts.error.stop"), ex.Message),
                Loc.T("tts.error.title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ── File actions ──────────────────────────────────────────────────────────

    private void LoadFile()
    {
        using var dlg = new OpenFileDialog
        {
            Title = Loc.T("tts.load.title"),
            Filter = Loc.T("tts.load.filter")
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            _textBox.Text = File.ReadAllText(dlg.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, string.Format(Loc.T("tts.load.error.msg"), ex.Message),
                Loc.T("tts.load.error.title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SaveWav()
    {
        var text = _textBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            MessageBox.Show(this, Loc.T("tts.save.empty.msg"), Loc.T("tts.save.empty.title"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var dlg = new SaveFileDialog
        {
            Title = Loc.T("tts.save.dialog.title"),
            Filter = Loc.T("tts.save.dialog.filter"),
            FileName = "speech.wav",
            DefaultExt = "wav"
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            _synth.SpeakAsyncCancelAll();
            _synth.SetOutputToWaveFile(dlg.FileName);
            _synth.Speak(text);
            _synth.SetOutputToDefaultAudioDevice();
            MessageBox.Show(this, string.Format(Loc.T("tts.save.success.msg"), dlg.FileName),
                Loc.T("tts.save.success.title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            try { _synth.SetOutputToDefaultAudioDevice(); } catch { /* restore output even on error */ }
            MessageBox.Show(this, string.Format(Loc.T("tts.save.error.msg"), ex.Message),
                Loc.T("tts.save.error.title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SetStatus(string text, Color color)
    {
        _statusLbl.Text = text;
        _statusLbl.ForeColor = color;
    }

    private void SetIdleState()
    {
        _playBtn.Enabled = true;
        _pauseBtn.Enabled = false;
        _resumeBtn.Enabled = false;
        _stopBtn.Enabled = false;
        _loadBtn.Enabled = true;
        _saveBtn.Enabled = true;
    }
}
