using System.Collections.Concurrent;

namespace ChessOpeningTrainer;

/// <summary>Persistent per-game workers analyze each newly recorded position once.</summary>
internal sealed class LiveGameAnalysis : IAsyncDisposable
{
    private readonly EnginePool pool;
    private readonly GameAnalysisOptions options;
    private readonly CancellationToken token;
    private readonly IProgress<string> progress;
    private readonly ConcurrentDictionary<string, Task<EngineResult>> pending = new(StringComparer.Ordinal);
    private int completed;
    public LiveGameAnalysis(StockfishEngine engine, IProgress<string> progress, CancellationToken token)
    {
        options = Preferences.Current.GameAnalysis.Copy(); options.Validate();
        this.progress = progress; this.token = token;
        pool = new(engine, options.Workers, options.Threads, token);
    }
    public void Enqueue(Position position)
    {
        string fen = position.ToFen();
        pending.GetOrAdd(fen, _ => Search(position.Clone()));
    }
    private async Task<EngineResult> Search(Position position)
    {
        var result = await pool.Search(position, options.Depth).WaitAsync(token);
        if (result.Error is not null) throw new IOException(result.Error);
        progress.Report($"Laufende Partieanalyse: {Interlocked.Increment(ref completed)}/{pending.Count} Stellungen · Tiefe {options.Depth}");
        return result;
    }
    public async Task<IReadOnlyDictionary<string, EngineResult>> Finish(CancellationToken cancellation)
    {
        var snapshot = pending.ToArray();
        await Task.WhenAll(snapshot.Select(p => p.Value)).WaitAsync(cancellation);
        return snapshot.ToDictionary(p => p.Key, p => p.Value.Result, StringComparer.Ordinal);
    }
    public ValueTask DisposeAsync() => pool.DisposeAsync();
}
