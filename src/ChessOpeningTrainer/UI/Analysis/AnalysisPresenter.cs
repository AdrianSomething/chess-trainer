namespace ChessOpeningTrainer;
// UI visibility is independent from search lifetime. Always request MultiPV 3
// so toggling either view consumes the same cached result.
public sealed class AnalysisPresenter : IDisposable
{
    private readonly StockfishEngine engine;
    private readonly EvalScale bar;
    private readonly VariationRows rows;
    private readonly Dictionary<string, EngineResult> cache = [];
    private CancellationTokenSource cancellation = new();
    private string? key;
    private EngineResult? current, painted;
    private bool running, done, disposed, wantLines;
    public int SearchesStarted { get; private set; }

    public AnalysisPresenter(StockfishEngine engine, EvalScale bar, VariationRows rows)
    {
        this.engine = engine;
        this.bar = bar;
        this.rows = rows;
    }

    public void ShowSaved(Position p, EngineResult result)
    {
        if (disposed)
            return;
        cancellation.Cancel();
        cancellation.Dispose();
        cancellation = new();
        key = p.ToFen();
        running = false;
        done = true;
        wantLines = true;
        bar.Visible = rows.Visible = true;
        current = new EngineResult
        {
            BestMove = result.BestMove,
            ScoreCentipawns = result.ScoreCentipawns,
            Mate = result.Mate,
            Lines = result.Lines.Take(3).Select((l, i) => new EngineLine { Rank = i + 1, Depth = l.Depth, BestMove = l.BestMove, ScoreCentipawns = l.ScoreCentipawns, Mate = l.Mate, Pv = [..l.Pv] }).ToList()
        };
        painted = null;
        Render(p);
    }

    public void Show(Position p, bool showBar, bool showLines, bool heavy = true, string? outcome = null, bool strong = true)
    {
        if (disposed)
            return;
        bar.Visible = showBar;
        rows.Visible = showLines;
        wantLines = showLines;
        string next = p.ToFen();
        if (next != key)
        {
            cancellation.Cancel();
            cancellation.Dispose();
            cancellation = new();
            key = next;
            running = false;
            done = false;
            painted = null;
            current = cache.GetValueOrDefault(next);
            if (current is null)
            {
                bar.Score = 0;
                bar.TextValue = "±0.00";
                bar.Invalidate();
                rows.Placeholder();
            }
            else
            {
                done = true;
                Render(p);
            }
        }

        if (outcome is not null && outcome is not ("" or "Schach"))
        {
            if (running)
            {
                cancellation.Cancel();
                running = false;
            }

            bool mate = outcome.Contains("matt", StringComparison.OrdinalIgnoreCase);
            bar.Score = mate ? (p.WhiteToMove ? -10000 : 10000) : 0;
            bar.TextValue = mate ? "Matt" : "0.00";
            bar.Invalidate();
            done = true;
            return;
        }

        if (current is not null)
            Render(p);
        if ((showBar || showLines) && !running && !done)
        {
            // A presenter can start from a constructor before a WinForms context
            // exists. Own a UI handle before any background result can arrive.
            _ = bar.Handle;
            running = true;
            SearchesStarted++;
            _ = Search(p.Clone(), cancellation.Token, heavy, strong);
        }
    }

    private void Render(Position p)
    {
        if (current is null)
            return;
        EvaluationPresentation.Show(bar, p, current);
        // Checkpoint restoration can clear the view without changing the FEN
        // or cached result. Repaint that result instead of rerunning the engine.
        if (wantLines && (painted != current || rows.Controls.Count == 0))
        {
            rows.ShowLines(p, current);
            painted = current;
        }
    }

    private async Task Search(Position p, CancellationToken token, bool heavy, bool strong)
    {
        string positionKey = p.ToFen();
        try
        {
            var result = await AnalysisSearch.Run(engine, p, 3, r =>
            {
                _ = OnUi(() =>
                {
                    if (disposed || token.IsCancellationRequested || key != positionKey)
                        return;
                    if (current is not null && (r.Lines.FirstOrDefault()?.Depth ?? 0) < (current.Lines.FirstOrDefault()?.Depth ?? 0))
                        return;
                    current = r;
                    cache[positionKey] = r;
                    Render(p);
                });
            }, token, strong, heavy);
            EngineResult? extension = null;
            await OnUi(() =>
            {
                if (token.IsCancellationRequested || disposed || key != positionKey)
                    return;
                done = result.Error is null;
                if (done && heavy) extension = current;
            });
            if (extension is not null && !token.IsCancellationRequested)
            {
                try
                {
                    await EngineServices.ExtendVariations(engine, p, extension, token);
                }
                catch (OperationCanceledException)
                {
                }

                await OnUi(() =>
                {
                    if (token.IsCancellationRequested || disposed || key != positionKey)
                        return;
                    painted = null;
                    Render(p);
                });
            }
        }
        catch (Exception)
        { /* Analysis is optional and never gates play. */
        }
        finally
        {
            await OnUi(() =>
            {
                if (disposed || token.IsCancellationRequested || key != positionKey)
                    return;
                running = false;
                if (cache.Count > 500)
                    cache.Remove(cache.Keys.First());
            });
        }
    }

    private Task OnUi(Action action)
    {
        if (disposed || bar.IsDisposed || bar.Disposing || !bar.IsHandleCreated)
            return Task.CompletedTask;
        if (bar.InvokeRequired) return bar.InvokeAsync(action);
        action();
        return Task.CompletedTask;
    }

    public void Suspend()
    {
        cancellation.Cancel();
        cancellation.Dispose();
        cancellation = new();
        running = false;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        cancellation.Cancel();
        cancellation.Dispose();
    }
}
