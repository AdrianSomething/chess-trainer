namespace ChessOpeningTrainer;

/// <summary>Geometric uniqueness and tactical quality are separate requirements.</summary>
public sealed class VisionSafetyGenerator(StockfishEngine engine)
{
    public async Task<VisionSafetyExercise> Generate(bool white, CancellationToken token)
    {
        await using var pool = new EnginePool(engine, 1, 1, token);
        while (true)
        {
            token.ThrowIfCancellationRequested();
            var exercise = await Task.Run(() => VisionSafety.Generate(white, token), token);
            var result = await pool.Search(exercise.Position, 10, cancellationToken: token);
            if (Accepts(exercise, result)) return exercise;
        }
    }
    public static bool Accepts(VisionSafetyExercise exercise, EngineResult result)
    {
        if (exercise.Destinations.Count != 1 || result.Lines.Count == 0) return false;
        var moves = MoveGenerator.GenerateLegal(exercise.Position)
            .Where(m => m.From == exercise.Origin && exercise.Destinations.Contains(m.To)).Select(m => m.Uci).ToHashSet();
        var best = result.Lines.Max(OpeningGenerator.Score);
        return result.Lines.Any(l => l.BestMove is not null && moves.Contains(l.BestMove)
            && (l.Depth >= 10 || l.Mate is not null) && best - OpeningGenerator.Score(l) <= 1 + 1e-8);
    }
}
