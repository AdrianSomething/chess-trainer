using System.Text.Json;

namespace ChessOpeningTrainer;
public sealed class EndgameFactory
{
    public static readonly string[] Kinds = ["Damenmatt", "Turmmatt", "Zwei Läufer", "Läufer + Springer", "Bauern: Durchbruch & Verteidigung", "Komplexe Bauernstellungen"];
    public static string[] Expand(string kind) => kind switch
    {
        "Bauern: Durchbruch & Verteidigung" => ["Bauernverteidigung", "Bauernumwandlung"],
        "Komplexe Bauernstellungen" => ["Komplexe Bauernendspiele", "Bauernverteidigung: mehrere Bauern"],
        _ => [kind]
    };
    private static bool Current(EndgameExercise e)
    {
        if (e.Puzzle.MateGoal is not null && e.Puzzle.MateDefenseVersion < 2)
            return false;
        if (e.Kind != "Bauernverteidigung: mehrere Bauern")
            return true;
        var p = Position.FromFen(e.Puzzle.Fen);
        return p.Squares.Contains('P') && p.Squares.Any(c => c is 'B' or 'N') && e.Puzzle.GoalKind == "AdvancedDraw";
    }

    private readonly StockfishEngine engine;
    private readonly string file;
    private readonly PuzzleConsumptionStore consumed;
    private readonly List<EndgameExercise> cache = [];
    private readonly SemaphoreSlim gate = new(1);
    private readonly object sync = new();
    public EndgameFactory(string directory, StockfishEngine? engine = null)
    {
        this.engine = engine ?? new StockfishEngine(AppContext.BaseDirectory);
        file = Path.Combine(directory, "endgame-cache.json");
        consumed = new(Path.Combine(directory, "endgame-consumed.json"));
        try
        {
            cache = (JsonSerializer.Deserialize<List<EndgameExercise>>(File.ReadAllText(file), OpeningStore.Json) ?? []).Where(e => (!AdvancedEndgames.Supports(e.Kind) || e.Puzzle.GoalKind.StartsWith("Advanced")) && Current(e)).ToList();
        }
        catch
        {
        }

        try
        {
            var seeds = new List<EndgameExercise>();
            foreach (string name in new[] { "endgame-seeds-v9.json", "endgame-seeds-v12.json" })
            {
                string seedPath = Path.Combine(AppContext.BaseDirectory, "Puzzles", name);
                if (File.Exists(seedPath)) seeds.AddRange(JsonSerializer.Deserialize<List<EndgameExercise>>(File.ReadAllText(seedPath), OpeningStore.Json) ?? []);
            }
            foreach (var seed in seeds.Where(Current))
                if (!consumed.Contains(seed.Puzzle) && !cache.Any(e => PuzzleConsumptionStore.Key(e.Puzzle) == PuzzleConsumptionStore.Key(seed.Puzzle)))
                    cache.Add(seed);
        }
        catch
        {
        }
        cache.RemoveAll(e => consumed.Contains(e.Puzzle));
    }

    public async Task<EndgameExercise> Next(string kind, CancellationToken token)
    {
        var choices = Expand(kind);
        kind = choices[Random.Shared.Next(choices.Length)];
        if (kind is "Bauernverteidigung" or "Bauernumwandlung")
        {
            var table = await PawnTablebase.Shared.WaitAsync(token);
            bool fortress = kind == "Bauernverteidigung" && Random.Shared.Next(2) == 0;
            for (int attempt = 0; attempt < 100; attempt++)
            {
                token.ThrowIfCancellationRequested();
                var p = fortress && attempt < 20 ? Fortress(table) : table.RandomPosition(kind == "Bauernumwandlung");
                var puzzle = new Puzzle
                {
                    Fen = p.ToFen(),
                    Theme = fortress && attempt < 20 ? "Festung / Opposition · vor dem Bauern bleiben" : kind,
                    Origin = "Exakte lokale KPK-Tablebase"
                };
                if (consumed.Contains(puzzle)) continue;
                consumed.Record([puzzle]);
                return new() { Kind = kind, Puzzle = puzzle };
            }
            throw new InvalidOperationException("Noch keine neue Bauernstellung gefunden.");
        }

        lock (sync)
        {
            var item = cache.FirstOrDefault(e => e.Kind == kind);
            if (item is not null)
            {
                consumed.Record([item.Puzzle]);
                cache.Remove(item);
                Save();
                return item;
            }
        }

        for (int attempt = 0; attempt < 10; attempt++)
        {
            var generated = await GenerateAsync(kind, token);
            if (consumed.Contains(generated.Puzzle)) continue;
            consumed.Record([generated.Puzzle]);
            return generated;
        }
        throw new InvalidOperationException("Noch kein neues Endspiel vorbereitet.");
    }

