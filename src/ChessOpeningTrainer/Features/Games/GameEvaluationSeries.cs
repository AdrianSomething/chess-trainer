namespace ChessOpeningTrainer;

/// <summary>Saved game evaluations in white's perspective; no new engine searches.</summary>
public sealed record GameEvaluationPoint(int PositionIndex, double WhitePawns, int? WhiteMate)
{
    public string Display => WhiteMate is int mate ? $"M{mate}" : $"{WhitePawns:+0.00;-0.00;0.00}";
}

public static class GameEvaluationSeries
{
    public static IReadOnlyList<GameEvaluationPoint> Build(PersonalGame game)
    {
        if (!game.HasCompleteAnalysis) return [];
        var positions = game.Positions();
        return game.PositionAnalyses.Select((result, i) =>
        {
            var first = result.Lines.FirstOrDefault();
            int? mate = result.Mate ?? first?.Mate;
            int sign = positions[i].WhiteToMove ? 1 : -1;
            int? whiteMate = mate is int distance ? sign * distance : null;
            double pawns = mate is int m ? Math.Sign(m) * 12 : (result.ScoreCentipawns ?? first?.ScoreCentipawns ?? 0) / 100d;
            string state = MoveGenerator.GameState(positions[i], new Dictionary<string, int>());
            if (state.StartsWith("Remis", StringComparison.Ordinal)) { pawns = 0; whiteMate = null; }
            else if (state == "Schachmatt") { pawns = -12; whiteMate = -sign; }
            return new GameEvaluationPoint(i, sign * pawns, whiteMate);
        }).ToArray();
    }
}
