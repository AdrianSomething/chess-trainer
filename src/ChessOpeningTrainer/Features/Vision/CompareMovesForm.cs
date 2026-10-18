namespace ChessOpeningTrainer;

public sealed class CompareMovesForm : VisionChallengeForm
{
    private readonly VisionChoiceBoard[] boards = [new() { InputEnabled = false }, new() { InputEnabled = false }];
    private readonly VisionComparisonEvaluation[] evaluations = [new() { Visible = false }, new() { Visible = false }];
    private readonly CheckBox original = new() { Visible = false };
    private readonly Button originalButton = new() { Text = "Ausgangsstellung", AutoSize = true };
    private readonly Button resultsButton = new() { Text = "Ergebnisse", AutoSize = true };
    private readonly Button[] freeplay = [new() { Text = "In Freies Spiel öffnen", Visible = false }, new() { Text = "In Freies Spiel öffnen", Visible = false }];
    private Position before = new();
    private Position[] after = [];
    private Move[] moves = [];
    private int correct;
    private double gap;
    private bool animating;
    private bool answerRevealed;
    private readonly StockfishEngine engine;
    private readonly StockfishEngine playingEngine;
    private readonly VisionComparisonStock stock;
    private List<VisionComparison>? run;
    private int viewedCount, evaluationRevision;
    public CompareMovesForm(string directory, StockfishEngine engine, VisionComparisonStock? stock = null) : base(directory, 4, "Zugvergleich",
        "Vergleiche die beiden Züge. Welche Stellung ist für die Seite am Zug besser?")
    {
        this.stock = stock ?? new(engine, directory);
        playingEngine = engine;
        this.engine = engine.ForGeneration(this.stock.Budget);
        if (stock is null) _ = this.stock.Maintain(Lifetime.Token);
        var tools = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 38, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Padding = new(0, 0, 12, 0) };
        tools.Controls.AddRange([resultsButton, originalButton]);
        Controls.Add(tools); Controls.SetChildIndex(tools, Controls.GetChildIndex(Content) + 1);
        originalButton.Click += (_, _) => { if (!animating) original.Checked = true; };
        resultsButton.Click += (_, _) => { if (!animating) original.Checked = false; };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        layout.ColumnStyles.Add(new(SizeType.Percent, 50)); layout.ColumnStyles.Add(new(SizeType.Percent, 50));
        layout.RowStyles.Add(new(SizeType.Percent, 100));
        for (int i = 0; i < 2; i++)
        {
            int index = i;
            var host = new Panel { Dock = DockStyle.Fill }; layout.Controls.Add(host, i, 0);
            host.Controls.AddRange([boards[i], evaluations[i], freeplay[i]]);
            void Arrange()
            {
                int height = Math.Max(1, Math.Min(host.ClientSize.Height - 72, host.ClientSize.Width + 28));
                boards[index].SetBounds(0, 0, host.ClientSize.Width, height);
                var area = boards[index].PlayingBounds;
                evaluations[index].SetBounds((int)area.Left, boards[index].Bottom + 1, (int)area.Width, 34);
                freeplay[index].SetBounds(Math.Max(4, (host.Width - 210) / 2), evaluations[index].Bottom + 3, 210, 30);
            }
            host.Resize += (_, _) => Arrange(); Arrange();
            freeplay[i].Click += (_, _) =>
            {
                if (!answerRevealed || after.Length != 2) return;
                var game = new FreeGameForm(playingEngine, []); game.Show(); game.PlayFrom(after[index].Clone(), before.WhiteToMove);
            };
            boards[i].MouseClick += (_, _) =>
            {
                if (!CanAnswer || animating) return;
                answerRevealed = true;
                original.Checked = false;
                for (int b = 0; b < 2; b++)
                {
                    boards[b].LockChoice(b == index, b != correct);
                    freeplay[b].Visible = true;
                }
                RefreshComparisonView();
                if (index != correct)
                {
                    Statistics.Miss(4, before.Squares[moves[index].From], "Schlechterer Vergleichszug");
                }
                Finish(index == correct ? 0 : 1, index == correct ? 1 : 0,
                    $"{(correct == 0 ? "Links" : "Rechts")} ist besser: {SanNotation.Format(before, moves[correct])}. Unterschied {gap:0.00} Bauern laut Engine.");
            };
        }
        original.CheckedChanged += (_, _) =>
        {
            if (after.Length != 2 || animating) return;
            RefreshComparisonView();
        };
        AddView(layout);
    }
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if(!animating && after.Length==2 && keyData is Keys.Left or Keys.Right)
        {
            original.Checked=keyData==Keys.Left;
            return true;
        }
        return base.ProcessCmdKey(ref msg,keyData);
    }
    protected override async Task Prepare()
    {
        var reserved = run ?? await stock.TakeRun(Lifetime.Token, count =>
        {
            if (!IsDisposed && IsHandleCreated) BeginInvoke(() => Status.Text = $"Zugvergleich: {count}/30 Aufgaben vorbereitet. Die Uhr startet erst mit der fertigen Runde.");
        });
        if (Lifetime.IsCancellationRequested || IsDisposed) { stock.ReturnUnviewed(reserved); Lifetime.Token.ThrowIfCancellationRequested(); return; }
        run = reserved;
        var exercise = run[Round];
        int version = ++evaluationRevision;
        await this.InvokeAsync(() =>
        {
            before = exercise.Before; moves = exercise.Moves; correct = exercise.BetterIndex; gap = exercise.Gap;
            answerRevealed = false;
            after = moves.Select(m => { var p = before.Clone(); p.Apply(m); return p; }).ToArray();
            original.Checked = false;
            for (int i = 0; i < 2; i++) { boards[i].ResetChoice(); freeplay[i].Visible = false; evaluations[i].Visible = false; evaluations[i].ScoreWhite = exercise.ScoresWhite is { Length: 2 } ? exercise.ScoresWhite[i] : null; }
            for (int i = 0; i < 2; i++) BoardPresentation.Preview(boards[i], before, !before.WhiteToMove);
        }, Lifetime.Token);
        if (exercise.ScoresWhite is not { Length: 2 }) _ = PrepareEvaluations(version);
    }
    protected override async void OnStarted()
    {
        animating = true; original.Enabled = originalButton.Enabled = resultsButton.Enabled = false;
        MarkCurrentViewed();
        try { await Task.WhenAll(Enumerable.Range(0, 2).Select(i => boards[i].AnimateAsync(before, moves[i], after[i]))); }
        finally { animating = false; if (!IsDisposed) { original.Enabled = originalButton.Enabled = resultsButton.Enabled = true; RefreshComparisonView(); } }
    }
    private void RefreshComparisonView()
    {
        if (after.Length != 2) return;
        originalButton.BackColor = UiPalette.FromArgb(original.Checked ? 57 : 43, original.Checked ? 91 : 49, original.Checked ? 119 : 55);
        resultsButton.BackColor = UiPalette.FromArgb(!original.Checked ? 57 : 43, !original.Checked ? 91 : 49, !original.Checked ? 119 : 55);
        for (int i = 0; i < 2; i++)
        {
            BoardPresentation.Preview(boards[i], original.Checked ? before : after[i], !before.WhiteToMove);
            boards[i].HintMove = original.Checked ? null : moves[i];
            boards[i].HintColor = answerRevealed && i != correct ? Color.FromArgb(220, 45, 55) : null;
            boards[i].ShowVerdict = answerRevealed && !original.Checked;
            evaluations[i].Visible = answerRevealed && !original.Checked; boards[i].Invalidate();
        }
    }
    private void MarkCurrentViewed()
    {
        if (!CanAnswer || run is null || Round >= run.Count || viewedCount > Round) return;
        stock.MarkViewed(run[Round]); viewedCount = Round + 1;
    }
    protected override void OnResumed() => MarkCurrentViewed();
    private async Task PrepareEvaluations(int version)
    {
        var positions = after.Select(p => p.Clone()).ToArray();
        int round = Round;
        try
        {
            await Task.WhenAll(Enumerable.Range(0, 2).Select(async index =>
            {
                var result = await engine.AnalyzeAsync(positions[index].ToFen(), 18, 1, 1, 128, Lifetime.Token, 1500);
                if (result.Error is not null || Lifetime.IsCancellationRequested) return;
                double score = positions[index].WhiteToMove ? 1 : -1;
                score *= result.Mate is int mate ? Math.Sign(mate) * 100 : (result.ScoreCentipawns ?? 0) / 100d;
                await this.InvokeAsync(() =>
                {
                    if (version != evaluationRevision) return;
                    evaluations[index].ScoreWhite = score; evaluations[index].Invalidate();
                    if (run is not null && round < run.Count && evaluations.All(e => e.ScoreWhite is not null))
                        run[round] = run[round] with { ScoresWhite = evaluations.Select(e => e.ScoreWhite!.Value).ToArray() };
                    // A prepared but still hidden position returns with its verified scores on abort.
                }, Lifetime.Token);
            }));
        }
        catch (OperationCanceledException) { }
        catch (InvalidOperationException) when (IsDisposed || Disposing) { }
    }
    protected override void DisposeTrainingResources()
    {
        evaluationRevision++;
        if (run is not null) stock.ReturnUnviewed(run.Skip(viewedCount));
    }
}
