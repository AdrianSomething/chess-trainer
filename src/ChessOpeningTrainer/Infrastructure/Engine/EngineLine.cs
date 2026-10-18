using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChessOpeningTrainer;
public sealed class EngineLine
{
    public bool ContinuationChecked { get; set; }
    public string? ContinuationNote { get; set; }
    public List<string> Pv { get; init; } = [];
    public int Depth { get; init; }
    public int Rank { get; init; }
    public string? BestMove { get; init; }
    public int? ScoreCentipawns { get; init; }
    public int? Mate { get; init; }
}
