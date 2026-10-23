using Microsoft.Data.Sqlite;

namespace ChessOpeningTrainer;

/// <summary>Disk-backed aggregation is temporary. Only the compact binary index
/// is published. A threshold never changes the counts of a retained position.</summary>
internal static class DatasetIndexBuilder
{
    private sealed record GamePositions(string Result, List<(byte[] Key, ushort? Move)> Items);

    private static GamePositions Positions(DatasetGame game)
    {
        var p = Position.FromFen(game.StartFen);
        var items = new List<(byte[], ushort?)>();
        foreach (string uci in game.Moves)
        {
            items.Add((DatasetPositionKey.Encode(p), PackedMove.Encode(uci)));
            var move = MoveGenerator.GenerateLegal(p).FirstOrDefault(m => m.Uci == uci) ?? throw new InvalidDataException("Invalid stored move.");
            p.Apply(move);
        }
        items.Add((DatasetPositionKey.Encode(p), null));
        return new(game.Result, items);
    }

    public static string Build(DatasetStore store, DatasetSettings settings, IProgress<string>? progress, CancellationToken token, DatasetOperation? operation = null)
    {
        var active = store.Manifest.Entries.Where(e => e.Active).ToArray();
        if (active.Length == 0)
            throw new InvalidOperationException("Select at least one dataset.");
        int cutoff = Math.Max(0, store.Manifest.StopAtGames);
        string signature = store.Signature, file = "index-" + Guid.NewGuid().ToString("N") + ".cotidx", path = store.IndexPath(file);
        string temporary = path + ".building.sqlite";
        try
        {
            using (var db = DatasetStore.Open(temporary))
            {
                using var schema = db.CreateCommand();
                schema.CommandText = "CREATE TABLE positions(k BLOB PRIMARY KEY,w INTEGER,d INTEGER,b INTEGER,u INTEGER) WITHOUT ROWID; " +
                    "CREATE TABLE edges(parent BLOB,move INTEGER,w INTEGER,d INTEGER,b INTEGER,u INTEGER,PRIMARY KEY(parent,move)) WITHOUT ROWID; " +
                    "CREATE TABLE roots(k BLOB PRIMARY KEY) WITHOUT ROWID;";
                schema.ExecuteNonQuery();
                Aggregate(db, active, store, settings, progress, token, operation);
                token.ThrowIfCancellationRequested();
                progress?.Report(L.T("Seltene Fortsetzungen entfernen und Index komprimieren …"));
                long reportAt = 0;
                IEnumerable<DatasetIndexRecord> Export()
                {
                    foreach (var record in Records(db, cutoff, token, operation))
                    {
                        operation?.Wait(token);
                        if (Environment.TickCount64 - reportAt > 500)
                        {
                            reportAt = Environment.TickCount64;
                            long bytes = operation?.ObserveWorkspace(temporary) ?? new FileInfo(temporary).Length;
                            progress?.Report(L.T("Index komprimieren") + " · " + L.T("Arbeitsraum belegt") + $": {bytes / 1e6:0.0} MB");
                        }
                        yield return record;
                    }
                }
                DatasetIndexFormat.Write(path, Export(), cutoff, token);
                operation?.ObserveWorkspace(temporary);
            }
            token.ThrowIfCancellationRequested();
            string? previousFile = store.Manifest.IndexFile, previousSignature = store.Manifest.IndexSignature;
            try
            {
                store.Manifest.IndexFile = file;
                store.Manifest.IndexSignature = signature;
                store.Save();
            }
            catch
            {
                store.Manifest.IndexFile = previousFile;
                store.Manifest.IndexSignature = previousSignature;
                throw;
            }
            return path;
        }
        catch { if (File.Exists(path)) File.Delete(path); throw; }
        finally
        {
            foreach (string owned in new[] { temporary, temporary + "-journal", temporary + "-wal", temporary + "-shm" })
                if (File.Exists(owned))
                    File.Delete(owned);
        }
    }

