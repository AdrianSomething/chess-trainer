namespace ChessOpeningTrainer;

/// <summary>Presentation-only spacing validation; independent of opening data.</summary>
public static class BranchLayoutOptions
{
    public const double MinimumSpacing = .25;
    public const double MaximumSpacing = 5;
    public static double ClampSpacing(double value) => double.IsFinite(value) ? Math.Clamp(value, MinimumSpacing, MaximumSpacing) : 1;
}
