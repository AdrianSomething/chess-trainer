namespace ChessOpeningTrainer;

/// <summary>Conservative post-pass: preserve columns/families and accept only globally fewer crossings.</summary>
public static class BranchCrossingReduction
{
    private sealed record Edge(int Id, BranchVertex From, BranchVertex To)
    {
        // X coordinates never change during this pass. Pairs in disjoint
        // columns or sharing an endpoint can never cross, whatever their Y.
        public List<Edge> Candidates { get; } = [];
    }

    public static int CountCrossings(IReadOnlyList<BranchVertex> vertices, IReadOnlySet<BranchVertex>? focus = null)
    {
        var edges = Edges(vertices); int count = 0;
        foreach (var a in edges) foreach (var b in a.Candidates)
            if (a.Id < b.Id && (focus is null || focus.Contains(a.From) || focus.Contains(a.To) || focus.Contains(b.From) || focus.Contains(b.To)) && Crosses(a, b)) count++;
        return count;
    }

    public static int Apply(IReadOnlyList<BranchVertex> vertices)
    {
        var edges = Edges(vertices);
        var parents = vertices.ToDictionary(v => v, _ => new List<BranchVertex>());
        foreach (var edge in edges) parents[edge.To].Add(edge.From);
        var columns = vertices.GroupBy(v => v.Location.X).Select(g => g.OrderBy(v => v.Location.Y).ToList()).ToList();
        int initial = edges.Sum(a => a.Candidates.Count(b => a.Id < b.Id && Crosses(a, b))), score = initial;
        var primary = vertices.ToDictionary(v => v, v => parents[v].OrderByDescending(p => p.Location.X).ThenBy(p => p.Id).FirstOrDefault());
        for (int pass = 0; pass < 6 && score > 0; pass++)
        {
            bool improved = false;
            int proposals = 0;
            var involved = vertices.ToDictionary(v => v, _ => 0);
            foreach (var a in edges) foreach (var b in a.Candidates)
                if (a.Id < b.Id && Crosses(a, b)) { involved[a.From]++; involved[a.To]++; involved[b.From]++; involved[b.To]++; }
            foreach (var column in columns.OrderByDescending(c => c.Sum(v => involved[v])))
            {
                if (proposals >= 512) break;
                // Move an intact primary-parent family, never an individual sibling
                // through another sibling. Merged nodes form their own block.
                var blocks = column.GroupBy(v => parents[v].Count == 1 ? parents[v][0] : v)
                    .Select(g => g.OrderBy(v => v.Location.Y).ToArray()).OrderBy(g => g[0].Location.Y).ToArray();
                foreach (var block in blocks.Take(160))
                {
                    if (++proposals > 512) break;
                    var moved = block.ToHashSet();
                    var affected = edges.Where(e => moved.Contains(e.From) || moved.Contains(e.To)).ToArray();
                    int oldLocal = IncidentCrossings(edges, affected);
                    var original = block.Select(v => v.Location).ToArray();
                    bool accepted = false;
                    foreach (float shift in new[] { -2f, 2f, -4f, 4f, -8f, 8f, -12f, 12f, -18f, 18f })
                    {
                        for (int i = 0; i < block.Length; i++) block[i].Location = new(original[i].X, original[i].Y + shift);
                        if (!NoOverlap(column)) continue;
                        int next = score - oldLocal + IncidentCrossings(edges, affected);
                        if (next >= score) continue;
                        score = next; improved = accepted = true; break;
                    }
                    if (!accepted)
                        for (int i = 0; i < block.Length; i++) block[i].Location = original[i];
                }
                for (int b = 0; b + 1 < blocks.Length && b < 160; b++)
                {
                    if (++proposals > 512) break;
                    var upper = blocks[b]; var lower = blocks[b + 1];
                    var moved = upper.Concat(lower).ToArray(); var original = moved.Select(v => v.Location).ToArray();
                    var affected = edges.Where(e => moved.Contains(e.From) || moved.Contains(e.To)).ToArray();
                    int oldLocal = IncidentCrossings(edges, affected);
                    float top = upper.Min(v => v.Location.Y - v.Height / 2), bottom = lower.Max(v => v.Location.Y + v.Height / 2);
                    float upperHeight = upper.Max(v => v.Location.Y + v.Height / 2) - top;
                    float lowerTop = lower.Min(v => v.Location.Y - v.Height / 2);
                    float lowerHeight = bottom - lowerTop;
                    float gap = Math.Max(22, lowerTop - (top + upperHeight));
                    foreach (var v in lower) v.Location = new(v.Location.X, v.Location.Y + top - lowerTop);
                    foreach (var v in upper) v.Location = new(v.Location.X, v.Location.Y + lowerHeight + gap);
                    int next = score - oldLocal + IncidentCrossings(edges, affected);
                    if (next < score && NoOverlap(column)) { score = next; improved = true; (blocks[b], blocks[b + 1]) = (blocks[b + 1], blocks[b]); }
                    else for (int i = 0; i < moved.Length; i++) moved[i].Location = original[i];
                }
            }
            // A compressed DAG often needs the related continuations in several
            // columns to move together. A one-column greedy swap cannot escape
            // that local minimum (e.g. Pirc f4/Bg7/Nf3 transpositions).
            int familyTrials = 0;
            foreach (var column in columns.OrderByDescending(c => c.Sum(v => involved[v])))
            {
                var blocks = column.GroupBy(v => primary[v] ?? v).Select(g => g.OrderBy(v => v.Location.Y).ToArray())
                    .OrderBy(g => g[0].Location.Y).ToArray();
                for (int a = 0; a < blocks.Length; a++) for (int b = a + 1; b < blocks.Length; b++)
                {
                    if (++familyTrials > 96) break;
                    if (TryFamilyExchange(blocks[a], blocks[b], vertices, columns, edges, primary, ref score)) improved = true;
                }
                if (familyTrials > 96) break;
            }
            if (!improved) break;
        }
        return initial - score;
    }

