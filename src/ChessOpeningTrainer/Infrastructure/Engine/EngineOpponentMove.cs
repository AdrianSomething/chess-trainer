namespace ChessOpeningTrainer;

internal static class EngineOpponentMove
{
    public static async Task<(Move? Move, string? Error)> Choose(StockfishEngine engine, Position position, int depth, int intelligence, CancellationToken token)
    {
        var legal = MoveGenerator.GenerateLegal(position);
        if (legal.Count == 0) return (null, null);
        if (intelligence == 50) return (legal[Random.Shared.Next(legal.Count)], null);
        var result = await engine.AnalyzeAsync(position.ToFen(), depth, intelligence == 100 ? 1 : legal.Count, 4, 256, token, 2500);
        var ranking = result.Lines.OrderBy(l => l.Rank).Select(l => l.BestMove).Where(u => u is not null && legal.Any(m => m.Uci == u)).Cast<string>().Distinct().ToList();
        if (ranking.Count == 0 && result.BestMove is not null) ranking.Add(result.BestMove);
        var selected = EngineMoveChoice.Select(ranking, intelligence, Random.Shared);
        return (legal.FirstOrDefault(m => m.Uci == selected), result.Error);
    }
}
