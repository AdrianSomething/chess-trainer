using System.Diagnostics;
namespace ChessOpeningTrainer;

public sealed partial class VisionForm : LocalizedForm
{
    private readonly VisionBoard board = new() { Dock = DockStyle.Fill, InputEnabled = false };
    private readonly Label info = new() { Dock = DockStyle.Fill, Padding = new(22, 4, 22, 4), UseMnemonic = false };
    private readonly FlowLayoutPanel menu = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new(0, 16, 0, 16) };
    private readonly Button confirm = new() { Text = "Bestätigen · Leertaste", Dock = DockStyle.Bottom, Height = 42, Visible = false };
    private readonly VisionScores scores;
    private readonly VisionStatistics statistics;
    private readonly Button pause = new() { Text = "Pause", Dock = DockStyle.Bottom, Height = 36, Visible = false };
    private readonly VisionRunTimer sessionTimer = new();
    private readonly Label curtain = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, UseMnemonic = false, BackColor = UiPalette.FromArgb(43, 49, 55), ForeColor = UiPalette.Gainsboro, Font = new Font("Segoe UI", 18) };
    private readonly CheckBox indirect = new() { Text = "Indirekte Verteidiger (Röntgenlinien) mitzählen", AutoSize = true };
    private readonly ComboBox memoryDifficulty = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 255 };
    private readonly MenuIconButton abort = new("back") { Left = 10, Top = 7, Width = 38, Height = 34, Visible = false, AccessibleName = "Zurück / Training abbrechen" };
    private bool instruction, paused, feedbackPending;
    private int roundErrors;
    private readonly CancellationTokenSource lifetime = new();
    private readonly Stopwatch clock = new();
    private readonly HashSet<string> used = [];
    private readonly Font hubFont = new("Segoe UI", 24, FontStyle.Bold), trainingFont = new("Segoe UI", 11);
    private Action? settingsSubscription;
    private bool resourcesDisposed;
    private VisionExercise? exercise;
    private int mode, rounds, correct, errors, revision;
    private double seconds;
    private Task<VisionExercise>? prepared;
    private Task? introPreparation;
    private VisionBuffer<VisionExercise>? buffer;
    private readonly Dictionary<(int Mode, bool Indirect), VisionBuffer<VisionExercise>> modeBuffers = [];
    private VisionBuffer<VisionSafetyExercise>? safetyBuffer;
    private readonly Dictionary<int, VisionBuffer<Position>> memoryBuffers = [];
    private VisionChallengeForm? embeddedTraining;
    public VisionForm(string directory, StockfishEngine? engine = null, VisionComparisonStock? comparisons = null)
    {
        scores = new(directory);
        statistics = new(directory);
        Text = "Schachblick"; Size = new(1000, 950); MinimumSize = new(800, 720);
        BackColor = UiPalette.FromArgb(31, 35, 39); ForeColor = UiPalette.Gainsboro;
        var body = new Panel { Dock = DockStyle.Fill };
        body.Controls.AddRange([board, menu]);
        void CenterModeButtons()
        {
            ModeHubLayout.Center(menu, menu.Controls.Count > 0 ? menu.Controls[^1] : null);
        }
        menu.SizeChanged += (_, _) => CenterModeButtons();
        var header = new Panel { Dock = DockStyle.Top, Height = ModeHubLayout.HeaderHeight };
        var navigation = new Panel { Dock = DockStyle.Top, Height = ModeHubLayout.NavigationHeight };
        sessionTimer.ReadTime = () => VisionRunTimer.Format(seconds + (!instruction && !feedbackPending && exercise is not null && !menu.Visible ? clock.Elapsed.TotalSeconds : 0));
        sessionTimer.Visible = false; sessionTimer.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        navigation.Resize += (_, _) => sessionTimer.Location = new(Math.Max(55, navigation.Width - sessionTimer.Width - 58), 5);
        navigation.Controls.AddRange([abort, sessionTimer]);
        header.Controls.AddRange([info, navigation]);
        Controls.AddRange([body, confirm, pause, header]);
        board.Visible = false;
        body.Controls.Add(curtain); curtain.BringToFront();
        body.Controls.Add(pauseOverlay);
        bool arrangingInstruction = false, instructionLayoutPending = false;
        void ArrangeInstruction()
        {
            if (!instruction || !curtain.Visible || arrangingInstruction) return;
            arrangingInstruction = true;
            bool layoutChanged;
            try
            {
                body.PerformLayout();
                Rectangle area = board.Bounds;
                VisionInstructionLayout.Center(curtain, area);
                layoutChanged = area != board.Bounds;
            }
            finally { arrangingInstruction = false; }
            if (layoutChanged) QueueInstructionLayout();
        }
        void QueueInstructionLayout()
        {
            if (!instruction || !curtain.Visible || arrangingInstruction || instructionLayoutPending || !IsHandleCreated || IsDisposed || Disposing) return;
            instructionLayoutPending = true;
            try
            {
                BeginInvoke((Action)(() =>
                {
                    instructionLayoutPending = false;
                    if (!IsDisposed && !Disposing) ArrangeInstruction();
                }));
            }
            catch (InvalidOperationException) { instructionLayoutPending = false; }
        }
        body.SizeChanged += (_, _) => QueueInstructionLayout();
        body.Layout += (_, _) => QueueInstructionLayout();
        // Docked board bounds settle after the parent's SizeChanged callback.
        board.SizeChanged += (_, _) => QueueInstructionLayout();
        board.LocationChanged += (_, _) => QueueInstructionLayout();
        async Task PrepareIntroduction(int version, string rules)
        {
            await Next();
            if (IsDisposed || lifetime.IsCancellationRequested) return;
            try
            {
                await this.InvokeAsync(() =>
                {
                    if (version != revision || IsDisposed) return;
                    clock.Reset(); board.Answering = false; pause.Visible = false;
                    curtain.Text = rules; curtain.Visible = true;
                    QueueInstructionLayout();
                }, lifetime.Token);
            }
            catch (OperationCanceledException) { }
        }
        void SyncSettings()
        {
            indirect.Checked = Preferences.Current.VisionIndirectDefenders;
            if (memoryDifficulty.Items.Count > 0) memoryDifficulty.SelectedIndex = Math.Clamp(Preferences.Current.VisionMemoryDifficulty, 0, 2);
        }
        Preferences.Changed += SyncSettings;
        settingsSubscription = SyncSettings;
        abort.Click += (_, _) =>
        {
            if (!ConfirmLeaveTraining()) return;
            revision++; clock.Stop(); board.Answering = false;
            buffer = null;
            instruction = paused = feedbackPending = false; exercise = null;
            body.Enabled = true; pauseOverlay.HideImmediately();
            curtain.Visible = board.Visible = confirm.Visible = pause.Visible = abort.Visible = false;
            menu.Visible = true; ShowHubHeading();
            sessionTimer.Visible = false;
        };
        curtain.Visible = false;
        string[] names = ["Angreifer & Verteidiger", "Angriffe im nächsten Zug", "Gegnerische Angriffe"];
        for (int i = 0; i < names.Length; i++)
        {
            int selectedMode = i;
            var b = new VisionModeButton(i, names[i]) { Record = RecordText(i) };
            b.Click += async (_, _) =>
            {
                revision++; mode = selectedMode; rounds = correct = errors = 0; seconds = 0; used.Clear();
                info.Font = trainingFont; info.TextAlign = ContentAlignment.MiddleLeft;
                statistics.BeginRun();
                bool includeIndirect = indirect.Checked;
                var key = (selectedMode, includeIndirect);
                if (!modeBuffers.TryGetValue(key, out buffer))
                    modeBuffers[key] = buffer = new((white, ct) => VisionRules.Generate(selectedMode, ct, white, includeIndirect), e => e.Position.ToFen() + "|" + e.Target, lifetime.Token);
                prepared = null; menu.Visible = false; confirm.Visible = true; pause.Visible = false;
                clock.Reset(); board.Answering = false; instruction = true; paused = feedbackPending = false;
                sessionTimer.Visible = true;
                string rules = names[mode] + "\n\n" + Rules();
                curtain.Text = "Stellung wird vorbereitet …";
                curtain.Visible = true;
                abort.Visible = true;
                int version = revision;
                // Starting via Space must also await the rule overlay, not just the board.
                introPreparation = PrepareIntroduction(version, rules);
                await introPreparation;
            };
            menu.Controls.Add(b);
        }
        string[] extraNames = ["Figur retten", "Zugvergleich", "Figuren merken"];
        for (int i = 0; i < extraNames.Length; i++)
        {
            int extra = i;
            var button = new VisionModeButton(i + 3, extraNames[i]) { Record = RecordText(i + 3) };
            void RefreshExtraRecord()
            {
                int recordMode = extra == 2 ? 5 + Math.Max(0, memoryDifficulty.SelectedIndex) : extra + 3;
                var latest = new VisionStatistics(directory);
                button.Record = latest.Values.Records.TryGetValue(recordMode, out var record)
                    ? $"Rekord {record.Errors} Fehler · {record.Rate:0.00}/s" : "Noch kein Rekord";
            }
            if (extra == 2) memoryDifficulty.SelectedIndexChanged += (_, _) => RefreshExtraRecord();
            button.Click += (_, _) =>
            {
                VisionChallengeForm training;
                if (extra == 0)
                {
                    safetyBuffer ??= new(VisionSafety.Generate, e => e.Position.ToFen() + "|" + e.Origin, lifetime.Token);
                    training = new SafeSquareForm(directory, safetyBuffer);
                }
                else if (extra == 1) training = new CompareMovesForm(directory, engine ?? new StockfishEngine(AppContext.BaseDirectory), comparisons);
                else
                {
                    int difficulty = Math.Clamp(memoryDifficulty.SelectedIndex, 0, 2);
                    if (!memoryBuffers.TryGetValue(difficulty, out var memoryBuffer))
                        memoryBuffers[difficulty] = memoryBuffer = new((white, ct) => VisionMemory.Generate(difficulty, white, ct), p => p.ToFen(), lifetime.Token);
                    training = new MemoryVisionForm(directory, difficulty, memoryBuffer);
                }
                embeddedTraining = training;
                training.TopLevel = false; training.FormBorderStyle = FormBorderStyle.None;
                training.MinimumSize = Size.Empty; training.Dock = DockStyle.Fill;
                body.Visible = header.Visible = confirm.Visible = pause.Visible = false;
                Controls.Add(training); training.BringToFront();
                training.FormClosed += (_, _) =>
                {
                    embeddedTraining = null;
                    if (!IsDisposed && !Disposing)
                    {
                        body.Visible = header.Visible = menu.Visible = true;
                        ShowHubHeading(); RefreshExtraRecord(); CenterModeButtons();
                    }
                };
                training.Show();
            };
            menu.Controls.Add(button);
        }
        memoryDifficulty.Items.AddRange(["Figuren merken: leicht (4–8)", "Figuren merken: mittel (9–16)", "Figuren merken: schwer (17–32)"]);
        SyncSettings();
        var optionsButton = new MenuIconButton("settings") { Dock = DockStyle.Right, Width = 38, AccessibleName = "Schachblick-Einstellungen" };
        navigation.Padding = new(0, 7, 10, 7);
        navigation.Controls.Add(optionsButton);
        optionsButton.Click += (_, _) => SettingsForm.OpenVision();
        var stats = new VisionModeButton(6, "Statistik") { Record = "Fehlerarten und Lernverlauf" };
        stats.Click += (_, _) =>
        {
            new VisionStatisticsForm(new VisionStatistics(directory).Values).Show();
        };
        menu.Controls.Add(stats);
        CenterModeButtons();
        pause.Click += (_, _) =>
        {
            if (instruction || feedbackPending || exercise is null) return;
            SetTrainingPaused(!paused);
        };
        Deactivate += (_, _) => { if (board.Answering && !paused) pause.PerformClick(); };
        ShowHubHeading();
        board.MarkPiece += square =>
        {
            if (exercise is null) return;
            if (!board.MarkedSquares.Add(square)) { board.MarkedSquares.Remove(square); board.ErrorSquares.Remove(square); }
            else if (!exercise.Pieces.Contains(square))
            {
                errors++;
                statistics.Miss(mode, exercise.Position.Squares[square], mode == 1
                    ? "Falsch markiert · kein möglicher Folgeangriff" : "Falsch markiert · kein Angriff auf das Zielfeld");
            }
        };
        board.MarkAttack += arrow =>
        {
            if (exercise is null) return;
            if (board.Arrows.Contains(arrow)) board.Arrows.Remove(arrow);
            else
            {
                board.Arrows.Add(arrow);
                if (!exercise.Attacks.Contains((arrow.From, arrow.To)))
                {
                    errors++;
                    char piece = exercise.Position.Squares[arrow.From];
                    string cause = char.IsUpper(piece) == exercise.White ? "Eigene Figur als Angreifer markiert"
                        : char.IsUpper(exercise.Position.Squares[arrow.To]) != exercise.White ? "Gegnerische Figur als Ziel markiert"
                        : "Nicht vorhandene Angriffslinie markiert";
                    statistics.Miss(mode, piece, cause);
                }
            }
        };
        confirm.Click += async (_, _) => await Confirm();
        KeyPreview = true;
        KeyDown += async (_, e) => { if (e.KeyCode == Keys.Space) { e.Handled = e.SuppressKeyPress = true; if (embeddedTraining is not null) await embeddedTraining.ConfirmOrAdvance(); else await Confirm(); } };
        Deactivate += (_, _) => embeddedTraining?.PauseFromHost();
        FormClosing += (_, e) => { if (e.CloseReason == CloseReason.UserClosing && !(embeddedTraining?.ConfirmHostClose() ?? ConfirmLeaveTraining())) e.Cancel = true; };
        FormClosed += (_, _) => { embeddedTraining?.CloseFromHost(); revision++; buffer = null; lifetime.Cancel(); Preferences.Changed -= SyncSettings; };
    }
    private string RecordText(int selected) => statistics.Values.Records.TryGetValue(selected, out var best)
        ? $"Rekord {best.Errors} Fehler · {best.Rate:0.00}/s" : "Noch kein Rekord";
    private void ShowHubHeading() { info.Text = "Schachblick"; info.Font = hubFont; info.TextAlign = ContentAlignment.MiddleCenter; }
    private string Rules() => mode switch
    {
        0 => "Finde alle Figuren, die das markierte Feld angreifen oder verteidigen." + (indirect.Checked ? " Auch Verteidiger hinter gleichgerichteten Figuren zählen." : ""),
        1 => "Finde die Figuren, die das markierte Feld nach einem Zug angreifen könnten – durch Umpositionierung oder Freiziehen.",
        _ => "Finde alle Angriffe der gegnerischen Figuren auf deine Figuren."
    };
    private Task<VisionExercise> Prepare()
    {
        if (buffer is not null) return buffer.Next(lifetime.Token, rounds % 2 == 0);
        int selectedMode = mode;
        bool white = rounds % 2 == 0, includeIndirect = indirect.Checked;
        return Task.Run(() => VisionRules.Generate(selectedMode, lifetime.Token, white, includeIndirect), lifetime.Token);
    }
    private async Task Next()
    {
        int version = revision;
        clock.Reset();
        board.Answering = false; confirm.Enabled = false; info.Text = "Neue Schachblick-Aufgabe wird vorbereitet …";
        try
        {
            while (!lifetime.IsCancellationRequested)
            {
                var candidate = await (prepared ?? Prepare()); prepared = null;
                bool accepted = false, stale = false;
                await this.InvokeAsync(() =>
                {
                    stale = version != revision || IsDisposed;
                    if (stale || !used.Add(candidate.Position.ToFen() + "|" + candidate.Target)) return;
                    accepted = true; exercise = candidate;
                    board.Position = exercise.Position.Clone(); board.Flip = !exercise.White;
                    board.MarkedSquares.Clear(); board.Arrows.Clear();
                    board.HintMove = mode == 2 ? null : new Move { From = exercise.Target, To = exercise.Target };
                    board.ArrowMode = mode == 2; board.Answering = !paused; board.Invalidate(); confirm.Enabled = true;
                    curtain.Visible = false; board.Visible = true; pause.Visible = true; pause.Text = paused ? "Fortsetzen" : "Pause"; roundErrors = errors;
                    info.Text = $"Aufgabe {rounds + 1}/30 · Fehler {errors}\n" + (mode switch
                    {
                        0 => "Markiere alle Figuren beider Farben, die das grüne Zielfeld angreifen oder verteidigen.",
                        1 => "Markiere Figuren, die das Zielfeld noch nicht angreifen, aber nach einem legalen Zug ihrer Seite dort wirken können – auch durch Freiziehen.",
                        _ => "Zeichne alle Pfeile von gegnerischen Figuren auf deine angegriffenen Figuren. Deine Seite steht unten."
                    });
                    clock.Restart(); if (paused) clock.Stop();
                }, lifetime.Token);
                if (stale || accepted) return;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            if (!IsDisposed && !lifetime.IsCancellationRequested)
                try { await this.InvokeAsync(() => info.Text = ex.Message, lifetime.Token); }
                catch (OperationCanceledException) { }
        }
    }
    private async Task Confirm()
    {
        if (paused) { SetTrainingPaused(false); return; }
        if (instruction)
        {
            int version = revision;
            if (introPreparation is not null) await introPreparation;
            if (version != revision || IsDisposed) return;
            bool retry = false;
            try
            {
                await this.InvokeAsync(() =>
                {
                    if (version != revision || IsDisposed || !confirm.Enabled) return;
                    instruction = false;
                    if (exercise is null) { retry = true; return; }
                    curtain.Visible = false; board.Answering = true; pause.Visible = true; clock.Restart();
                }, lifetime.Token);
                if (retry) await Next();
            }
            catch (OperationCanceledException) { }
            return;
        }
        if (feedbackPending) { feedbackPending = false; await Advance(); return; }
        if (!board.Answering || exercise is null) return;
        board.Answering = false; clock.Stop(); seconds += clock.Elapsed.TotalSeconds;
        int found = mode == 2 ? exercise.Attacks.Count(a => board.Arrows.Contains(new(a.From, a.To))) : exercise.Pieces.Count(board.MarkedSquares.Contains);
        correct += found;
        errors += (mode == 2 ? exercise.Attacks.Count : exercise.Pieces.Count) - found;
        var missingAttacks = exercise.Attacks.Where(a => !board.Arrows.Contains(new(a.From, a.To))).ToArray();
        var missing = mode == 2 ? missingAttacks.Select(a => a.From).ToList()
            : exercise.Pieces.Where(p => !board.MarkedSquares.Contains(p)).ToList();
        foreach (int square in missing)
        {
            char piece = exercise.Position.Squares[square];
            string pattern = mode == 1 ? VisionPotentialMistake.Classify(exercise.Position, square, exercise.Target) : char.ToLowerInvariant(piece) switch
            {
                'n' => "Springersprung", 'p' => "Bauernangriff", 'k' => "Königsnähe",
                _ => mode == 0 && !VisionRules.Attacks(exercise.Position, square, exercise.Target) ? "Röntgenlinie" : "Linienangriff"
            };
            statistics.Miss(mode, piece, pattern);
        }
        try { statistics.Save(); }
        catch (IOException ex) { info.Text = "Statistik konnte nicht gespeichert werden: " + ex.Message; }
        bool wrong = errors != roundErrors;
        AppSounds.Play(wrong ? "error" : "success");
        if (wrong)
        {
            if (mode == 2)
                foreach (var arrow in board.Arrows.Where(a => !exercise.Attacks.Contains((a.From, a.To))))
                { board.ErrorSquares.Add(arrow.From); board.ErrorSquares.Add(arrow.To); }
            else board.ErrorSquares.UnionWith(board.MarkedSquares.Where(s => !exercise.Pieces.Contains(s)));
            board.MarkedSquares.Clear(); board.Arrows.Clear();
            board.MarkedSquares.UnionWith(missing);
            if (mode == 2)
            {
                board.Arrows.AddRange(exercise.Attacks.Select(a => new BoardArrow(a.From, a.To)));
                board.ErrorArrows.UnionWith(missingAttacks.Select(a => new BoardArrow(a.From, a.To)));
            }
            board.Invalidate();
            info.Text = $"Fehler: {errors - roundErrors} · Gesamt: {errors}\nÜbersehen (orange): " + string.Join(", ", missing.Select(s => $"{exercise.Position.Squares[s]} {(char)('a' + s % 8)}{s / 8 + 1}")) + "\nLeertaste: nächste Aufgabe";
            feedbackPending = true;
            return;
        }
        await Advance();
    }
    private async Task Advance()
    {
        if (++rounds < 30) { await Next(); return; }
        double score = correct / Math.Max(.1, seconds);
        try { scores.Record(mode, score); statistics.Record(mode, errors, score); }
        catch (IOException ex) { info.Text = ex.Message; return; }
        var record = statistics.Values.Records[mode];
        info.Text = $"30/30 · {correct} richtige Markierungen · {errors} Fehler · {seconds:0.0} s\nScore {errors} Fehler · {score:0.00}/s · Rekord {record.Errors} Fehler · {record.Rate:0.00}/s";
        confirm.Visible = pause.Visible = abort.Visible = board.Visible = false; menu.Visible = true;
        sessionTimer.Visible = false;
        ShowHubHeading();
        buffer = null;
        for (int i = 0; i < 3; i++)
            ((VisionModeButton)menu.Controls[i]).Record = RecordText(i);
    }
    protected override void Dispose(bool disposing)
    {
        bool cleanup = disposing && !resourcesDisposed;
        if (cleanup)
        {
            resourcesDisposed = true;
            if (settingsSubscription is not null) Preferences.Changed -= settingsSubscription;
            revision++; buffer = null; lifetime.Cancel();
            foreach (var modeBuffer in modeBuffers.Values) modeBuffer.Dispose(); modeBuffers.Clear();
            safetyBuffer?.Dispose(); safetyBuffer = null;
            foreach (var memoryBuffer in memoryBuffers.Values) memoryBuffer.Dispose(); memoryBuffers.Clear();
        }
        base.Dispose(disposing);
        if (cleanup) { hubFont.Dispose(); trainingFont.Dispose(); }
    }
}
