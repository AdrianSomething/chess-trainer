using System.Diagnostics;
using System.Text.Json;

namespace ChessOpeningTrainer;

/// <summary>Optional training launcher/monitor. No AnalysisPresenter, history or puzzle supply is created here.</summary>
public sealed partial class EngineLabForm : LocalizedForm
{
    private readonly BoardControl board = new() { Dock = DockStyle.Fill, InputEnabled = false };
    private readonly FlowLayoutPanel options = new() { Dock = DockStyle.Right, Width = 420, Padding = new(12), AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
    private readonly ComboBox mode = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
    private readonly ComboBox source = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
    private readonly TextBox python = new() { Text = "python", Width = 180 };
    private readonly TextBox fen = new() { Width = 365, PlaceholderText = "FEN" };
    private readonly CheckBox visual = new() { Text = "Brett anzeigen", Checked = true, AutoSize = true };
    private readonly CheckBox timing = new() { Text = "Denkdauer speichern", Checked = true, AutoSize = true };
    private readonly ScrollNumber rounds = Number(1, 10000, 100), population = Number(2, 1000, 100), maxMoves = Number(1, 2000, 500),
        depth = Number(1, 25, 1), teacherDepth = Number(0, 25, 0), instances = Number(1, 256, 1), instance = Number(1, 256, 1);
    private readonly Label status = new() { Dock = DockStyle.Bottom, Height = 85, Padding = new(12) };
    private readonly Button start = new() { Text = "Experiment starten", Width = 365, Height = 38 };
    private readonly Button resume = new() { Text = "Weitere Runden / fortsetzen", Width = 365, Height = 38 };
    private readonly Button stop = new() { Text = "Anhalten", Width = 365, Height = 34, Enabled = false };
    private readonly TextBox report = new() { Dock = DockStyle.Bottom, Height = 130, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
    private readonly ToolTip tips = new() { AutoPopDelay = 25000 };
    private readonly System.Windows.Forms.Timer refresh = new() { Interval = 250 };
    private readonly string storage;
    private string? runDirectory;
    private Process? process;
    private readonly object messageLock = new();
    private string? pendingMessage;
    private bool closing;

    public EngineLabForm(string dataDirectory)
    {
        storage = Path.Combine(dataDirectory, "EngineLab"); Directory.CreateDirectory(storage);
        string previous = Path.Combine(storage, "latest-run.txt");
        if (File.Exists(previous)) { string candidate = File.ReadAllText(previous); if (Directory.Exists(candidate)) runDirectory = candidate; }
        Text = "Engine-Labor · Esc: Hauptmenü"; Size = new(1250, 850); MinimumSize = new(950, 650);
        BackColor = UiPalette.FromArgb(31, 35, 39); ForeColor = UiPalette.Gainsboro;
        var title = new Label { Text = "Engine-Labor · Esc: Hauptmenü", Dock = DockStyle.Top, Height = 50, Padding = new(12), Font = new("Segoe UI", 16, FontStyle.Bold) };
        Controls.Add(board); Controls.Add(report); Controls.Add(status); Controls.Add(options); Controls.Add(title);
        mode.Items.AddRange(["Populationstraining", "Stockfish-Destillation", "Stockfish-Vergleich", "Stellungslabor"]); mode.SelectedIndex = 0;
        source.Items.AddRange(["Grundstellung", "Generierte Stellungen", "Eigene FEN"]); source.SelectedIndex = 0;
        Row("Modus", mode, "Population: evolutionäres Turnier. Destillation: überwachte Imitation von Stockfish. Vergleich: gespeichertes Modell gegen steigende Suchtiefen, keine Elo-Schätzung. Stellungslabor: direkte Spiel- und Stellungsabfragen ohne neuronales Modell.");
        Row("Python", python, "Python-Programm mit CUDA-fähigem PyTorch. Die normalen App-Funktionen benötigen diese optionale Abhängigkeit nicht.");
        Row("Runden", rounds, "Zusätzliche Trainingsrunden; standardmäßig 100. Fortsetzen beginnt beim letzten vollständig gespeicherten Rundenende.");
        Row("Population", population, "Anzahl Modelle. Nur ein Modell gleichzeitig auf der GPU; übrige Gewichte liegen im Arbeitsspeicher. Standard: 100.");
        Row("Maximale volle Züge", maxMoves, "Nach so vielen Zügen beider Seiten endet eine nicht bereits entschiedene Partie mit einem Punkteabzug. 500 entspricht 1000 Halbzügen.");
        Row("Stockfish-Starttiefe", depth, "Gegnertiefe; im Populationstraining alle zehn Runden um eins erhöht. Beim Vergleich wird die Tiefe schrittweise erhöht.");
        Row("Feedback-Tiefe", teacherDepth, "0 deaktiviert zusätzliche Analysen. Sonst bewertet Stockfish nach jedem gespielten Zug mit dieser Suchtiefe; das kostet zusätzliche Rechenzeit.");
        Row("Startstellungen", source, "Grundstellung, durch legale Züge generierte Stellungen oder eine eigene FEN. Im abschließenden Tiefenvergleich wird die Grundstellung verwendet.");
        options.Controls.Add(fen); L.SetToolTip(tips, fen, "Gültige FEN einschließlich Zugrecht, Rochade und en passant. Wird nur bei Eigene FEN verwendet.");
        options.Controls.Add(visual); options.Controls.Add(timing);
        L.SetToolTip(tips, visual, "Nur das ausgewählte beziehungsweise zuletzt berechnete Brett zeichnen. Ohne Anzeige läuft das Experiment weiter und vermeidet Brett-Neuzeichnungen.");
        L.SetToolTip(tips, timing, "Mittlere Entscheidungsdauer je Modell aufzeichnen, einschließlich Modellwechsel und Eingabeverarbeitung. Kein Elo-Maß.");
        visual.CheckedChanged += (_, _) => board.Visible = visual.Checked;
        Row("Labor: parallele Bretter", instances, "Anzahl unabhängig gespeicherter Spiele im Stellungslabor. Keine Engineprozesse, solange keine Bewertung angefordert wird.");
        Row("Labor: angezeigtes Brett", instance, "Wählt eine unabhängige Spielinstanz zur Anzeige und Eingabe aus.");
        instance.ValueChanged += async (_, _) => { if (!labBusy && lab is not null && instance.Value <= instances.Value) await ShowLabState(); };
        options.Controls.AddRange([start, resume, stop]);
        var files = new Button { Text = "Experimentdateien öffnen", Width = 365, Height = 34 };
        files.Click += (_, _) => Process.Start(new ProcessStartInfo(runDirectory ?? storage) { UseShellExecute = true }); options.Controls.Add(files);
        L.SetToolTip(tips, files, "Konfiguration, Rundenprotokoll, gespeicherte Gewichte und Vergleichsbericht. Diese Dateien sind private lokale Trainingsdaten.");
        start.Click += async (_, _) => { if (mode.SelectedIndex == 3) await StartLab(); else await StartExperiment(false); };
        resume.Click += async (_, _) => await StartExperiment(true);
        stop.Click += (_, _) => RequestStop();
        mode.SelectedIndexChanged += (_, _) => { resume.Enabled = mode.SelectedIndex != 3 && process is null; };
        refresh.Tick += (_, _) => PaintLatest(); refresh.Start();
        FormClosing += async (_, e) =>
        {
            if (process is not null && !closing)
            {
                if (L.Message(this, "Experiment anhalten und zum Hauptmenü zurückkehren? Die letzte vollständig gespeicherte Runde bleibt erhalten.", "Engine-Labor", MessageBoxButtons.YesNo) != DialogResult.Yes) { e.Cancel = true; return; }
                e.Cancel = true; closing = true; RequestStop();
                var running = process;
                try { await running.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10)); }
                catch (TimeoutException) { try { running.Kill(entireProcessTree: true); } catch (InvalidOperationException) { } }
                Close();
            }
        };
        FormClosed += async (_, _) => { refresh.Dispose(); tips.Dispose(); if (lab is not null) await lab.DisposeAsync(); };
        board.SquareDragged += async (from, to) => await PlayLabMove(from, to);
        board.SquareClicked += async square =>
        {
            if (lab is null || board.Position is null) return;
            if (labSelected is int from && await PlayLabMove(from, square)) return;
            labSelected = square; board.SelectedSquare = square; board.CandidateMoves = MoveGenerator.GenerateLegal(board.Position).Where(m => m.From == square).ToArray(); board.Invalidate();
        };
        board.Position = Position.FromFen(Position.InitialFen);
    }
    private static ScrollNumber Number(int min, int max, int value) => new() { Minimum = min, Maximum = max, Value = value, Width = 180 };
    private void Row(string text, Control input, string description)
    {
        var row = new FlowLayoutPanel { Width = 380, Height = 43, WrapContents = false };
        var label = new Label { Text = text, Width = 182, Height = 40 };
        row.Controls.AddRange([label, input]); options.Controls.Add(row);
        L.SetToolTip(tips, input, description); L.SetToolTip(tips, label, description);
    }
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    { if (keyData == Keys.Escape) { Close(); return true; } return base.ProcessCmdKey(ref msg, keyData); }
}
