using System.Diagnostics;

namespace ChessOpeningTrainer;

/// <summary>Optional bounded search beyond the greedy layout's local optimum.
/// Only the best globally measured arrangement is retained. Graph data is read-only.</summary>
public static class BranchGlobalRefinement
{
    private sealed record Edge(BranchVertex A, BranchVertex B);
    private sealed record Pair(Edge A, Edge B);
    public static int Apply(IReadOnlyList<BranchVertex> nodes, int budgetMilliseconds = 3500)
    {
        if (nodes.Count < 4) return 0;
        var watch = Stopwatch.StartNew();
        var original = nodes.Select(v => v.Location).ToArray();
        var edges = nodes.SelectMany(v => v.Children.Select(c => new Edge(v, c))).OrderBy(e => e.A.Location.X).ToArray();
        var pairs = new List<Pair>();
        var incident = nodes.ToDictionary(v => v, _ => new HashSet<int>());
        // Sweep in X; edges in disjoint depth intervals need no testing.
        for (int i = 0; i < edges.Length; i++)
        {
            if (watch.ElapsedMilliseconds > budgetMilliseconds || pairs.Count > 1_000_000) return 0;
            var a = edges[i];
            for (int j = i + 1; j < edges.Length && edges[j].A.Location.X < a.B.Location.X; j++)
            {
                var b = edges[j];
                if (a.A == b.A || a.A == b.B || a.B == b.A || a.B == b.B) continue;
                int id = pairs.Count; pairs.Add(new(a, b));
                incident[a.A].Add(id); incident[a.B].Add(id); incident[b.A].Add(id); incident[b.B].Add(id);
            }
        }
        int initial = pairs.Count(Crosses), score = initial, best = initial;
        if (initial == 0) return 0;
        PointF[] saved = original.ToArray();
        var columns = nodes.GroupBy(n => n.Location.X).Where(g => g.Count() > 1).Select(g => g.OrderBy(n => n.Location.Y).ToList()).ToArray();
        var random = new Random(251);
        int limit = Math.Min(40000, Math.Max(5000, nodes.Count * 24));
        var parents = nodes.ToDictionary(n => n, _ => new List<BranchVertex>());
        foreach (var e in edges) parents[e.B].Add(e.A);
        var family = nodes.ToDictionary(n => n, n => parents[n].OrderByDescending(p => p.Location.X).ThenBy(p => p.Id).FirstOrDefault() ?? n);
        // Coordinated barycentric sweeps can cross barriers that isolated
        // swaps cannot: entire continuation columns must sometimes reorder.
        var allColumns = nodes.GroupBy(n => n.Location.X).OrderBy(g => g.Key).Select(g => g.OrderBy(n => n.Location.Y).ToList()).ToArray();
        for (int seed = 0; seed < 6 && watch.ElapsedMilliseconds < budgetMilliseconds; seed++)
        {
            for (int i = 0; i < nodes.Count; i++) nodes[i].Location = original[i];
            for (int sweep = 0; sweep < 16 && watch.ElapsedMilliseconds < budgetMilliseconds; sweep++)
            {
                bool right = sweep % 2 == 0;
                foreach (var column in right ? allColumns : allColumns.Reverse())
                {
                    float Wanted(BranchVertex v)
                    {
                        var linked = right ? parents[v] : v.Children.ToList();
                        if (linked.Count == 0) return v.Location.Y;
                        var ys = linked.Select(n => n.Location.Y).Order().ToArray();
                        return seed is 1 or 4 ? ys[ys.Length / 2] : ys.Average();
                    }
                    var groups = column.GroupBy(v => family[v]).OrderBy(g => g.Average(Wanted)).ThenBy(g => g.Key.Id);
                    var ordered = seed < 3 ? groups.SelectMany(g => g.OrderBy(Wanted).ThenBy(v => v.Id)).ToArray()
                        : column.OrderBy(Wanted).ThenBy(v => family[v].Id).ThenBy(v => v.Id).ToArray();
                    column.Clear(); column.AddRange(ordered);
                    var wanted = ordered.Select(Wanted).ToArray();
                    if (seed is 2 or 5)
                    {
                        // Compact seed: avoid inherited whitespace dominating
                        // the crossing objective in shallow/deep columns.
                        float y = -ordered.Sum(v => v.Height + 22) / 2;
                        foreach (var v in ordered) { v.Location = new(v.Location.X, y + v.Height / 2); y += v.Height + 22; }
                    }
                    else Project(ordered, wanted);
                }
                int measured = pairs.Count(Crosses);
                if (measured < best) { best = measured; saved = nodes.Select(v => v.Location).ToArray(); }
            }
        }
        for (int i = 0; i < nodes.Count; i++) nodes[i].Location = saved[i];
        foreach (var row in columns) row.Sort((a, b) => a.Location.Y.CompareTo(b.Location.Y));
        score = best;
        // Exchange complete primary-parent subtrees across all affected layers.
        // Moving only one layer can add crossings on the next layer and hide a
        // much better coordinated ordering behind a local minimum.
        var descendants = nodes.ToDictionary(v => v, v => new HashSet<BranchVertex> { v });
        foreach (var v in nodes.OrderByDescending(v => v.Location.X))
            if (family[v] != v) descendants[family[v]].UnionWith(descendants[v]);
        for (int trial = 0; trial < 1400 && watch.ElapsedMilliseconds < budgetMilliseconds * .65 && best > 0; trial++)
        {
            var crossing = pairs.Where(Crosses).ToArray();
            if (crossing.Length == 0) break;
            var picked = crossing[random.Next(crossing.Length)];
            var a = trial % 2 == 0 ? picked.A.A : picked.A.B;
            var b = trial % 2 == 0 ? picked.B.A : picked.B.B;
            while (a.Location.X > b.Location.X && family[a] != a) a = family[a];
            while (b.Location.X > a.Location.X && family[b] != b) b = family[b];
            if (a == b || descendants[a].Contains(b) || descendants[b].Contains(a)) continue;
            if (a.Location.Y > b.Location.Y) (a, b) = (b, a);
            var first = descendants[a]; var second = descendants[b];
            var before = nodes.Select(v => v.Location).ToArray();
            foreach (var column in allColumns)
            {
                var order = column.OrderBy(v => v.Location.Y).ToArray();
                var one = order.Where(first.Contains).ToArray(); var two = order.Where(second.Contains).ToArray();
                if (one.Length == 0 || two.Length == 0) continue;
                // Exchange the occupied slots; intervening unrelated families
                // remain in their order. Reproject for unequal node heights.
                var slots = order.Select(v => v.Location.Y).ToArray();
                var replacement = two.Concat(one).ToArray(); int take = 0;
                for (int i = 0; i < order.Length; i++) if (first.Contains(order[i]) || second.Contains(order[i])) order[i] = replacement[take++];
                Project(order, slots);
            }
            int measured = pairs.Count(Crosses);
            if (measured < best) { best = measured; saved = nodes.Select(v => v.Location).ToArray(); }
            else for (int i = 0; i < nodes.Count; i++) nodes[i].Location = before[i];
        }
        foreach (var row in columns) row.Sort((a, b) => a.Location.Y.CompareTo(b.Location.Y));
        score = best;
        for (int attempt = 0; attempt < limit && watch.ElapsedMilliseconds < budgetMilliseconds && best > 0; attempt++)
        {
            var column = columns[random.Next(columns.Length)];
            int at = random.Next(column.Count - 1);
            // Exchange contiguous sibling blocks, or reorder two siblings.
            int lo = at, mid = at + 1, hi = mid + 1;
            if (family[column[at]] != family[column[mid]] && random.Next(3) != 0)
            {
                while (lo > 0 && family[column[lo - 1]] == family[column[at]]) lo--;
                while (hi < column.Count && family[column[hi]] == family[column[mid]]) hi++;
            }
            var upper = column.GetRange(lo, mid - lo); var lower = column.GetRange(mid, hi - mid);
            var moving = upper.Concat(lower).ToArray(); var before = moving.Select(v => v.Location).ToArray();
            var affected = moving.SelectMany(v => incident[v]).Distinct().ToArray();
            int old = affected.Count(i => Crosses(pairs[i]));
            float top = upper[0].Location.Y - upper[0].Height / 2;
            float upperBottom = upper[^1].Location.Y + upper[^1].Height / 2;
            float lowerTop = lower[0].Location.Y - lower[0].Height / 2;
            float bottom = lower[^1].Location.Y + lower[^1].Height / 2;
            float gap = Math.Max(12, lowerTop - upperBottom);
            foreach (var v in lower) v.Location = new(v.Location.X, v.Location.Y + top - lowerTop);
            foreach (var v in upper) v.Location = new(v.Location.X, v.Location.Y + bottom - lowerTop + gap);
            int next = score - old + affected.Count(i => Crosses(pairs[i]));
            double phase = (attempt % 5000) / 5000d;
            double temperature = Math.Max(.08, 1.5 * (1 - phase));
            bool accept = next < score || random.NextDouble() < Math.Exp((score - next) / temperature);
            if (accept)
            {
                score = next;
                column.RemoveRange(lo, hi - lo); column.InsertRange(lo, lower.Concat(upper));
                if (score < best) { best = score; saved = nodes.Select(v => v.Location).ToArray(); }
            }
            else for (int i = 0; i < moving.Length; i++) moving[i].Location = before[i];
            if ((attempt + 1) % 5000 == 0)
            {
                for (int i = 0; i < nodes.Count; i++) nodes[i].Location = saved[i];
                foreach (var row in columns) row.Sort((a, b) => a.Location.Y.CompareTo(b.Location.Y));
                score = best;
            }
        }
        // Even if the time budget expires during an uphill exploration, rollback
        // to the global best. No ties or worse candidates replace the baseline.
        for (int i = 0; i < nodes.Count; i++) nodes[i].Location = best < initial ? saved[i] : original[i];
        // Continuous refinement within existing row gaps. Long transposition
        // edges often cross even with the right node order; their crossing
        // breakpoints provide useful Y candidates without an arbitrary grid.
        for (int pass = 0; pass < 6 && watch.ElapsedMilliseconds < budgetMilliseconds; pass++)
        {
            bool changed = false;
            foreach (var column in columns)
            {
                column.Sort((a, b) => a.Location.Y.CompareTo(b.Location.Y));
                for (int at = 0; at < column.Count && watch.ElapsedMilliseconds < budgetMilliseconds; at++)
                {
                    var v = column[at]; var affected = incident[v].ToArray();
                    var crossed = affected.Where(i => Crosses(pairs[i])).ToArray();
                    if (crossed.Length == 0) continue;
                    float min = at == 0 ? v.Location.Y - 400 : column[at-1].Location.Y + (column[at-1].Height + v.Height) / 2 + 12;
                    float max = at == column.Count-1 ? v.Location.Y + 400 : column[at+1].Location.Y - (column[at+1].Height + v.Height) / 2 - 12;
                    var candidates = new HashSet<float> { min, max, (min + max) / 2 };
                    foreach (int id in crossed.Take(80))
                    {
                        var pair = pairs[id];
                        var own = pair.A.A == v || pair.A.B == v ? pair.A : pair.B;
                        var other = own == pair.A ? pair.B : pair.A;
                        var fixedPoint = own.A == v ? own.B.Location : own.A.Location;
                        void Boundary(PointF a, PointF b)
                        {
                            if (Math.Abs(b.X - a.X) < .01) return;
                            float y = a.Y + (v.Location.X - a.X) * (b.Y - a.Y) / (b.X - a.X);
                            if (float.IsFinite(y)) { candidates.Add(Math.Clamp(y - .5f, min, max)); candidates.Add(Math.Clamp(y + .5f, min, max)); }
                        }
                        Boundary(other.A.Location, other.B.Location);
                        Boundary(fixedPoint, other.A.Location); Boundary(fixedPoint, other.B.Location);
                    }
                    var before = v.Location; int localBest = crossed.Length; float wanted = before.Y;
                    foreach (float y in candidates)
                    {
                        v.Location = new(before.X, y);
                        int count = affected.Count(i => Crosses(pairs[i]));
                        if (count < localBest) { wanted = y; localBest = count; }
                    }
                    v.Location = new(before.X, wanted);
                    if (localBest < crossed.Length) { best -= crossed.Length - localBest; changed = true; }
                }
            }
            if (!changed) break;
        }
        return initial - best;
    }
    private static bool Crosses(Pair pair)
    {
        var p = pair.A.A.Location; var q = pair.A.B.Location; var r = pair.B.A.Location; var s = pair.B.B.Location;
        static double Side(PointF a, PointF b, PointF c) => ((double)b.X - a.X) * (c.Y - a.Y) - ((double)b.Y - a.Y) * (c.X - a.X);
        return Side(p, q, r) * Side(p, q, s) < 0 && Side(r, s, p) * Side(r, s, q) < 0;
    }
    private static void Project(BranchVertex[] order, float[] wanted)
    {
        var offsets = new float[order.Length];
        for (int i = 1; i < order.Length; i++) offsets[i] = offsets[i-1] + (order[i-1].Height + order[i].Height) / 2 + 22;
        var blocks = new List<(int Start, int Count, double Sum)>();
        for (int i = 0; i < order.Length; i++)
        {
            blocks.Add((i, 1, wanted[i] - offsets[i]));
            while (blocks.Count > 1 && blocks[^2].Sum / blocks[^2].Count > blocks[^1].Sum / blocks[^1].Count)
            {
                var a = blocks[^2]; var b = blocks[^1]; blocks.RemoveAt(blocks.Count - 1);
                blocks[^1] = (a.Start, a.Count + b.Count, a.Sum + b.Sum);
            }
        }
        foreach (var b in blocks) for (int i = b.Start; i < b.Start + b.Count; i++) order[i].Location = new(order[i].Location.X, (float)(b.Sum / b.Count) + offsets[i]);
    }
}
