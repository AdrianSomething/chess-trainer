namespace ChessOpeningTrainer;

public sealed class DatasetGraphView : UserControl
{
    private readonly string indexPath;
    private readonly StockfishEngine engine;
    private readonly BranchCanvas canvas;
    private readonly BoardControl board = new() { InputEnabled = false };
    private readonly EvalScale scale = new();
    private readonly VariationRows hiddenLines = new();
    private readonly AnalysisPresenter presenter;
    private readonly DatasetResultsStrip results = new();
    private readonly Panel right = new() { Dock = DockStyle.Right, Width = 380, Visible = false };
    private readonly Label status = new() { Dock = DockStyle.Bottom, Height = 30 };
    private readonly Dictionary<int, string> expanded = [];
    private DatasetGraphModel? model;
    private BranchVertex? selected;
    private CancellationTokenSource? load;
    public event Action? BackRequested;
    public DatasetGraphView(string indexPath, StockfishEngine engine)
    {
        this.indexPath = indexPath;
        this.engine = engine;
        presenter = new(engine, scale, hiddenLines);
        canvas = new(new OpeningDefinition(), new ProgressStore(Path.GetDirectoryName(indexPath)!), null, deferBuild: true);
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48 };
        var back = new MenuIconButton("back") { Width = 40, Height = 38, AccessibleName = "Zurück" };
        back.Click += (_, _) => BackRequested?.Invoke();
        top.Controls.AddRange([back, DatasetControls.Button("Gesamter Graph", canvas.Fit, 160), new Label { Text = "Ziehen: Kamera · Mausrad: Zoom", AutoSize = true, Margin = new(12) }]);
        Controls.AddRange([canvas, right, top, status]);
        right.Controls.AddRange([board, scale, results]);
        board.SetBounds(45, 10, 325, 325);
        scale.SetBounds(2, 30, 38, 285);
        results.SetBounds(10, 350, 355, 65);
        var free = DatasetControls.Button("Im Freeplaymodus öffnen", () => { if (selected is null) return; var form = new FreeGameForm(engine, []); form.PlayFrom(selected.Position.Clone(), selected.Position.WhiteToMove); form.Show(); }, 355);
        free.SetBounds(10, 430, 355, 42);
        var generator = DatasetControls.Button("In Opening-Branch-Generator öffnen", () => { if (selected is null || model is null) return; var form = new OpeningGeneratorForm(engine, AppContext.BaseDirectory); form.PrepareSequence(model.Paths[selected]); form.Show(); }, 355);
        generator.SetBounds(10, 480, 355, 42);
        right.Controls.AddRange([free, generator]);
        canvas.EdgeWeight = (a, b) => model is null ? 1 : 1 + 4 * (float)(model.Frequencies.GetValueOrDefault((a, b)) / (double)Math.Max(1, a.Children.Sum(c => model.Frequencies.GetValueOrDefault((a, c)))));
        canvas.NodeBorder = v =>
        {
            var c = model?.Counts.GetValueOrDefault(v) ?? default;
            if (c.White == c.Black)
                return (Color.Gray, 1);
            double confidence = Math.Abs(c.White - c.Black) / (double)Math.Max(1, c.Total) * Math.Min(1, Math.Sqrt(c.Total / 30d));
            return (c.White > c.Black ? Color.White : Color.Black, 1 + (float)confidence * 3);
        };
        canvas.VertexSelected += v => { selected = v; right.Visible = true; BoardPresentation.Preview(board, v.Position, false); results.Counts = model!.Counts[v]; presenter.Show(v.Position, true, false, true, ""); };
        canvas.SelectionCleared += () => { selected = null; right.Visible = false; };
        canvas.ToggleSubbranch += v =>
        {
            if (model is null)
                return;
            string path = string.Join(' ', model.Paths[v]);
            bool wasOpen = expanded.GetValueOrDefault(v.Depth) == path;
            foreach (int depth in expanded.Keys.Where(d => d >= v.Depth).ToArray())
                expanded.Remove(depth);
            if (!wasOpen)
                expanded[v.Depth] = path;
            _ = Reload();
        };
        Load += async (_, _) => await Reload();
    }
    private async Task Reload()
    {
        load?.Cancel();
        var cts = new CancellationTokenSource();
        load = cts;
        var snapshot = new Dictionary<int, string>(expanded);
        status.Text = L.T("Datensatzbaum wird geladen …");
        canvas.Enabled = false;
        try
        {
            var built = await Task.Run(() => DatasetGraphModel.Build(indexPath, snapshot, cts.Token), cts.Token);
            if (IsDisposed || cts.IsCancellationRequested)
                return;
            bool first = model is null;
            model = built;
            selected = null;
            right.Visible = false;
            canvas.ClearSelection();
            canvas.Vertices.Clear();
            canvas.Vertices.AddRange(built.Vertices);
            if (first)
                canvas.Fit();
            canvas.Invalidate();
            status.Text = L.T("Häufigkeiten enthalten Transpositionen; wiederholte Stellungen zählen pro Partie einmal.");
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { if (!IsDisposed) status.Text = ex.GetBaseException().Message; }
        finally { if (load == cts) { load = null; if (!IsDisposed) canvas.Enabled = true; } cts.Dispose(); }
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            load?.Cancel();
            presenter.Dispose();
            hiddenLines.Dispose();
        }
        base.Dispose(disposing);
    }
}