    public async Task Fill(IEnumerable<string> kinds, CancellationToken token)
    {
        if (token.IsCancellationRequested || !await gate.WaitAsync(0, token))
            return;
        try
        {
            foreach (string kind in kinds.SelectMany(Expand).Where(k => k is not ("Bauernverteidigung" or "Bauernumwandlung")))
            {
                lock (sync)
                {
                    if (cache.Count(e => e.Kind == kind) >= Math.Clamp(Preferences.Current.TacticStockPerLane, 1, 50))
                        continue;
                }

                try
                {
                    var generated = await GenerateAsync(kind, token);
                    lock (sync)
                    {
                        if (!consumed.Contains(generated.Puzzle) && cache.All(e => PuzzleConsumptionStore.Key(e.Puzzle) != PuzzleConsumptionStore.Key(generated.Puzzle)))
                            cache.Add(generated);
                        Save();
                    }
                }
                catch (InvalidOperationException)
                {
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            gate.Release();
        }
    }

    public static Position Fortress(PawnTablebase table)
    {
        var pool = new List<Position>
        {
            Position.FromFen("k7/8/P1K5/8/8/8/8/8 b - - 0 1"),
            Position.FromFen("7k/8/5K1P/8/8/8/8/8 b - - 0 1")
        };
        for (int file = 2; file <= 5; file++)
            for (int rank = 2; rank <= 4; rank++)
            {
                var p = new Position
                {
                    Castling = "",
                    WhiteToMove = false
                };
                p.Squares[rank * 8 + file] = 'P';
                p.Squares[(rank - 1) * 8 + file] = 'K';
                p.Squares[(rank + 1) * 8 + file] = 'k';
                if (!table.PawnWins(p) && table.PreservingMoves(p, false).All(m => m.To != rank * 8 + file))
                    pool.Add(p);
            }

        return pool[Random.Shared.Next(pool.Count)];
    }

    private async Task<EndgameExercise> GenerateAsync(string kind, CancellationToken token)
    {
        if (AdvancedEndgames.Supports(kind))
        {
            using var budget = CancellationTokenSource.CreateLinkedTokenSource(token);
            budget.CancelAfter(TimeSpan.FromSeconds(90));
            return new()
            {
                Kind = kind,
                Puzzle = await Task.Run(() => AdvancedEndgames.Generate(kind, engine, budget.Token), budget.Token)
            };
        }

        return await Task.Run(() => Generate(kind, token), token);
    }

    private void Save()
    {
        File.WriteAllText(file + ".tmp", JsonSerializer.Serialize(cache, OpeningStore.Json));
        File.Move(file + ".tmp", file, true);
    }

    public static EndgameExercise Generate(string kind, CancellationToken token)
    {
        if (AdvancedEndgames.Supports(kind))
            throw new InvalidOperationException("Dieses Endspiel benötigt die asynchrone Engineprüfung.");
        string fen = kind switch
        {
            "Damenmatt" => "k7/8/1QK5/8/8/8/8/8 w - - 0 1",
            "Turmmatt" => "k7/2K5/8/8/8/8/8/1R6 w - - 0 1",
            "Zwei Läufer" => "k7/2K5/B7/2B5/8/8/8/8 w - - 0 1",
            _ => "k7/2K5/B7/1N6/8/8/8/8 w - - 0 1"
        };
        for (int attempt = 0; attempt < 16; attempt++)
        {
            token.ThrowIfCancellationRequested();
            var p = Position.FromFen(fen);
            // Rewind legal quiet halfmoves, then transform the board. Every
            // resulting mate is independently proved, never assumed from seed.
            if (attempt < 12)
                for (int step = 0; step < 2; step++)
                {
                    var previous = Predecessors(p).OrderBy(_ => Random.Shared.Next()).FirstOrDefault();
                    if (previous is not null)
                        p = previous;
                }

            p = Transform(p, Random.Shared.Next(8), Random.Shared.Next(2) == 0);
            for (int goal = 1; goal <= 3; goal++)
            {
                var puzzle = new Puzzle
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Fen = p.ToFen(),
                    Theme = "Matt in " + goal,
                    Origin = "Endspielkomposition · vollständig bewiesener Mattbaum",
                    MinimumDepth = 0
                };
                try
                {
                    puzzle.PreparedMateRoutes = MatePreparation.Prepare(puzzle, token, 120000);
                    return new()
                    {
                        Kind = kind,
                        Puzzle = puzzle
                    };
                }
                catch (InvalidOperationException)
                {
                }
            }
        }

        throw new InvalidOperationException("Kein Endspiel innerhalb des Prüfbudgets gefunden. Erneut versuchen.");
    }

    private static IEnumerable<Position> Predecessors(Position p)
    {
        bool side = !p.WhiteToMove;
        for (int to = 0; to < 64; to++)
        {
            char piece = p.Squares[to];
            if (piece == '\0' || char.IsUpper(piece) != side)
                continue;
            for (int from = 0; from < 64; from++)
            {
                if (p.Squares[from] != '\0')
                    continue;
                var before = p.Clone();
                before.Squares[from] = piece;
                before.Squares[to] = '\0';
                before.WhiteToMove = side;
                before.Halfmove = 0;
                before.Fullmove = 1;
                if (MoveGenerator.IsKingInCheck(before, !side))
                    continue;
                var m = MoveGenerator.GenerateLegal(before).FirstOrDefault(m => m.From == from && m.To == to);
                if (m is not null)
                    yield return before;
            }
        }
    }

    private static Position Transform(Position p, int symmetry, bool swap)
    {
        var result = new Position
        {
            Castling = "",
            WhiteToMove = swap ? !p.WhiteToMove : p.WhiteToMove
        };
        for (int i = 0; i < 64; i++)
        {
            int f = i % 8, r = i / 8;
            if ((symmetry & 1) != 0)
                f = 7 - f;
            if ((symmetry & 2) != 0)
                r = 7 - r;
            if ((symmetry & 4) != 0)
                (f, r) = (r, f);
            char piece = p.Squares[i];
            result.Squares[r * 8 + f] = swap ? (char.IsUpper(piece) ? char.ToLowerInvariant(piece) : char.ToUpperInvariant(piece)) : piece;
        }

        return result;
    }
}
