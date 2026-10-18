using System.Text.Json;

namespace ChessOpeningTrainer;

/// <summary>App-owned, persisted stock. A session reserves 30 distinct exercises before its clock starts.</summary>
public sealed class VisionComparisonStock
{
    public sealed record Entry(string Fen, string[] Moves, int BetterIndex, double Gap, double[]? ScoresWhite = null)
    {
        [System.Text.Json.Serialization.JsonIgnore]
        public string Key => OpeningStore.PositionKey(Position.FromFen(Fen));
        public VisionComparison Exercise()
        {
            var p = Position.FromFen(Fen); var legal = MoveGenerator.GenerateLegal(p);
            return new(p, Moves.Select(u => legal.Single(m => m.Uci == u)).ToArray(), BetterIndex, Gap, ScoresWhite);
        }
        public static Entry From(VisionComparison x) => new(x.Before.ToFen(), x.Moves.Select(m => m.Uci).ToArray(), x.BetterIndex, x.Gap, x.ScoresWhite);
    }
    public sealed class State
    {
        public List<Entry> Ready { get; set; } = [];
        public HashSet<string> Used { get; set; } = [];
        public List<Entry> Reserved { get; set; } = [];
    }
    private readonly string file;
    private readonly VisionComparisonGenerator generator;
    private readonly object sync = new();
    private readonly State state;
    private int running;
    private int preparingWhite, preparingBlack;
    private sealed record EntryQuality(bool Suitable, int Priority);
    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<Entry, EntryQuality> quality = new();
    public GenerationBudget Budget { get; } = new(() => Preferences.Current.VisionGenerationWorkers, () => Preferences.Current.VisionGenerationThreads);
    public string? LastError { get; private set; }
    private EntryQuality Quality(Entry item) => quality.GetValue(item, entry =>
    {
        try { var exercise = entry.Exercise(); return new(VisionComparisonQuality.Suitable(exercise), VisionComparisonQuality.Priority(exercise)); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or FormatException) { return new(false, int.MaxValue); }
    });
    private bool Suitable(Entry item) => Quality(item).Suitable;
    public int Count { get { lock (sync) return state.Ready.Count(Suitable); } }
    public VisionComparisonStock(StockfishEngine engine, string directory, string? appDirectory = null)
    {
        file = Path.Combine(directory, "vision-comparison-stock.json");
        generator = new(engine.ForGeneration(Budget));
        try { state = JsonSerializer.Deserialize<State>(File.ReadAllText(file), OpeningStore.Json) ?? new(); }
        catch (Exception ex) when (ex is IOException or JsonException) { state = new(); }
        // Reservations from an interrupted app run were never shown and remain usable.
        foreach (var item in state.Reserved)
            if (!state.Used.Contains(item.Key) && state.Ready.All(p => p.Key != item.Key)) state.Ready.Add(item);
        state.Reserved.Clear();
        foreach (string asset in new[] { "vision-comparison-v21.json", "vision-comparison-v14.json" })
            try
            {
                string path = Path.Combine(appDirectory ?? AppContext.BaseDirectory, "Puzzles", asset);
                if (!File.Exists(path)) continue;
                foreach (var seed in JsonSerializer.Deserialize<List<Entry>>(File.ReadAllText(path), OpeningStore.Json) ?? [])
                    if (!state.Used.Contains(seed.Key) && state.Ready.All(p => p.Key != seed.Key)) { seed.Exercise(); state.Ready.Add(seed); }
            }
            catch (Exception ex) when (ex is IOException or JsonException or InvalidOperationException) { LastError = ex.Message; }
        state.Ready.RemoveAll(p => state.Used.Contains(p.Key));
    }
    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file + ".tmp", JsonSerializer.Serialize(state));
        File.Move(file + ".tmp", file, true);
    }
    public Task Maintain(CancellationToken token) => Task.Run(async () =>
    {
        if (Interlocked.Exchange(ref running, 1) != 0) return;
        try
        {
            await Task.WhenAll(Enumerable.Range(0, 8).Select(worker => Fill(worker, token)));
        }
        finally { Interlocked.Exchange(ref running, 0); }
    }, CancellationToken.None);
    private async Task Fill(int worker, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                if (worker >= Budget.Workers) { await Task.Delay(250, token); continue; }
                bool? side;
                lock (sync)
                {
                    int white = state.Ready.Count(p => Suitable(p) && Position.FromFen(p.Fen).WhiteToMove) + preparingWhite;
                    int black = state.Ready.Count(p => Suitable(p) && !Position.FromFen(p.Fen).WhiteToMove) + preparingBlack;
                    side = white >= 15 && black >= 15 ? null : white <= black;
                    if (side == true) preparingWhite++; else if (side == false) preparingBlack++;
                }
                if (side is null) { await Task.Delay(1000, token); continue; }
                try
                {
                    var item = Entry.From(await generator.Generate(side.Value, token));
                    lock (sync)
                        if (!state.Used.Contains(item.Key) && state.Ready.All(p => p.Key != item.Key) && state.Reserved.All(p => p.Key != item.Key)) { state.Ready.Add(item); Save(); }
                }
                catch (Exception ex) when (ex is IOException or InvalidOperationException or OperationCanceledException)
                {
                    if (token.IsCancellationRequested) break;
                    LastError = ex.Message; await Task.Delay(2000, token);
                }
                finally { lock (sync) { if (side.Value) preparingWhite--; else preparingBlack--; } }
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
    }
    public async Task<List<VisionComparison>> TakeRun(CancellationToken token, Action<int>? progress = null)
    {
        while (true)
        {
            token.ThrowIfCancellationRequested();
            lock (sync)
            {
                var eligible = state.Ready.Where(Suitable).ToArray();
                var white = eligible.Where(p => Position.FromFen(p.Fen).WhiteToMove).OrderBy(p => Quality(p).Priority).ThenBy(_ => Random.Shared.Next()).Take(15).ToArray();
                var black = eligible.Where(p => !Position.FromFen(p.Fen).WhiteToMove).OrderBy(p => Quality(p).Priority).ThenBy(_ => Random.Shared.Next()).Take(15).ToArray();
                if (white.Length == 15 && black.Length == 15)
                {
                    var entries = white.Zip(black).SelectMany(pair => new[] { pair.First, pair.Second }).ToList();
                    foreach (var p in entries) { state.Ready.Remove(p); state.Reserved.Add(p); }
                    Save(); return entries.Select(p => p.Exercise()).ToList();
                }
            }
            progress?.Invoke(Count);
            await Task.Delay(400, token);
        }
    }
    public void MarkViewed(VisionComparison exercise)
    {
        string key = Entry.From(exercise).Key;
        lock (sync)
        {
            state.Reserved.RemoveAll(p => p.Key == key);
            if (state.Used.Add(key)) SaveSessionChange();
        }
    }
    public void ReturnUnviewed(IEnumerable<VisionComparison> exercises)
    {
        lock (sync)
        {
            bool changed = false;
            foreach (var exercise in exercises)
            {
                var item = Entry.From(exercise);
                if (state.Reserved.RemoveAll(p => p.Key == item.Key) == 0 || state.Used.Contains(item.Key)) continue;
                if (state.Ready.All(p => p.Key != item.Key)) state.Ready.Add(item);
                changed = true;
            }
            if (changed) SaveSessionChange();
        }
    }
    private void SaveSessionChange()
    {
        try { Save(); }
        catch (IOException ex) { LastError = ex.Message; }
    }
}
