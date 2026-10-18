namespace ChessOpeningTrainer;
public static class AdvancedEndgames
{
    public static bool Supports(string kind) => kind is "Komplexe Bauernendspiele" or "Bauern + Läufer" or "Bauern + Springer" or "Bauernverteidigung: mehrere Bauern";
    public static async Task<Puzzle> Generate(string kind, StockfishEngine engine, CancellationToken token)
    {
        bool draw = kind.Contains("Verteidigung", StringComparison.OrdinalIgnoreCase) || (kind is "Bauern + Läufer" or "Bauern + Springer") && Random.Shared.Next(4) == 0;
        for (int attempt = 0; attempt < 150; attempt++)
        {
            token.ThrowIfCancellationRequested();
            var p = Compose(kind);
            if (p is null)
                continue;
            var root = await engine.AnalyzeAsync(p.ToFen(), 20, 2, 2, 128, token, 700);
            if (root.Error is not null)
                throw new IOException(root.Error);
            int value = root.ScoreCentipawns ?? (root.Mate > 0 ? 10000 : -10000);
            if (draw ? Math.Abs(value) > 25 : value < 160 || value > 1400)
                continue;
            var initial = p.Clone();
            var line = new List<string>();
            var ids = Enumerable.Range(0, 64).ToArray();
            var moved = new HashSet<int>();
            int minorMoves = 0;
            bool success = false;
            var reps = new Dictionary<string, int>();
            var pv = new Queue<string>(root.Lines.FirstOrDefault()?.Pv ?? []);
            for (int ply = 0; ply < 100; ply++)
            {
                token.ThrowIfCancellationRequested();
                var legal = MoveGenerator.GenerateLegal(p);
                string state = MoveGenerator.GameState(p, reps);
                if (state is not ("" or "Schach"))
                {
                    success = draw ? state.StartsWith("Remis") : state == "Schachmatt" && !p.WhiteToMove;
                    break;
                }

                if (draw && !kind.Contains("Verteidigung", StringComparison.OrdinalIgnoreCase) && line.Count >= 5 && p.Squares.Count(c => char.ToLowerInvariant(c) == 'p') <= 1)
                {
                    var table = await PawnTablebase.Shared;
                    if (DrawPreparation.Secured(p, true, table))
                    {
                        success = true;
                        break;
                    }
                }

                if (pv.Count == 0)
                {
                    var r = await engine.AnalyzeAsync(p.ToFen(), 20, 1, 2, 128, token, 450);
                    if (r.Error is not null)
                        break;
                    foreach (var u in r.Lines.FirstOrDefault()?.Pv ?? [])
                        pv.Enqueue(u);
                    if (pv.Count == 0)
                        break;
                }

                string uci = pv.Dequeue();
                var move = legal.FirstOrDefault(m => m.Uci == uci);
                if (move is null)
                    break;
                bool own = p.WhiteToMove;
                char piece = p.Squares[move.From];
                if (own)
                {
                    moved.Add(ids[move.From]);
                    if (char.ToLowerInvariant(piece)is 'b' or 'n')
                        minorMoves++;
                }

                ids[move.To] = ids[move.From];
                ids[move.From] = -1;
                line.Add(move.Uci);
                p.Apply(move);
                reps[p.RepetitionKey()] = reps.GetValueOrDefault(p.RepetitionKey()) + 1;
                if (!draw && own && move.Promotion != '\0' && kind is "Bauern + Läufer" or "Bauern + Springer")
                {
                    if (minorMoves < 2 || line.Count < 7)
                        break;
                    var check = await engine.AnalyzeAsync(p.ToFen(), 23, 1, 2, 128, token, 1000);
                    success = check.Mate < 0 || check.ScoreCentipawns <= -180;
                    break;
                }

                if (own && move.Promotion != '\0' && line.Count < 5)
                    break;
            }

            if (!success || line.Count < 5 || kind.Contains("Verteidigung", StringComparison.OrdinalIgnoreCase) && minorMoves < 1 || !draw && kind == "Komplexe Bauernendspiele" && moved.Count < 3 || kind is "Bauern + Läufer" or "Bauern + Springer" && minorMoves < 2)
                continue;
            var confirmation = await engine.AnalyzeAsync(initial.ToFen(), 24, 2, 2, 256, token, 1600);
            if (draw ? Math.Abs(confirmation.ScoreCentipawns ?? 1000) > 35 : confirmation.Mate is null && (confirmation.ScoreCentipawns ?? 0) < 150)
                continue;
            return new Puzzle
            {
                Id = Guid.NewGuid().ToString("N"),
                Fen = initial.ToFen(),
                Moves = line,
                Theme = draw ? "Remis retten · Bauern und Leichtfigur" : kind == "Komplexe Bauernendspiele" ? "Bis zum Matt ausspielen · mindestens drei Figuren im geprüften Musterweg" : "Leichtfigur aktivieren · sichere Umwandlung oder Matt",
                GoalKind = draw ? "AdvancedDraw" : "AdvancedWin",
                Origin = "Komposition · zwei Engineprüfungen und legaler Musterweg",
                MinimumDepth = confirmation.Lines.FirstOrDefault()?.Depth ?? 0
            };
        }

        throw new InvalidOperationException("Keine Stellung mit den geforderten Qualitätsmerkmalen gefunden. Die Generierung kann erneut gestartet werden; es wird keine triviale Ersatzaufgabe verwendet.");
    }

