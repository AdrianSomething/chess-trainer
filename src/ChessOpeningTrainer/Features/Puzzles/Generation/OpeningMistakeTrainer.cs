using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ChessOpeningTrainer;
// Never changes the repertoire: candidates come exclusively from omitted opponent moves.
public sealed class OpeningMistakeTrainer
{
    private readonly StockfishEngine engine;
    private readonly OpeningDefinition opening;
    private readonly string file;
    private readonly PuzzleConsumptionStore consumed;
    private readonly bool anyDeviation;
    private readonly SemaphoreSlim gate = new(1);
    private readonly List<Puzzle> ready = [];
    private readonly object cacheSync = new();
    private readonly HashSet<string> recent = [];
    public event Action<string>? Progress;
    public static string CacheKey(OpeningDefinition opening, bool anyDeviation)
    {
        string source = opening.Id + "|" + string.Join(";", GraphOperations.Positions(opening).Select(w => w.Position.ToFen() + ":" + string.Join(",", w.Node.Moves.Select(m => m.Uci))));
        return "opening-errors-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)))[..20] + (anyDeviation ? "-deviations" : "") + ".json";
    }
    public OpeningMistakeTrainer(StockfishEngine engine, OpeningDefinition opening, string directory, bool anyDeviation = false)
    {
        this.engine = engine;
        this.opening = opening;
        this.anyDeviation = anyDeviation;
        file = Path.Combine(directory, CacheKey(opening, false));
        consumed = new(file + ".consumed.json");
        if (anyDeviation) file = file.Replace(".json", "-deviations.json");
        try
        {
            ready = JsonSerializer.Deserialize<List<Puzzle>>(File.ReadAllText(file), OpeningStore.Json) ?? [];
        }
        catch (IOException)
        {
        }
        catch (JsonException)
        {
        }
        try
        {
            var seeds = JsonSerializer.Deserialize<List<OpeningPuzzleSeed>>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Puzzles", "opening-seeds-v12.json")), OpeningStore.Json) ?? [];
            foreach (var seed in seeds.Where(s => s.CacheKey == Path.GetFileName(file)))
                if (!consumed.Contains(seed.Puzzle) && ready.All(p => PuzzleConsumptionStore.Key(p) != PuzzleConsumptionStore.Key(seed.Puzzle))) ready.Add(seed.Puzzle);
        }
        catch (IOException) { }
        catch (JsonException) { }
        ready.RemoveAll(p => consumed.Contains(p) || p.SetupFen is null);
    }

    private Puzzle? Take()
    {
        lock (cacheSync)
        {
            if (ready.Count == 0) return null;
            int i = Random.Shared.Next(ready.Count);
            var puzzle = ready[i];
            consumed.Record([puzzle]); ready.RemoveAt(i); Save();
            return puzzle;
        }
    }
    public async Task<Puzzle> Next(CancellationToken token)
    {
        // Cached tasks must never wait behind an in-progress refill.
        if (Take() is { } cached) return cached;
        await gate.WaitAsync(token);
        try
        {
            if (Take() is { } prepared) return prepared;
            var puzzle = await Generate(token);
            consumed.Record([puzzle]);
            return puzzle;
        }
        finally { gate.Release(); }
    }

