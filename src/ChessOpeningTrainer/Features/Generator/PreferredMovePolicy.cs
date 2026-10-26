namespace ChessOpeningTrainer;

public static class PreferredMovePolicy
{
    public static EngineLine? Choose(Position position, IReadOnlyList<EngineLine> lines, GeneratorOptions options, PreferredProgress progress)
    {
        if (progress.Deviations >= options.PreferenceDeviationLimit || options.PreferredMoves.Count == 0) return null;
        var legal = MoveGenerator.GenerateLegal(position).ToDictionary(m => m.Uci);
        double best = OpeningGenerator.Score(lines[0]);
        foreach (var step in options.PreferredMoves.Skip(progress.Next))
        {
            if (position.Squares[step.From] != step.Piece) continue;
            var line = lines.FirstOrDefault(l => l.BestMove is { } u && legal.TryGetValue(u, out var m) && m.From == step.From && m.To == step.To
                && OpeningGenerator.Score(l) > options.PreferenceMinimumEvaluation
                && best - OpeningGenerator.Score(l) <= options.PreferenceMaximumLoss + 1e-8);
            if (line is not null) return line;
        }
        return null;
    }
    public static PreferredProgress Advance(Position before, Move move, GeneratorOptions options, PreferredProgress progress)
    {
        if (progress.Deviations >= options.PreferenceDeviationLimit) return progress;
        for (int i = progress.Next; i < options.PreferredMoves.Count; i++)
        {
            var step = options.PreferredMoves[i];
            if (move.From == step.From && move.To == step.To && before.Squares[move.From] == step.Piece) return new(i + 1, 0);
        }
        return progress with { Deviations = progress.Deviations + 1 };
    }
}
