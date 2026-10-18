using System.Text.Json;

namespace ChessOpeningTrainer;
public sealed class PuzzleProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Training";
    public int MoveSeconds { get; set; }
    public int TotalSeconds { get; set; }
    public int Lives { get; set; }
    public bool RevealOnError { get; set; }
    public bool ContinueAfterMoveTimeout { get; set; }
    public string Difficulty { get; set; } = "Alle";
    public bool Mate { get; set; } = true;
    public bool Material { get; set; } = true;
    public bool Draw { get; set; }
    public bool Positional { get; set; }
    public bool PositionalSubtle { get; set; }
    public int MinGoodMoves { get; set; } = 2;
    public int MaxGoodMoves { get; set; } = 4;
    public List<string> Endgames { get; set; } = [];
    public bool Tactics { get; set; } = true;
    public int DefenseMoves { get; set; } = 10;
    public bool FullEndgames { get; set; } = true;
    public int HighScore { get; set; }
    public int Completed { get; set; }
    public bool ExplicitlySaved { get; set; }
    public bool StrictOptimal { get; set; }
    public bool CanLose => Lives > 0 || TotalSeconds > 0 || MoveSeconds > 0;

    public PuzzleProfile Copy() => JsonSerializer.Deserialize<PuzzleProfile>(JsonSerializer.Serialize(this))!;
    public override string ToString() => Name;
}
