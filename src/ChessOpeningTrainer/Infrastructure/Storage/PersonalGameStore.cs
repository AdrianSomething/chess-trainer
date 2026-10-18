using System.Text.Json;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;

namespace ChessOpeningTrainer;
public sealed class PersonalGameStore
{
    private static readonly object storageSync = new();
    private static readonly Dictionary<string, List<PersonalGame>> shared = new(StringComparer.OrdinalIgnoreCase);
    private List<PersonalGame> games = [];
    public IReadOnlyList<PersonalGame> Games
    {
        get
        {
            lock (storageSync)
                return games.ToArray();
        }
    }

    private readonly string file;
    public void Add(PersonalGame game)
    {
        lock (storageSync)
        {
            games.Add(game);
            Save();
        }
    }

    public void Remove(PersonalGame game)
    {
        lock (storageSync)
        {
            games.Remove(game);
            Save();
        }
    }

    public PersonalGameStore(string dir)
    {
        Directory.CreateDirectory(dir);
        file = Path.GetFullPath(Path.Combine(dir, "personal-games.json"));
        lock (storageSync)
        {
            if (shared.TryGetValue(file, out var existing))
                games = existing;
            else
            {
                try
                {
                    games = JsonSerializer.Deserialize<List<PersonalGame>>(File.ReadAllText(file), OpeningStore.Json) ?? [];
                }
                catch (FileNotFoundException)
                {
                }

                shared[file] = games;
            }
        }
    }

    public void Save()
    {
        lock (storageSync)
        {
            File.WriteAllText(file + ".tmp", JsonSerializer.Serialize(games, OpeningStore.Json));
            File.Move(file + ".tmp", file, true);
        }
    }

    public int Import(string pgn) => ImportGames(PgnImport.Parse(pgn));
    public int ImportGames(IEnumerable<PersonalGame> parsed)
    {
        lock (storageSync)
        {
            int count = 0;
            foreach (var g in parsed)
                if (games.All(e => e.Id != g.Id))
                {
                    games.Add(g);
                    count++;
                }

            Save();
            return count;
        }
    }

    public async Task Analyze(PersonalGame game, StockfishEngine engine, IProgress<string>? progress, CancellationToken token, IReadOnlyDictionary<string, EngineResult>? prepared = null)
    {
        var options = Preferences.Current.GameAnalysis.Copy();
        options.Validate();
        var moves = game.Moves.ToArray();
        var positions = new PersonalGame
        {
            StartFen = game.StartFen,
            Moves = moves.ToList()
        }.Positions();
        var results = new EngineResult[positions.Count];
        int completed = 0;
        bool Cached(Position p, out EngineResult? result)
        {
            result = null;
            if (prepared is null || !prepared.TryGetValue(p.ToFen(), out var existing)) return false;
            if (existing.Error is not null || existing.Lines.Count == 0 && MoveGenerator.GenerateLegal(p).Count > 0 || existing.Lines.Any(l => l.Depth < options.Depth)) return false;
            result = existing; return true;
        }
        await using var pool = positions.Any(p => !Cached(p, out _)) ? new EnginePool(engine, options.Workers, options.Threads, token) : null;
        await Task.WhenAll(positions.Select(async (p, i) =>
        {
            results[i] = Cached(p, out var cached) ? cached! : await pool!.Search(p, options.Depth).WaitAsync(token);
            token.ThrowIfCancellationRequested();
            if (results[i].Error is not null)
                throw new IOException(results[i].Error);
            progress?.Report($"{Interlocked.Increment(ref completed)}/{positions.Count} Stellungen · Tiefe {options.Depth} · {options.Workers} Arbeiter × {options.Threads} Threads");
        }));
        double Eval(int i)
        {
            string state = MoveGenerator.GameState(positions[i], new Dictionary<string, int>());
            return state == "Schachmatt" ? -1000 : state.StartsWith("Remis") ? 0 : Value(results[i]);
        }

        var moments = new List<GameMoment>();
        for (int i = 0; i < moves.Length; i++)
        {
            var r = results[i];
            double best = Eval(i);
            var chosen = r.Lines.FirstOrDefault(l => l.BestMove == moves[i]);
            double played = chosen is null ? -Eval(i + 1) : OpeningGenerator.Score(chosen);
            bool isBest = moves[i] == r.BestMove;
            double loss = isBest ? 0 : Math.Max(0, best - played);
            var matches = r.Lines.Where(l => OpeningGenerator.Score(l) >= best - options.OpportunityTolerance).Select(l => l.BestMove!).ToList();
            string category = options.Classify(best, played, isBest, moments.LastOrDefault(), matches.Count);
            moments.Add(new() { Ply = i, Category = category, Loss = loss, WeightedLoss = isBest ? 0 : options.WeightedLoss(best, played), Before = best, After = played, BestMoves = matches, Pv = r.Lines.FirstOrDefault()?.Pv ?? [] });
        }

        token.ThrowIfCancellationRequested();
        lock(storageSync)
        {
            if (!game.Moves.SequenceEqual(moves))
                throw new IOException("Die Partie wurde während der Analyse fortgesetzt. Bitte nach Ende der Partie erneut analysieren.");
            game.Analysis = moments;
            game.PositionAnalyses = results.ToList();
            game.AnalysisOptions = options;
            Save();
        }
    }

    public static double Value(EngineResult r) => r.Lines.Count > 0 ? OpeningGenerator.Score(r.Lines[0]) : r.Mate is int m ? Math.Sign(m) * 1000 : (r.ScoreCentipawns ?? 0) / 100d;
    public IEnumerable<(PersonalGame Game, GameMoment Moment)> Mistakes(string username, int excludeRecent)
    {
        var sorted = Games.OrderByDescending(g => g.Tags.GetValueOrDefault("Date")).ThenByDescending(g => g.ImportedUtc).Skip(excludeRecent);
        return sorted.SelectMany(g => g.Analysis.Where(m => m.Category is "Patzer" or "Verpasste Chance" && (((Position.FromFen(g.StartFen).WhiteToMove == (m.Ply % 2 == 0)) ? g.White : g.Black).Equals(username, StringComparison.OrdinalIgnoreCase))).Select(m => (g, m)));
    }
}
