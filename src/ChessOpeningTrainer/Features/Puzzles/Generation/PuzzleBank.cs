using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ChessOpeningTrainer;
// Composed legal positions, not imported human games. Every solver decision is
// checked separately with MultiPV. Uniqueness is bounded by the search depth.
public sealed partial class PuzzleBank
{
    private readonly string file;
    private readonly StockfishEngine engine;
    private readonly PuzzleConsumptionStore consumed;
    private readonly List<Puzzle> items = [];
    private readonly SemaphoreSlim filling = new(1);
    private readonly object sync = new();
    private readonly SemaphoreSlim saving = new(1);
    private long saveSerial;
    private sealed class SelfplayState
    {
        public Position Position = Position.FromFen(Position.InitialFen);
        public int Ply;
    }
    private readonly List<string> recentThemes = [];
    public string? LastError { get; private set; }

    public int Count
    {
        get
        {
            lock (sync)
                return items.Count;
        }
    }

    public PuzzleBank(string dataDirectory, string appDirectory, StockfishEngine engine)
    {
        Directory.CreateDirectory(dataDirectory);
        file = Path.Combine(dataDirectory, "puzzles.json");
        consumed = new(Path.Combine(dataDirectory, "puzzles-consumed.json"));
        this.engine = engine;
        foreach (var source in new[]
        {
            file,
            Path.Combine(appDirectory, "Puzzles", "seed.json"),
            Path.Combine(appDirectory, "Puzzles", "draw-seeds-v9.json"),
            Path.Combine(appDirectory, "Puzzles", "draw-seeds-v10.json"),
            Path.Combine(appDirectory, "Puzzles", "mate-seeds-v10.json")
            ,Path.Combine(appDirectory, "Puzzles", "draw-seeds-v12.json")
            ,Path.Combine(appDirectory, "Puzzles", "draw-seeds-v13.json")
            ,Path.Combine(appDirectory, "Puzzles", "mate-seeds-v13.json")
            ,Path.Combine(appDirectory, "Puzzles", "mate-seeds-v14.json")
        }

        )
        {
            if (!File.Exists(source))
                continue;
            try
            {
                foreach (var p in JsonSerializer.Deserialize<List<Puzzle>>(File.ReadAllText(source), OpeningStore.Json) ?? [])
                {
                    // Endgame exercises belong in the endgame trainer, even if an
                    // earlier release copied them into the personal tactic cache.
                    if (p.MateGoal is not null && Position.FromFen(p.Fen).Squares
                        .Where(c => c != '\0' && char.ToLowerInvariant(c) != 'k')
                        .Select(char.IsUpper).Distinct().Count() < 2) continue;
                    if (!Valid(p))
                        continue;
                    if (p.MateGoal is not null && p.MateDefenseVersion < 2)
                        p.PreparedMateRoutes = null;
                    var existing = items.FirstOrDefault(x => x.Id == p.Id);
                    if (existing is not null && p.MateDefenseVersion > existing.MateDefenseVersion && p.PreparedMateRoutes is not null && p.Fen == existing.Fen)
                    {
                        p.LastPlayedUtc = existing.LastPlayedUtc;
                        items.Remove(existing);
                        items.Add(p);
                    }
                    else if (existing is null)
                        items.Add(p);
                    else if (existing.PreparedMateRoutes is null && existing.Fen == p.Fen && existing.MateGoal == p.MateGoal)
                        existing.PreparedMateRoutes = p.PreparedMateRoutes;
                }
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
            }
        }
        if (consumed.Error is null) consumed.Record(items.Where(p => p.LastPlayedUtc is not null));
        else LastError = consumed.Error;
        items.RemoveAll(consumed.Contains);
    }

