using ChessOpeningTrainer;
using System.Reflection;
using static Program;

internal static class CompactStorageChecks
{
    internal static void Run(string root)
    {
        var store = new DatasetStore(Path.Combine(root, "compact"));
        var games = new List<DatasetGame>(); var random = new Random(263);
        DatasetGame Game(string san, string result = "1-0", string fen = Position.InitialFen) => new(fen, MainlineMoves.Parse(san, Position.FromFen(fen)), result, 1500, 1500, 180, 0, "Normal");
        games.AddRange([Game("1. Nf3 Nf6 2. Ng1 Ng8 3. Nf3 d5 4. g3"), Game("1. g3 d5 2. Nf3 Nf6", "0-1"),
            Game("1. e4 e5 2. Nf3 Nc6 3. Bc4 Nf6 4. O-O", "1/2-1/2"), Game("1. e4 a6 2. e5 d5 3. exd6", "*"),
            Game("1. a8=N", "*", "7k/P7/8/8/8/8/8/7K w - - 0 1")]);
        for (int i = 0; i < 170; i++)
        {
            var p = Position.FromFen(Position.InitialFen); var moves = new List<string>();
            for (int j = 0; j < 32; j++) { var legal = MoveGenerator.GenerateLegal(p); if (legal.Count == 0) break; var move = legal[random.Next(legal.Count)]; moves.Add(move.Uci); p.Apply(move); }
            games.Add(new(Position.InitialFen, moves.ToArray(), new[] { "1-0", "0-1", "1/2-1/2", "*" }[i % 4], 1500, 1500, 180, 0, "Normal"));
        }
        games.AddRange(games.Take(5).ToArray());
        store.Register(store.Write(games, "Synthetic compact regression", 0, 100, null, default));
        var positions = new Dictionary<string, Position>();
        var expected = new Dictionary<string, DatasetCounts>();
        DatasetCounts Sum(DatasetCounts a, DatasetCounts b) => new(a.White + b.White, a.Draw + b.Draw, a.Black + b.Black, a.Unknown + b.Unknown);
        foreach (var game in games)
        {
            var p = Position.FromFen(game.StartFen); var visited = new HashSet<string>();
            void Observe() { string key = Convert.ToHexString(DatasetIndex.Key(p)); positions[key] = p.Clone(); if (visited.Add(key)) expected[key] = Sum(expected.GetValueOrDefault(key), DatasetCounts.From(game.Result)); }
            Observe(); foreach (string uci in game.Moves) { p.Apply(MoveGenerator.GenerateLegal(p).Single(m => m.Uci == uci)); Observe(); }
        }
        long fullPositions = 0;
        foreach (int cutoff in new[] { 0, 1, 3 })
        {
            store.Manifest.StopAtGames = cutoff;
            using var stable = new DatasetIndex(DatasetIndex.Build(store, new(), null, default));
            Check(stable.FormatVersion == 4, "Streaming format is the only default builder");
            if (cutoff == 0) fullPositions = stable.PositionCount;
            if (cutoff == 1) Check(stable.PositionCount < fullPositions, "Cutoff 1 removes singleton continuations; 0 retains them");
            using var operation = new DatasetOperation();
            // Small runs force >16 runs and multiple merge passes in CI without
            // relying on large downloads or a special production setting.
            var builder = typeof(DatasetIndex).Assembly.GetType("ChessOpeningTrainer.DatasetStreamingBuilder")!;
            var reports = new List<string>();
            string path = (string)builder.GetMethod("Build", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null,
                [store, new DatasetSettings { IndexWorkers = 2 }, new CallbackProgress(reports.Add), CancellationToken.None, operation, 31])!;
            Check(reports.Any(r => r.Contains("2/2")), "Merge progress exposes total passes");
            Check(reports.Any(r => r.Contains(L.T("Blockgruppe")) && r.Contains(L.T("Verbleibend"))), "Merge groups and separate ETA are visible");
            using var compact = new DatasetIndex(path);
            Check(compact.FormatVersion == 4, "New compact format selected");
            Check(stable.PositionCount == compact.PositionCount, "Same exact pruned reachability");
            foreach (var pair in positions)
            {
                var p = pair.Value;
                Check(stable.Counts(p) == compact.Counts(p), "Single-run and multi-pass result counters agree");
                Check(stable.Children(p).SequenceEqual(compact.Children(p)), "Single-run and multi-pass continuation counters agree");
                if (cutoff == 0) Check(compact.Counts(p) == expected[pair.Key], "Independent once-per-game counter oracle");
            }
            Check(!Directory.EnumerateDirectories(store.DirectoryPath, "*.runs").Any(), "Owned merge workspace removed after success");
            Check(operation.PeakWorkspaceBytes > 0 && !store.IsStale, "Compact snapshot and measured workspace published");
        }
        string previous = store.Manifest.IndexFile!;
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        try { DatasetIndex.Build(store, new(), null, cancelled.Token); Check(false, "Cancellation must throw"); }
        catch (OperationCanceledException) { }
        Check(store.Manifest.IndexFile == previous && File.Exists(store.IndexPath(previous)), "Compact cancellation keeps previous snapshot");
        Check(!Directory.EnumerateDirectories(store.DirectoryPath, "*.runs").Any(), "Cancelled workspace removed");
        using (var midMerge = new CancellationTokenSource())
        using (var operation = new DatasetOperation())
        {
            var builder = typeof(DatasetIndex).Assembly.GetType("ChessOpeningTrainer.DatasetStreamingBuilder")!;
            try
            {
                builder.GetMethod("Build", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null,
                    [store, new DatasetSettings(), new CallbackProgress(_ => midMerge.Cancel()), midMerge.Token, operation, 31]);
                Check(false, "Mid-merge cancellation must throw");
            }
            catch (TargetInvocationException ex) when (ex.InnerException is OperationCanceledException) { }
            Check(store.Manifest.IndexFile == previous && File.Exists(store.IndexPath(previous)), "Mid-merge cancellation preserves last published index");
            Check(!Directory.EnumerateDirectories(store.DirectoryPath, "*.runs").Any(), "Partially written compressed runs cleaned on cancellation");
        }
        Check(new UserPreferences().BranchLayout == "sugiyama", "Sugiyama is default");
        Check(new DatasetManifest().StopAtGames == 0, "Default keeps every continuation");
        var progressType = typeof(DatasetIndex).Assembly.GetType("ChessOpeningTrainer.DatasetMergeProgress")!;
        var countPasses = progressType.GetMethod("CountPasses", BindingFlags.Static | BindingFlags.NonPublic)!;
        foreach (var pair in new[] { (0, 0), (1, 0), (16, 1), (17, 2), (256, 2), (257, 3) })
            Check((int)countPasses.Invoke(null, [pair.Item1])! == pair.Item2, "Merge pass total handles fan-in boundaries");
        using (var timing = new DatasetOperation())
        {
            var merge = Activator.CreateInstance(progressType, timing, 17)!;
            progressType.GetMethod("StartPass", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(merge, null);
            var describe = progressType.GetMethod("Describe", BindingFlags.Instance | BindingFlags.NonPublic)!;
            Check(((string)describe.Invoke(merge, [1, 1, 2, 2L, 500000L])!).EndsWith("…"), "Merge reader startup is not extrapolated into an absurd ETA");
            Check(((string)describe.Invoke(merge, [2, 1, 1, 500000L, 500000L])!).EndsWith("00:00:00"), "Finished merge has zero remaining work");
            timing.TogglePause();
            var active = typeof(DatasetOperation).GetProperty("ActiveSeconds", BindingFlags.Instance | BindingFlags.NonPublic)!;
            double pausedAt = (double)active.GetValue(timing)!;
            Thread.Sleep(20);
            Check((double)active.GetValue(timing)! == pausedAt, "Merge phase clock excludes pause time");
            timing.TogglePause();
        }
        var existingScratch = Directory.GetDirectories(Path.GetTempPath(), "cot-estimate-*").ToHashSet();
        var estimate = DatasetForecast.Estimate(store.DirectoryPath, store.Manifest.Entries.ToArray(), 0, new() { SampleSize = 100 }, default);
        var prunedEstimate = DatasetForecast.Estimate(store.DirectoryPath, store.Manifest.Entries.ToArray(), 3, new() { SampleSize = 100 }, default);
        Check(estimate.LowerBytes == estimate.UpperBytes && estimate.UpperBytes > 0, "Numeric point forecast");
        Check(estimate.TemporaryBytes >= estimate.UpperBytes * 2, "Scaled forecast reserves simultaneous merge inputs and output");
        Check(prunedEstimate.UpperBytes > 0 && prunedEstimate.UpperBytes <= estimate.UpperBytes, "Pruned sample extrapolation is bounded by full tree");
        Check(prunedEstimate.TemporaryBytes >= estimate.UpperBytes * 2, "Pruning does not hide full aggregation workspace");
        Check(Directory.GetDirectories(Path.GetTempPath(), "cot-estimate-*").All(existingScratch.Contains), "Forecast owns and cleans its scratch directories");
        var small = new DatasetStore(Path.Combine(root, "small-forecast"));
        small.Register(small.Write(games.Take(5), "Exact forecast fixture", 0, 100, null, default));
        var exactEstimate = DatasetForecast.Estimate(small.DirectoryPath, small.Manifest.Entries.ToArray(), 1, new(), default);
        small.Manifest.StopAtGames = 1;
        using (var measured = new DatasetOperation())
        {
            string built = DatasetIndex.Build(small, new(), null, default, measured);
            Check(exactEstimate.UpperBytes == new FileInfo(built).Length, "Whole-sample forecast measures actual selected cutoff");
            Check(exactEstimate.TemporaryBytes == measured.PeakWorkspaceBytes, "Whole-sample forecast includes pruning workspace overlap");
        }
    }
    private sealed class CallbackProgress(Action<string> action) : IProgress<string>
    {
        public void Report(string value) => action(value);
    }
}
