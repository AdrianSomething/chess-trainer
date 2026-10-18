namespace ChessOpeningTrainer;
public static class PromotionProof
{
    public static EndgameExercise Generate(CancellationToken token)
    {
        // Multiple pawns on both wings, randomized legal kings. The objective
        // is a safe first promotion, not a claim of a tablebase-proven game win.
        for (int attempt = 0; attempt < 80; attempt++)
        {
            token.ThrowIfCancellationRequested();
            var p = new Position
            {
                Castling = ""
            };
            int file = Random.Shared.Next(8);
            p.Squares[48 + file] = 'P';
            p.Squares[16 + (file + 3) % 8] = 'P';
            p.Squares[40 + (file + 5) % 8] = 'p';
            int wk = Random.Shared.Next(8, 40), bk = Random.Shared.Next(24, 64);
            if (p.Squares[wk] != '\0' || p.Squares[bk] != '\0' || wk == bk)
                continue;
            p.Squares[wk] = 'K';
            p.Squares[bk] = 'k';
            if (MoveGenerator.IsKingInCheck(p, false) || MoveGenerator.IsKingInCheck(p, true))
                continue;
            var puzzle = new Puzzle
            {
                Id = Guid.NewGuid().ToString("N"),
                Fen = p.ToFen(),
                Theme = "Sichere Umwandlung in höchstens 3 Zügen",
                Origin = "Komposition · vollständige Zielprüfung",
                MinimumDepth = 0
            };
            try
            {
                puzzle.PreparedMateRoutes = Prepare(p, 3, token);
                return new()
                {
                    Kind = "Komplexe Bauernendspiele",
                    Puzzle = puzzle
                };
            }
            catch (InvalidOperationException)
            {
            }
        }

        throw new InvalidOperationException("Keine vollständig geprüfte Bauernaufgabe gefunden.");
    }

    public static bool SafePromotion(Position p, bool attacker)
    {
        int promoted = Array.FindIndex(p.Squares, c => char.IsUpper(c) == attacker && char.ToLowerInvariant(c)is 'q' or 'r');
        if (promoted < 0)
            return false;
        var legal = MoveGenerator.GenerateLegal(p);
        return (legal.Count > 0 || MoveGenerator.IsKingInCheck(p, p.WhiteToMove)) && legal.All(m => m.To != promoted && m.Promotion == '\0');
    }

    public static Dictionary<string, List<MateRoute>> Prepare(Position root, int goal, CancellationToken token)
    {
        bool attacker = root.WhiteToMove;
        int visits = 0;
        var memo = new Dictionary<string, bool>();
        var routes = new Dictionary<string, List<MateRoute>>();
        bool Wins(Position p, int plies)
        {
            token.ThrowIfCancellationRequested();
            if (++visits > 150000)
                throw new InvalidOperationException("Prüfbudget");
            if (SafePromotion(p, attacker) && p.WhiteToMove != attacker)
                return true;
            if (p.Squares.Any(c => char.IsUpper(c) != attacker && char.ToLowerInvariant(c)is 'q' or 'r'))
                return false;
            if (plies <= 0)
                return false;
            string key = MatePreparation.Key(p, plies);
            if (memo.TryGetValue(key, out bool result))
                return result;
            var legal = MoveGenerator.GenerateLegal(p);
            if (legal.Count == 0)
                return false;
            bool own = p.WhiteToMove == attacker;
            foreach (var move in legal.OrderByDescending(m => m.Promotion != '\0'))
            {
                var next = p.Clone();
                next.Apply(move);
                bool won = Wins(next, plies - 1);
                if (own && won)
                    return memo[key] = true;
                if (!own && !won)
                    return memo[key] = false;
            }

            return memo[key] = !own;
        }

        void Build(Position p, int left)
        {
            string key = MatePreparation.Key(p, left);
            if (routes.ContainsKey(key))
                return;
            var moves = new List<MateRoute>();
            routes[key] = moves;
            foreach (var m in MoveGenerator.GenerateLegal(p))
            {
                var after = p.Clone();
                after.Apply(m);
                if (!Wins(after, left * 2 - 2))
                    continue;
                if (SafePromotion(after, attacker))
                {
                    moves.Add(new(m.Uci, null));
                    continue;
                }

                var defenses = MoveGenerator.GenerateLegal(after);
                var defense = defenses.OrderBy(_ => Random.Shared.Next()).First();
                after.Apply(defense);
                moves.Add(new(m.Uci, defense.Uci));
                Build(after, left - 1);
            }
        }

        if (!Wins(root, goal * 2 - 1))
            throw new InvalidOperationException("Umwandlung nicht bewiesen");
        Build(root, goal);
        return routes;
    }
}
