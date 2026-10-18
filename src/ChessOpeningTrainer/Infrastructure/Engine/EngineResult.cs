using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChessOpeningTrainer;
public sealed class EngineResult
{
    public string? BestMove { get; init; }
    public int? ScoreCentipawns { get; init; }
    public int? Mate { get; init; }
    public string? Error { get; init; }
    public IReadOnlyList<EngineLine> Lines { get; init; } = [];
    public string Display => Mate is int mate ? $"Matt in {mate}" : ScoreCentipawns is int score ? $"{(score >= 0 ? "+" : "")}{score / 100.0:0.00}" : "—";
}
