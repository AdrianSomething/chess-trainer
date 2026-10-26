using ChessOpeningTrainer;
using static Program;

internal static class RulesChecks
{
    internal static void Run()
    {
        long Perft(Position position, int depth)
        {
            if (depth == 0) return 1;
            long count = 0;
            foreach (var move in MoveGenerator.GenerateLegal(position))
            {
                var next = position.Clone(); next.Apply(move);
                count += Perft(next, depth - 1);
            }
            return count;
        }
        var initial = Position.FromFen(Position.InitialFen);
        Check(Perft(initial, 1) == 20, "Initial perft 1");
        Check(Perft(initial, 2) == 400, "Initial perft 2");
        Check(Perft(initial, 3) == 8902, "Initial perft 3");
        var castles = Position.FromFen("r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1");
        Check(MoveGenerator.GenerateLegal(castles).Count(m => m.Uci is "e1g1" or "e1c1") == 2, "Both castlings");
        var ep = Position.FromFen("4k3/8/8/3pP3/8/8/8/4K3 w - d6 0 1");
        Check(MoveGenerator.GenerateLegal(ep).Any(m => m.Uci == "e5d6"), "En passant");
        foreach (char promotion in new[] { '\0', 'q', 'r', 'b', 'n' })
            for (int from = 0; from < 64; from++)
                for (int to = 0; to < 64; to++)
                {
                    string uci = Square.Name(from) + Square.Name(to) + (promotion == '\0' ? "" : promotion.ToString());
                    Check(PackedMove.Decode(PackedMove.Encode(uci)) == uci, "Packed move roundtrip");
                }
        var p = Position.FromFen(Position.InitialFen);
        foreach (var uci in new[] { "f2f3", "e7e5", "g2g4", "d8h4" })
            p.Apply(MoveGenerator.GenerateLegal(p).Single(m => m.Uci == uci));
        Check(MoveGenerator.GenerateLegal(p).Count == 0, "Fool's mate has no legal reply");
    }
}
