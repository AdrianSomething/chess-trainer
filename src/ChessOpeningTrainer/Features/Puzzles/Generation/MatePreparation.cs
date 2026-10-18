namespace ChessOpeningTrainer;
public static class MatePreparation
{
    public static string Key(Position p, int remaining) => p.ToFen() + "|" + remaining;
    public static Dictionary<string, List<MateRoute>> Prepare(Puzzle puzzle, CancellationToken token, int nodeBudget = 750000)
    {
        if (puzzle.MateGoal is not int goal)
            return[];
        var root = Position.FromFen(puzzle.Fen);
        bool attacker = root.WhiteToMove;
        int visited = 0;
        var memo = new Dictionary<string, bool>();
        var routes = new Dictionary<string, List<MateRoute>>();
        void Check()
        {
            token.ThrowIfCancellationRequested();
            if (++visited > nodeBudget)
                throw new InvalidOperationException("Mattbaum überschreitet Vorbereitungsbudget.");
        }

        bool Wins(Position p, int plies)
        {
            Check();
            string key = Key(p, plies);
            if (memo.TryGetValue(key, out bool result))
                return result;
            var legal = MoveGenerator.GenerateLegal(p);
            if (legal.Count == 0)
                return memo[key] = p.WhiteToMove != attacker && MoveGenerator.IsKingInCheck(p, p.WhiteToMove);
            if (plies <= 0 || MoveGenerator.GameState(p, new Dictionary<string, int>())is not ("" or "Schach"))
                return memo[key] = false;
            bool own = p.WhiteToMove == attacker;
            // Checking moves first accelerates the proof, without excluding quiet moves.
            var children = legal.Select(m =>
            {
                var n = p.Clone();
                n.Apply(m);
                return n;
            }).OrderByDescending(n => MoveGenerator.IsKingInCheck(n, n.WhiteToMove));
            foreach (var child in children)
            {
                bool wins = Wins(child, plies - 1);
                if (own && wins)
                    return memo[key] = true;
                if (!own && !wins)
                    return memo[key] = false;
            }

            return memo[key] = !own;
        }

        void Build(Position p, int remaining)
        {
            Check();
            string key = Key(p, remaining);
            if (routes.ContainsKey(key))
                return;
            var options = new List<MateRoute>();
            routes[key] = options;
            foreach (var m in MoveGenerator.GenerateLegal(p))
            {
                var after = p.Clone();
                after.Apply(m);
                if (!Wins(after, remaining * 2 - 2))
                    continue;
                var defenses = MoveGenerator.GenerateLegal(after);
                if (defenses.Count == 0)
                {
                    options.Add(new(m.Uci, null));
                    continue;
                }

                // Maximize mate distance first; use fewer winning replies only as a tie-break.
                var alternatives = new List<(Move Move, Position Position, int Distance, int Count)>();
                foreach (var d in defenses)
                {
                    var n = after.Clone();
                    n.Apply(d);
                    int distance = remaining - 1;
                    for (int turns = 1; turns < remaining - 1; turns++)
                        if (Wins(n, turns * 2 - 1))
                        {
                            distance = turns;
                            break;
                        }

                    int count = 0;
                    foreach (var answer in MoveGenerator.GenerateLegal(n))
                    {
                        var a = n.Clone();
                        a.Apply(answer);
                        if (Wins(a, remaining * 2 - 4))
                            count++;
                    }

                    alternatives.Add((d, n, distance, count));
                }

                var defense = alternatives.OrderByDescending(d => d.Distance).ThenBy(d => d.Count).First();
                options.Add(new(m.Uci, defense.Move.Uci));
                Build(defense.Position, remaining - 1);
            }
        }

        if (!Wins(root, goal * 2 - 1))
            throw new InvalidOperationException("Angegebenes Mattziel nicht bewiesen.");
        for (int turns = 1; turns < goal; turns++)
            if (Wins(root, turns * 2 - 1))
            {
                goal = turns;
                puzzle.Theme = "Matt in " + turns;
                break;
            }

        Build(root, goal);
        puzzle.MateDefenseVersion = 2;
        return routes;
    }
}
