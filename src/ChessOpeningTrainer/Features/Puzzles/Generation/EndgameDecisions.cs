namespace ChessOpeningTrainer;
// Hints and validation deliberately share the same immutable all-moves result.
public sealed class EndgameDecisions : IAsyncDisposable
{
    private readonly StockfishEngine engine;
    private readonly CancellationToken token;
    private EnginePool? pool;
    private readonly Dictionary<string, Task<EndgameDecision>> cache = [];
    public EndgameDecisions(StockfishEngine engine, CancellationToken token)
    {
        this.engine = engine;
        this.token = token;
    }

    public Task<EndgameDecision> Get(Position position, bool draw, bool shortest)
    {
        string key = position.ToFen() + "|" + draw + "|" + shortest;
        if (cache.TryGetValue(key, out var ready) && !ready.IsFaulted && !ready.IsCanceled)
            return ready;
        pool ??= new(engine, 2, 2, token);
        var task = Prepare(position.Clone(), draw, shortest);
        cache[key] = task;
        return task;
    }

    private async Task<EndgameDecision> Prepare(Position p, bool draw, bool shortest)
    {
        var result = await pool!.Search(p, 14).WaitAsync(token);
        var lines = result.Lines.OrderByDescending(OpeningGenerator.Score).ToList();
        if (lines.Count == 0)
            return new(lines, []);
        double best = OpeningGenerator.Score(lines[0]);
        var accepted = new List<string>();
        foreach (var line in lines)
        {
            var move = MoveGenerator.GenerateLegal(p).FirstOrDefault(m => m.Uci == line.BestMove);
            if (move is null)
                continue;
            var after = p.Clone();
            after.Apply(move);
            string state = MoveGenerator.GameState(after, new Dictionary<string, int>());
            bool terminal = state is not ("" or "Schach");
            bool good = terminal ? draw ? state.StartsWith("Remis") : state == "Schachmatt" : draw ? OpeningGenerator.Score(line) >= -.35 : line.Mate > 0 || OpeningGenerator.Score(line) >= 1;
            bool distance = shortest && lines[0].Mate is> 0 ? line.Mate == lines[0].Mate : best - OpeningGenerator.Score(line) <= (shortest ? .10 : .80);
            if (good && (terminal || distance))
                accepted.Add(move.Uci);
        }

        return new(lines, accepted);
    }

    public async Task Prefetch(Position p, bool draw, bool shortest)
    {
        try
        {
            var result = await Get(p, draw, shortest);
            foreach (var line in result.Lines.Where(l => result.Accepted.Contains(l.BestMove!)).Take(3))
            {
                var after = p.Clone();
                bool valid = true;
                foreach (string u in line.Pv.Take(2))
                {
                    var move = MoveGenerator.GenerateLegal(after).FirstOrDefault(m => m.Uci == u);
                    if (move is null)
                    {
                        valid = false;
                        break;
                    }

                    after.Apply(move);
                }

                if (valid && line.Pv.Count >= 2)
                    _ = Observe(Get(after, draw, shortest));
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (IOException)
        {
        }
    }

    private static async Task Observe(Task task)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
        }
        catch (IOException)
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (pool is not null)
            await pool.DisposeAsync();
    }
}
