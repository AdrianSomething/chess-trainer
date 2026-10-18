using System.Text.Json;

namespace ChessOpeningTrainer;
public sealed class PositionalTrainer
{
    public event Action<string>? Progress;
    private readonly StockfishEngine engine;
    private readonly string file;
    private readonly PuzzleConsumptionStore consumed;
    private readonly List<Puzzle> cache = [];
    private readonly object sync = new();
    private readonly SemaphoreSlim gate = new(1);
    private Position game = Position.FromFen(Position.InitialFen);
    private int ply;
    public PositionalTrainer(StockfishEngine engine, string directory)
    {
        this.engine = engine;
        file = Path.Combine(directory, "positional-puzzles.json");
        consumed = new(Path.Combine(directory, "positional-consumed.json"));
        try
        {
            cache = JsonSerializer.Deserialize<List<Puzzle>>(File.ReadAllText(file), OpeningStore.Json) ?? [];
        }
        catch (IOException)
        {
        }
        catch (JsonException)
        {
        }

        cache.RemoveAll(p => p.PositionalStyle is not ("clear" or "subtle"));
        if (!File.Exists(file))
        {
            try
            {
                cache.AddRange(JsonSerializer.Deserialize<List<Puzzle>>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Puzzles", "positional-seeds-v10.json")), OpeningStore.Json) ?? []);
            }
            catch (IOException)
            {
            }
            catch (JsonException)
            {
            }
        }
    }

    private Puzzle? Take(int min, int max, bool subtle)
    {
        lock (sync)
        {
            var ready = cache.Where(p => !consumed.Contains(p) && p.PositionalStyle == (subtle ? "subtle" : "clear") && p.AcceptedMoves.Count >= min && p.AcceptedMoves.Count <= max).OrderBy(_ => Random.Shared.Next()).FirstOrDefault();
            if (ready is not null)
            {
                consumed.Record([ready]);
                cache.Remove(ready);
                Save();
            }

            return ready;
        }
    }

    public async Task<Puzzle> Next(int min, int max, CancellationToken token, bool subtle = false)
    {
        if (Take(min, max, subtle)is { } cached)
            return cached;
        await gate.WaitAsync(token);
        try
        {
            if (Take(min, max, subtle) is { } prepared) return prepared;
            for (int attempt = 0; attempt < 10; attempt++)
            {
                var generated = await Generate(min, max, subtle, token);
                if (consumed.Contains(generated)) continue;
                consumed.Record([generated]);
                return generated;
            }
            throw new IOException("Noch keine neue Mittelspielaufgabe gefunden.");
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task Fill(int min, int max, CancellationToken token, bool subtle = false)
    {
        if (!await gate.WaitAsync(0, token))
            return;
        try
        {
            bool needed;
            lock (sync)
                needed = cache.Count(p => !consumed.Contains(p) && p.PositionalStyle == (subtle ? "subtle" : "clear") && p.AcceptedMoves.Count >= min && p.AcceptedMoves.Count <= max) < Math.Clamp(Preferences.Current.TacticStockPerLane, 1, 50);
            if (needed)
            {
                var puzzle = await Generate(min, max, subtle, token);
                lock (sync)
                {
                    if (!consumed.Contains(puzzle) && cache.All(p => PuzzleConsumptionStore.Key(p) != PuzzleConsumptionStore.Key(puzzle)))
                        cache.Add(puzzle);
                    Save();
                }
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

    private void Save() => File.WriteAllText(file, JsonSerializer.Serialize(cache, OpeningStore.Json));
    public static List<EngineLine> SelectGroup(Position p, IEnumerable<EngineLine> input, int min, int max, bool subtle)
    {
        var lines = input.OrderByDescending(OpeningGenerator.Score).ToList();
        if (lines.Count <= min || lines[0].Mate is not null)
            return[];
        double best = OpeningGenerator.Score(lines[0]);
        if (best is < -1 or > 4)
            return[];
        for (int count = min; count <= max && count < lines.Count; count++)
        {
            double worst = OpeningGenerator.Score(lines[count - 1]), gap = worst - OpeningGenerator.Score(lines[count]);
            if (best - worst > (subtle ? .20 : .40) || gap < (subtle ? .05 : .50) || subtle && gap > .45)
                continue;
            var group = lines.Take(count).ToList();
            if (group.All(l => Quiet(p, l)))
                return group;
        }

        return[];
    }

    private static bool Quiet(Position initial, EngineLine line)
    {
        var p = initial.Clone();
        int step = 0;
        foreach (string u in line.Pv.Take(3))
        {
            var m = MoveGenerator.GenerateLegal(p).FirstOrDefault(m => m.Uci == u);
            if (m is null)
                return false;
            if (step % 2 == 0 && (p.Squares[m.To] != '\0' || char.ToLowerInvariant(p.Squares[m.From]) == 'p' && m.From % 8 != m.To % 8))
                return false;
            p.Apply(m);
            step++;
        }

        return step >= 3;
    }

    private async Task<Puzzle> Generate(int min, int max, bool subtle, CancellationToken token)
    {
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(token);
        budget.CancelAfter(TimeSpan.FromMinutes(3));
        await using var pool = new EnginePool(engine, 2, 2, budget.Token);
        int checkedPositions = 0, depth = subtle ? 16 : 12;
        try
        {
            for (int attempt = 0; attempt < 240; attempt++)
            {
                budget.Token.ThrowIfCancellationRequested();
                if (ply > 85 || MoveGenerator.GameState(game, new Dictionary<string, int>())is not ("" or "Schach"))
                {
                    game = Position.FromFen(Position.InitialFen);
                    ply = 0;
                }

                var r = await pool.Search(game, 8, 5).WaitAsync(budget.Token);
                if (r.Lines.Count == 0 || Math.Abs(OpeningGenerator.Score(r.Lines[0])) > 4)
                {
                    ply = 86;
                    continue;
                }

                if (ply >= 12 && !MoveGenerator.IsKingInCheck(game, game.WhiteToMove) && game.Squares.Count(c => c != '\0') >= 12)
                {
                    var shallow = await pool.Search(game, 8).WaitAsync(budget.Token);
                    checkedPositions++;
                    var scores = shallow.Lines.OrderByDescending(OpeningGenerator.Score).ToList();
                    double best = OpeningGenerator.Score(scores[0]);
                    int plausible = scores.Count(l => best - OpeningGenerator.Score(l) <= (subtle ? .45 : .75));
                    if (plausible >= min && plausible <= max + 3)
                    {
                        var deeper = await pool.Search(game, depth).WaitAsync(budget.Token);
                        var good = SelectGroup(game, deeper.Lines, min, max, subtle);
                        Progress?.Invoke($"{checkedPositions} Stellungen geprüft · Tiefe {depth} · {good.Count} passende ruhige Züge");
                        if (good.Count > 0)
                        {
                            var puzzle = new Puzzle
                            {
                                Id = Guid.NewGuid().ToString("N"),
                                Fen = game.ToFen(),
                                Moves = good[0].Pv,
                                AcceptedMoves = good.Select(l => l.BestMove!).ToList(),
                                SolutionLines = good.Concat(deeper.Lines.OrderByDescending(OpeningGenerator.Score).Where(l => !good.Contains(l)).Take(1)).ToList(),
                                GoalKind = "Positional",
                                PositionalStyle = subtle ? "subtle" : "clear",
                                Theme = (subtle ? "Feines Stellungsgefühl" : "Klare Stellungszüge") + $" · {good.Count} gute Züge",
                                Origin = $"Engine-Selbstspiel · alle legalen Antworten, Tiefe {depth}",
                                MinimumDepth = depth
                            };
                            Advance(r);
                            return puzzle;
                        }
                    }
                }

                Advance(r);
            }
        }
        catch (OperationCanceledException)when (!token.IsCancellationRequested)
        {
            throw new IOException($"Vorbereitungsbudget von 3 Minuten erreicht ({checkedPositions} Mittelspielstellungen geprüft). Erneut vorbereiten setzt das Selbstspiel fort; keine ungeprüfte Ersatzaufgabe.");
        }

        throw new IOException($"{checkedPositions} Mittelspielstellungen geprüft, noch keine passende Aufgabengruppe gefunden. Erneut vorbereiten setzt das Selbstspiel fort.");
    }

    private void Advance(EngineResult result)
    {
        var lines = result.Lines.Where(l => l.BestMove is not null).Take(5).ToList();
        if (lines.Count == 0)
        {
            ply = 86;
            return;
        }

        double best = OpeningGenerator.Score(lines[0]);
        lines = lines.Where(l => best - OpeningGenerator.Score(l) <= .8).ToList();
        string u = lines[Random.Shared.NextDouble() < .65 ? 0 : Random.Shared.Next(lines.Count)].BestMove!;
        var move = MoveGenerator.GenerateLegal(game).FirstOrDefault(m => m.Uci == u);
        if (move is null)
        {
            ply = 86;
            return;
        }

        game.Apply(move);
        ply++;
    }
}
