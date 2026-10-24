using ChessOpeningTrainer;
using static Program;

internal static class StorageChecks
{
    private static DatasetGame Line(string moves, string result = "1-0") => new(Position.InitialFen,
        MainlineMoves.Parse(moves, Position.FromFen(Position.InitialFen)), result, 1500, 1500, 180, 2, "Normal");
    internal static void Run(string root)
    {
        var store = new DatasetStore(root);
        var settings = new DatasetSettings { IndexWorkers = 2, FilterWorkers = 2 };
        var games = new[] { Line("1. Nf3 d5 2. g3 Nf6"), Line("1. g3 Nf6 2. Nf3 d5", "1/2-1/2") };
        var entry = store.Write(games, "Synthetic transpositions", 0, 100, null, default);
        store.Register(entry);
        string path = DatasetIndex.Build(store, settings, null, default);
        Check(!store.IsStale, "Published snapshot matches active inputs");
        Check(store.Manifest.IndexPeakWorkspaceBytes > 0, "Actual workspace recorded");
        using (var index = new DatasetIndex(path))
        {
            var p = Position.FromFen(Position.InitialFen);
            MainlineMoves.Parse("1. Nf3 d5 2. g3 Nf6", p);
            Check(index.Counts(p) == new DatasetCounts(1, 1, 0, 0), "Exact transposition counts");
            Check(index.Children(Position.FromFen(Position.InitialFen)).Count == 2, "Both root moves");
        }
        store.Manifest.StopAtGames = 1;
        long? prediction = DatasetPruningForecast.Measure(store, default);
        string pruned = DatasetIndex.Build(store, settings, null, default);
        Check(prediction == new FileInfo(pruned).Length, "Cutoff measured from full counts equals rebuilt size");
        string previous = store.Manifest.IndexFile!;
        using (var cancel = new CancellationTokenSource())
        {
            cancel.Cancel();
            try { DatasetIndex.Build(store, settings, null, cancel.Token); }
            catch (OperationCanceledException) { }
        }
        Check(store.Manifest.IndexFile == previous && File.Exists(pruned), "Cancellation preserves complete tree");
        var lineStore = new DatasetStore(Path.Combine(root, "forced"));
        lineStore.Register(lineStore.Write([Line("1. e4 e5 2. Nf3 Nc6 3. Bb5 a6 4. Ba4 Nf6")], "Synthetic line", 0, 100, null, default));
        string linePath = DatasetIndex.Build(lineStore, settings, null, default);
        var expanded = new Dictionary<int, string> { [0] = "" };
        var compact = DatasetGraphModel.Build(linePath, expanded, default, true);
        Check(compact.Vertices.Count == 2, "Entire terminal line in one wide node");
        var anchor = compact.Expansion[compact.Vertices[1]];
        expanded[anchor.Depth] = anchor.Path;
        var full = DatasetGraphModel.Build(linePath, expanded, default, true);
        Check(full.Vertices.Count == 9, "Expansion shows all plies");
        Check(full.Vertices[1].Nodes.Any(n => n.IsSubbranch), "Expanded forced line retains collapse arrow");
        Check(full.Expansion[full.Vertices[1]] == anchor, "Same arrow collapses line again");
        lineStore.Register(lineStore.Write([Line("1. e4 e5 2. Nf3 Nc6 3. Bc4")], "Synthetic later fork", 0, 100, null, default));
        linePath = DatasetIndex.Build(lineStore, settings, null, default);
        var forked = DatasetGraphModel.Build(linePath, new Dictionary<int, string> { [0] = "" }, default, true);
        Check(forked.Vertices.Take(5).All(v => v.DisplayWidth is null), "No compaction before a later fork");
    }
}
