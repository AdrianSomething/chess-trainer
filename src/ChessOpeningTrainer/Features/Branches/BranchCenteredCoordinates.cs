using System.Diagnostics;

namespace ChessOpeningTrainer;

/// <summary>Joint column projection can move blocking neighbours together, unlike
/// independent clamping. Column order is fixed and no crossing increase is accepted.</summary>
public static class BranchCenteredCoordinates
{
    public static double Error(IReadOnlyList<BranchVertex> nodes) => nodes.Where(v => v.Children.Count > 0)
        .Sum(v => Math.Abs(v.Location.Y - (v.Children.Min(c => c.Location.Y) + v.Children.Max(c => c.Location.Y)) / 2));

    internal static float[] Project(float[] desired, float[] heights, float gap = 12)
    {
        // Pool-adjacent-violators: least-squares projection with exact minimum
        // gaps, not a downward-only packing pass that drifts at the boundaries.
        int n = desired.Length;
        var offsets = new float[n];
        for (int i = 1; i < n; i++) offsets[i] = offsets[i - 1] + (heights[i - 1] + heights[i]) / 2 + gap;
        var pools = new List<(int Start, int End, double Sum, int Count)>();
        for (int i = 0; i < n; i++)
        {
            pools.Add((i, i, desired[i] - offsets[i], 1));
            while (pools.Count > 1 && pools[^2].Sum / pools[^2].Count > pools[^1].Sum / pools[^1].Count)
            {
                var a = pools[^2]; var b = pools[^1];
                pools.RemoveRange(pools.Count - 2, 2);
                pools.Add((a.Start, b.End, a.Sum + b.Sum, a.Count + b.Count));
            }
        }
        var result = new float[n];
        foreach (var pool in pools)
            for (int i = pool.Start; i <= pool.End; i++) result[i] = (float)(pool.Sum / pool.Count) + offsets[i];
        return result;
    }

    public static void Apply(IReadOnlyList<BranchVertex> nodes, int budgetMilliseconds = 800)
    {
        if (nodes.Count < 2 || nodes.Count > 20000) return;
        var watch = Stopwatch.StartNew();
        var columns = nodes.GroupBy(n => n.Location.X).OrderBy(g => g.Key).Select(g => g.OrderBy(n => n.Location.Y).ToArray()).ToArray();
        var parents = nodes.ToDictionary(n => n, _ => new List<BranchVertex>());
        foreach (var v in nodes) foreach (var c in v.Children) parents[c].Add(v);
        var saved = nodes.Select(n => n.Location).ToArray();
        int crossings = BranchCrossingReduction.CountCrossings(nodes);
        double bestError = Error(nodes);
        for (int pass = 0; pass < 12 && watch.ElapsedMilliseconds < budgetMilliseconds; pass++)
        {
            bool backwards = pass % 3 != 1;
            foreach (var column in backwards ? columns.Reverse() : columns)
            {
                var wanted = column.Select(v =>
                {
                    var family = backwards ? v.Children.ToArray() : v.Children.Count > 1 ? [] : parents[v].ToArray();
                    return family.Length == 0 ? v.Location.Y : (family.Min(c => c.Location.Y) + family.Max(c => c.Location.Y)) / 2;
                }).ToArray();
                var y = Project(wanted, column.Select(v => v.Height).ToArray());
                for (int i = 0; i < column.Length; i++) column[i].Location = new(column[i].Location.X, y[i]);
            }
            double error = Error(nodes);
            if (error < bestError - .01 && BranchCrossingReduction.CountCrossings(nodes) <= crossings)
            {
                bestError = error;
                saved = nodes.Select(n => n.Location).ToArray();
            }
            // Several coordinated sweeps may be needed before accepting a
            // candidate. Never leave the intermediate candidate on screen.
        }
        for (int i = 0; i < nodes.Count; i++) nodes[i].Location = saved[i];
    }
}
