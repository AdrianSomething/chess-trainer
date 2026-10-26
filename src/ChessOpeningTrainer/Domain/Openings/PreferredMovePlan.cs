namespace ChessOpeningTrainer;

public sealed record PreferredStep(int From, int To, char Piece);
public readonly record struct PreferredProgress(int Next, int Deviations);

/// <summary>Geometric learner-side plan; it never mutates the actual starting position.</summary>
public static class PreferredMovePlan
{
    public static Position Preview(Position start, IEnumerable<PreferredStep> steps)
    {
        var preview = start.Clone();
        foreach (var step in steps) { preview.Squares[step.From] = '\0'; preview.Squares[step.To] = step.Piece; }
        return preview;
    }
    public static bool CanAppend(Position preview, bool white, int from, int to)
    {
        if (from is < 0 or > 63 || to is < 0 or > 63 || from == to) return false;
        char piece = preview.Squares[from], target = preview.Squares[to];
        if (piece == '\0' || char.IsUpper(piece) != white || target != '\0' && char.IsUpper(target) == white) return false;
        int dx = to % 8 - from % 8, dy = to / 8 - from / 8, ax = Math.Abs(dx), ay = Math.Abs(dy);
        bool geometry = char.ToLowerInvariant(piece) switch
        {
            'n' => ax * ay == 2,
            'b' => ax == ay,
            'r' => dx == 0 || dy == 0,
            'q' => ax == ay || dx == 0 || dy == 0,
            'k' => Math.Max(ax, ay) == 1,
            'p' => ax <= 1 && dy == (white ? 1 : -1) || ax == 0 && from / 8 == (white ? 1 : 6) && dy == (white ? 2 : -2),
            _ => false
        };
        if (!geometry) return false;
        if (char.ToLowerInvariant(piece) != 'n')
            for (int f = from % 8 + Math.Sign(dx), r = from / 8 + Math.Sign(dy); f != to % 8 || r != to / 8; f += Math.Sign(dx), r += Math.Sign(dy))
            { char blocker = preview.Squares[r * 8 + f]; if (blocker != '\0' && char.IsUpper(blocker) == white) return false; }
        return true;
    }
}