    private static Position? Compose(string kind)
    {
        if (kind.Contains("Verteidigung", StringComparison.OrdinalIgnoreCase))
        {
            var defense = new Position
            {
                Castling = ""
            };
            int king = Random.Shared.Next(24), farKing = Random.Shared.Next(32, 64);
            defense.Squares[king] = 'K';
            defense.Squares[farKing] = 'k';
            var pieces = new[]
            {
                ('P', Random.Shared.Next(16, 48)),
                (Random.Shared.Next(2) == 0 ? 'N' : 'B', Random.Shared.Next(64)),
                ('p', Random.Shared.Next(8, 24)),
                ('p', Random.Shared.Next(8, 32))
            };
            foreach (var(piece, square)in pieces)
            {
                if (defense.Squares[square] != '\0')
                    return null;
                defense.Squares[square] = piece;
            }

            if (MoveGenerator.IsKingInCheck(defense, true) || MoveGenerator.IsKingInCheck(defense, false))
                return null;
            return defense;
        }

        var p = new Position
        {
            Castling = ""
        };
        int wk = Random.Shared.Next(8, 40), bk = Random.Shared.Next(24, 64);
        if (wk == bk)
            return null;
        p.Squares[wk] = 'K';
        p.Squares[bk] = 'k';
        bool multi = kind.Contains("Verteidigung", StringComparison.OrdinalIgnoreCase);
        int own = multi ? 0 : kind == "Komplexe Bauernendspiele" ? Random.Shared.Next(2, 4) : Random.Shared.Next(1, 3), enemy = multi ? (Random.Shared.Next(4) == 0 ? 3 : 2) : Random.Shared.Next(1, 3);
        for (int i = 0; i < own + enemy; i++)
        {
            int sq = i < own ? Random.Shared.Next(16, 40) : Random.Shared.Next(24, 48);
            if (p.Squares[sq] != '\0')
                return null;
            p.Squares[sq] = i < own ? 'P' : 'p';
        }

        if (multi)
        {
            var files = Enumerable.Range(0, 64).Where(i => p.Squares[i] == 'p').Select(i => i % 8).ToList();
            if (files.Max() - files.Min() < 4)
                return null;
        }

        if (kind is "Bauern + Läufer" or "Bauern + Springer")
        {
            char minor = kind.Contains("Läufer") ? 'B' : 'N';
            for (int i = 0; i < (Random.Shared.NextDouble() < .35 ? 2 : 1); i++)
            {
                int sq = Random.Shared.Next(64);
                if (p.Squares[sq] != '\0')
                    return null;
                p.Squares[sq] = i == 0 ? minor : char.ToLowerInvariant(minor);
            }
        }

        if (MoveGenerator.IsKingInCheck(p, true) || MoveGenerator.IsKingInCheck(p, false))
            return null;
        return p;
    }
}
