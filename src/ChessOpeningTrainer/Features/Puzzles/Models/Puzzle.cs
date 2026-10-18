using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ChessOpeningTrainer;
public sealed class Puzzle
{
    public string? SetupFen { get; set; }
    public List<string> SetupMoves { get; set; } = [];
    public string? OpeningAnchorFen { get; set; }
    public string PositionalStyle { get; set; } = "";
    public string? PastMove { get; set; }
    public int MateDefenseVersion { get; set; }
    public int DrawProofVersion { get; set; }
    public List<string> AcceptedMoves { get; set; } = [];
    public List<EngineLine> SolutionLines { get; set; } = [];
    public string Id { get; set; } = "";
    public string Fen { get; set; } = "";
    public List<string> Moves { get; set; } = [];
    public string Theme { get; set; } = "Materialgewinn";
    public int MinimumDepth { get; set; }
    public DateTime? LastPlayedUtc { get; set; }
    public string Origin { get; set; } = "Komposition";
    public string Fingerprint { get; set; } = "";
    public int GoalMoves { get; set; } = 3;
    public string GoalKind { get; set; } = "";
    public Dictionary<string, List<MateRoute>>? PreparedMateRoutes { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public int? MateGoal => Theme.StartsWith("Matt in ") && int.TryParse(Theme[8..], out int n) ? n : null;

    [System.Text.Json.Serialization.JsonIgnore]
    public string Difficulty
    {
        get
        {
            if (MateGoal == 1) return "Leicht";
            string key = Fen + "|" + Theme + "|" + Moves.Count;
            if (difficultyKey == key && difficultyValue is not null) return difficultyValue;
            int decisions = MateGoal ?? (Moves.Count + 1) / 2;
            int options = MoveGenerator.GenerateLegal(Position.FromFen(Fen)).Count;
            difficultyKey = key;
            return difficultyValue = decisions >= 4 || decisions >= 3 && options >= 28 || options >= 40 ? "Schwer" : decisions <= 2 && options <= 28 ? "Leicht" : "Normal";
        }
    }
    private string? difficultyKey, difficultyValue;
}
