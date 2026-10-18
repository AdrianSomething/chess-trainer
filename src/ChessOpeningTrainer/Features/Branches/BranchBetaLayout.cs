namespace ChessOpeningTrainer;

/// <summary>Default family-first DAG layout. Does not alter nodes, moves or grouping. The historical type name is retained.</summary>
public static class BranchBetaLayout
{
    private const float ColumnGap = 292.5f;
    private const float RowGap = 22;

    public static void Apply(IReadOnlyList<BranchVertex> vertices, IReadOnlyDictionary<BranchVertex, int> layers, IReadOnlyDictionary<BranchVertex, List<BranchVertex>> parents)
    {
        if (vertices.Count == 0) return;
        var primary = vertices.ToDictionary(v => v, v => parents[v].OrderByDescending(p => layers[p]).ThenBy(p => p.Id).FirstOrDefault());
        var families = vertices.ToDictionary(v => v, v => v.Children.Where(c => primary[c] == v).OrderBy(c => c.Children.Count == 0 ? 1 : 0).ThenBy(c => c.Id).ToArray());
        var spans = new Dictionary<BranchVertex, float>();
        float Span(BranchVertex v)
        {
            if (spans.TryGetValue(v, out float value)) return value;
            var children = families[v];
            return spans[v] = Math.Max(v.Height, children.Length == 0 ? 0 : children.Sum(Span) + (children.Length - 1) * RowGap);
        }
        void Place(BranchVertex v, float center)
        {
            v.Location = new(layers[v] * ColumnGap, center);
            var children = families[v];
            float childSpan = children.Sum(Span) + Math.Max(0, children.Length - 1) * RowGap;
            float top = center - childSpan / 2;
            foreach (var child in children) { Place(child, top + Span(child) / 2); top += Span(child) + RowGap; }
        }
        float rootTop = 0;
        foreach (var root in vertices.Where(v => primary[v] is null).OrderBy(v => v.Id))
        {
            Place(root, rootTop + Span(root) / 2);
            rootTop += Span(root) + RowGap;
        }
        var rows = vertices.GroupBy(v => layers[v]).OrderBy(g => g.Key).Select(g => g.OrderBy(v => v.Location.Y).ToList()).ToArray();

        // Whole sibling blocks move together during the crossing-reduction sweeps.
        // A merged node takes the center of its parents rather than dragging the
        // whole previous column downwards like a one-sided overlap projection.
        for (int pass = 0; pass < 8; pass++)
        {
            bool forward = pass % 2 == 0;
            foreach (var row in forward ? rows : rows.Reverse())
            {
                float Related(BranchVertex v)
                {
                    var family = forward ? parents[v] : v.Children.ToList();
                    return family.Count == 0 ? v.Location.Y : family.Average(n => n.Location.Y);
                }
                var ordered = row.GroupBy(v => primary[v] ?? v).OrderBy(g => g.Average(Related)).ThenBy(g => g.Key.Id)
                    .SelectMany(g => g.OrderBy(v => v.Children.Count == 0 ? 1 : 0).ThenBy(Related).ThenBy(v => v.Id)).ToArray();
                row.Clear(); row.AddRange(ordered);
                Project(row, row.Select(Related).ToArray());
            }
        }

        for (int pass = 0; pass < 28; pass++)
            foreach (var row in pass % 2 == 0 ? rows.Reverse() : rows)
            {
                float Wanted(BranchVertex v)
                {
                    float desired;
                    if (v.IsLearnerMove && v.Children.Count > 0)
                        desired = (v.Children.Min(c => c.Location.Y) + v.Children.Max(c => c.Location.Y)) / 2;
                    else if (!v.IsLearnerMove && v.Children.Count == 1)
                        desired = v.Children.First().Location.Y;
                    else if (parents[v].Count > 0)
                        desired = parents[v].Average(p => p.Location.Y);
                    else desired = v.Location.Y;
                    if (v.IsLearnerMove && parents[v].Count > 1)
                        desired = (desired + parents[v].Average(p => p.Location.Y)) / 2;
                    return .75f * desired + .25f * v.Location.Y;
                }
                Project(row, row.Select(Wanted).ToArray());
            }
    }

    // Exact least-squares projection onto non-overlap constraints (PAVA).
    // Symmetric movement preserves the column center; it cannot push every node
    // progressively outward merely because another column has more siblings.
    private static void Project(IReadOnlyList<BranchVertex> order, IReadOnlyList<float> wanted)
    {
        if (order.Count == 0) return;
        var offsets = new float[order.Count];
        for (int i = 1; i < offsets.Length; i++) offsets[i] = offsets[i - 1] + (order[i - 1].Height + order[i].Height) / 2 + RowGap;
        var blocks = new List<(int Start, int Count, double Sum)>();
        for (int i = 0; i < order.Count; i++)
        {
            blocks.Add((i, 1, wanted[i] - offsets[i]));
            while (blocks.Count >= 2)
            {
                var previous = blocks[^2]; var current = blocks[^1];
                if (previous.Sum / previous.Count <= current.Sum / current.Count) break;
                blocks.RemoveAt(blocks.Count - 1);
                blocks[^1] = (previous.Start, previous.Count + current.Count, previous.Sum + current.Sum);
            }
        }
        foreach (var block in blocks)
            for (int i = block.Start; i < block.Start + block.Count; i++)
                order[i].Location = new(order[i].Location.X, (float)(block.Sum / block.Count) + offsets[i]);
    }
}
