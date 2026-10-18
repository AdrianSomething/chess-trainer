namespace ChessOpeningTrainer;
public static class MatingPractice
{
    public static bool Supports(string kind) => kind is "Damenmatt" or "Turmmatt" or "Zwei Läufer" or "Läufer + Springer";
    public static Position Generate(string kind)
    {
        string pieces = kind switch
        {
            "Damenmatt" => "Q",
            "Turmmatt" => "R",
            "Zwei Läufer" => "BB",
            _ => "BN"
        };
        for (int attempt = 0; attempt < 10000; attempt++)
        {
            var p = new Position
            {
                Castling = ""
            };
            var squares = Enumerable.Range(0, 64).OrderBy(_ => Random.Shared.Next()).Take(pieces.Length + 2).ToArray();
            p.Squares[squares[0]] = 'K';
            p.Squares[squares[1]] = 'k';
            for (int i = 0; i < pieces.Length; i++)
                p.Squares[squares[i + 2]] = pieces[i];
            if (kind == "Zwei Läufer" && (squares[2] / 8 + squares[2] % 8) % 2 == (squares[3] / 8 + squares[3] % 8) % 2)
                continue;
            if (MoveGenerator.IsKingInCheck(p, true) || MoveGenerator.IsKingInCheck(p, false) || SafeMoves(p, new Dictionary<string, int>()).Count == 0)
                continue;
            return p;
        }

        throw new InvalidOperationException("Keine geeignete Endspielstellung gefunden.");
    }

    public static List<Move> SafeMoves(Position p, IReadOnlyDictionary<string, int> repetitions)
    {
        bool attacker = p.WhiteToMove;
        return MoveGenerator.GenerateLegal(p).Where(move =>
        {
            var after = p.Clone();
            after.Apply(move);
            var replies = MoveGenerator.GenerateLegal(after);
            if (replies.Count == 0)
                return MoveGenerator.IsKingInCheck(after, after.WhiteToMove);
            if (after.Halfmove >= 100 || repetitions.GetValueOrDefault(after.RepetitionKey()) >= 2)
                return false;
            return replies.All(reply => after.Squares[reply.To] == '\0' || char.IsUpper(after.Squares[reply.To]) != attacker);
        }).ToList();
    }
}
