namespace ChessOpeningTrainer;

public sealed record DatasetSizeEstimate(long LowerBytes, long UpperBytes, long TemporaryBytes, int SampleGames);

/// <summary>Measures the actual codec on a bounded, stratified sample. Sparse
/// tails are uncertain: this is an extrapolation, never a promised compression factor.</summary>
public static class DatasetForecast
{
    public static DatasetSizeEstimate Estimate(string dataDirectory, DatasetEntry[] active, int cutoff,
        DatasetSettings settings, CancellationToken token)
    {
        long total = active.Sum(e => e.Games);
        if (total == 0)
            return new(0, 0, 0, 0);
        int limit = Math.Clamp(settings.SampleSize, 100, 2000);
        var sample = new List<DatasetGame>();
        var random = new Random(260);
        var workers = new DatasetSettings { IndexWorkers = Math.Clamp(settings.EstimateWorkers, 1, 16), FilterWorkers = Math.Clamp(settings.EstimateWorkers, 1, 16), SampleSize = 100 };
        foreach (var entry in active)
        {
            token.ThrowIfCancellationRequested();
            int take = Math.Max(1, (int)Math.Round(limit * entry.Games / (double)total));
            sample.AddRange(entry.Sample.OrderBy(_ => random.Next()).Take(take));
        }
        if (sample.Count == 0)
            return new(0, 0, 0, 0);
        // Only this uniquely created workspace is removed. Source datasets are never touched.
        // Keep native SQLite journal paths short even with a deeply nested profile.
        string workspace = Path.Combine(Path.GetTempPath(), "cot-estimate-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspace);
        try
        {
            double scale = Math.Max(1, total / (double)sample.Count);
            (long Full, long Pruned, long Peak) Measure(IEnumerable<DatasetGame> games, string name)
            {
                var store = new DatasetStore(Path.Combine(workspace, name));
                store.Register(store.Write(games, "estimate", 0, 100, null, token));
                // If the sample is the entire dataset, measure the real selected
                // build, including pruning's input/output overlap, without fitting.
                store.Manifest.StopAtGames = scale == 1 ? cutoff : 0;
                using var operation = new DatasetOperation();
                string path = DatasetIndex.Build(store, workers, null, token, operation);
                long full = new FileInfo(path).Length;
                store.Manifest.StopAtGames = cutoff;
                long pruned = scale == 1 || cutoff == 0 ? full : DatasetPruningForecast.Measure(store, token)
                    ?? new FileInfo(DatasetIndex.Build(store, workers, null, token, operation)).Length;
                return (full, pruned, operation.PeakWorkspaceBytes);
            }
            var measured = Measure(sample, "full");
            if (scale == 1) return new(measured.Pruned, measured.Pruned, measured.Peak, sample.Count);
            // Measure growth at the SAME absolute cutoff on two nested samples.
            // This retains evidence of repeated openings instead of multiplying
            // every singleton's frequency by the dataset/sample ratio.
            sample = sample.OrderBy(_ => random.Next()).ToList();
            int halfCount = Math.Max(1, sample.Count / 2);
            var half = Measure(sample.Take(halfCount), "half");
            double ratio = sample.Count / (double)halfCount;
            long fullEstimate = Extrapolate(half.Full, measured.Full, ratio, scale, 1);
            long prunedEstimate = cutoff == 0 ? fullEstimate
                : Math.Min(fullEstimate, Extrapolate(half.Pruned, measured.Pruned, ratio, scale, 1.25));
            // Merge inputs and outputs coexist. A small sample usually makes one
            // run, so scaling its measured workspace alone misses this peak.
            // Pruning happens AFTER full aggregation; it cannot reduce this budget.
            long temporary = Saturate(Math.Max(measured.Peak * scale, fullEstimate * 2.25));
            return new(prunedEstimate, prunedEstimate, temporary, sample.Count);
        }
        finally { Directory.Delete(workspace, true); }
    }
    private static long Extrapolate(long half, long full, double ratio, double scale, double maxExponent)
    {
        double exponent = ratio > 1 && half > 0 ? Math.Log(Math.Max(1, full / (double)half)) / Math.Log(ratio) : 1;
        // Counter widths can grow even where topology is already saturated.
        exponent = Math.Clamp(exponent, .05, maxExponent);
        return Saturate(Math.Max(full, full * Math.Pow(scale, exponent)));
    }
    private static long Saturate(double bytes) => (long)Math.Min(long.MaxValue / 2, Math.Max(0, bytes));
}
