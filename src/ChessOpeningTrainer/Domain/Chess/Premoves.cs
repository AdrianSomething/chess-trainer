namespace ChessOpeningTrainer;
public static class Premoves
{
    public static List<Move> Candidates(Position p, int from, bool white)
    {
        char piece = p.Squares[from];
        if (piece == '\0' || char.IsUpper(piece) != white)
            return[];
        var result = new List<Move>();
        for (int to = 0; to < 64; to++)
        {
            if (to == from)
                continue;
            int dx = Math.Abs(to % 8 - from % 8), dy = to / 8 - from / 8, ay = Math.Abs(dy);
            bool shape = char.ToLowerInvariant(piece) switch
            {
                'p' => dy == (white ? 1 : -1) && dx <= 1 || dx == 0 && dy == (white ? 2 : -2) && from / 8 == (white ? 1 : 6),
                'n' => dx * ay == 2,
                'b' => dx == ay,
                'r' => dx == 0 || dy == 0,
                'q' => dx == ay || dx == 0 || dy == 0,
                'k' => Math.Max(dx, ay) == 1 || dy == 0 && dx == 2,
                _ => false
            };
            if (shape)
                result.Add(new Move { From = from, To = to, Promotion = char.ToLowerInvariant(piece) == 'p' && to / 8 is 0 or 7 ? 'q' : '\0' });
        }

        return result;
    }
}