    public async Task Fill(CancellationToken token)
    {
        if (!await gate.WaitAsync(0, token))
            return;
        try
        {
            bool needed;
            lock (cacheSync) needed = ready.Count < Math.Clamp(Preferences.Current.TacticStockPerLane, 1, 50);
            if (needed)
            {
                var puzzle = await Generate(token);
                lock (cacheSync)
                    if (!consumed.Contains(puzzle) && ready.All(p => PuzzleConsumptionStore.Key(p) != PuzzleConsumptionStore.Key(puzzle)))
                    { ready.Add(puzzle); Save(); }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (IOException)
        {
        }
        finally
        {
            gate.Release();
        }
    }

    private void Save() => File.WriteAllText(file, JsonSerializer.Serialize(ready, OpeningStore.Json));
    private async Task<Puzzle> Generate(CancellationToken token)
    {
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(token);
        budget.CancelAfter(TimeSpan.FromMinutes(3));
        await using var pool = new EnginePool(engine, 2, 2, budget.Token);
        int tested = 0;
        try
        {
            foreach (var node in GraphOperations.Positions(opening).Where(w => w.Position.WhiteToMove != opening.LearnsWhite && w.Node.Moves.Count > 0).OrderBy(_ => Random.Shared.Next()).Take(100))
            {
                budget.Token.ThrowIfCancellationRequested();
                var excluded = MoveGenerator.GenerateLegal(node.Position).Where(m => node.Node.Moves.All(d => d.Uci != m.Uci)).ToList();
                if (excluded.Count == 0)
                    continue;
                var result = await pool.Search(node.Position, 12).WaitAsync(budget.Token);
                if (result.Lines.Count == 0)
                    continue;
                double best = OpeningGenerator.Score(result.Lines[0]);
                var bad = result.Lines.Where(l => excluded.Any(m => m.Uci == l.BestMove) &&
                    (anyDeviation ? l.BestMove != result.BestMove : best - OpeningGenerator.Score(l) >= 1.25 && OpeningGenerator.Score(l) <= -1.0))
                    .OrderBy(_ => Random.Shared.Next()).Take(6);
                foreach (var line in bad)
                {
                    var move = excluded.First(m => m.Uci == line.BestMove);
                    var position = node.Position.Clone();
                    string san = SanNotation.Format(position, move);
                    position.Apply(move);
                    string key = OpeningStore.PositionKey(position);
                    bool cached;
                    lock (cacheSync) cached = ready.Any(p => OpeningStore.PositionKey(Position.FromFen(p.Fen)) == key);
                    if (consumed.Contains(new Puzzle { Fen = position.ToFen() }) || recent.Contains(key) || cached)
                        continue;
                    Progress?.Invoke($"{++tested} Fehler geprüft · {san} außerhalb von {opening.Name}");
                    var puzzle = await Continue(position, pool, budget.Token, anyDeviation);
                    if (puzzle is null)
                        continue;
                    puzzle.Theme = "Gegnerfehler ausnutzen · nach " + san;
                    puzzle.Origin = opening.Name + " · ausgelassener Gegnerzug " + san + " · Prüfung Tiefe 14";
                    puzzle.SetupFen = opening.StartFen ?? Position.InitialFen;
                    puzzle.SetupMoves = [..node.Path, move.Uci];
                    puzzle.OpeningAnchorFen = node.Position.ToFen();
                    return puzzle;
                }
            }
        }
        catch (OperationCanceledException)when (!token.IsCancellationRequested)
        {
            throw new IOException("3 Minuten Vorbereitungsbudget erreicht. Noch kein klarer Gegnerfehler vorbereitet; erneut versuchen oder ein anderes Opening wählen.");
        }

        throw new IOException("Noch kein eindeutig ausnutzbarer ausgelassener Gegnerzug gefunden. Ein größeres Opening wählen oder erneut vorbereiten.");
    }

    private static async Task<Puzzle?> Continue(Position initial, EnginePool pool, CancellationToken token, bool allowChoice)
    {
        var p = initial.Clone();
        var moves = new List<string>();
        var seen = new HashSet<string>();
        for (int own = 0; own < 6; own++)
        {
            if (!seen.Add(p.RepetitionKey()) || MoveGenerator.GenerateLegal(p).Count == 0)
                break;
            var r = await pool.Search(p, 14, own == 0 && allowChoice ? 0 : 3).WaitAsync(token);
            var lines = r.Lines.OrderByDescending(OpeningGenerator.Score).ToList();
            if (lines.Count == 0)
                break;
            double gap = lines.Count < 2 ? 100 : OpeningGenerator.Score(lines[0]) - OpeningGenerator.Score(lines[1]);
            // Stop before ambiguous decisions rather than rejecting an equally good alternative.
            bool clear = lines.Count == 1 || lines[0].Mate is> 0 && lines[1].Mate is not > 0 || lines[0].Mate is null && gap >= .65;
            if (!clear)
            {
                if (own == 0 && allowChoice)
                {
                    var best = lines[0];
                    var accepted = lines.Where(l => best.Mate is int mate ? l.Mate == mate : l.Mate is null && OpeningGenerator.Score(best) - OpeningGenerator.Score(l) <= .2)
                        .Select(l => l.BestMove!).ToList();
                    return new Puzzle { Id = Guid.NewGuid().ToString("N"), Fen = initial.ToFen(), Moves = [best.BestMove!], AcceptedMoves = accepted, GoalKind = "OpeningChoice", MinimumDepth = 14 };
                }
                break;
            }
            var move = MoveGenerator.GenerateLegal(p).FirstOrDefault(m => m.Uci == lines[0].BestMove);
            if (move is null)
                break;
            moves.Add(move.Uci);
            p.Apply(move);
            if (MoveGenerator.GameState(p, new Dictionary<string, int>())is not ("" or "Schach"))
                break;
            var defense = await pool.Search(p, 14, 1).WaitAsync(token);
            var reply = MoveGenerator.GenerateLegal(p).FirstOrDefault(m => m.Uci == defense.BestMove);
            if (reply is null)
                break;
            moves.Add(reply.Uci);
            p.Apply(reply);
        }

        return moves.Count == 0 ? null : new Puzzle
        {
            Id = Guid.NewGuid().ToString("N"),
            Fen = initial.ToFen(),
            Moves = moves,
            GoalKind = "OpeningMistake",
            MinimumDepth = 14
        };
    }
}
