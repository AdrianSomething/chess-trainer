namespace ChessOpeningTrainer;
public static class DrawPreparation
{
    public static bool Secured(Position p, bool solver, PawnTablebase table)
    {
        string state = MoveGenerator.GameState(p, new Dictionary<string, int>());
        if (state.StartsWith("Remis"))
            return true;
        if (state == "Schachmatt")
            return false;
        var material = p.Squares.Where(c => c != '\0' && char.ToLowerInvariant(c) != 'k').ToList();
        if (material.Count == 1 && char.ToLowerInvariant(material[0]) == 'p')
            return !table.PawnWins(p);
        return false;
    }

    public static Dictionary<string, List<MateRoute>> Prepare(Position root, int turns, PawnTablebase table, CancellationToken token, int budget = 200000)
    {
        bool solver = root.WhiteToMove;
        var memo = new Dictionary<string, bool>();
        var routes = new Dictionary<string, List<MateRoute>>();
        int nodes = 0;
        bool Wins(Position p, int plies)
        {
            token.ThrowIfCancellationRequested();
            if (++nodes > budget)
                throw new InvalidOperationException("Remis-Prüfbudget erreicht.");
            if (Secured(p, solver, table))
                return true;
            if (plies <= 0)
                return false;
            string k = MatePreparation.Key(p, plies);
            if (memo.TryGetValue(k, out bool v))
                return v;
            var legal = MoveGenerator.GenerateLegal(p);
            if (legal.Count == 0)
                return false;
            bool own = p.WhiteToMove == solver;
            foreach (var m in legal.OrderByDescending(m => p.Squares[m.To] != '\0'))
            {
                var n = p.Clone();
                n.Apply(m);
                bool good = Wins(n, plies - 1);
                if (own && good)
                    return memo[k] = true;
                if (!own && !good)
                    return memo[k] = false;
            }

            return memo[k] = !own;
        }

        void Build(Position p, int left)
        {
            string key = MatePreparation.Key(p, left);
            if (routes.ContainsKey(key))
                return;
            var options = new List<MateRoute>();
            routes[key] = options;
            foreach (var m in MoveGenerator.GenerateLegal(p))
            {
                var n = p.Clone();
                n.Apply(m);
                if (!Wins(n, left * 2 - 2))
                    continue;
                if (Secured(n, solver, table))
                {
                    options.Add(new(m.Uci, null));
                    continue;
                }

                var defense = MoveGenerator.GenerateLegal(n).OrderByDescending(d => n.Squares[d.To] != '\0').First();
                n.Apply(defense);
                options.Add(new(m.Uci, defense.Uci));
                Build(n, left - 1);
            }
        }

        if (Secured(root, solver, table) || !Wins(root, turns * 2 - 1))
            throw new InvalidOperationException("Kein nachgewiesenes Remismanöver.");
        Build(root, turns);
        return routes;
    }

    public static Task<Puzzle> Compose(StockfishEngine engine, CancellationToken token) => Task.Run(() => ComposeCore(engine, token), token);
    private static async Task<Puzzle> ComposeCore(StockfishEngine engine, CancellationToken token)
    {
        var table = await PawnTablebase.Shared.WaitAsync(token);
        for (int attempt = 0; attempt < 300; attempt++)
        {
            token.ThrowIfCancellationRequested();
            int family = Random.Shared.Next(7);
            var p = new Position
            {
                Castling = ""
            };
            int goal = family == 0 ? 2 : family >= 3 ? Random.Shared.Next(1, 4) : 1;
            if (family == 6)
            {
                // Rf1+ forces Kxf1: the bare king is stalemated, not "saved"
                // in a winning rook ending. Extra remote enemy material and
                // symmetries vary the pattern; every instance is re-proved.
                p = Position.FromFen("8/8/8/8/8/4ppq1/4nk2/4b1RK w - - 0 1");
                for (int i = 0, extra = Random.Shared.Next(4); i < extra; i++)
                {
                    int square = Random.Shared.Next(24, 56);
                    if (p.Squares[square] == '\0') p.Squares[square] = "pnbr"[Random.Shared.Next(4)];
                }
                if (Random.Shared.Next(2) == 0)
                {
                    var mirrored = p.Clone();
                    for (int i = 0; i < 64; i++) mirrored.Squares[i ^ 7] = p.Squares[i];
                    p = mirrored;
                }
                if (Random.Shared.Next(2) == 0) p = PawnTablebase.SwapColors(p);
                if (MoveGenerator.IsKingInCheck(p, p.WhiteToMove)) continue;
                goal = 2;
            }
            else if (family == 0)
            {
                int fork = Random.Shared.Next(64);
                var empty = new Position
                {
                    Castling = ""
                };
                empty.Squares[fork] = 'N';
                var attacks = Premoves.Candidates(empty, fork, true).Select(m => m.To).OrderBy(_ => Random.Shared.Next()).ToArray();
                if (attacks.Length < 3)
                    continue;
                p.Squares[attacks[0]] = 'k';
                p.Squares[attacks[1]] = 'q';
                p.Squares[attacks[2]] = 'N';
                int wk = Random.Shared.Next(64);
                if (p.Squares[wk] != '\0')
                    continue;
                p.Squares[wk] = 'K';
            }
            else
            {
                var squares = Enumerable.Range(0, 64).OrderBy(_ => Random.Shared.Next()).Take(6).ToArray();
                p.Squares[squares[0]] = 'K';
                p.Squares[squares[1]] = 'k';
                p.Squares[squares[2]] = family == 1 ? 'r' : 'q';
                if (family == 2)
                    p.Squares[squares[3]] = 'B';
                if (family >= 3)
                {
                    p.Squares[squares[3]] = family == 3 ? 'R' : family == 4 ? 'Q' : 'B';
                    p.Squares[squares[2]] = family == 4 ? 'r' : 'q';
                    if (squares[4] / 8 is > 0 and < 7) p.Squares[squares[4]] = 'p';
                    if (family == 5) p.Squares[squares[5]] = 'N';
                }
            }

            if (MoveGenerator.IsKingInCheck(p, !p.WhiteToMove))
                continue;
            try
            {
                if (goal > 1)
                {
                    bool one = false;
                    try
                    {
                        Prepare(p, 1, table, token, 12000);
                        one = true;
                    }
                    catch (InvalidOperationException)
                    {
                    }

                    if (one)
                        continue;
                }

                var routes = Prepare(p, goal, table, token, 70000);
                string source = family switch
                {
                    0 => "Springergabel",
                    1 => "Königsressource gegen Turm",
                    2 => "Läuferressource gegen Dame",
                    3 => "Turmressource gegen Dame und Bauer",
                    4 => "Damenressource gegen Turm und Bauer",
                    6 => "Pattressource durch Turmopfer",
                    _ => "Leichtfigurenressource gegen Dame"
                };
                return new()
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Fen = p.ToFen(),
                    Theme = family == 6 ? "Patt erzwingen · Turmopfer" : $"Remis sichern in höchstens {goal} eigenen Zügen",
                    GoalKind = "Draw",
                    GoalMoves = goal,
                    DrawProofVersion = 2,
                    PreparedMateRoutes = routes,
                    Origin = source + " · vollständige begrenzte Remisprüfung"
                };
            }
            catch (InvalidOperationException)
            {
            }
        }

        throw new InvalidOperationException("Keine passende Remisaufgabe im Prüfbudget. Erneut vorbereiten.");
    }
}
