namespace ChessOpeningTrainer;

public sealed partial class PuzzleBank
{
    private static int GenerationThreads => Math.Clamp(Preferences.Current.TacticGenerationThreads, 1, 8);
    private readonly SemaphoreSlim maintenance = new(1);
    private static readonly string[] lanes = ["Mate/Leicht", "Mate/Normal", "Mate/Schwer", "Material/Leicht", "Material/Normal", "Material/Schwer"];
    private long attempts, rejected, generated;
    private int working;
    public string Diagnostics
    {
        get
        {
            lock (sync) return $"Taktik-Arbeiter: {Volatile.Read(ref working)}/{Math.Clamp(Preferences.Current.TacticGenerationWorkers, 1, 8)} · Versuche: {Interlocked.Read(ref attempts)} · verworfen: {Interlocked.Read(ref rejected)} · neu: {Interlocked.Read(ref generated)}\n"
                + string.Join(" · ", Stock().Select(p => p.Key + ": " + p.Value))
                + (LastError is null ? "" : "\nLetzte Meldung: " + LastError);
        }
    }
    private readonly Dictionary<string, int> reservations = [];
    private readonly HashSet<string> preparationAttempted = [];
    private readonly System.Collections.Concurrent.ConcurrentQueue<SelfplayState> selfplayStates = new();
    private static string Lane(Puzzle p) => p.GoalKind == "Draw" ? "Draw" : (p.MateGoal is null ? "Material/" : "Mate/") + p.Difficulty;

    public IReadOnlyDictionary<string, int> Stock()
    {
        lock (sync)
            return lanes.ToDictionary(l => l, l => items.Count(p => p.LastPlayedUtc is null && Ready(p) && Lane(p) == l));
    }
    private static bool Ready(Puzzle p) => p.MateGoal is null || p.PreparedMateRoutes is { Count: > 0 };

    /// <summary>One lifetime-owned replenisher per bank; sleeps when stocked, never loops on the UI thread.</summary>
    public async Task MaintainAsync(CancellationToken token)
    {
        if (!await maintenance.WaitAsync(0, token)) return;
        try
        {
            while (!token.IsCancellationRequested)
            {
                await FillAsync(token);
                await Task.Delay(TimeSpan.FromSeconds(5), token);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        finally { maintenance.Release(); }
    }

    public Task FillAsync(CancellationToken token, int desired = 0) => Task.Run(async () =>
    {
        if (consumed.Error is not null || token.IsCancellationRequested || !await filling.WaitAsync(0)) return;
        try
        {
            int target = Math.Clamp(desired > 0 ? desired : Preferences.Current.TacticStockPerLane, 1, 50);
            List<Puzzle> prepare;
            lock (sync)
            {
                prepare = items.Where(p => !Ready(p) && !preparationAttempted.Contains(p.Id)).Take(2).ToList();
                foreach (var p in prepare) preparationAttempted.Add(p.Id);
            }
            int workers = Math.Clamp(Preferences.Current.TacticGenerationWorkers, 1, 8);
            await Task.WhenAll(Enumerable.Range(0, workers).Select(_ => RefillWorker(target, token))
                .Concat(prepare.Select(p => PrepareMate(p, token))));
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception ex) { LastError = ex.Message; }
        finally { filling.Release(); }
    }, CancellationToken.None);

    private async Task<bool> PrepareMate(Puzzle puzzle, CancellationToken token)
    {
        if (puzzle.MateGoal is null) return true;
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(token);
        budget.CancelAfter(TimeSpan.FromSeconds(15));
        try
        {
            var routes = await Task.Run(() => MatePreparation.Prepare(puzzle, budget.Token), budget.Token);
            lock (sync) { puzzle.PreparedMateRoutes = routes; Save(); }
            return true;
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested) { return false; }
        catch (InvalidOperationException) { return false; }
    }

    private async Task RefillWorker(int target, CancellationToken token)
    {
        var game = selfplayStates.TryDequeue(out var saved) ? saved : new SelfplayState();
        try
        {
        // Bounded batches re-read changed settings and avoid a permanently occupied bad lane.
        for (int attempt = 0; attempt < 12; attempt++)
        {
            token.ThrowIfCancellationRequested();
            string? lane;
            lock (sync)
            {
                var counts = Stock();
                lane = lanes.Where(l => counts[l] + reservations.GetValueOrDefault(l) < target)
                    .OrderBy(l => counts[l] + reservations.GetValueOrDefault(l)).ThenBy(_ => Random.Shared.Next()).FirstOrDefault();
                if (lane is null) return;
                reservations[lane] = reservations.GetValueOrDefault(lane) + 1;
            }
            try
            {
                Interlocked.Increment(ref working);
                Interlocked.Increment(ref attempts);
                Puzzle? puzzle;
                bool selfplay = attempt % 3 != 0;
                using var budget = CancellationTokenSource.CreateLinkedTokenSource(token);
                budget.CancelAfter(TimeSpan.FromSeconds(25));
                if (lane == "Mate/Leicht" && attempt % 2 == 0)
                    puzzle = await Task.Run(() => EasyMateComposer.Generate(budget.Token), budget.Token);
                else
                {
                    var candidate = selfplay ? await SelfplayCandidate(game, budget.Token) : Compose(lane.StartsWith("Mate/", StringComparison.Ordinal));
                    if (candidate is null) { Interlocked.Increment(ref rejected); continue; }
                    puzzle = await VerifyAsync(candidate, budget.Token);
                    if (puzzle is null || !await PrepareMate(puzzle, budget.Token)) { Interlocked.Increment(ref rejected); continue; }
                    puzzle.Origin = selfplay ? "Engine-Selbstspiel" : "Komposition";
                }
                lock (sync)
                {
                    string actualLane = Lane(puzzle);
                    if (!consumed.Contains(puzzle) && Stock().GetValueOrDefault(actualLane, target) < target &&
                        items.All(p => PuzzleConsumptionStore.Key(p) != PuzzleConsumptionStore.Key(puzzle)))
                    {
                        items.Add(puzzle);
                        Interlocked.Increment(ref generated);
                        Save();
                    }
                    else Interlocked.Increment(ref rejected);
                }
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested) { Interlocked.Increment(ref rejected); }
            catch (InvalidOperationException ex) { LastError = ex.Message; Interlocked.Increment(ref rejected); }
            catch (IOException ex) { LastError = ex.Message; Interlocked.Increment(ref rejected); }
            finally { Interlocked.Decrement(ref working); lock (sync) reservations[lane]--; }
            await Task.Delay(50, token);
        }
        }
        finally { selfplayStates.Enqueue(game); }
    }
}
