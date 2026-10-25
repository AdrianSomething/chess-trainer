using System.Diagnostics;

namespace ChessOpeningTrainer;

/// <summary>Measure a stricter cutoff against complete, exact frequencies, not
/// a small game sample. The existing index is read-only; scratch output is owned.</summary>
public static class DatasetPruningForecast
{
    public static long? Measure(DatasetStore store, CancellationToken token, bool compact = false)
    {
        if (!store.HasIndex || store.Manifest.IndexSourceSignature != store.SourceSignature || store.Manifest.IndexRoots.Count == 0)
            return null;
        using var index = new DatasetIndex(store.IndexPath(store.Manifest.IndexFile!));
        int cutoff = store.Manifest.StopAtGames;
        if (cutoff == index.StopAtGames && index.FormatVersion == (compact ? 4 : 3)) return new FileInfo(store.IndexPath(store.Manifest.IndexFile!)).Length;
        if (cutoff < index.StopAtGames) return null;
        var watch = Stopwatch.StartNew();
        var pending = new Stack<Position>(store.Manifest.IndexRoots.Select(k => DatasetIndex.PositionFromKey(Convert.FromHexString(k))));
        var visited = new HashSet<string>();
        var records = new List<DatasetIndexRecord>();
        while (pending.TryPop(out var position))
        {
            token.ThrowIfCancellationRequested();
            // Bound memory and latency even for adversarial/highly connected DAGs.
            if (records.Count > 100000 || watch.ElapsedMilliseconds > 5000) return null;
            if (!visited.Add(Convert.ToHexString(DatasetIndex.Key(position)))) continue;
            var record = index.Find(position);
            if (record is null) continue;
            bool stop = cutoff > 0 && record.Counts.Total <= cutoff;
            records.Add(stop ? record with { Edges = [] } : record);
            if (stop) continue;
            var legal = MoveGenerator.GenerateLegal(position).ToDictionary(m => m.Uci);
            foreach (var edge in record.Edges)
            {
                var next = position.Clone();
                next.Apply(legal[PackedMove.Decode(edge.Move)]);
                pending.Push(next);
            }
        }
        string scratch = Path.Combine(store.DirectoryPath, "estimate-" + Guid.NewGuid().ToString("N") + ".cotidx");
        try
        {
            records.Sort((a, b) => a.Key.AsSpan().SequenceCompareTo(b.Key));
            if (compact) DatasetCompactFormat.Write(scratch, records, cutoff, token);
            else DatasetIndexFormat.Write(scratch, records, cutoff, token);
            return new FileInfo(scratch).Length;
        }
        finally { if (File.Exists(scratch)) File.Delete(scratch); }
    }
}
