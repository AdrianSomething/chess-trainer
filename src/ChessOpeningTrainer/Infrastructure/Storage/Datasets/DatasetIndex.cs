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
    private readonly Queue<int> cacheOrder = [];
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
            if (!reader.ReadBytes(8).SequenceEqual(DatasetIndexFormat.Magic))
                throw new InvalidDataException("Unsupported dataset index. Rebuild it in Data sets.");
            long offset = reader.ReadInt64();
            StopAtGames = reader.ReadInt32();
            PositionCount = reader.ReadInt64();
            if (offset < 28 || offset > file.Length - 4)
                throw new InvalidDataException("Invalid index directory.");
            file.Position = offset;
            int count = reader.ReadInt32();
            if (count < 0 || count > (file.Length - offset - 4) / 47)
                throw new InvalidDataException("Invalid index directory size.");
            for (int i = 0; i < count; i++)
            {
                byte[] first = reader.ReadBytes(35);
                long at = reader.ReadInt64();
                int length = reader.ReadInt32();
                if (at < 28 || length < 1 || length > 4_000_000 || at + length > offset)
                    throw new InvalidDataException("Invalid index block address.");
                blocks.Add(new(first, at, length));
            }
        }
        catch { Dispose(); throw; }
    }

    public static byte[] Key(Position p)
    {
        // Exact key, not a hash: no collision ambiguity. Counters are excluded.
        var key = new byte[35];
        const string pieces = "\0PNBRQKpnbrqk";
        for (int i = 0; i < 64; i++)
            key[i / 2] |= (byte)(Math.Max(0, pieces.IndexOf(p.Squares[i])) << ((i % 2) * 4));
        key[32] = (byte)(p.WhiteToMove ? 1 : 0);
        foreach (char c in p.Castling)
        {
            int bit = "KQkq".IndexOf(c);
            if (bit >= 0)
                key[33] |= (byte)(1 << bit);
        }
        key[34] = (byte)(p.EnPassant is { } ep && MoveGenerator.GenerateLegal(p).Any(m => m.IsEnPassant) ? ep + 1 : 0);
        return key;
    }

    private DatasetIndexRecord? Find(Position position)
    {
        byte[] key = Key(position);
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
        if (!cache.TryGetValue(candidate, out var records))
        {
            var block = blocks[candidate];
            file.Position = block.Offset;
            try
            {
                records = DatasetIndexFormat.Decode(decompressor.Unwrap(reader.ReadBytes(block.Length), 4_000_000).ToArray());
            }
            catch (ZstdException ex) { throw new InvalidDataException("Damaged dataset index. Rebuild it in Data sets.", ex); }
            while (cacheOrder.Count >= 32)
                cache.Remove(cacheOrder.Dequeue());
            cache[candidate] = records;
            cacheOrder.Enqueue(candidate);
        }
        low = 0;
        high = records.Count - 1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2, compare = records[middle].Key.AsSpan().SequenceCompareTo(key);
            if (compare == 0)
                return records[middle];
            if (compare < 0)
                low = middle + 1;
            else
                high = middle - 1;
        }
        return null;
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
    public static string Build(DatasetStore store, DatasetSettings settings, IProgress<string>? progress, CancellationToken token)
        => DatasetIndexBuilder.Build(store, settings, progress, token);
}
