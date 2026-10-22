namespace ChessOpeningTrainer;

public sealed record DatasetSizeEstimate(long LowerBytes, long UpperBytes, long TemporaryBytes, int SampleGames);

/// <summary>Measures the actual codec on a bounded, stratified sample. Sparse
/// tails are uncertain: show a range, never a promised compression factor.</summary>
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
        string workspace = Path.Combine(dataDirectory, "estimates", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workspace);
        try
        {
            var store = new DatasetStore(workspace);
            store.Register(store.Write(sample, "estimate", 0, 100, null, token));
            double scale = Math.Max(1, total / (double)sample.Count);
            long Measure(int threshold)
            {
                store.Manifest.StopAtGames = threshold;
                string path = DatasetIndex.Build(store, workers, null, token);
                return new FileInfo(path).Length;
            }
            long upper = Measure((int)Math.Floor(cutoff / scale));
            long lower = cutoff > 0 ? Measure(cutoff) : upper;
            // A conservative workspace indication, separate from final archive size.
            long temporary = (long)Math.Min(long.MaxValue, sample.Sum(g => (long)g.Moves.Length + 1) * 160d * scale);
            return new((long)(Math.Min(lower, upper) * scale), (long)(Math.Max(lower, upper) * scale), temporary, sample.Count);
        }
        finally { Directory.Delete(workspace, true); }
    }
}