    private static void Aggregate(SqliteConnection db, DatasetEntry[] active, DatasetStore store, DatasetSettings settings,
        IProgress<string>? progress, CancellationToken token, DatasetOperation? operation)
    {
        using var tx = db.BeginTransaction();
        using var pos = db.CreateCommand();
        using var edge = db.CreateCommand();
        using var root = db.CreateCommand();
        root.Transaction = tx;
        root.CommandText = "INSERT OR IGNORE INTO roots VALUES($key)";
        root.Parameters.Add("$key", SqliteType.Blob);
        foreach (var command in new[] { pos, edge })
        {
            command.Transaction = tx;
            command.Parameters.Add("$key", SqliteType.Blob);
            foreach (string name in new[] { "$w", "$d", "$b", "$u" })
                command.Parameters.Add(name, SqliteType.Integer);
        }
        edge.Parameters.Add("$move", SqliteType.Integer);
        pos.CommandText = "INSERT INTO positions VALUES($key,$w,$d,$b,$u) ON CONFLICT(k) DO UPDATE SET w=w+$w,d=d+$d,b=b+$b,u=u+$u";
        edge.CommandText = "INSERT INTO edges VALUES($key,$move,$w,$d,$b,$u) ON CONFLICT(parent,move) DO UPDATE SET w=w+$w,d=d+$d,b=b+$b,u=u+$u";
        long done = 0, total = active.Sum(e => e.Games);
        var games = active.SelectMany(e => DatasetStore.Games(store.PathFor(e), token)).AsParallel()
            .WithDegreeOfParallelism(Math.Clamp(settings.IndexWorkers, 1, 32)).WithCancellation(token).Select(Positions);
        foreach (var game in games)
        {
            operation?.Wait(token);
            token.ThrowIfCancellationRequested();
            var counts = DatasetCounts.From(game.Result);
            root.Parameters["$key"].Value = game.Items[0].Key;
            root.ExecuteNonQuery();
            foreach (var command in new[] { pos, edge })
            {
                command.Parameters["$w"].Value = counts.White;
                command.Parameters["$d"].Value = counts.Draw;
                command.Parameters["$b"].Value = counts.Black;
                command.Parameters["$u"].Value = counts.Unknown;
            }
            var seenPositions = new HashSet<string>();
            var seenEdges = new HashSet<(string, ushort)>();
            for (int i = 0; i < game.Items.Count; i++)
            {
                var item = game.Items[i];
                string key = Convert.ToHexString(item.Key);
                if (seenPositions.Add(key))
                {
                    pos.Parameters["$key"].Value = item.Key;
                    pos.ExecuteNonQuery();
                }
                if (item.Move is ushort move && seenEdges.Add((key, move)))
                {
                    edge.Parameters["$key"].Value = item.Key;
                    edge.Parameters["$move"].Value = move;
                    edge.ExecuteNonQuery();
                }
            }
            if (++done % 1000 == 0)
            {
                long bytes = operation?.ObserveWorkspace(db.DataSource) ?? new FileInfo(db.DataSource).Length;
                progress?.Report($"{done:N0}/{total:N0} " + L.T("Partien") + " · " + L.T("Arbeitsraum belegt") + $": {bytes / 1e6:0.0} MB\n" + operation?.Timing(done, total));
            }
        }
        tx.Commit();
        operation?.ObserveWorkspace(db.DataSource);
    }

    private static IEnumerable<DatasetIndexRecord> Records(SqliteConnection db, int cutoff, CancellationToken token, DatasetOperation? operation)
    {
        using var query = db.CreateCommand();
        query.Parameters.AddWithValue("$cutoff", cutoff);
        if (cutoff > 0)
        {
            db.CreateFunction<byte[], long, byte[]>("position_after", (key, code) =>
            {
                operation?.Wait(token);
                token.ThrowIfCancellationRequested();
                var position = DatasetIndex.PositionFromKey(key);
                string uci = PackedMove.Decode(checked((ushort)code));
                position.Apply(MoveGenerator.GenerateLegal(position).First(m => m.Uci == uci));
                return DatasetPositionKey.Encode(position);
            });
            using var prune = db.CreateCommand();
            prune.Parameters.AddWithValue("$cutoff", cutoff);
            prune.CommandText = "CREATE TABLE kept(k BLOB PRIMARY KEY) WITHOUT ROWID; " +
                "WITH RECURSIVE reachable(k) AS (SELECT k FROM roots UNION SELECT position_after(e.parent,e.move) FROM reachable r " +
                "JOIN positions p ON p.k=r.k JOIN edges e ON e.parent=r.k WHERE p.w+p.d+p.b+p.u>$cutoff) INSERT INTO kept SELECT k FROM reachable;";
            token.ThrowIfCancellationRequested();
            using var cancellation = token.Register(prune.Cancel);
            prune.ExecuteNonQuery();
        }
        // RLE preserves the expanded key's order. No dataset-sized final sort.
        db.CreateFunction<byte[], byte[]>("position_expand", DatasetPositionKey.Expand);
        query.CommandText = "SELECT position_expand(p.k) AS sortkey,p.w,p.d,p.b,p.u,e.move,e.w,e.d,e.b,e.u FROM positions p " +
            (cutoff > 0 ? "JOIN kept ON kept.k=p.k " : "") +
            "LEFT JOIN edges e ON e.parent=p.k AND ($cutoff=0 OR p.w+p.d+p.b+p.u>$cutoff) ORDER BY p.k,e.move";
        using var sortingCancellation = token.Register(query.Cancel);
        using var reader = query.ExecuteReader();
        DatasetIndexRecord? current = null;
        while (reader.Read())
        {
            token.ThrowIfCancellationRequested();
            var key = (byte[])reader[0];
            if (current is null || !current.Key.AsSpan().SequenceEqual(key))
            {
                if (current is not null)
                    yield return current;
                current = new(key, new(reader.GetInt64(1), reader.GetInt64(2), reader.GetInt64(3), reader.GetInt64(4)), []);
            }
            if (!reader.IsDBNull(5))
                current.Edges.Add((checked((ushort)reader.GetInt32(5)), new(reader.GetInt64(6), reader.GetInt64(7), reader.GetInt64(8), reader.GetInt64(9))));
        }
        if (current is not null)
            yield return current;
    }
}
