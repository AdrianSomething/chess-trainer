namespace ChessOpeningTrainer;

public static class VisionComparisonQuality
{
    private static int Value(char piece) => char.ToLowerInvariant(piece) switch { 'p' => 1, 'n' or 'b' => 3, 'r' => 5, 'q' => 9, _ => 0 };
    public static int ImmediateGain(Position before, Move move)
    {
        int Balance(Position p) => p.Squares.Where(c => c != '\0').Sum(c => (char.IsUpper(c) == before.WhiteToMove ? 1 : -1) * Value(c));
        var after = before.Clone(); after.Apply(move); return Balance(after) - Balance(before);
    }
    public static bool Suitable(VisionComparison exercise) => exercise.Moves.Length == 2 && exercise.Moves.All(m => ImmediateGain(exercise.Before, m) <= 1)
        && (exercise.ScoresWhite is null || exercise.ScoresWhite.Length == 2 && exercise.ScoresWhite.All(s => double.IsFinite(s) && Math.Abs(s) <= 3));
    public static int Priority(VisionComparison exercise) => (exercise.ScoresWhite is null ? 2 : 0) + exercise.Moves.Max(m => Math.Max(0, ImmediateGain(exercise.Before, m)));
}