    private static bool Valid(Puzzle p)
    {
        try
        {
            if (p.MateGoal is int mate && p.MateDefenseVersion >= 2 && p.PreparedMateRoutes?.GetValueOrDefault(MatePreparation.Key(Position.FromFen(p.Fen), mate)) is { Count: > 0 })
                return true;
            if (p.GoalKind == "Draw")
                return p.DrawProofVersion >= 2 && p.PreparedMateRoutes?.ContainsKey(MatePreparation.Key(Position.FromFen(p.Fen), p.GoalMoves)) == true;
            if (p.Moves.Count < 3 || p.Moves.Count % 2 != 1 || p.MinimumDepth < 12)
                return false;
            var board = Position.FromFen(p.Fen);
            foreach (var u in p.Moves)
            {
                var m = MoveGenerator.GenerateLegal(board).FirstOrDefault(m => m.Uci == u);
                if (m is null)
                    return false;
                board.Apply(m);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public Puzzle? Next(string difficulty = "Alle", bool mates = true, bool material = true)
    {
        lock (sync)
        {
            var filtered = items.Where(x => x.GoalKind != "Draw" && (x.MateGoal is null ? material : mates) && (x.MateGoal is null || x.PreparedMateRoutes is { Count: > 0 }) && (difficulty == "Alle" || x.Difficulty == difficulty)).ToList();
            var unused = filtered.Where(x => x.LastPlayedUtc is null).ToList();
            var pool = unused;
            var p = pool.OrderBy(x => recentThemes.Count(t => t == x.Theme)).ThenBy(_ => Random.Shared.Next()).FirstOrDefault();
            if (p is not null)
            {
                consumed.Record([p]);
                items.RemoveAll(x => PuzzleConsumptionStore.Key(x) == PuzzleConsumptionStore.Key(p));
                p.LastPlayedUtc = DateTime.UtcNow;
                recentThemes.Add(p.Theme);
                if (recentThemes.Count > 5)
                    recentThemes.RemoveAt(0);
                Save();
            }

            return p;
        }
    }

    public void Complete(string id)
    {
        lock (sync)
        {
            var p = items.Find(x => x.Id == id);
            if (p is not null)
                p.LastPlayedUtc = DateTime.UtcNow;
            Save();
        }
    }

    public Puzzle? NextDraw()
    {
        lock (sync)
        {
            var next = items.Where(p => p.GoalKind == "Draw" && p.GoalMoves >= 1 && p.LastPlayedUtc is null).OrderBy(_ => Random.Shared.Next()).FirstOrDefault();
            if (next is not null)
            {
                consumed.Record([next]);
                items.RemoveAll(x => PuzzleConsumptionStore.Key(x) == PuzzleConsumptionStore.Key(next));
                next.LastPlayedUtc = DateTime.UtcNow;
                Save();
            }

            return next;
        }
    }

    public void AddDraw(Puzzle puzzle)
    {
        lock (sync)
        {
            if (!consumed.Contains(puzzle) && items.All(p => p.Id != puzzle.Id && PuzzleConsumptionStore.Key(p) != PuzzleConsumptionStore.Key(puzzle)))
            {
                items.Add(puzzle);
                if (items.Count(p => p.GoalKind == "Draw") > 100)
                {
                    var oldest = items.Where(p => p.GoalKind == "Draw").OrderBy(p => p.LastPlayedUtc ?? DateTime.MaxValue).First();
                    items.Remove(oldest);
                }

                Save();
            }
        }
    }

    private void Save()
    {
        var snapshot = items.ToArray();
        long version = Interlocked.Increment(ref saveSerial);
        _ = Task.Run(async () =>
        {
            await saving.WaitAsync();
            try
            {
                if (version != Volatile.Read(ref saveSerial))
                    return;
                var temporary = file + ".tmp";
                await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(snapshot, OpeningStore.Json));
                File.Move(temporary, file, true);
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
            }
            finally
            {
                saving.Release();
            }
        });
    }


    private async Task<Position?> SelfplayCandidate(SelfplayState game, CancellationToken token)
    {
        var selfplay = game.Position;
        for (int step = 0; step < 3; step++)
        {
            var legal = MoveGenerator.GenerateLegal(selfplay);
            var state = MoveGenerator.GameState(selfplay, new Dictionary<string, int>());
            if (game.Ply >= 140 || legal.Count == 0 || state != "" && state != "Schach")
            {
                selfplay = Position.FromFen(Position.InitialFen);
                game.Position = selfplay;
                game.Ply = 0;
                legal = MoveGenerator.GenerateLegal(selfplay);
            }

            var r = await engine.AnalyzeAsync(selfplay.ToFen(), 10, Math.Min(5, legal.Count), GenerationThreads, 64, token, 350);
            token.ThrowIfCancellationRequested();
            if (r.Error is not null)
                throw new IOException(r.Error);
            if (r.Lines.Count == 0)
                return null;
            // Mostly good play, but plausible inaccuracies create tactical chances.
            int rank = Random.Shared.NextDouble() < .55 ? 0 : Random.Shared.Next(r.Lines.Count);
            var m = legal.FirstOrDefault(m => m.Uci == r.Lines[rank].BestMove);
            if (m is null)
                return null;
            selfplay.Apply(m);
            game.Ply++;
        }

        return game.Ply >= 12 ? selfplay.Clone() : null;
    }

    private static Position? Compose(bool mating = false)
    {
        var p = new Position
        {
            Castling = ""
        };
        var random = Random.Shared;
        int wk = random.Next(64), bk = random.Next(64);
        if (Math.Max(Math.Abs(wk % 8 - bk % 8), Math.Abs(wk / 8 - bk / 8)) < 3)
            return null;
        p.Squares[wk] = 'K';
        p.Squares[bk] = 'k';
        var sets = new[]
        {
            "QRPrpp",
            "RNPqrpp",
            "QBPqrpp",
            "RRPrnp",
            "QPrpp",
            "RPPPrpp",
            "QNBqrnp",
            "RBNPPPrbnppp",
            "BNPPbnpp",
            "RPPPPPrpppp",
            "QRRBNPPqrrbnpp",
            "NNPPPrbpp",
            "BBPPrnpp"
        };
        if (mating) sets = ["QRr", "QBrp", "QNrb", "RRrq", "QRbn", "RNrp", "Qrrp", "Qnpp"];
        foreach (char piece in sets[random.Next(sets.Length)])
        {
            int square = random.Next(char.ToLower(piece) == 'p' ? 8 : 0, char.ToLower(piece) == 'p' ? 56 : 64);
            if (p.Squares[square] != '\0')
                return null;
            p.Squares[square] = piece;
        }

        p.WhiteToMove = random.Next(2) == 0;
        // The side that just moved must not have left its own king in check.
        if (MoveGenerator.IsKingInCheck(p, !p.WhiteToMove))
            return null;
        return MoveGenerator.GenerateLegal(p).Count > 1 ? p : null;
    }

    private static int Value(EngineLine l) => l.Mate is int m ? (m > 0 ? 100000 - m * 100 : -100000 - m * 100) : l.ScoreCentipawns ?? -100000;
    private static bool Clear(EngineResult r)
    {
        if (r.Lines.Count < 2)
            return false;
        var best = r.Lines[0];
        var second = r.Lines[1];
        // A mate puzzle explicitly asks for the shortest forced mate. Slower
        // mates are not solutions to that stated move limit.
        if (best.Mate is> 0 and <= 6)
            return second.Mate is null || second.Mate < 0 || second.Mate > best.Mate;
        if (r.Lines.Take(2).Any(l => l.Depth < 12)) return false;
        return best.Mate is null && Value(best) >= 180 && Value(best) - Value(second) >= 180 && Value(second) < 300;
    }

    private static int Material(Position p, bool white)
    {
        int total = 0;
        foreach (char c in p.Squares)
        {
            int v = char.ToLower(c) switch
            {
                'q' => 9,
                'r' => 5,
                'b' or 'n' => 3,
                'p' => 1,
                _ => 0
            };
            total += (char.IsUpper(c) == white ? 1 : -1) * v;
        }

        return total;
    }

    public async Task<Puzzle?> VerifyAsync(Position start, CancellationToken token)
    {
        var p = start.Clone();
        var moves = new List<string>();
        int depth = 99;
        int material = Material(start, start.WhiteToMove);
        bool mate = false;
        int? mateGoal = null;
        for (int step = 0; step < 6; step++)
        {
            var result = await engine.AnalyzeAsync(p.ToFen(), 18, 2, GenerationThreads, 64, token, 650);
            if (!Clear(result))
                return null;
            if (step == 0)
                mateGoal = result.Lines[0].Mate;
            depth = Math.Min(depth, result.Lines.Min(l => l.Depth));
            var move = MoveGenerator.GenerateLegal(p).FirstOrDefault(m => m.Uci == result.BestMove);
            if (move is null)
                return null;
            moves.Add(move.Uci);
            p.Apply(move);
            if (MoveGenerator.GenerateLegal(p).Count == 0)
            {
                mate = MoveGenerator.IsKingInCheck(p, p.WhiteToMove);
                break;
            }

            if (step >= 1 && mateGoal is null && Material(p, start.WhiteToMove) - material >= 2)
                break;
            if (step == 5)
                return null;
            var answer = await engine.AnalyzeAsync(p.ToFen(), 18, 1, GenerationThreads, 64, token, 500);
            var reply = MoveGenerator.GenerateLegal(p).FirstOrDefault(m => m.Uci == answer.BestMove);
            if (reply is null)
                return null;
            moves.Add(reply.Uci);
            p.Apply(reply);
        }

        if (moves.Count < (mate ? 1 : 3) || (!mate && Material(p, start.WhiteToMove) - material < 2))
            return null;
        // A second, longer verification rejects unstable one-search tactics.
        p = start.Clone();
        for (int i = 0; i < moves.Count; i++)
        {
            if (i % 2 == 0)
            {
                var check = await engine.AnalyzeAsync(p.ToFen(), 20, 2, GenerationThreads, 64, token, 1800);
                if (!Clear(check) || check.BestMove != moves[i])
                    return null;
                if (mateGoal is int goal && check.Mate != goal - i / 2)
                    return null;
            }

            p.Apply(MoveGenerator.GenerateLegal(p).First(m => m.Uci == moves[i]));
        }

        string materialSet = new string (start.Squares.Where(c => c != '\0').Order().ToArray());
        string signature = materialSet + "|" + string.Join(',', moves.Where((_, i) => i % 2 == 0).Select(u => u[..2] + u[2..]));
        return new Puzzle
        {
            Id = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(start.ToFen())))[..20],
            Fen = start.ToFen(),
            Moves = moves,
            Theme = mate ? $"Matt in {mateGoal ?? (moves.Count + 1) / 2}" : moves.Any(u => u.Length == 5) ? "Umwandlung" : "Materialgewinn",
            MinimumDepth = depth,
            Fingerprint = signature
        };
    }
}