    private static bool TryFamilyExchange(BranchVertex[] first, BranchVertex[] second, IReadOnlyList<BranchVertex> vertices,
        List<List<BranchVertex>> columns, List<Edge> edges, Dictionary<BranchVertex, BranchVertex?> primary, ref int score)
    {
        HashSet<BranchVertex> Descendants(BranchVertex[] roots)
        {
            var result = roots.ToHashSet();
            foreach (var v in vertices.OrderBy(v => v.Location.X)) if (primary[v] is { } parent && result.Contains(parent)) result.Add(v);
            return result;
        }
        var left = Descendants(first); var right = Descendants(second);
        if (left.Overlaps(right)) return false;
        var moved = left.Concat(right).ToHashSet(); var original = moved.ToDictionary(v => v, v => v.Location);
        var affected = edges.Where(e => moved.Contains(e.From) || moved.Contains(e.To)).ToArray();
        int oldLocal = IncidentCrossings(edges, affected);
        float firstCenter = first.Average(v => v.Location.Y), secondCenter = second.Average(v => v.Location.Y);
        foreach (var column in columns)
        {
            var a = column.Where(left.Contains).ToArray(); var b = column.Where(right.Contains).ToArray();
            if (a.Length == 0 && b.Length == 0) continue;
            if (a.Length == 0 || b.Length == 0)
            {
                foreach (var v in a) v.Location = new(v.Location.X, v.Location.Y + secondCenter - firstCenter);
                foreach (var v in b) v.Location = new(v.Location.X, v.Location.Y + firstCenter - secondCenter);
                continue;
            }
            var upper = a.Min(v => v.Location.Y) < b.Min(v => v.Location.Y) ? a : b;
            var lower = upper == a ? b : a;
            float top = upper.Min(v => v.Location.Y - v.Height / 2), upperBottom = upper.Max(v => v.Location.Y + v.Height / 2);
            float lowerTop = lower.Min(v => v.Location.Y - v.Height / 2), bottom = lower.Max(v => v.Location.Y + v.Height / 2);
            if (lowerTop < upperBottom || column.Any(v => !moved.Contains(v) && v.Location.Y > top && v.Location.Y < bottom))
            { foreach (var (v, p) in original) v.Location = p; return false; }
            float gap = Math.Max(22, lowerTop - upperBottom), upperHeight = upperBottom - top, lowerHeight = bottom - lowerTop;
            foreach (var v in upper) v.Location = new(v.Location.X, v.Location.Y + lowerHeight + gap);
            foreach (var v in lower) v.Location = new(v.Location.X, v.Location.Y - upperHeight - gap);
        }
        int next = score - oldLocal + IncidentCrossings(edges, affected);
        if (next < score && columns.All(NoOverlap)) { score = next; return true; }
        foreach (var (v, p) in original) v.Location = p;
        return false;
    }

    private static bool NoOverlap(IEnumerable<BranchVertex> column)
    {
        var order = column.OrderBy(v => v.Location.Y).ToArray();
        return Enumerable.Range(1, Math.Max(0, order.Length - 1)).All(i => order[i].Location.Y - order[i - 1].Location.Y >= (order[i].Height + order[i - 1].Height) / 2 + 12);
    }
    private static List<Edge> Edges(IReadOnlyList<BranchVertex> vertices)
    {
        var edges = vertices.SelectMany(v => v.Children.Select(c => (v, c))).Select((e, i) => new Edge(i, e.v, e.c)).ToList();
        for (int i = 0; i < edges.Count; i++) for (int j = i + 1; j < edges.Count; j++)
        {
            var a = edges[i]; var b = edges[j];
            if (a.From == b.From || a.To == b.To || a.From == b.To || a.To == b.From ||
                Math.Min(a.To.Location.X, b.To.Location.X) <= Math.Max(a.From.Location.X, b.From.Location.X)) continue;
            a.Candidates.Add(b); b.Candidates.Add(a);
        }
        return edges;
    }
    private static int IncidentCrossings(List<Edge> all, Edge[] affected)
    {
        var selected = affected.ToHashSet(); int count = 0;
        foreach (var edge in affected) foreach (var other in edge.Candidates)
            if (edge != other && (!selected.Contains(other) || edge.Id < other.Id) && Crosses(edge, other)) count++;
        return count;
    }
    private static bool Crosses(Edge a, Edge b)
    {
        if (a.From == b.From || a.To == b.To || a.From == b.To || a.To == b.From) return false;
        PointF p = a.From.Location, q = a.To.Location, r = b.From.Location, s = b.To.Location;
        if (Math.Min(q.X, s.X) <= Math.Max(p.X, r.X)) return false;
        static float Side(PointF a, PointF b, PointF c) => (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        return Side(p, q, r) * Side(p, q, s) < 0 && Side(r, s, p) * Side(r, s, q) < 0;
    }
}
