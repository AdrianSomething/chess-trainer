namespace ChessOpeningTrainer;

/// <summary>Read-only preview consumes immutable snapshots, never the generator's live graph.</summary>
public sealed class GeneratorPreviewForm : LocalizedForm
{
    private readonly GraphView graph = new() { Dock = DockStyle.Fill };
    private readonly ProgressBar progress = new GeneratorProgressBar() { Dock = DockStyle.Bottom, Height = 20, Maximum = 1000 };
    private readonly Label caption = new() { Dock = DockStyle.Bottom, Height = 78 };
    private readonly System.Windows.Forms.Timer clock = new() { Interval = 500 };
    private readonly GeneratorForecast forecast;
    private GeneratorSearchDepth? searchDepth;
    public GeneratorPreviewForm(GeneratorForecast? forecast = null)
    {
        this.forecast = forecast ?? new(200);
        Text = "Wachsender Opening-Branch"; Size = new(850, 600);
        MinimumSize = new(400, 260);
        BackColor = UiPalette.FromArgb(31, 35, 39); ForeColor = UiPalette.Gainsboro;
        Controls.AddRange([graph, caption, progress]);
        clock.Tick += (_, _) => RefreshCaption();
        clock.Start();
        RefreshCaption();
    }
    public void UpdateGraph(GeneratorGraphSnapshot snapshot)
    {
        if (IsDisposed) return;
        graph.Snapshot = snapshot; graph.Invalidate();
        forecast.Update(snapshot);
        progress.Value = (int)(forecast.Progress * 1000);
        progress.Invalidate();
        if (snapshot.Stopped || snapshot.Finished) clock.Stop();
        RefreshCaption();
    }
    public void UpdateSearchDepth(GeneratorSearchDepth value)
    {
        if (IsDisposed) return;
        searchDepth = value;
        RefreshCaption();
    }
    public void Stop()
    {
        if (IsDisposed) return;
        forecast.Stop();
        clock.Stop();
        RefreshCaption();
    }
    private static string Duration(TimeSpan duration) => duration.TotalHours >= 1 ? $"{duration.TotalHours:0.0} h" : duration.TotalMinutes >= 1 ? $"{duration.TotalMinutes:0} min" : $"{duration.TotalSeconds:0} s";
    private void RefreshCaption()
    {
        var snapshot = graph.Snapshot;
        string counts = snapshot is null ? $"Startprognose ≈ {forecast.EstimatedCards:N0} Lernkarten" : $"{snapshot.Cards:N0} / ≈ {forecast.EstimatedCards:N0} Lernkarten · {snapshot.Nodes.Length:N0} Knoten · {snapshot.Pending:N0} offen";
        string timing = forecast.Finished ? "Generierung abgeschlossen" : forecast.Stopped ? $"Generierung angehalten · Teilbaum · Dauer {Duration(forecast.Elapsed)}" : $"Fortschritt ≈ {progress.Value / 10d:0.0}% · Dauer {Duration(forecast.Elapsed)} · " +
            (forecast.Remaining is TimeSpan remaining ? $"Restzeit ≈ {Duration(remaining)} (laufende / frühere Generierungen)" : "Restzeit wird aus ersten Karten ermittelt");
        string search = searchDepth is null ? "Engine-Suche wird vorbereitet" : $"Engine-Suchtiefe {searchDepth.Achieved}/{searchDepth.Target} · Branch-Tiefe {searchDepth.TreeDepth}/{(searchDepth.MaxTreeDepth > 0 ? searchDepth.MaxTreeDepth : snapshot?.MaxDepth ?? 0)}";
        caption.Text = counts + "\n" + timing + "\n" + search;
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) clock.Dispose();
        base.Dispose(disposing);
    }
    private sealed class GraphView : Control
    {
        public GeneratorGraphSnapshot? Snapshot { get; set; }
        public GraphView() { DoubleBuffered = true; SetStyle(ControlStyles.ResizeRedraw, true); }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(UiPalette.FromArgb(29, 33, 38));
            if (Snapshot is not { } data || data.Nodes.Length == 0) return;
            data = GeneratorPreviewGraph.Visible(data);
            var ends = GeneratorPreviewGraph.Ends(data);
            var points = GeneratorGraphLayout.Points(data, ClientSize);
            using var edge = new Pen(UiPalette.FromArgb(80, 102, 119));
            foreach (var link in data.Edges)
                if (points.TryGetValue(link.From, out var a) && points.TryGetValue(link.To, out var b)) e.Graphics.DrawLine(edge, a, b);
            using var dot = new SolidBrush(UiPalette.FromArgb(110, 183, 150));
            using var finished = new SolidBrush(UiPalette.FromArgb(213, 84, 85));
            foreach (var (id, p) in points) e.Graphics.FillEllipse(ends.Contains(id) ? finished : dot, p.X - 2, p.Y - 2, 4, 4);
        }
    }
}
