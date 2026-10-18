using System.Text.Json;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;

namespace ChessOpeningTrainer;
public sealed class PersonalGame
{
    public List<EngineResult> PositionAnalyses { get; set; } = [];
    public GameAnalysisOptions? AnalysisOptions { get; set; }
    public string Id { get; set; } = "";
    public Dictionary<string, string> Tags { get; set; } = [];
    public string StartFen { get; set; } = Position.InitialFen;
    public List<string> Moves { get; set; } = [];
    public List<GameMoment> Analysis { get; set; } = [];
    public DateTime ImportedUtc { get; set; } = DateTime.UtcNow;
    public string White => Tags.GetValueOrDefault("White") ?? "Weiß";
    public string Black => Tags.GetValueOrDefault("Black") ?? "Schwarz";

    // Imported or interrupted analyses from older versions may contain only a
    // subset. Never present those isolated ratings as a completed game review.
    [System.Text.Json.Serialization.JsonIgnore]
    public bool HasCompleteAnalysis => Analysis.Count == Moves.Count
        && PositionAnalyses.Count == Moves.Count + 1
        && Analysis.Select(m => m.Ply).Order().SequenceEqual(Enumerable.Range(0, Moves.Count))
        && PositionAnalyses.All(r => r.Error is null);

    public override string ToString() => $"{Tags.GetValueOrDefault("Date") ?? "?"} · {White} – {Black} · {Tags.GetValueOrDefault("Result") ?? "*"}";
    public List<Position> Positions()
    {
        var p = Position.FromFen(StartFen);
        var result = new List<Position>
        {
            p.Clone()
        };
        foreach (var u in Moves)
        {
            var m = MoveGenerator.GenerateLegal(p).First(m => m.Uci == u);
            p.Apply(m);
            result.Add(p.Clone());
        }

        return result;
    }
}
