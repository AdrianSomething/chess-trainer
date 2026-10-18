namespace ChessOpeningTrainer;

/// <summary>Chooses from a complete best-to-worst ranking; the midpoint is uniform.</summary>
public static class EngineMoveChoice
{
    public static string? Select(IReadOnlyList<string> bestToWorst, int intelligence, Random random)
    {
        if (bestToWorst.Count == 0) return null;
        intelligence = Math.Clamp(intelligence, 0, 100);
        if (intelligence == 0) return bestToWorst[^1];
        if (intelligence == 100) return bestToWorst[0];
        if (intelligence == 50) return bestToWorst[random.Next(bestToWorst.Count)];
        double exponent = intelligence < 50 ? 1 / (1 + (50 - intelligence) / 10d) : 1 + (intelligence - 50) / 10d;
        int rank = Math.Min(bestToWorst.Count - 1, (int)(Math.Pow(random.NextDouble(), exponent) * bestToWorst.Count));
        return bestToWorst[rank];
    }
}
