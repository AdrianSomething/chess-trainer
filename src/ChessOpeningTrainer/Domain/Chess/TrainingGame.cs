namespace ChessOpeningTrainer;

/// <summary>Headless standard-chess session. No controls, timers or engine processes.</summary>
public sealed class TrainingGame
{
    public Position Position { get; private set; }
    public int Plies { get; private set; }
    public int MaximumPlies { get; }
    private readonly Dictionary<string, int> repetitions = [];
    private string? cachedStatus;
    public TrainingGame(string? fen = null, int maximumFullMoves = 500)
    {
        Position = Position.FromFen(fen ?? Position.InitialFen);
        MaximumPlies = Math.Clamp(maximumFullMoves, 1, 10000) * 2;
        repetitions[Position.RepetitionKey()] = 1;
    }
    public string Status
    {
        get
        {
            if (cachedStatus is not null) return cachedStatus;
            string rules = MoveGenerator.GameState(Position, repetitions);
            if (rules is not ("" or "Schach")) return cachedStatus = rules == "Schachmatt" ? "checkmate" : "draw";
            return cachedStatus = Plies >= MaximumPlies ? "move_limit" : "playing";
        }
    }
    public string Result => Status == "checkmate" ? Position.WhiteToMove ? "0-1" : "1-0" : Status == "playing" ? "*" : "1/2-1/2";
    public bool Apply(string uci)
    {
        if (Status != "playing") return false;
        var move = MoveGenerator.GenerateLegal(Position).FirstOrDefault(m => m.Uci == uci);
        if (move is null) return false;
        Position.Apply(move); Plies++;
        cachedStatus = null;
        string key = Position.RepetitionKey(); repetitions[key] = repetitions.GetValueOrDefault(key) + 1;
        return true;
    }
    public static int ActionIndex(Move move) => (move.From * 64 + move.To) * 5 + (char.ToLowerInvariant(move.Promotion) switch { 'q' => 1, 'r' => 2, 'b' => 3, 'n' => 4, _ => 0 });
}
