namespace ChessOpeningTrainer;
public static class AnalysisSearch
{
    private static readonly Dictionary<string, EnginePool> quickPools = [];
    // Owned by the existing analysis runtime; one GPU inference process is shared by board views.
    private static readonly NeuralEvaluationSession neural = new();
    internal static Task<EngineResult> RunNeural(SavedEngine model,Position position,int count,CancellationToken token)=>neural.Analyze(model,position,count,token);
    public static void Shutdown()
    {
        neural.Dispose();
        lock (quickPools)
        {
            foreach (var pool in quickPools.Values)
                pool.DisposeAsync().AsTask().GetAwaiter().GetResult();
            quickPools.Clear();
        }
    }

    public static void Warm(StockfishEngine engine)
    {
        lock (quickPools)
        {
            string key = engine.Path ?? "missing";
            if (!quickPools.ContainsKey(key))
                quickPools[key] = new EnginePool(engine, 1, 1, default);
        }
    }

    // Reports resume on the caller's captured context, when one exists. Views
    // must also handle context-free callers (see AnalysisPresenter.OnUi).
    // A late shallow result must never replace a deeper result from another worker.
    public static async Task<EngineResult> Run(StockfishEngine engine, Position position, int multiPv, Action<EngineResult> update, CancellationToken token, bool strong = true, bool includeHeavy = true)
    {
        if(EngineRegistry.Selected(engine) is {} model)
        { var result=await RunNeural(model,position,multiPv,token);if(result.Error is null)update(result);return result; }
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(token);
        budget.CancelAfter(TimeSpan.FromSeconds(10));
        int bestDepth = -1;
        EngineResult? best = null;
        string? error = null;
        async Task Search(int depth, int milliseconds, int threads, int hash, bool quick = false)
        {
            EngineResult result;
            try
            {
                if (quick)
                {
                    Warm(engine);
                    EnginePool pool;
                    lock (quickPools)
                        pool = quickPools[engine.Path ?? "missing"];
                    result = await pool.Search(position, depth, multiPv, budget.Token).WaitAsync(budget.Token);
                }
                else
                    result = await engine.AnalyzeAsync(position.ToFen(), depth, multiPv, threads, hash, budget.Token, milliseconds);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return;
            }

            if (token.IsCancellationRequested)
                return;
            if (result.Error is not null)
            {
                error = result.Error;
                return;
            }

            int reached = result.Lines.FirstOrDefault()?.Depth ?? 0;
            if (reached < bestDepth)
                return;
            bestDepth = reached;
            best = result;
            update(result);
        }

        var settings = Preferences.Current;
        int count = Math.Clamp(settings.EvaluationWorkers, 1, 3);
        var searches = new List<Task> {
            Search(Math.Clamp(settings.QuickEvaluationDepth, 1, 12), 25, 1, 64, true)
        };
        if (count >= 2)
            searches.Add(Search(Math.Clamp(settings.LightEvaluationDepth, 1, 30), 650, 1, 64));
        if (count >= 3 && includeHeavy)
            searches.Add(Search(Math.Clamp(strong ? settings.HeavyEvaluationDepth : Math.Min(18, settings.HeavyEvaluationDepth), 1, 40), strong ? 8000 : 1800, strong ? Math.Clamp(Environment.ProcessorCount - 2, 1, 6) : 2, 256));
        await Task.WhenAll(searches);
        return best ?? new EngineResult
        {
            Error = error ?? "Keine Engineantwort"
        };
    }
}
