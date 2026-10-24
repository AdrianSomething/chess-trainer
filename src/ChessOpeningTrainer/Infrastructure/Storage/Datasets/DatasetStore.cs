using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using ZstdSharp;

namespace ChessOpeningTrainer;

/// <summary>Owns only files inside DataSets. Imports never change the user's source file.</summary>
public sealed class DatasetStore
{
    public string DirectoryPath
    {
        get;
    }
    public DatasetManifest Manifest
    {
        get; private set;
    }
    private string ManifestPath => Path.Combine(DirectoryPath, "manifest.json");
    public DatasetStore(string dataDirectory)
    {
        DirectoryPath = Path.Combine(dataDirectory, "DataSets");
        Directory.CreateDirectory(DirectoryPath);
        Manifest = File.Exists(ManifestPath) ? JsonSerializer.Deserialize<DatasetManifest>(File.ReadAllText(ManifestPath), OpeningStore.Json) ?? throw new InvalidDataException("Invalid dataset catalog.") : new();
        if (Manifest.Version != 1)
            throw new InvalidDataException("Unsupported dataset manifest version.");
        if (Manifest.Entries is null || Manifest.Entries.Any(e => !Guid.TryParseExact(e.Id, "N", out _) || e.Sample is null))
            throw new InvalidDataException("Invalid dataset entries.");
    }
    public string PathFor(DatasetEntry entry) => Path.Combine(DirectoryPath, Guid.ParseExact(entry.Id, "N").ToString("N") + ".sqlite");
    public string Signature => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("compact-v3:" + Manifest.StopAtGames + ":" + string.Join("|", Manifest.Entries.Where(e => e.Active).OrderBy(e => e.Id).Select(e => e.Id + ":" + e.ContentHash)))));
    public bool HasIndex => Manifest.IndexFile is { } file && File.Exists(IndexPath(file));
    public string SourceSignature => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("|", Manifest.Entries.Where(e => e.Active).OrderBy(e => e.Id).Select(e => e.Id + ":" + e.ContentHash)))));
    public bool IsStale => Manifest.IndexSignature != Signature && (HasIndex || Manifest.Entries.Any(e => e.Active));
    public string IndexPath(string file)
    {
        if (Path.GetFileName(file) != file || !file.StartsWith("index-"))
            throw new InvalidDataException("Invalid index filename.");
        return Path.Combine(DirectoryPath, file);
    }
    public void Save()
    {
        string temp = ManifestPath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(Manifest, OpeningStore.Json));
        File.Move(temp, ManifestPath, true);
    }
    public void Delete(DatasetEntry entry)
    {
        if (!Manifest.Entries.Contains(entry))
            throw new InvalidOperationException();
        File.Delete(PathFor(entry));
        Manifest.Entries.Remove(entry);
        Save();
    }
    public static SqliteConnection Open(string path, bool readOnly = false)
    {
        var db = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Mode = readOnly ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWriteCreate, Pooling = false }.ToString());
        db.Open();
        using var command = db.CreateCommand();
        command.CommandText = "PRAGMA cache_size=-32768; PRAGMA temp_store=FILE;";
        command.ExecuteNonQuery();
        return db;
    }
    public static IEnumerable<DatasetGame> Games(string path, CancellationToken token)
    {
        using var db = Open(path, true);
        using var command = db.CreateCommand();
        command.CommandText = "SELECT payload FROM games ORDER BY id";
        using var reader = command.ExecuteReader();
        using var decompressor = new Decompressor();
        while (reader.Read())
        {
            token.ThrowIfCancellationRequested();
            yield return DatasetGameCodec.Decode(decompressor.Unwrap((byte[])reader[0]).ToArray());
        }
    }
    public DatasetEntry Import(string source, DatasetSettings settings, IProgress<string>? progress, CancellationToken token, DatasetOperation? operation = null)
    {
        using var owned = operation is null ? new DatasetOperation() : null;
        return DatasetImportJob.Run(this, source, settings, progress, token, operation ?? owned!);
    }
    public DatasetEntry Write(IEnumerable<DatasetGame> games, string name, long sourceBytes, int sampleSize, IProgress<string>? progress, CancellationToken token, DatasetOperation? operation = null)
    {
        var entry = new DatasetEntry { Name = name, SourceBytes = sourceBytes };
        string path = PathFor(entry), temp = path + ".partial";
        try
        {
            using (var db = Open(temp))
            {
                using var setup = db.CreateCommand();
                setup.CommandText = "CREATE TABLE games(id INTEGER PRIMARY KEY, payload BLOB NOT NULL);";
                setup.ExecuteNonQuery();
                using var transaction = db.BeginTransaction();
                using var insert = db.CreateCommand();
                insert.Transaction = transaction;
                insert.CommandText = "INSERT INTO games(payload) VALUES($data)";
                var value = insert.Parameters.Add("$data", SqliteType.Blob);
                using var compressor = new Compressor(3);
                var random = new Random(260);
                sampleSize = Math.Clamp(sampleSize, 100, 10000);
                foreach (var game in games)
                {
                    operation?.Wait(token);
                    if (operation?.SaveRequested == true)
                        break;
                    token.ThrowIfCancellationRequested();
                    value.Value = compressor.Wrap(DatasetGameCodec.Encode(game)).ToArray();
                    insert.ExecuteNonQuery();
                    entry.Games++;
                    if (entry.Sample.Count < sampleSize)
                        entry.Sample.Add(game);
                    else
                    {
                        long at = random.NextInt64(entry.Games);
                        if (at < sampleSize)
                            entry.Sample[(int)at] = game;
                    }
                    if (entry.Games % 1000 == 0)
                        progress?.Report($"{entry.Games:N0} games");
                }
                if (entry.Games == 0)
                    throw new InvalidDataException("No supported legal games found.");
                transaction.Commit();
            }
            token.ThrowIfCancellationRequested();
            File.Move(temp, path);
            using var input = File.OpenRead(path);
            entry.ContentHash = Convert.ToHexString(SHA256.HashData(input));
            return entry;
        }
        catch { if (File.Exists(temp)) File.Delete(temp); throw; }
    }
    public void Register(DatasetEntry entry)
    {
        if (Manifest.Entries.All(existing => existing.Id != entry.Id))
            Manifest.Entries.Add(entry);
        Save();
    }
    public DatasetEntry Trim(DatasetEntry source, DatasetFilter filter, DatasetSettings settings, IProgress<string>? progress, CancellationToken token, DatasetOperation? operation = null)
    {
        long scanned = 0;
        long lastReport = 0;
        long prediction = source.Sample.Count == 0 ? 0 : (long)(source.Games * source.Sample.Count(filter.Matches) / (double)source.Sample.Count);
        var matches = Games(PathFor(source), token).Select((game, id) =>
        {
            operation?.Wait(token);
            Interlocked.Increment(ref scanned);
            if (Environment.TickCount64 - lastReport > 400)
            {
                lastReport = Environment.TickCount64;
                progress?.Report(source.Name + " · " + L.T("Erwartete Partien") + $": {Math.Min(filter.Limit ?? long.MaxValue, prediction):N0}\n" + operation?.Timing(scanned, source.Games));
            }
            return (Game: game, Id: (long)id + 1);
        }).TakeWhile(_ => operation?.SaveRequested != true).AsParallel().AsOrdered().WithDegreeOfParallelism(Math.Clamp(settings.FilterWorkers, 1, 32)).WithCancellation(token).Where(item => filter.Matches(item.Game));
        // Reservoir sampling has a bounded result set and no bias toward early games.
        IEnumerable<DatasetGame> selected = matches.Select(item => item.Game);
        if (filter.Limit is > 0)
        {
            if (filter.Limit > 1_000_000)
                throw new InvalidDataException("Random subset limit exceeds 1,000,000 games.");
            var reservoir = new List<long>();
            long seen = 0;
            var random = Random.Shared;
            foreach (var item in matches)
            {
                token.ThrowIfCancellationRequested();
                seen++;
                if (reservoir.Count < filter.Limit)
                    reservoir.Add(item.Id);
                else
                {
                    long at = random.NextInt64(seen);
                    if (at < filter.Limit)
                        reservoir[(int)at] = item.Id;
                }
            }
            selected = SelectedGames(PathFor(source), reservoir, token);
        }
        return Write(selected, source.Name + " (subset)", new FileInfo(PathFor(source)).Length, settings.SampleSize, null, token, filter.Limit is > 0 ? null : operation);
    }
    private static IEnumerable<DatasetGame> SelectedGames(string path, List<long> ids, CancellationToken token)
    {
        ids.Sort();
        using var db = Open(path, true);
        using var query = db.CreateCommand();
        using var decompressor = new Decompressor();
        query.CommandText = "SELECT payload FROM games WHERE id=$id";
        var id = query.Parameters.Add("$id", SqliteType.Integer);
        foreach (long value in ids)
        {
            token.ThrowIfCancellationRequested();
            id.Value = value;
            yield return DatasetGameCodec.Decode(decompressor.Unwrap((byte[])query.ExecuteScalar()!).ToArray());
        }
    }
}
