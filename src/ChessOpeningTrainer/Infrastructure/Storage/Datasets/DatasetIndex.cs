using ZstdSharp;

namespace ChessOpeningTrainer;

/// <summary>Read-only compact position DAG, including transpositions.
/// One position/edge counts at most once per game. Instances are not shared across threads.</summary>
public sealed class DatasetIndex : IDisposable
{
    private readonly FileStream file;
    private readonly BinaryReader reader;
    private readonly Decompressor decompressor = new();
    private readonly List<DatasetIndexFormat.Block> blocks = [];
    private readonly Dictionary<int, List<DatasetIndexRecord>> cache = [];
    private readonly Dictionary<int, DatasetCompactBlock> compactCache = [];
    private readonly Queue<int> cacheOrder = [];
    private readonly int version;
    public int FormatVersion => version;
    public int StopAtGames
    {
        get;
    }
    public long PositionCount
    {
        get;
    }

    public DatasetIndex(string path)
    {
        file = new(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
        reader = new(file);
        try
        {
            byte[] magic = reader.ReadBytes(8);
            version = magic.SequenceEqual("COTDSI02"u8.ToArray()) ? 2 : magic.SequenceEqual(DatasetIndexFormat.Magic) ? 3 : magic.SequenceEqual(DatasetCompactFormat.Magic) ? 4 : 0;
            if (version == 0)
                throw new InvalidDataException("Unsupported dataset index. Rebuild it in Datasets.");
            long offset = reader.ReadInt64();
            StopAtGames = reader.ReadInt32();
            PositionCount = reader.ReadInt64();
            if (offset < 28 || offset > file.Length - 4)
                throw new InvalidDataException("Invalid index directory.");
            file.Position = offset;
            int count = reader.ReadInt32();
            if (count < 0 || count > (file.Length - offset - 4) / (version == 4 ? 59 : 47))
                throw new InvalidDataException("Invalid index directory size.");
            for (int i = 0; i < count; i++)
            {
                const int keyLength = 35;
                if (keyLength < 5 || keyLength > 51)
                    throw new InvalidDataException("Invalid directory key.");
                byte[] first = reader.ReadBytes(keyLength);
                long at = reader.ReadInt64();
                int length = reader.ReadInt32();
                if (at < 28 || length < 1 || length > 4_000_000 || at + length > offset)
                    throw new InvalidDataException("Invalid index block address.");
                long ordinal = version == 4 ? reader.ReadInt64() : (long)i * DatasetIndexFormat.BlockRecords;
                int records = version == 4 ? reader.ReadInt32() : (int)Math.Min(DatasetIndexFormat.BlockRecords, PositionCount - ordinal);
                long expected = blocks.Count == 0 ? 0 : blocks[^1].FirstOrdinal + blocks[^1].Records;
                if (ordinal != expected || records < 1 || records > (version == 4 ? DatasetCompactFormat.MaxRecords : DatasetIndexFormat.BlockRecords))
                    throw new InvalidDataException("Invalid index block ordinal.");
                blocks.Add(new(first, at, length, ordinal, records));
            }
            if ((blocks.Count == 0 ? 0 : blocks[^1].FirstOrdinal + blocks[^1].Records) != PositionCount)
                throw new InvalidDataException("Invalid index position count.");
        }
        catch { Dispose(); throw; }
    }

    public static byte[] Key(Position p) => DatasetPositionKey.Legacy(p);
    internal static Position PositionFromKey(byte[] key) => DatasetPositionKey.Decode(key);

    internal DatasetIndexRecord? Find(Position position) => FindKey(Key(position), out _);
    internal DatasetIndexRecord? FindKey(byte[] key, out long ordinal)
    {
        ordinal = -1;
        int low = 0, high = blocks.Count - 1, candidate = -1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            if (blocks[middle].FirstKey.AsSpan().SequenceCompareTo(key) <= 0)
            {
                candidate = middle;
                low = middle + 1;
            }
            else
                high = middle - 1;
        }
        if (candidate < 0)
            return null;
        if (version == 4)
        {
            var block = ReadCompactBlock(candidate);
            int at = block.Find(key);
            if (at < 0) return null;
            ordinal = blocks[candidate].FirstOrdinal + at;
            return block.At(at);
        }
        var records = ReadBlock(candidate);
        low = 0;
        high = records.Count - 1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2, compare = records[middle].Key.AsSpan().SequenceCompareTo(key);
            if (compare == 0) { ordinal = blocks[candidate].FirstOrdinal + middle; return records[middle]; }
            if (compare < 0) low = middle + 1;
            else high = middle - 1;
        }
        return null;
    }
    private List<DatasetIndexRecord> ReadBlock(int candidate, bool remember = true)
    {
        if (!cache.TryGetValue(candidate, out var records))
        {
            var block = blocks[candidate];
            file.Position = block.Offset;
            try
            {
                byte[] raw = decompressor.Unwrap(reader.ReadBytes(block.Length), 4_000_000).ToArray();
                records = DatasetIndexFormat.Decode(raw, version);
                if (records.Count != block.Records) throw new InvalidDataException("Invalid block record count.");
            }
            catch (ZstdException ex) { throw new InvalidDataException("Damaged dataset index. Rebuild it in Datasets.", ex); }
            if (!remember) return records;
            while (cacheOrder.Count >= 32)
                cache.Remove(cacheOrder.Dequeue());
            cache[candidate] = records;
            cacheOrder.Enqueue(candidate);
        }
        return records;
    }
    private DatasetCompactBlock ReadCompactBlock(int candidate, bool remember = true)
    {
        if (compactCache.TryGetValue(candidate, out var found)) return found;
        var block = blocks[candidate]; file.Position = block.Offset;
        byte kind = reader.ReadByte();
        DatasetCompactBlock decoded;
        try { decoded = new(decompressor.Unwrap(reader.ReadBytes(block.Length - 1), 4_000_000).ToArray(), kind); }
        catch (ZstdException ex) { throw new InvalidDataException("Damaged compact index. Rebuild it in Datasets.", ex); }
        if (decoded.Count != block.Records) throw new InvalidDataException("Invalid compact block record count.");
        if (remember)
        {
            while (cacheOrder.Count >= 8) compactCache.Remove(cacheOrder.Dequeue());
            compactCache[candidate] = decoded; cacheOrder.Enqueue(candidate);
        }
        return decoded;
    }
    internal DatasetIndexRecord At(long ordinal)
    {
        if (ordinal < 0 || ordinal >= PositionCount) throw new ArgumentOutOfRangeException(nameof(ordinal));
        int low = 0, high = blocks.Count - 1;
        while (low < high)
        {
            int middle = (low + high + 1) / 2;
            if (blocks[middle].FirstOrdinal <= ordinal) low = middle; else high = middle - 1;
        }
        int at = (int)(ordinal - blocks[low].FirstOrdinal);
        return version == 4 ? ReadCompactBlock(low).At(at) : ReadBlock(low)[at];
    }
    internal IEnumerable<DatasetIndexRecord> Records(CancellationToken token)
    {
        for (int i = 0; i < blocks.Count; i++)
        {
            token.ThrowIfCancellationRequested();
            if (version == 4)
            {
                var block = ReadCompactBlock(i, false);
                for (int j = 0; j < block.Count; j++) yield return block.At(j);
            }
            else foreach (var record in ReadBlock(i, false)) yield return record;
        }
    }
    public DatasetCounts Counts(Position p) => Find(p)?.Counts ?? default;
    public IReadOnlyList<(string Move, DatasetCounts Counts)> Children(Position p) => Find(p)?.Edges
        .OrderByDescending(e => e.Counts.Total).ThenBy(e => e.Move).Select(e => (PackedMove.Decode(e.Move), e.Counts)).ToArray() ?? [];
    public void Dispose()
    {
        reader.Dispose();
        file.Dispose();
        decompressor.Dispose();
    }
    public static string Build(DatasetStore store, DatasetSettings settings, IProgress<string>? progress, CancellationToken token, DatasetOperation? operation = null)
    {
        using var owned = operation is null ? new DatasetOperation() : null;
        return DatasetStreamingBuilder.Build(store, settings, progress, token, operation ?? owned!);
    }
}
