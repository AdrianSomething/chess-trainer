using System.Text;
using System.Text.Json;

namespace ChessOpeningTrainer;

/// <summary>Versioned JSON-lines adapter for optional local training clients. Never opens the normal app.</summary>
public sealed class TrainingProtocol : IAsyncDisposable
{
    private readonly Dictionary<string, TrainingGame> games = [];
    private readonly StockfishEngine engine;
    private EnginePool? teacher;
    private readonly CancellationTokenSource lifetime = new();
    private readonly CancellationToken token;
    private int disposed;
    private static readonly JsonSerializerOptions json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    public TrainingProtocol(string appDirectory) { engine = new(appDirectory); token = lifetime.Token; }
    private static int Int(JsonElement e, string name, int fallback) => e.TryGetProperty(name, out var value) ? value.GetInt32() : fallback;
    private static string? Str(JsonElement e, string name) => e.TryGetProperty(name, out var value) ? value.GetString() : null;
    private static object State(string id, TrainingGame game) => new
    {
        id, fen = game.Position.ToFen(), board = new string(game.Position.Squares.Select(c => c == '\0' ? '.' : c).ToArray()),
        white = game.Position.WhiteToMove, castling = game.Position.Castling, plies = game.Plies, status = game.Status, result = game.Result,
        legal = game.Status == "playing" ? MoveGenerator.GenerateLegal(game.Position).Select(m => new { uci = m.Uci, index = TrainingGame.ActionIndex(m) }).ToArray() : []
    };
    private async Task<EngineResult> Analyze(Position p, int depth, int multi = 1)
    {
        teacher ??= new EnginePool(engine, 1, 2, token);
        return await teacher.Search(p, Math.Clamp(depth, 1, 64), Math.Clamp(multi, 1, 10), token);
    }
    public async Task<object> Execute(JsonElement request)
    {
        token.ThrowIfCancellationRequested();
        switch (Str(request, "op"))
        {
            case "hello": return new { protocol = 1, actions = 20480, stockfish = engine.Path is not null };
            case "reset":
            {
                string id = Str(request, "id") ?? "0";
                if (!games.ContainsKey(id) && games.Count >= 4096) throw new InvalidOperationException("Maximum 4096 concurrent games.");
                var game = new TrainingGame(Str(request, "fen"), Int(request, "maxMoves", 500));
                var random = new Random(Int(request, "seed", 0));
                int randomPlies = Math.Clamp(Int(request, "randomPlies", 0), 0, 300);
                for (int i = 0; i < randomPlies && game.Status == "playing"; i++)
                { token.ThrowIfCancellationRequested(); var legal = MoveGenerator.GenerateLegal(game.Position); game.Apply(legal[random.Next(legal.Count)].Uci); }
                // A supplied/generated position is the start of the measured game.
                games[id] = new TrainingGame(game.Position.ToFen(), Int(request, "maxMoves", 500));
                return State(id, games[id]);
            }
            case "state": { string id = Str(request, "id") ?? "0"; return State(id, games[id]); }
            case "step":
            {
                string id = Str(request, "id") ?? "0"; var game = games[id];
                bool accepted = game.Apply(Str(request, "move") ?? "");
                return new { accepted, state = State(id, game) };
            }
            case "remove": games.Remove(Str(request, "id") ?? "0"); return new { removed = true };
            case "analyze":
            {
                var position = Str(request, "fen") is string fen ? Position.FromFen(fen) : games[Str(request, "id") ?? "0"].Position;
                var result = await Analyze(position, Int(request, "depth", 10), Int(request, "multi", 1));
                if (result.Error is not null) throw new IOException(result.Error);
                return new { lines = result.Lines.Select(l => new { move = l.BestMove, cp = l.ScoreCentipawns, mate = l.Mate, depth = l.Depth, pv = l.Pv }) };
            }
            case "batch":
            {
                var results = new List<object>();
                foreach (var operation in request.GetProperty("items").EnumerateArray())
                { if (Str(operation, "op") == "batch") throw new ArgumentException("Nested batches are not supported."); results.Add(await Execute(operation)); }
                return results;
            }
            default: throw new ArgumentException("Unknown operation.");
        }
    }
    public static async Task Run(string appDirectory)
    {
        using var input = new StreamReader(Console.OpenStandardInput(), Encoding.UTF8);
        using var output = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true };
        await using var host = new TrainingProtocol(appDirectory);
        while (await input.ReadLineAsync() is { } line)
        {
            try
            {
                if (line.Length > 8_000_000) throw new ArgumentException("Request too large.");
                using var document = JsonDocument.Parse(line);
                await output.WriteLineAsync(JsonSerializer.Serialize(new { ok = true, result = await host.Execute(document.RootElement) }, json));
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException or JsonException or IOException or FormatException)
            { await output.WriteLineAsync(JsonSerializer.Serialize(new { ok = false, error = ex.Message }, json)); }
        }
    }
    public async ValueTask DisposeAsync() { if (Interlocked.Exchange(ref disposed, 1) != 0) return; lifetime.Cancel(); if (teacher is not null) await teacher.DisposeAsync(); lifetime.Dispose(); }
}
