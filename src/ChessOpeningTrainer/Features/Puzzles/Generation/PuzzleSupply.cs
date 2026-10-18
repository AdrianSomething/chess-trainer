namespace ChessOpeningTrainer;

// One app-owned supply is injected into all puzzle windows: no competing file caches.
public sealed class PuzzleSupply
{
    public PuzzleBank Bank { get; }
    public EndgameFactory Endgames { get; }
    public PositionalTrainer Positional { get; }
    public GenerationBudget Budget { get; } = new();
    public VisionComparisonStock Comparisons { get; }
    private readonly StockfishEngine engine;
    private readonly string directory, appDirectory;
    private readonly Dictionary<string, OpeningMistakeTrainer> openings = [];
    private readonly object sync = new();
    private int started;
    public PuzzleSupply(StockfishEngine engine, string directory, string appDirectory, PuzzleBank? bank = null)
    {
        this.engine = engine.ForGeneration(Budget);
        this.directory = directory; this.appDirectory = appDirectory;
        Bank = bank ?? new(directory, appDirectory, this.engine);
        Endgames = new(directory, this.engine);
        Positional = new(this.engine, directory);
        Comparisons = new(this.engine, directory, appDirectory);
    }
    public OpeningMistakeTrainer Opening(OpeningDefinition opening, bool all)
    {
        lock (sync)
        {
            string key = opening.Id + "|" + all;
            if (!openings.TryGetValue(key, out var trainer))
                openings[key] = trainer = new(engine, opening, directory, all);
            return trainer;
        }
    }
    public async Task Maintain(CancellationToken token)
    {
        if (Interlocked.Exchange(ref started, 1) != 0) return;
        try
        {
            _ = PawnTablebase.Shared;
            await Task.WhenAll(Bank.MaintainAsync(token), Comparisons.Maintain(token),
                Loop(() => Endgames.Fill(EndgameFactory.Kinds, token), token),
                Loop(async () => { await Positional.Fill(2, 4, token); await Positional.Fill(2, 4, token, true); }, token),
                Loop(async () =>
                {
                    var items = OpeningStore.Load(Path.Combine(appDirectory, "Openings"), out _);
                    foreach (var item in items.OrderBy(_ => Random.Shared.Next()))
                        foreach (bool all in new[] { false, true })
                        {
                            token.ThrowIfCancellationRequested();
                            await Opening(item, all).Fill(token);
                        }
                }, token));
        }
        finally { Interlocked.Exchange(ref started, 0); }
    }
    private static Task Loop(Func<Task> fill, CancellationToken token) => Task.Run(async () =>
    {
        while (!token.IsCancellationRequested)
        {
            try { await fill(); }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { break; }
            catch (Exception ex) when (ex is IOException or InvalidOperationException or OperationCanceledException) { }
            try { await Task.Delay(5000, token); }
            catch (OperationCanceledException) { break; }
        }
    }, CancellationToken.None);
}
