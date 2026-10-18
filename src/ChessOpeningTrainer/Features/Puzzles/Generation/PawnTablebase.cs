namespace ChessOpeningTrainer;
// Exact retrograde K+P versus K, normalized to a white pawn. All legal king
// moves, double pawn pushes, capture, stalemate and promotion are included.
// Unresolved states after the winning attractor closes are draws, not losses.
public sealed class PawnTablebase
{
    private const int Count = 48 * 64 * 64 * 2;
    private readonly bool[] valid = new bool[Count], win = new bool[Count];
    private readonly short[] distance = new short[Count];
    public static Task<PawnTablebase> Shared { get; } = Task.Run(() => new PawnTablebase());

    private static int Id(int pawn, int own, int enemy, bool turn) => (((pawn - 8) * 64 + own) * 64 + enemy) * 2 + (turn ? 0 : 1);
    private static void Decode(int id, out int pawn, out int own, out int enemy, out bool turn)
    {
        turn = (id & 1) == 0;
        id /= 2;
        enemy = id % 64;
        id /= 64;
        own = id % 64;
        pawn = id / 64 + 8;
    }

    private static bool Near(int a, int b) => Math.Max(Math.Abs(a % 8 - b % 8), Math.Abs(a / 8 - b / 8)) <= 1;
    private static bool PawnAttack(int pawn, int square) => square / 8 == pawn / 8 + 1 && Math.Abs(pawn % 8 - square % 8) == 1;
    private static bool Valid(int p, int a, int b, bool turn) => p != a && p != b && a != b && !Near(a, b) && (!turn || !PawnAttack(p, b));
    private static IEnumerable<int> Neighbours(int square)
    {
        for (int dr = -1; dr <= 1; dr++)
            for (int df = -1; df <= 1; df++)
            {
                int f = square % 8 + df, r = square / 8 + dr;
                if ((df != 0 || dr != 0) && f >= 0 && f < 8 && r >= 0 && r < 8)
                    yield return r * 8 + f;
            }
    }

    // -1 = terminal draw; -2 = terminal pawn-side win.
    private static IEnumerable<int> Children(int p, int a, int b, bool turn)
    {
        if (turn)
        {
            foreach (int next in Neighbours(a))
                if (next != p && !Near(next, b))
                    yield return Id(p, next, b, false);
            int to = p + 8;
            if (to != a && to != b)
            {
                if (to >= 56)
                {
                    yield return PromotionWins(to, a, b) ? -2 : -1;
                }
                else
                {
                    yield return Id(to, a, b, false);
                    if (p / 8 == 1 && p + 16 != a && p + 16 != b)
                        yield return Id(p + 16, a, b, false);
                }
            }
        }
        else
            foreach (int next in Neighbours(b))
                if (!Near(next, a) && !PawnAttack(p, next))
                    yield return next == p ? -1 : Id(p, a, next, true);
    }

    private static bool PromotionWins(int square, int own, int enemy)
    {
        // KQK/KRK are won except an immediate legal capture or stalemate.
        if (Near(square, enemy) && !Near(square, own))
            return false;
        foreach (char piece in new[]
        {
            'Q',
            'R'
        }

        )
        {
            var p = new Position
            {
                Castling = "",
                WhiteToMove = false
            };
            p.Squares[own] = 'K';
            p.Squares[enemy] = 'k';
            p.Squares[square] = piece;
            if (MoveGenerator.GenerateLegal(p).Count > 0 || MoveGenerator.IsKingInCheck(p, false))
                return true;
        }

        return false;
    }

    private PawnTablebase()
    {
        var incoming = new int[Count];
        var remaining = new byte[Count];
        var queue = new Queue<int>();
        for (int i = 0; i < Count; i++)
        {
            Decode(i, out int p, out int a, out int b, out bool turn);
            if (!Valid(p, a, b, turn))
                continue;
            valid[i] = true;
            int total = 0;
            bool promotion = false;
            foreach (int child in Children(p, a, b, turn))
            {
                total++;
                if (child >= 0)
                    incoming[child]++;
                if (child == -2)
                    promotion = true;
            }

            remaining[i] = (byte)total;
            if (promotion || total == 0 && !turn && PawnAttack(p, b))
            {
                win[i] = true;
                distance[i] = 1;
                queue.Enqueue(i);
            }
        }

        var offsets = new int[Count + 1];
        for (int i = 0; i < Count; i++)
            offsets[i + 1] = offsets[i] + incoming[i];
        var predecessors = new int[offsets[^1]];
        Array.Clear(incoming);
        for (int i = 0; i < Count; i++)
            if (valid[i])
            {
                Decode(i, out int p, out int a, out int b, out bool turn);
                foreach (int child in Children(p, a, b, turn))
                    if (child >= 0)
                        predecessors[offsets[child] + incoming[child]++] = i;
            }

        while (queue.TryDequeue(out int child))
            for (int at = offsets[child]; at < offsets[child + 1]; at++)
            {
                int parent = predecessors[at];
                if (win[parent])
                    continue;
                bool pawnTurn = (parent & 1) == 0;
                distance[parent] = Math.Max(distance[parent], (short)(distance[child] + 1));
                if (pawnTurn || --remaining[parent] == 0)
                {
                    win[parent] = true;
                    queue.Enqueue(parent);
                }
            }
    }

