namespace ChessOpeningTrainer;

public static class GeneratorGraphLayout
{
    /// <summary>Fit the whole graph; increasingly reserve horizontal space for branched work instead of the setup stem.</summary>
    public static Dictionary<string, PointF> Points(GeneratorGraphSnapshot data, Size size)
    {
        var layers = data.Nodes.GroupBy(n => n.Depth).OrderBy(g => g.Key).ToArray();
        var points = new Dictionary<string, PointF>();
        if (layers.Length == 0) return points;
        int maxDepth = Math.Max(1, layers[^1].Key), widest = layers.Max(g => g.Count());
        var depths = data.Nodes.ToDictionary(n => n.Id, n => n.Depth);
        int branchDepth = data.Edges.GroupBy(e => e.From).Where(g => g.Count() > 1)
            .Select(g => depths.GetValueOrDefault(g.Key)).DefaultIfEmpty(maxDepth).Min();
        float stemWeight = widest > 1 ? (float)Math.Clamp(1 / (1 + Math.Log2(widest)), .08, .7) : 1;
        double WeightedDepth(int depth) => Math.Min(depth, branchDepth) * stemWeight + Math.Max(0, depth - branchDepth);
        double extent = Math.Max(.01, WeightedDepth(maxDepth));
        foreach (var layer in layers)
        {
            int i = 0, count = layer.Count();
            foreach (var node in layer)
                points[node.Id] = new(18 + (float)(WeightedDepth(layer.Key) / extent) * Math.Max(1, size.Width - 36f),
                    18 + (++i) * Math.Max(1, size.Height - 36f) / (count + 1));
        }
        return points;
    }
}
