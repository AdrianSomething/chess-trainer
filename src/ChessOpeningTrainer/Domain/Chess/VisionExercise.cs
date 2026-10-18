namespace ChessOpeningTrainer;

public sealed record VisionExercise(Position Position, int Target, bool White, HashSet<int> Pieces, HashSet<(int From, int To)> Attacks);
public static class VisionRules
{
    // Geometric attacks include pinned defenders, as in chess's attacked-square definition.
    public static bool Attacks(Position p, int from, int to)
    {
        if (from == to || p.Squares[from] == '\0') return false;
        int dx = to % 8 - from % 8, dy = to / 8 - from / 8;
        char c = p.Squares[from];
        bool ray = false;
        switch (char.ToLowerInvariant(c))
        {
            case 'p': return Math.Abs(dx) == 1 && dy == (char.IsUpper(c) ? 1 : -1);
            case 'n': return Math.Abs(dx) * Math.Abs(dy) == 2;
            case 'k': return Math.Max(Math.Abs(dx), Math.Abs(dy)) == 1;
            case 'b': ray = Math.Abs(dx) == Math.Abs(dy); break;
            case 'r': ray = dx == 0 || dy == 0; break;
            case 'q': ray = dx == 0 || dy == 0 || Math.Abs(dx) == Math.Abs(dy); break;
        }
        if (!ray) return false;
        int step = Math.Sign(dx) + 8 * Math.Sign(dy);
        for (int sq = from + step; sq != to; sq += step) if (p.Squares[sq] != '\0') return false;
        return true;
    }
    public static HashSet<int> Direct(Position p, int target) =>
        Enumerable.Range(0, 64).Where(i => Attacks(p, i, target)).ToHashSet();

    public static HashSet<int> Indirect(Position p, int target)
    {
        var result = Direct(p, target);
        // X-ray chains: only friendly sliders already aimed along the same ray
        // may be passed through (a pawn/knight or enemy piece still blocks it).
        bool changed;
        do
        {
            changed = false;
            foreach (int origin in Enumerable.Range(0, 64).Where(i => "brqBRQ".Contains(p.Squares[i]) && !result.Contains(i)))
            {
                var exposed = p.Clone();
                foreach (int blocker in result)
                    if (blocker != origin && char.IsUpper(p.Squares[blocker]) == char.IsUpper(p.Squares[origin]) &&
                        "brqBRQ".Contains(p.Squares[blocker])) exposed.Squares[blocker] = '\0';
                if (Attacks(exposed, origin, target)) changed |= result.Add(origin);
            }
        } while (changed);
        return result;
    }

    public static HashSet<int> Potential(Position p, int target)
    {
        var now = Direct(p, target);
        var result = new HashSet<int>();
        foreach (bool side in new[] { true, false })
        {
            var turn = p.Clone(); turn.WhiteToMove = side; turn.EnPassant = null;
            // A hypothetical side cannot move while its opponent is already in check.
            if (MoveGenerator.IsKingInCheck(turn, !side)) continue;
            foreach (var move in MoveGenerator.GenerateLegal(turn).Where(m => m.To != target))
            {
                var next = turn.Clone(); next.Apply(move);
                for (int origin = 0; origin < 64; origin++)
                {
                    if (now.Contains(origin) || origin == target || p.Squares[origin] == '\0' || char.IsUpper(p.Squares[origin]) != side) continue;
                    int location = origin == move.From ? move.To : origin;
                    if (move.IsCastle && char.ToLowerInvariant(p.Squares[origin]) == 'r')
                    {
                        int rook = move.To > move.From ? move.From + 3 : move.From - 4;
                        if (origin == rook) location = move.To > move.From ? move.From + 1 : move.From - 1;
                    }
                    if (Attacks(next, location, target)) result.Add(origin);
                }
            }
        }
        return result;
    }
    public static HashSet<(int From, int To)> EnemyAttacks(Position p, bool white) =>
        (from a in Enumerable.Range(0, 64)
         from b in Enumerable.Range(0, 64)
         where p.Squares[a] != '\0' && p.Squares[b] != '\0' && char.IsUpper(p.Squares[a]) != white &&
             char.IsUpper(p.Squares[b]) == white && Attacks(p, a, b)
         select (a, b)).ToHashSet();
    public static VisionExercise Generate(int mode, CancellationToken token, bool? desiredSide = null, bool indirect = false)
    {
        for (int attempt = 0; ; attempt++)
        {
            token.ThrowIfCancellationRequested();
            var p = Position.FromFen(Position.InitialFen);
            for (int ply = 0, total = Random.Shared.Next(16, 70); ply < total; ply++)
            {
                token.ThrowIfCancellationRequested();
                var moves = MoveGenerator.GenerateLegal(p);
                if (moves.Count == 0) break;
                var choices = moves.Where(m => char.ToLowerInvariant(p.Squares[m.To]) != 'k').ToList();
                if (choices.Count == 0) break;
                p.Apply(choices[Random.Shared.Next(choices.Count)]);
            }
            if (MoveGenerator.GameState(p, new Dictionary<string, int>()) is not ("" or "Schach")) continue;
            bool side = p.WhiteToMove;
            if (desiredSide is bool requested && side != requested) continue;
            var targets = new HashSet<int>();
            foreach (var m in MoveGenerator.GenerateLegal(p).Where(m => "nbrq".Contains(char.ToLowerInvariant(p.Squares[m.From]))))
            {
                var next = p.Clone(); next.Apply(m);
                if (Enumerable.Range(0, 64).Any(t => next.Squares[t] != '\0' && char.IsUpper(next.Squares[t]) != side && Attacks(next, m.To, t)))
                    targets.Add(m.To);
            }
            if (targets.Count == 0) continue;
            int target = targets.ElementAt(Random.Shared.Next(targets.Count));
            var pieces = mode == 1 ? Potential(p, target) : indirect ? Indirect(p, target) : Direct(p, target);
            var attacks = EnemyAttacks(p, side);
            if (indirect && mode == 2)
                foreach (int targetSquare in Enumerable.Range(0, 64).Where(i => p.Squares[i] != '\0' && char.IsUpper(p.Squares[i]) == side))
                    foreach (int origin in Indirect(p, targetSquare).Where(i => char.IsUpper(p.Squares[i]) != side)) attacks.Add((origin, targetSquare));
            if (mode == 2 ? attacks.Count == 0 : pieces.Count == 0) continue;
            return new(p, target, side, pieces, attacks);
        }
    }
}
