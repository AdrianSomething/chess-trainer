namespace ChessOpeningTrainer;
public static class EvaluationPresentation
{
    public static void Show(EvalScale bar, Position p, EngineResult result)
    {
        int white = (p.WhiteToMove ? 1 : -1) * (result.Mate is int m ? Math.Sign(m) * 10000 : result.ScoreCentipawns ?? 0);
        bar.Score = white;
        bar.TextValue = result.Mate is int mate ? $"M{(p.WhiteToMove ? mate : -mate)}" : $"{white / 100d:+0.00;-0.00;0.00}";
        bar.Invalidate();
    }
}
