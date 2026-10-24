using System.Diagnostics;

namespace ChessOpeningTrainer;

/// <summary>Optional Sugiyama-family alternative: existing DAG ranks, bounded
/// long-edge normalization, median/barycentre sweeps, constrained coordinates.
/// This is not a reproduction of the paper's sparse-container complexity proof.
/// See docs/BRANCH_LAYOUT.md for the reference and deliberate limits.</summary>
public static class BranchSugiyamaLayout
{
    private sealed class Node(BranchVertex? original, int rank, float y, int id)
    {
        public BranchVertex? Original { get; } = original;
        public int Rank { get; } = rank;
        public int Id { get; } = id;
        public float Y { get; set; } = y;
        public float Height => Original?.Height ?? 0;
        public List<Node> Parents { get; } = [];
        public List<Node> Children { get; } = [];
    }
    public static int Apply(IReadOnlyList<BranchVertex> vertices, int budgetMilliseconds = 2800)
    {
        if (vertices.Count < 4) return 0;
        var watch = Stopwatch.StartNew();
        var ranks = vertices.Select(v => v.Location.X).Distinct().Order().Select((x, i) => (x, i)).ToDictionary(v => v.x, v => v.i);
        int estimated = vertices.Count + vertices.Sum(v => v.Children.Sum(c => Math.Max(0, ranks[c.Location.X] - ranks[v.Location.X] - 1)));
        // Bound normalization space rather than silently allocating O(VE).
        if (estimated > Math.Min(50000, Math.Max(2000, vertices.Count * 12))) return 0;
        var nodes = vertices.Select((v, i) => new Node(v, ranks[v.Location.X], v.Location.Y, i)).ToList();
        var map = nodes.ToDictionary(n => n.Original!);
        foreach (var v in vertices)
            foreach (var c in v.Children)
            {
                var previous = map[v]; var target = map[c];
                if (target.Rank <= previous.Rank) return 0;
                for (int layer = previous.Rank + 1; layer < target.Rank; layer++)
                {
                    float fraction = (layer - map[v].Rank) / (float)(target.Rank - map[v].Rank);
                    var dummy = new Node(null, layer, v.Location.Y + (c.Location.Y - v.Location.Y) * fraction, nodes.Count);
                    nodes.Add(dummy); previous.Children.Add(dummy); dummy.Parents.Add(previous); previous = dummy;
                }
                previous.Children.Add(target); target.Parents.Add(previous);
            }
        var columns = nodes.GroupBy(n => n.Rank).OrderBy(g => g.Key).Select(g => g.OrderBy(n => n.Y).ToList()).ToArray();
        var saved = vertices.Select(v => v.Location).ToArray();
        int initial = BranchCrossingReduction.CountCrossings(vertices), best = initial;
        double error = BranchCenteredCoordinates.Error(vertices);
        for (int sweep = 0; sweep < 32 && watch.ElapsedMilliseconds < budgetMilliseconds; sweep++)
        {
            bool forward = sweep % 2 == 0, median = sweep % 8 < 4;
            foreach (var column in forward ? columns : columns.Reverse())
            {
                float Wanted(Node n)
                {
                    var linked = forward ? n.Parents : n.Children;
                    if (linked.Count == 0) return n.Y;
                    var ys = linked.Select(c => c.Y).Order().ToArray();
                    return median ? (ys[(ys.Length - 1) / 2] + ys[ys.Length / 2]) / 2 : ys.Average();
                }
                var ordered = column.OrderBy(Wanted).ThenBy(n => n.Y).ThenBy(n => n.Id).ToArray();
                column.Clear(); column.AddRange(ordered);
                float[] ys = BranchCenteredCoordinates.Project(ordered.Select(Wanted).ToArray(), ordered.Select(n => n.Height).ToArray());
                for (int i = 0; i < ordered.Length; i++) ordered[i].Y = ys[i];
            }
            foreach (var n in nodes.Where(n => n.Original is not null)) n.Original!.Location = new(n.Original.Location.X, n.Y);
            int score = BranchCrossingReduction.CountCrossings(vertices);
            double nextError = BranchCenteredCoordinates.Error(vertices);
            // Evaluate the existing straight-centreline graph objective, not
            // just crossings between normalized/dummy segments. The UI draws curves.
            if (score < best || score == best && nextError < error)
            {
                best = score; error = nextError; saved = vertices.Select(v => v.Location).ToArray();
            }
        }
        for (int i = 0; i < vertices.Count; i++) vertices[i].Location = saved[i];
        BranchCenteredCoordinates.Apply(vertices, 350);
        return initial - BranchCrossingReduction.CountCrossings(vertices);
    }
}
