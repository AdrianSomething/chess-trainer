namespace ChessOpeningTrainer;

/// <summary>Read-only preview consumes immutable snapshots, never the generator's live graph.</summary>
public sealed class GeneratorPreviewForm : LocalizedForm
{
    private readonly GraphView graph = new() { Dock = DockStyle.Fill };
    private readonly ProgressBar progress = new GeneratorProgressBar() { Dock = DockStyle.Bottom, Height = 20, Maximum = 1000 };
    private readonly Label caption = new() { Dock = DockStyle.Bottom, Height = 78 };
    private readonly System.Windows.Forms.Timer clock = new() { Interval = 500 };
    private GeneratorForecast forecast;
    private readonly GeneratorWorkQueue? queue;
    private GeneratorJob? selectedJob;
    private readonly ProgressBar totalProgress = new GeneratorProgressBar { Dock = DockStyle.Bottom, Height = 20, Maximum = 1000, Visible = false };
    private readonly Label totalCaption = new() { Dock = DockStyle.Fill, Visible = false };
    private readonly PreviewArrow previous = new("<"), next = new(">");
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
    public GeneratorPreviewForm(GeneratorWorkQueue queue, bool followJobs) : this((GeneratorForecast?)null)
    {
        this.queue = queue;
        Controls.Remove(caption);
        var captions = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 108, ColumnCount = 2, RowCount = 1 };
        captions.ColumnStyles.Add(new(SizeType.Percent, 55)); captions.ColumnStyles.Add(new(SizeType.Percent, 45));
        caption.Dock = DockStyle.Fill; caption.Padding = totalCaption.Padding = new Padding(8, 4, 8, 4);
        captions.Controls.Add(caption, 0, 0); captions.Controls.Add(totalCaption, 1, 0);
        var bars = new Panel { Dock = DockStyle.Bottom, Height = 40 };
        Controls.Remove(progress); progress.Dock = DockStyle.Top; totalProgress.Dock = DockStyle.Bottom;
        bars.Controls.AddRange([progress, totalProgress]);
        Controls.Add(captions); Controls.Add(bars); graph.BringToFront();
        graph.Controls.AddRange([previous, next]);
        previous.Click += (_, _) => SelectJob(-1); next.Click += (_, _) => SelectJob(1);
        void ArrangeArrows() { previous.SetBounds(4, graph.Height / 2 - 36, 42, 72); next.SetBounds(graph.Width - 46, graph.Height / 2 - 36, 42, 72); }
        graph.Resize += (_, _) => ArrangeArrows(); ArrangeArrows();
        RefreshCaption();
    }
    private void SelectJob(int delta)
    {
        if (queue is null || queue.Jobs.Count == 0) return;
        var jobs = queue.Jobs; int index = selectedJob is null ? 0 : jobs.ToList().IndexOf(selectedJob);
        selectedJob = jobs[Math.Clamp(index + delta, 0, jobs.Count - 1)]; RefreshCaption();
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
        if (queue is not null)
        {
            var jobs = queue.Jobs;
            if (selectedJob is null || !jobs.Contains(selectedJob)) selectedJob = jobs.FirstOrDefault();
            if (selectedJob is null)
            {
                graph.Snapshot = null; graph.Invalidate(); progress.Value = totalProgress.Value = 0;
                previous.Visible = next.Visible = totalProgress.Visible = totalCaption.Visible = false;
                caption.Text = ""; searchDepth = null; return;
            }
            if (selectedJob is not null)
            {
                forecast = selectedJob.Forecast; searchDepth = selectedJob.SearchDepth;
                if (!ReferenceEquals(graph.Snapshot, selectedJob.Graph)) { graph.Snapshot = selectedJob.Graph; graph.Invalidate(); }
                progress.Value = Math.Clamp((int)(forecast.Progress * 1000), 0, 1000);
            }
            int index = selectedJob is null ? -1 : jobs.ToList().IndexOf(selectedJob);
            previous.Visible = index > 0; next.Visible = index >= 0 && index < jobs.Count - 1;
            totalProgress.Visible = totalCaption.Visible = jobs.Count > 1;
            long estimated = jobs.Sum(j => j.Forecast.EstimatedCards), completed = jobs.Sum(j => (long)j.Forecast.CompletedCards);
            totalProgress.Value = (int)Math.Clamp(1000d * completed / Math.Max(1, estimated), 0, 1000);
            var elapsed = queue.WorkElapsed;
            var totalRemaining = jobs.All(j => j.Forecast.Remaining is not null) ? Duration(TimeSpan.FromTicks(jobs.Sum(j => j.Forecast.Remaining!.Value.Ticks))) : L.T("noch unbekannt");
            totalCaption.Text = L.T($"Gesamt: {completed:N0} / ≈ {estimated:N0} Lernkarten · {totalProgress.Value / 10d:0.0}%") + "\n" + L.T($"Arbeitsdauer: {Duration(elapsed)} · Restzeit ≈ {totalRemaining}");
        }
        var snapshot = graph.Snapshot;
        string counts = snapshot is null ? $"Startprognose ≈ {forecast.EstimatedCards:N0} Lernkarten" : $"{snapshot.Cards:N0} / ≈ {forecast.EstimatedCards:N0} Lernkarten · {snapshot.Nodes.Length:N0} Knoten · {snapshot.Pending:N0} offen";
        string timing = forecast.Finished ? "Generierung abgeschlossen" : forecast.Stopped ? $"Generierung angehalten · Teilbaum · Dauer {Duration(forecast.Elapsed)}" : $"Fortschritt ≈ {progress.Value / 10d:0.0}% · Dauer {Duration(forecast.Elapsed)} · " +
            (forecast.Remaining is TimeSpan remaining ? $"Restzeit ≈ {Duration(remaining)} (laufende / frühere Generierungen)" : "Restzeit wird aus ersten Karten ermittelt");
        string search = searchDepth is null ? "Engine-Suche wird vorbereitet" : $"Engine-Suchtiefe {searchDepth.Achieved}/{searchDepth.Target} · Branch-Tiefe {searchDepth.TreeDepth}/{(searchDepth.MaxTreeDepth > 0 ? searchDepth.MaxTreeDepth : snapshot?.MaxDepth ?? 0)}";
        caption.Text = (selectedJob is null ? "" : selectedJob.Name + "\n") + L.T(counts) + "\n" + L.T(timing) + "\n" + L.T(search);
    }
    private sealed class PreviewArrow : Control
    {
        private bool hover;
        public PreviewArrow(string text)
        {
            Text = text; Cursor = Cursors.Hand;
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            BackColor = Color.Transparent;
            MouseEnter += (_, _) => { hover = true; Invalidate(); };
            MouseLeave += (_, _) => { hover = false; Invalidate(); };
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            if (hover) { using var background = new SolidBrush(Color.FromArgb(65, UiPalette.Gainsboro)); e.Graphics.FillRectangle(background, ClientRectangle); }
            using var ink = new SolidBrush(Color.FromArgb(180, UiPalette.Gainsboro));
            using var font = new Font("Segoe UI", 24);
            using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString(Text, font, ink, ClientRectangle, format);
        }
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
