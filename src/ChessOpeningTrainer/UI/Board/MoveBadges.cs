namespace ChessOpeningTrainer;
public static class MoveBadges
{
    public static List<MoveBadge> Between(Position before, Position after)
    {
        var result = new List<MoveBadge>();
        if (after.Captures.Count > before.Captures.Count)
            result.Add(new(after.Captures[^1], true));
        char pawn = before.WhiteToMove ? 'P' : 'p';
        if (after.Squares.Count(c => c == pawn) < before.Squares.Count(c => c == pawn))
            foreach (char kind in "qrbn")
            {
                char piece = before.WhiteToMove ? char.ToUpperInvariant(kind) : kind;
                if (after.Squares.Count(c => c == piece) > before.Squares.Count(c => c == piece))
                {
                    result.Add(new(pawn));
                    result.Add(new(piece, false, true));
                    break;
                }
            }

        if (before.Squares.Count(c => c == 'K') == 1 && before.Squares.Count(c => c == 'k') == 1 &&
            after.Squares.Count(c => c == 'K') == 1 && after.Squares.Count(c => c == 'k') == 1 && MoveGenerator.IsKingInCheck(after, after.WhiteToMove))
            result.Add(new(after.WhiteToMove ? 'K' : 'k', MoveGenerator.GenerateLegal(after).Count == 0));
        return result;
    }
}
