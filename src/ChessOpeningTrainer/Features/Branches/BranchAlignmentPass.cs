using System.Diagnostics;

namespace ChessOpeningTrainer;

/// <summary>Order-preserving centering after crossing reduction. Every accepted
/// move checks all affected crossing pairs; a time cap keeps large graphs responsive.</summary>
public static class BranchAlignmentPass
{
    private sealed record Edge(BranchVertex A, BranchVertex B);
    public static int Apply(IReadOnlyList<BranchVertex> nodes, int budgetMilliseconds = 600)
    {
        var watch = Stopwatch.StartNew();
        int moved = 0;
        var columns = nodes.GroupBy(n => n.Location.X).OrderBy(g => g.Key).Select(g => g.OrderBy(n => n.Location.Y).ToArray()).ToArray();
        var edges = nodes.SelectMany(n => n.Children.Select(c => new Edge(n, c))).OrderBy(e => e.A.Location.X).ToArray();
        var parents = nodes.ToDictionary(n => n, _ => new List<BranchVertex>());
        var incident = nodes.ToDictionary(n => n, _ => new HashSet<int>());
        var pairs = new List<(Edge A, Edge B)>();
        foreach (var edge in edges)
            parents[edge.B].Add(edge.A);
        for (int i = 0; i < edges.Length; i++)
        {
            if (watch.ElapsedMilliseconds >= budgetMilliseconds || pairs.Count > 500000)
                return 0;
            var a = edges[i];
            for (int j = i + 1; j < edges.Length && edges[j].A.Location.X < a.B.Location.X; j++)
            {
                var b = edges[j];
                if (a.A == b.A || a.A == b.B || a.B == b.A || a.B == b.B)
                    continue;
                int id = pairs.Count;
                pairs.Add((a, b));
                incident[a.A].Add(id);
                incident[a.B].Add(id);
                incident[b.A].Add(id);
                incident[b.B].Add(id);
            }
        }
        bool Cross(int id)
        {
            var (a, b) = pairs[id];
            static double Side(PointF p, PointF q, PointF r) => (q.X - (double)p.X) * (r.Y - p.Y) - (q.Y - (double)p.Y) * (r.X - p.X);
            return Side(a.A.Location, a.B.Location, b.A.Location) * Side(a.A.Location, a.B.Location, b.B.Location) < 0 &&
                Side(b.A.Location, b.B.Location, a.A.Location) * Side(b.A.Location, b.B.Location, a.B.Location) < 0;
        }
        // Finish with a child-to-parent sweep: ending with the opposite sweep
        // used to undo the fan-out centering just established in the previous pass.
        for (int pass = 0; pass < 5; pass++)
            foreach (var col in pass % 2 == 0 ? columns.Reverse() : columns)
                for (int i = 0; i < col.Length; i++)
                {
                    if (watch.ElapsedMilliseconds >= budgetMilliseconds)
                        return moved;
                    var n = col[i];
                    if (pass % 2 == 1 && n.Children.Count > 1) continue;
                    var links = pass % 2 == 0 ? n.Children.ToArray() : parents[n].ToArray();
                    if (links.Length == 0)
                        continue;
                    // Center a parent over its fan-out; align a continuation with its predecessors.
                    float desired = (links.Min(v => v.Location.Y) + links.Max(v => v.Location.Y)) / 2;
                    float low = i == 0 ? Math.Min(0, desired) : col[i - 1].Location.Y + (col[i - 1].Height + n.Height) / 2 + 12;
                    float high = i == col.Length - 1 ? Math.Max(desired, n.Location.Y) : col[i + 1].Location.Y - (col[i + 1].Height + n.Height) / 2 - 12;
                    if (low > high)
                        continue;
                    desired = Math.Clamp(desired, low, high);
                    if (Math.Abs(desired - n.Location.Y) < .5)
                        continue;
                    int before = incident[n].Count(Cross);
                    var previous = n.Location;
                    // A full move can cross another edge even when a smaller
                    // order-preserving adjustment is useful. Back off, don't
                    // force a crossing or push the node beyond its target.
                    for (int attempt = 0; attempt < 6; attempt++)
                    {
                        n.Location = new(previous.X, previous.Y + (desired - previous.Y) / (1 << attempt));
                        if (incident[n].Count(Cross) <= before) { moved++; break; }
                        n.Location = previous;
                    }
                }
        return moved;
    }
}
