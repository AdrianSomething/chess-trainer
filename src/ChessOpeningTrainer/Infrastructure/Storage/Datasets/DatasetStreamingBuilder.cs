namespace ChessOpeningTrainer;

/// <summary>Bounded aggregation, compressed sorted runs, 16-way merge/reduce.
/// n means input plies, not games: O(n log n) work with bounded chess degree.
/// No SQL position/edge table and no dataset-sized in-memory dictionary.</summary>
internal static class DatasetStreamingBuilder
{
    internal static string Build(DatasetStore store, DatasetSettings settings, IProgress<string>? progress,
        CancellationToken token, DatasetOperation operation, int runLimit = 100_000)
    {
        var active = store.Manifest.Entries.Where(e => e.Active).ToArray();
        if (active.Length == 0) throw new InvalidOperationException("Select at least one dataset.");
        int cutoff = Math.Max(0, store.Manifest.StopAtGames);
        string signature = store.Signature, sourceSignature = store.SourceSignature;
        string file = "index-" + Guid.NewGuid().ToString("N") + ".cotidx", path = store.IndexPath(file);
        string workspace = path + ".runs";
        Directory.CreateDirectory(workspace);
        var roots = new HashSet<string>();
        var owned = new List<string>();
        string NewRun() { string p = Path.Combine(workspace, Guid.NewGuid().ToString("N") + ".run"); owned.Add(p); return p; }
        long Observe() => operation.ObserveFiles(Directory.EnumerateFiles(workspace).Append(path));
        void Report(string phase) => progress?.Report(phase + " · " + L.T("Arbeitsraum belegt") + $": {Observe() / 1e6:0.0} MB");
        try
        {
            var runs = new List<string>();
            var runCounts = new Dictionary<string, long>();
            var buffer = new Dictionary<string, DatasetIndexRecord>();
            int bufferedEdges = 0;
            void Flush()
            {
                if (buffer.Count == 0) return;
                operation.Wait(token);
                string run = NewRun();
                DatasetCompactFormat.Write(run, buffer.Values.OrderBy(r => r.Key, Keys.Instance), 0, token);
                runCounts[run] = buffer.Count;
                runs.Add(run); buffer.Clear(); bufferedEdges = 0; Observe();
            }
            long done = 0, total = active.Sum(e => e.Games);
            var games = active.SelectMany(e => DatasetStore.Games(store.PathFor(e), token)).AsParallel()
                .WithDegreeOfParallelism(Math.Clamp(settings.IndexWorkers, 1, 32)).WithCancellation(token)
                .Select(DatasetGamePositions.Project);
            foreach (var game in games)
            {
                operation.Wait(token);
                roots.Add(Convert.ToHexString(game.Items[0].Key));
                var counts = DatasetCounts.From(game.Result);
                // Identical semantics to the stable builder: count every position
                // and distinct outgoing move once per game, including repetitions.
                foreach (var items in game.Items.GroupBy(i => Convert.ToHexString(i.Key)))
                {
                    var edges = items.Where(i => i.Move.HasValue).Select(i => i.Move!.Value).Distinct().Select(m => (m, counts)).ToList();
                    var record = new DatasetIndexRecord(DatasetPositionKey.Expand(items.First().Key), counts, edges);
                    bufferedEdges += edges.Count;
                    buffer[items.Key] = buffer.TryGetValue(items.Key, out var prior) ? Combine(prior, record) : record;
                    if (buffer.Count >= runLimit || bufferedEdges >= runLimit * 3) Flush();
                }
                if (++done % 1000 == 0)
                    Report($"{done:N0}/{total:N0} " + L.T("Partien") + " · " + operation.Timing(done, total));
            }
            Flush();
            if (runs.Count == 0) { string empty = NewRun(); DatasetCompactFormat.Write(empty, [], 0, token); runs.Add(empty); runCounts[empty] = 0; }
            int round = 0;
            var mergeProgress = new DatasetMergeProgress(operation, runs.Count);
            while (runs.Count > 1)
            {
                var next = new List<string>();
                long consumedRecords = 0, inputRecords = runs.Sum(p => runCounts[p]);
                int groupNumber = 0, groups = (runs.Count + 15) / 16;
                mergeProgress.StartPass();
                void MergeReport() => Report(mergeProgress.Describe(round + 1, groupNumber, groups, consumedRecords, inputRecords));
                foreach (var group in runs.Chunk(16))
                {
                    operation.Wait(token);
                    groupNumber++;
                    MergeReport();
                    if (group.Length == 1) { next.Add(group[0]); consumedRecords += runCounts[group[0]]; MergeReport(); continue; }
                    string merged = NewRun();
                    long outputRecords = 0;
                    long reportAt = 0;
                    IEnumerable<DatasetIndexRecord> Tracked()
                    {
                        foreach (var record in Merge(group, token, () => consumedRecords++))
                        {
                            operation.Wait(token);
                            if (Environment.TickCount64 - reportAt > 750)
                            { reportAt = Environment.TickCount64; MergeReport(); }
                            outputRecords++;
                            yield return record;
                        }
                    }
                    DatasetCompactFormat.Write(merged, Tracked(), 0, token);
                    runCounts[merged] = outputRecords;
                    Observe(); next.Add(merged);
                    foreach (string consumed in group) { File.Delete(consumed); runCounts.Remove(consumed); }
                    MergeReport();
                }
                runs = next; round++;
            }
            operation.Wait(token);
            if (cutoff == 0) File.Move(runs[0], path);
            else
            {
                Report(L.T("Seltene Fortsetzungen entfernen und Index komprimieren …"));
                DatasetStreamingPrune.Write(runs[0], path, workspace, roots, cutoff, operation, token,
                    () => Report(L.T("Index komprimieren")));
            }
            Observe(); operation.Wait(token);
            var m = store.Manifest;
            var old = (m.IndexFile, m.IndexSignature, m.IndexPeakWorkspaceBytes, m.IndexSourceSignature, m.IndexRoots);
            try
            {
                m.IndexFile = file; m.IndexSignature = signature; m.IndexPeakWorkspaceBytes = operation.PeakWorkspaceBytes;
                m.IndexSourceSignature = sourceSignature; m.IndexRoots = roots.ToList(); store.Save();
            }
            catch
            {
                (m.IndexFile, m.IndexSignature, m.IndexPeakWorkspaceBytes, m.IndexSourceSignature, m.IndexRoots) = old;
                throw;
            }
            return path;
        }
        catch { File.Delete(path); throw; }
        finally
        {
            // Only files created for this uniquely named operation are owned here.
            foreach (string p in owned) File.Delete(p);
            if (!Directory.EnumerateFileSystemEntries(workspace).Any()) Directory.Delete(workspace);
        }
    }
    private static DatasetCounts Add(DatasetCounts a, DatasetCounts b) => new(checked(a.White + b.White), checked(a.Draw + b.Draw), checked(a.Black + b.Black), checked(a.Unknown + b.Unknown));
    private static DatasetIndexRecord Combine(DatasetIndexRecord a, DatasetIndexRecord b)
    {
        var edges = a.Edges.ToDictionary(e => e.Move, e => e.Counts);
        foreach (var e in b.Edges) edges[e.Move] = Add(edges.GetValueOrDefault(e.Move), e.Counts);
        return new(a.Key, Add(a.Counts, b.Counts), edges.OrderBy(e => e.Key).Select(e => (e.Key, e.Value)).ToList());
    }
    private static IEnumerable<DatasetIndexRecord> Merge(string[] paths, CancellationToken token, Action consumed)
    {
        var readers = new List<DatasetIndex>(); var iterators = new List<IEnumerator<DatasetIndexRecord>>();
        var queue = new PriorityQueue<int, byte[]>(Keys.Instance);
        try
        {
            foreach (string path in paths)
            {
                var reader = new DatasetIndex(path); readers.Add(reader);
                var iterator = reader.Records(token).GetEnumerator(); iterators.Add(iterator);
                if (iterator.MoveNext()) queue.Enqueue(iterators.Count - 1, iterator.Current.Key);
            }
            DatasetIndexRecord? pending = null;
            while (queue.TryDequeue(out int i, out _))
            {
                token.ThrowIfCancellationRequested();
                consumed();
                var record = iterators[i].Current;
                if (pending is not null && !pending.Key.AsSpan().SequenceEqual(record.Key)) { yield return pending; pending = null; }
                pending = pending is null ? record : Combine(pending, record);
                if (iterators[i].MoveNext()) queue.Enqueue(i, iterators[i].Current.Key);
            }
            if (pending is not null) yield return pending;
        }
        finally { foreach (var i in iterators) i.Dispose(); foreach (var r in readers) r.Dispose(); }
    }
    private sealed class Keys : IComparer<byte[]>
    {
        internal static readonly Keys Instance = new();
        public int Compare(byte[]? a, byte[]? b) => a.AsSpan().SequenceCompareTo(b);
    }
}
