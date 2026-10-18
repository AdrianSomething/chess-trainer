namespace ChessOpeningTrainer;
public static class LearningGroups
{
    // Group sibling opponent variations by the exact same own move. Their
    // continuations remain separate; this never merges distinct chess positions.
    public static string Signature(OpeningStore.LearningTarget t) => string.Join("/", t.Prefix.SkipLast(1).Select(m => m.Uci)) + "|" + t.Expected.Uci;
    public static string VariantName(OpeningDefinition opening, OpeningStore.LearningTarget t)
    {
        var p = Position.FromFen(opening.StartFen ?? Position.InitialFen);
        string san = "Start";
        foreach (var d in t.Prefix)
        {
            var m = MoveGenerator.GenerateLegal(p).First(m => m.Uci == d.Uci);
            san = SanNotation.Format(p, m);
            p.Apply(m);
        }

        return san;
    }

    public static string Shape(OpeningNode n, int depth) => depth == 0 ? "" : string.Join(",", n.Moves.OrderBy(m => m.Uci).Select(m => m.Uci + "(" + Shape(m.Next, depth - 1) + ")"));
}