    public bool PawnWins(Position p)
    {
        int pawn = Array.FindIndex(p.Squares, c => char.ToLowerInvariant(c) == 'p');
        if (pawn < 0)
            return false;
        bool white = p.Squares[pawn] == 'P';
        int own = Array.IndexOf(p.Squares, white ? 'K' : 'k'), enemy = Array.IndexOf(p.Squares, white ? 'k' : 'K');
        if (!white)
        {
            pawn ^= 56;
            own ^= 56;
            enemy ^= 56;
        }

        int id = Id(pawn, own, enemy, p.WhiteToMove == white);
        return id >= 0 && id < Count && valid[id] && win[id];
    }

    public Position RandomPosition(bool promotion)
    {
        for (int attempt = 0; attempt < 200000; attempt++)
        {
            int id = Random.Shared.Next(Count);
            if (!valid[id] || win[id] != promotion)
                continue;
            Decode(id, out int pawn, out int own, out int enemy, out bool turn);
            if (turn != promotion || promotion && (distance[id] < 7 || pawn / 8 > 4 || Math.Max(Math.Abs(pawn % 8 - enemy % 8), Math.Abs(pawn / 8 - enemy / 8)) > 3))
                continue;
            var p = new Position
            {
                Castling = "",
                WhiteToMove = turn
            };
            p.Squares[pawn] = 'P';
            p.Squares[own] = 'K';
            p.Squares[enemy] = 'k';
            var moves = PreservingMoves(p, promotion);
            if (moves.Count == 0 || promotion && moves.Any(m => m.From == pawn) || !promotion && moves.All(m => m.To == pawn))
                continue;
            return p;
        }

        throw new InvalidOperationException("Keine passende Bauernstellung gefunden.");
    }

    public List<Move> PreservingMoves(Position p, bool pawnSide)
    {
        return MoveGenerator.GenerateLegal(p).Where(m =>
        {
            var next = p.Clone();
            next.Apply(m);
            var legal = m.Promotion != '\0' ? MoveGenerator.GenerateLegal(next) : [];
            bool won = m.Promotion != '\0' ? char.ToLowerInvariant(m.Promotion)is 'q' or 'r' && legal.All(r => r.To != m.To) && (legal.Count > 0 || MoveGenerator.IsKingInCheck(next, next.WhiteToMove)) : PawnWins(next);
            return pawnSide ? won : !won;
        }).ToList();
    }

    public Move? Suggest(Position p, bool pawnSide)
    {
        int Distance(Move m)
        {
            if (m.Promotion != '\0')
                return 0;
            var after = p.Clone();
            after.Apply(m);
            return WinDistance(after);
        }

        var moves = PreservingMoves(p, pawnSide);
        return pawnSide ? moves.OrderBy(Distance).FirstOrDefault() : moves.FirstOrDefault();
    }

    public int WinDistance(Position p)
    {
        int pawn = Array.FindIndex(p.Squares, c => char.ToLowerInvariant(c) == 'p');
        if (pawn < 0)
            return 0;
        bool white = p.Squares[pawn] == 'P';
        int own = Array.IndexOf(p.Squares, white ? 'K' : 'k'), enemy = Array.IndexOf(p.Squares, white ? 'k' : 'K');
        if (!white)
        {
            pawn ^= 56;
            own ^= 56;
            enemy ^= 56;
        }

        return distance[Id(pawn, own, enemy, p.WhiteToMove == white)];
    }

    public Move? TrainingReply(Position p, bool promotionGoal)
    {
        var legal = MoveGenerator.GenerateLegal(p);
        if (promotionGoal)
            return legal.OrderByDescending(m =>
            {
                var n = p.Clone();
                n.Apply(m);
                return WinDistance(n);
            }).FirstOrDefault();
        double Pressure(Move m)
        {
            var n = p.Clone();
            n.Apply(m);
            int pawn = Array.FindIndex(n.Squares, c => char.ToLowerInvariant(c) == 'p');
            if (pawn < 0)
                return -100;
            bool white = n.Squares[pawn] == 'P';
            int own = Array.IndexOf(n.Squares, white ? 'K' : 'k'), enemy = Array.IndexOf(n.Squares, white ? 'k' : 'K');
            int Dist(int a, int b) => Math.Max(Math.Abs(a % 8 - b % 8), Math.Abs(a / 8 - b / 8));
            if (MoveGenerator.GameState(n, new Dictionary<string, int>()).StartsWith("Remis"))
                return -80;
            if (MoveGenerator.GenerateLegal(n).Any(r => r.To == pawn))
                return -100;
            return (white ? pawn / 8 : 7 - pawn / 8) * 4 - Dist(own, pawn) * 3 - Dist(own, enemy) + (Dist(own, pawn) <= 1 ? 12 : 0);
        }

        // Keep pressure without repeating a deterministic king/pawn oscillation.
        // Avoid immediate pawn donations and terminal draws while alternatives exist.
        var options = legal.Select(m => (Move: m, Pressure: Pressure(m))).ToList();
        if (options.Count == 0) return null;
        double best = options.Max(x => x.Pressure);
        var varied = options.Where(x => x.Pressure >= Math.Max(-79, best - 12)).ToList();
        return varied.Count == 0 ? options.MaxBy(x => x.Pressure).Move
            : varied[Random.Shared.Next(varied.Count)].Move;
    }

    public static Position SwapColors(Position p)
    {
        var next = p.Clone();
        for (int i = 0; i < 64; i++)
        {
            char c = p.Squares[i];
            next.Squares[i ^ 56] = char.IsUpper(c) ? char.ToLowerInvariant(c) : char.ToUpperInvariant(c);
        }

        next.WhiteToMove = !p.WhiteToMove;
        return next;
    }
}
