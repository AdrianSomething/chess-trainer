using System.Diagnostics;
namespace ChessOpeningTrainer;

// Shared session mechanics, not a second board implementation.
public abstract partial class VisionChallengeForm : LocalizedForm
{
    protected readonly Panel Content = new() { Dock = DockStyle.Fill };
    protected readonly Label Status = new() { Dock = DockStyle.Top, Height = 80 };
    protected readonly CancellationTokenSource Lifetime = new();
    protected readonly Stopwatch Clock = new();
    protected int Round { get; private set; }
    protected bool Playing { get; private set; }
    protected readonly VisionStatistics Statistics;
    private readonly Label cover = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, UseMnemonic = false, Font = new Font("Segoe UI", 18), BackColor = UiPalette.FromArgb(43, 49, 55), ForeColor = UiPalette.Gainsboro };
    private readonly Button next = new() { Text = "Start · Leertaste", Dock = DockStyle.Bottom, Height = 72, Visible = false };
    private readonly MenuIconButton pause = new("pause") { Width = 38, Height = 34, AccessibleName = "Pause" };
    private readonly VisionRunTimer sessionTimer = new();
    private readonly int mode;
    private bool paused, ready = true;
    private bool introduction = true;
    private int errors, correct;
    private double seconds;
    protected VisionChallengeForm(string directory, int mode, string title, string rules)
    {
        this.mode = mode; Statistics = new(directory);
        Text = title; Size = new(1200, 900); MinimumSize = new(950, 700);
        BackColor = UiPalette.FromArgb(31, 35, 39); ForeColor = UiPalette.Gainsboro;
        Controls.AddRange([Content, Status, next]);
        Content.Controls.Add(pauseOverlay);
        Controls.Add(cover); cover.BringToFront(); Content.Visible = false;
        var abort = new MenuIconButton("back") { Left = 10, Top = 7, Width = 38, Height = 34, AccessibleName = "Zurück / Training abbrechen" };
        var navigation = new Panel { Dock = DockStyle.Top, Height = 48 };
        sessionTimer.ReadTime = () => TimerText;
        navigation.Controls.AddRange([abort, sessionTimer, pause]); Controls.Add(navigation);
        void ArrangeNavigation()
        {
            pause.Location = new(Math.Max(58,navigation.ClientSize.Width - 50), 7);
            sessionTimer.Location = new(Math.Max(58,pause.Left - sessionTimer.Width - 10), 5);
        }
        navigation.Resize += (_, _) => ArrangeNavigation();
        ArrangeNavigation();
        Shown += (_,_)=>ArrangeNavigation();
        abort.Click += (_, _) => Close();
        cover.Text = title + "\n\n" + rules;
        bool arrangingInstruction = false, instructionLayoutPending = false;
        void IntroLayout()
        {
            if (!introduction || arrangingInstruction) return;
            arrangingInstruction = true;
            bool layoutChanged;
            try
            {
                Content.Visible = true;
                PerformLayout();
                Rectangle area = Content.Bounds;
                VisionInstructionLayout.Center(cover, area);
                layoutChanged = area != Content.Bounds;
            }
            finally { arrangingInstruction = false; }
            if (layoutChanged) QueueIntroLayout();
        }
        void QueueIntroLayout()
        {
            if (!introduction || arrangingInstruction || instructionLayoutPending || !IsHandleCreated || IsDisposed || Disposing) return;
            instructionLayoutPending = true;
            try
            {
                BeginInvoke((Action)(() =>
                {
                    instructionLayoutPending = false;
                    if (!IsDisposed && !Disposing) IntroLayout();
                }));
            }
            catch (InvalidOperationException) { instructionLayoutPending = false; }
        }
        Shown += (_, _) => QueueIntroLayout(); Resize += (_, _) => QueueIntroLayout(); Layout += (_, _) => QueueIntroLayout();
        Content.SizeChanged += (_, _) => QueueIntroLayout(); Content.LocationChanged += (_, _) => QueueIntroLayout();
        pause.Enabled = false;
        next.Click += async (_, _) => await ConfirmOrAdvance();
        pause.Click += (_, _) => TogglePause();
        KeyPreview = true;
        KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Space) { e.SuppressKeyPress = true; await ConfirmOrAdvance(); }
        };
        Deactivate += (_, _) => { if (Playing && !paused) TogglePause(); };
        FormClosing += (_, e) => { if (!closingConfirmed && e.CloseReason == CloseReason.UserClosing && !ConfirmLeaveTraining()) e.Cancel = true; };
        FormClosed += (_, _) => DisposeSessionResources();
        EnableIntroductionConfirmation();
    }
    private bool closingConfirmed;
    private bool sessionResourcesDisposed;
    internal bool ConfirmHostClose() => ConfirmLeaveTraining();
    internal void CloseFromHost() { closingConfirmed = true; Close(); }
    protected virtual void ConfirmAnswer() { }
    protected virtual double ActiveAnswerSeconds => Clock.Elapsed.TotalSeconds;
    protected virtual string TimerText => VisionRunTimer.Format(seconds + (Playing ? ActiveAnswerSeconds : 0));
    protected void EnableIntroductionConfirmation()
    {
        if (!introduction) return;
        next.Text = "Bestätigen · Leertaste"; next.Enabled = true; next.Visible = true;
    }
    protected void EnableAnswerConfirmation()
    {
        next.Text = "Bestätigen · Leertaste"; next.Enabled = true; next.Visible = true;
    }
    internal async Task ConfirmOrAdvance()
    {
        if (paused) { SetTrainingPaused(false); return; }
        if (CanAnswer) ConfirmAnswer();
        else if (ready) await Advance();
    }
    private void TogglePause()
    {
        if (!Playing) return;
        SetTrainingPaused(!paused);
    }
    protected bool CanAnswer => Playing && !paused;
    protected void AddView(Control view)
    {
        Content.Controls.Add(view); view.BringToFront();
        if (introduction)
        {
            void Preview(Control c)
            {
                if (c is BoardControl b && b.Position is null) b.Position = Position.FromFen(Position.InitialFen);
                foreach (Control child in c.Controls) Preview(child);
            }
            Preview(view);
        }
    }
    private async Task Advance()
    {
        if (!ready || paused || Round >= 30) return;
        introduction = false; cover.Dock = DockStyle.Fill;
        ready = false; next.Enabled = false; next.Visible = false; Clock.Reset();
        cover.Text = "Aufgabe wird vorbereitet …"; Content.Visible = false; cover.Visible = true; cover.BringToFront();
        try
        {
            await Prepare();
            if (IsDisposed) return;
            await this.InvokeAsync(() =>
            {
                Playing = true; pause.Enabled = true; cover.Visible = false; Content.Visible = true;
                Status.Text = $"Aufgabe {Round + 1}/30 · {(Round % 2 == 0 ? "Weiß" : "Schwarz")} · Fehler {errors}";
                Clock.Restart(); if (paused) Clock.Stop();
                OnStarted();
            }, Lifetime.Token);
        }
        catch (OperationCanceledException)
        {
            if (!Lifetime.IsCancellationRequested && !IsDisposed)
            {
                await this.InvokeAsync(() =>
                {
                    cover.Text = "Im Zeitbudget wurde keine passende Aufgabe gefunden.\nLeertaste: erneut vorbereiten";
                    ready = true; next.Enabled = true;
                });
            }
        }
        catch (Exception ex)
        {
            if (!IsDisposed) await this.InvokeAsync(() => { cover.Text = "Vorbereitung: " + ex.Message + "\nLeertaste: erneut versuchen"; ready = true; next.Enabled = true; });
        }
    }
    protected abstract Task Prepare();
    protected virtual void OnStarted() { }
    protected virtual void OnResumed() { }
    protected virtual void DisposeTrainingResources() { }
    private void DisposeSessionResources()
    {
        if (sessionResourcesDisposed) return;
        sessionResourcesDisposed = true;
        Clock.Stop(); Lifetime.Cancel();
        DisposeTrainingResources();
    }
    protected override void Dispose(bool disposing)
    {
        // Embedded forms are also disposed through their parent, without a FormClosed event.
        if (disposing) DisposeSessionResources();
        base.Dispose(disposing);
    }
    protected void Finish(int mistakes, int hits, string explanation)
    {
        if (!CanAnswer) return;
        Clock.Stop(); seconds += ActiveAnswerSeconds; Playing = false;
        errors += mistakes; correct += hits; Round++;
        AppSounds.Play(mistakes == 0 ? "success" : "error");
        pause.Enabled = false;
        Status.Text = $"{Round}/30 · Fehler {errors}\n" + explanation;
        try { Statistics.Save(); }
        catch (IOException ex) { Status.Text += "\nStatistik konnte nicht gespeichert werden: " + ex.Message; }
        if (Round == 30)
        {
            double rate = correct / Math.Max(.1, seconds);
            try
            {
                Statistics.Record(mode, errors, rate);
                var best = Statistics.Values.Records[mode];
                Status.Text += $"\nErgebnis {errors} Fehler · {rate:0.00}/s. Rekord: {best.Errors} Fehler · {best.Rate:0.00}/s.";
            }
            catch (IOException ex) { Status.Text += "\nSpeichern fehlgeschlagen: " + ex.Message; }
            next.Text = "Training beendet";
        }
        else { ready = true; next.Text = "Nächste Aufgabe · Leertaste"; next.Enabled = true; next.Visible = true; }
        sessionTimer.UpdateTime();
    }
}
