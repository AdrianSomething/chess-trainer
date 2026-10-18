namespace ChessOpeningTrainer;
public sealed class GameAnalysisOptions
{
    public int Workers { get; set; } = 8;
    public int Threads { get; set; } = 2;
    public int Depth { get; set; } = 14;
    public double Blunder { get; set; } = -2;
    public double Mistake { get; set; } = -.75;
    public double OpportunityTolerance { get; set; } = .5;
    public int MaxOpportunityMoves { get; set; } = 3;
    public double EdgeDamping { get; set; } = .08;

    public GameAnalysisOptions Copy() => (GameAnalysisOptions)MemberwiseClone();
    public void Validate()
    {
        if (Workers < 1 || Workers > 32 || Threads < 1 || Threads > 32 || Depth < 5 || Depth > 24 || Blunder >= Mistake || Mistake >= 0 || OpportunityTolerance < 0 || MaxOpportunityMoves < 1 || EdgeDamping < 0)
            throw new ArgumentException("Ungültige Partieanalyse-Einstellungen.");
    }

    public double WeightedLoss(double best, double played)
    {
        double edge = Math.Sign(best) == Math.Sign(played) ? Math.Min(Math.Abs(best), Math.Abs(played)) : 0;
        return Math.Max(0, best - played) / (1 + EdgeDamping * edge * edge);
    }

    public string Classify(double best, double played, bool bestMove, GameMoment? previous, int goodMoves)
    {
        if (bestMove)
            return "Bester Zug";
        double loss = WeightedLoss(best, played);
        if (previous?.Category == "Patzer" && goodMoves <= MaxOpportunityMoves && best > -previous.Before + OpportunityTolerance)
        {
            double original = -previous.Before;
            if (played < original - OpportunityTolerance)
                return "Patzer";
            if (Math.Abs(played - original) <= OpportunityTolerance)
                return "Verpasste Chance";
        }

        return loss >= -Blunder ? "Patzer" : loss >= -Mistake ? "Schlecht" : "Gut";
    }
}
