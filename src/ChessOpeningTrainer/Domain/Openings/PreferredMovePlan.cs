namespace ChessOpeningTrainer;

public sealed record PreferredStep(int From, int To, char Piece);
public readonly record struct PreferredProgress(int Next, int Deviations);

/// <summary>Geometric learner-side plan; it never mutates the actual starting position.</summary>
public static class PreferredMovePlan
{
    public static Position Preview(Position start, IEnumerable<PreferredStep> steps)
    {
        var preview = start.Clone();
        foreach (var step in steps)
        {
            bool castle = char.ToLowerInvariant(step.Piece) == 'k' && Math.Abs(step.To - step.From) == 2;
            preview.Squares[step.From] = '\0'; preview.Squares[step.To] = step.Piece;
            if (castle) { int rook = step.To > step.From ? step.From + 3 : step.From - 4; preview.Squares[step.To > step.From ? step.From + 1 : step.From - 1] = preview.Squares[rook]; preview.Squares[rook] = '\0'; }
            if (char.ToLowerInvariant(step.Piece) == 'k') preview.Castling = preview.Castling.Replace(char.IsUpper(step.Piece) ? "K" : "k", "").Replace(char.IsUpper(step.Piece) ? "Q" : "q", "");
            if (step.From is 0 or 7 or 56 or 63) preview.Castling = preview.Castling.Replace(step.From switch { 0 => "Q", 7 => "K", 56 => "q", _ => "k" }, "");
        }
        return preview;
    }
    public static bool CanAppend(Position preview, bool white, int from, int to)
    {
        if (from is < 0 or > 63 || to is < 0 or > 63 || from == to) return false;
        char piece = preview.Squares[from], target = preview.Squares[to];
        if (piece == '\0' || char.IsUpper(piece) != white || target != '\0' && char.IsUpper(target) == white) return false;
        int dx = to % 8 - from % 8, dy = to / 8 - from / 8, ax = Math.Abs(dx), ay = Math.Abs(dy);
        if (char.ToLowerInvariant(piece) == 'k' && from == (white ? 4 : 60) && dy == 0 && ax == 2)
        {
            bool shortSide = dx > 0;
            char right = white ? shortSide ? 'K' : 'Q' : shortSide ? 'k' : 'q';
            int rook = from + (shortSide ? 3 : -4);
            if (!preview.Castling.Contains(right) || preview.Squares[rook] != (white ? 'R' : 'r')) return false;
            for (int square = from + Math.Sign(dx); square != rook; square += Math.Sign(dx))
                if (preview.Squares[square] != '\0' && char.IsUpper(preview.Squares[square]) == white) return false;
            return true; // Attack checks belong to actual legal generation, not the geometric preference plan.
        }
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
