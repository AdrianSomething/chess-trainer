using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using ZstdSharp;

namespace ChessOpeningTrainer;

/// <summary>Transactional import checkpoints. A compressed stream must be replayed
/// to its saved record boundary after restart, but saved games are not reparsed.</summary>
internal static class DatasetImportJob
{
    internal sealed class Checkpoint
    {
        public string Source { get; set; } = "";
        public long Bytes
        {
            get; set;
        }
        public long ModifiedTicks
        {
            get; set;
        }
        public long Records
        {
            get; set;
        }
        public bool Ready
        {
            get; set;
        }
        public DatasetEntry Entry { get; set; } = new();
    }
    public static string Journal(DatasetStore store) => Path.Combine(store.DirectoryPath, "import.json");
    public static string? PendingSource(DatasetStore store) => File.Exists(Journal(store)) ? JsonSerializer.Deserialize<Checkpoint>(File.ReadAllText(Journal(store)))?.Source : null;
    public static DatasetEntry Run(DatasetStore store, string source, DatasetSettings settings, IProgress<string>? progress, CancellationToken token, DatasetOperation operation)
    {
        var info = new FileInfo(source);
        var state = File.Exists(Journal(store)) ? JsonSerializer.Deserialize<Checkpoint>(File.ReadAllText(Journal(store)))! :
            new Checkpoint { Source = info.FullName, Bytes = info.Length, ModifiedTicks = info.LastWriteTimeUtc.Ticks, Entry = new() { Name = DatasetNames.FromFile(source), SourceBytes = info.Length } };
        if (!state.Ready && (state.Source != info.FullName || state.Bytes != info.Length || state.ModifiedTicks != info.LastWriteTimeUtc.Ticks))
            throw new InvalidDataException("A different import is pending, or the source file changed. Resume or discard that import first.");
        string final = store.PathFor(state.Entry), partial = final + ".partial";
        void SaveJournal()
        {
            File.WriteAllText(Journal(store) + ".tmp", JsonSerializer.Serialize(state));
            File.Move(Journal(store) + ".tmp", Journal(store), true);
        }
        if (state.Ready)
        {
            if (!File.Exists(final))
                File.Move(partial, final);
            store.Register(state.Entry);
            File.Delete(Journal(store));
            return state.Entry;
        }
        SaveJournal();
        long bytesRead = 0, lastReport = 0;
        try
        {
            using (var db = DatasetStore.Open(partial))
            {
                using var setup = db.CreateCommand();
                setup.CommandText = "CREATE TABLE IF NOT EXISTS games(id INTEGER PRIMARY KEY,payload BLOB NOT NULL); CREATE TABLE IF NOT EXISTS checkpoint(id INTEGER PRIMARY KEY,json TEXT);";
                setup.ExecuteNonQuery();
                setup.CommandText = "SELECT json FROM checkpoint WHERE id=1";
                if (setup.ExecuteScalar() is string saved)
                    state = JsonSerializer.Deserialize<Checkpoint>(saved)!;
                var input = DatasetPgn.Read(source, token, (read, _) => Interlocked.Exchange(ref bytesRead, read));
                long skip = state.Records;
                IEnumerable<string> Remaining()
                {
                    foreach (string text in input)
                    {
                        operation.Wait(token);
                        if (operation.SaveRequested)
                            yield break;
                        if (skip > 0)
                        {
                            skip--;
                            continue;
                        }
                        yield return text;
                    }
                }
                var parsed = Remaining().AsParallel().AsOrdered().WithDegreeOfParallelism(Math.Clamp(settings.FilterWorkers, 1, 32)).WithCancellation(token)
                    .Select(text => { try { return (Game: (DatasetGame?)DatasetPgn.Parse(text), Error: ""); } catch (Exception ex) when (ex is InvalidDataException or InvalidOperationException or FormatException or ArgumentException) { return (Game: (DatasetGame?)null, Error: ex.Message); } });
                using var compressor = new Compressor(3);
                using var insert = db.CreateCommand();
                insert.CommandText = "INSERT INTO games(payload) VALUES($data)";
                var data = insert.Parameters.Add("$data", SqliteType.Blob);
                using var checkpoint = db.CreateCommand();
                checkpoint.CommandText = "INSERT OR REPLACE INTO checkpoint VALUES(1,$json)";
                var json = checkpoint.Parameters.Add("$json", SqliteType.Text);
                SqliteTransaction transaction = db.BeginTransaction();
                void Save()
                {
                    checkpoint.Transaction = transaction;
                    json.Value = JsonSerializer.Serialize(state);
                    checkpoint.ExecuteNonQuery();
                    transaction.Commit();
                    transaction.Dispose();
                    transaction = db.BeginTransaction();
                }
                try
                {
                    foreach (var item in parsed)
                    {
                        operation.Wait(token);
                        if (operation.SaveRequested)
                            break;
                        state.Records++;
                        if (item.Game is not { } game)
                        {
                            state.Entry.Skipped++;
                            string reason = item.Error.StartsWith("Illegal", StringComparison.OrdinalIgnoreCase) ? "Illegal or unrecognized move" : item.Error;
                            if (state.Entry.ImportIssues.Count >= 20 && !state.Entry.ImportIssues.ContainsKey(reason))
                                reason = "Other parse errors";
                            state.Entry.ImportIssues[reason] = state.Entry.ImportIssues.GetValueOrDefault(reason) + 1;
                        }
                        else
                        {
                            insert.Transaction = transaction;
                            data.Value = compressor.Wrap(DatasetGameCodec.Encode(game)).ToArray();
                            insert.ExecuteNonQuery();
                            state.Entry.Games++;
                            int size = Math.Clamp(settings.SampleSize, 100, 10000);
                            if (state.Entry.Sample.Count < size)
                                state.Entry.Sample.Add(game);
                            else
                            {
                                long at = Random.Shared.NextInt64(state.Entry.Games);
                                if (at < size)
                                    state.Entry.Sample[(int)at] = game;
                            }
                        }
                        if (state.Records % 1000 == 0)
                            Save();
                        if (state.Records % 1000 == 0 || Environment.TickCount64 - lastReport > 350)
                        {
                            lastReport = Environment.TickCount64;
                            progress?.Report($"{state.Entry.Name} · {state.Entry.Games:N0} " + L.T("Partien") + $" · {state.Entry.Skipped:N0} " + L.T("übersprungen") + "\n" + operation.Timing(Interlocked.Read(ref bytesRead), info.Length));
                        }
                    }
                    Save();
                }
                finally { transaction.Dispose(); }
            }
            token.ThrowIfCancellationRequested();
            if (state.Entry.Games == 0)
                throw new InvalidDataException("No supported games were processed.");
            if (operation.SaveRequested)
                state.Entry.Name += " (partial)";
            SaveJournal();
            // Keep the checkpoint until publication is recoverable, including a
            // crash between moving the file and updating the catalog.
            using (var db = DatasetStore.Open(partial))
            {
                using var clean = db.CreateCommand();
                clean.CommandText = "PRAGMA secure_delete=ON; DROP TABLE checkpoint;";
                clean.ExecuteNonQuery();
            }
            using (var stream = File.OpenRead(partial))
                state.Entry.ContentHash = Convert.ToHexString(SHA256.HashData(stream));
            state.Ready = true;
            SaveJournal();
            File.Move(partial, final);
            store.Register(state.Entry);
            File.Delete(Journal(store));
            return state.Entry;
        }
        catch
        {
            if (!operation.PreserveCheckpoint)
            {
                if (File.Exists(partial))
                    File.Delete(partial);
                File.Delete(Journal(store));
            }
            throw;
        }
    }
    public static void Discard(DatasetStore store)
    {
        if (!File.Exists(Journal(store)))
            return;
        var state = JsonSerializer.Deserialize<Checkpoint>(File.ReadAllText(Journal(store)))!;
        string partial = store.PathFor(state.Entry) + ".partial";
        File.Delete(partial);
        File.Delete(partial + "-journal");
        File.Delete(Journal(store));
    }
}
