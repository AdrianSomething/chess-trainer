namespace ChessOpeningTrainer;

public sealed class DatasetTrimForm : LocalizedForm
{
    private readonly DatasetEntry entry;
    private readonly DatasetFilter filter = new();
    private readonly Label estimate = new() { Dock = DockStyle.Bottom, Height = 48, Padding = new(10) };
    private CancellationTokenSource? estimation;
    private readonly System.Windows.Forms.Timer debounce = new() { Interval = 180 };
    private readonly InputFocusCommit inputCommit;
    public DatasetFilter Filter => filter;
    public bool ReplaceOriginal
    {
        get; private set;
    }
    public DatasetTrimForm(DatasetEntry entry, StockfishEngine engine)
    {
        this.entry = entry;
        inputCommit = new(this);
        Text = "Datensatz zuschneiden";
        Size = new(580, 800);
        MinimumSize = new(550, 500);
        BackColor = UiPalette.FromArgb(31, 35, 39);
        ForeColor = UiPalette.Gainsboro;
        var layout = DatasetControls.Column();
        Controls.Add(layout);
        Controls.Add(estimate);
        var submit = DatasetControls.Button("Subdatensatz erstellen", () => { DialogResult = DialogResult.OK; Close(); });
        submit.Dock = DockStyle.Bottom;
        Controls.Add(submit);
        void Changed()
        {
            debounce.Stop();
            debounce.Start();
        }
        void Number(Control parent, string label, int value, int min, int max, Action<int> change, int step = 1) => parent.Controls.Add(DatasetControls.Number(label, value, min, max, n => { change((int)n); Changed(); }, step));
        FlowLayoutPanel Group(string title)
        {
            var outer = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Width = 495, Margin = new(0, 2, 0, 2) };
            var inner = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Width = 490 };
            var toggle = new Button { Text = "▾ " + L.T(title), Width = 480, Height = 27, FlatStyle = FlatStyle.Flat, BackColor = UiPalette.FromArgb(43, 49, 55), ForeColor = UiPalette.Gainsboro, Margin = new(0, 1, 0, 1), TextAlign = ContentAlignment.MiddleLeft };
            toggle.FlatAppearance.BorderSize = 0;
            toggle.Click += (_, _) => { inner.Visible = !inner.Visible; toggle.Text = (inner.Visible ? "▾ " : "▸ ") + L.T(title); };
            outer.Controls.AddRange([toggle, inner]);
            layout.Controls.Add(outer);
            return inner;
        }
        void Check(Control parent, string text, bool initial, Action<bool> change)
        {
            var c = new CheckBox { Text = text, Checked = initial, Width = 475, Height = 30 };
            c.CheckedChanged += (_, _) => { change(c.Checked); Changed(); };
            parent.Controls.Add(c);
        }
        var elo = Group("Spielstärke beider Spieler (Lichess: Glicko-2)");
        Number(elo, "Minimum", 0, 0, 3500, n => filter.MinElo = n, 100);
        Number(elo, "Maximum (3500 = offen)", 3500, 0, 3500, n => filter.MaxElo = n, 100);
        var endings = Group("Ergebnis und Spielende");
        foreach (var pair in new[] { ("Weiß gewinnt", "1-0"), ("Schwarz gewinnt", "0-1"), ("Remis", "1/2-1/2"), ("Unbekanntes Ergebnis", "*") })
            Check(endings, pair.Item1, true, v => { if (v) filter.Results.Add(pair.Item2); else filter.Results.Remove(pair.Item2); });
        foreach (var pair in new[] { ("Zeitüberschreitung", "Time forfeit"), ("Normal (Aufgabe / Einigung / unbekannt)", "Normal"), ("Erkennbare Endstellung", "Final position"), ("Abgebrochen", "Abandoned"), ("Regelverstoß (falls angegeben)", "Rules infraction"), ("Unbekannter Grund", "Unknown") })
            Check(endings, pair.Item1, true, v => { if (v) filter.Endings.Add(pair.Item2); else filter.Endings.Remove(pair.Item2); });
        endings.Controls.Add(new Label { Text = "Aufgabe, Remisvereinbarung und Sperren sind aus diesen PGN-Daten nicht zuverlässig einzeln bestimmbar.", Width = 475, Height = 48 });
        var clock = Group("Bedenkzeit");
        Check(clock, "Partien ohne Zeitangabe einschließen", true, v => filter.IncludeUntimed = v);
        Number(clock, "Grundzeit mindestens (s, 0 = offen)", 0, 0, 604800, n => filter.MinSeconds = n == 0 ? null : n);
        Number(clock, "Grundzeit höchstens (s, 0 = offen)", 0, 0, 604800, n => filter.MaxSeconds = n == 0 ? null : n);
        Number(clock, "Inkrement mindestens (s, 0 = offen)", 0, 0, 3600, n => filter.MinIncrement = n == 0 ? null : n);
        Number(clock, "Inkrement höchstens (s, 0 = offen)", 0, 0, 3600, n => filter.MaxIncrement = n == 0 ? null : n);
        var subset = Group("Zufällige Teilmenge");
        Number(subset, "Maximale Partien (0 = alle)", 0, 0, 1000000, n => filter.Limit = n == 0 ? null : n, 100);
        var sequence = Group("Zugfolge ab Grundstellung");
        var editor = new DatasetSequenceEditor(engine) { Visible = false };
        bool usePrefix = false;
        Check(sequence, "Nur Partien mit dieser Startzugfolge", false, v => { usePrefix = v; editor.Visible = v; filter.Prefix = v ? editor.Moves : []; });
        editor.Changed += () => { if (usePrefix) { filter.Prefix = editor.Moves; Changed(); } };
        sequence.Controls.Add(editor);
        Check(layout, "Originaldatensatz nach erfolgreichem Zuschneiden löschen", false, v => ReplaceOriginal = v);
        Number(layout, "Arbeiter: Zuschneiden", Preferences.Current.Datasets.FilterWorkers, 1, 32, n => { Preferences.Current.Datasets.FilterWorkers = n; Preferences.Save(); });
        debounce.Tick += async (_, _) => { debounce.Stop(); await Estimate(); };
        Shown += (_, _) => Changed();
    }
    private async Task Estimate()
    {
        estimation?.Cancel();
        var cts = new CancellationTokenSource();
        estimation = cts;
        var copy = System.Text.Json.JsonSerializer.Deserialize<DatasetFilter>(System.Text.Json.JsonSerializer.Serialize(filter))!;
        var sample = entry.Sample.ToArray();
        int workers = Math.Clamp(Preferences.Current.Datasets.EstimateWorkers, 1, 16);
        try
        {
            int count = await Task.Run(() => sample.AsParallel().WithDegreeOfParallelism(workers).WithCancellation(cts.Token).Count(copy.Matches), cts.Token);
            if (IsDisposed || cts.IsCancellationRequested)
                return;
            double ratio = sample.Length == 0 ? 0 : (double)count / sample.Length;
            long predicted = (long)(ratio * entry.Games);
            if (copy.Limit is long limit)
                predicted = Math.Min(predicted, limit);
            estimate.Text = L.T("Erwartete Partien") + $": {predicted:N0}";
        }
        catch (OperationCanceledException) { }
        finally { if (estimation == cts) estimation = null; cts.Dispose(); }
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            debounce.Dispose();
            inputCommit.Dispose();
            estimation?.Cancel();
        }
        base.Dispose(disposing);
    }
}
