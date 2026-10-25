namespace ChessOpeningTrainer;

/// <summary>Decoded key/offset slab. A lookup materializes only its requested
/// record, not thousands of unused adjacency lists. Used only for v4 blocks.</summary>
internal sealed class DatasetCompactBlock
{
    private readonly byte[] raw, keys = new byte[DatasetCompactFormat.MaxRecords * 35];
    private readonly int[] countsAt = new int[DatasetCompactFormat.MaxRecords], edgesAt = new int[DatasetCompactFormat.MaxRecords];
    private readonly bool columns;
    internal int Count { get; }
    internal DatasetCompactBlock(byte[] raw, byte kind)
    {
        this.raw = raw;
        if (kind > 1) throw new InvalidDataException("Invalid compact block codec.");
        columns = kind == 1;
        using var stream = new MemoryStream(raw, false); using var reader = new BinaryReader(stream);
        void Key(int i)
        {
            int prefix = reader.ReadByte();
            if (prefix > 35 || i >= DatasetCompactFormat.MaxRecords) throw new InvalidDataException("Invalid compact key.");
            if (i > 0) keys.AsSpan((i - 1) * 35, prefix).CopyTo(keys.AsSpan(i * 35));
            stream.ReadExactly(keys.AsSpan(i * 35 + prefix, 35 - prefix));
            if (i > 0 && keys.AsSpan((i - 1) * 35, 35).SequenceCompareTo(keys.AsSpan(i * 35, 35)) >= 0)
                throw new InvalidDataException("Unsorted compact block.");
        }
        void Counts(int i) { countsAt[i] = checked((int)stream.Position); DatasetIndexFormat.ReadCounts(reader, false); }
        void Edges(int i)
        {
            edgesAt[i] = checked((int)stream.Position);
            int count = EdgeCount(reader);
            for (int e = 0; e < count; e++) if ((reader.ReadUInt16() & 0x8000) == 0) DatasetIndexFormat.ReadCounts(reader, false);
        }
        if (columns)
        {
            Count = reader.ReadUInt16();
            if (Count is < 1 or > DatasetCompactFormat.MaxRecords) throw new InvalidDataException("Invalid compact block count.");
            for (int i = 0; i < Count; i++) Key(i);
            for (int i = 0; i < Count; i++) Counts(i);
            for (int i = 0; i < Count; i++) Edges(i);
        }
        else
        {
            int i = 0;
            while (stream.Position < stream.Length) { Key(i); Counts(i); Edges(i); i++; }
            Count = i;
        }
        if (Count == 0 || stream.Position != stream.Length) throw new InvalidDataException("Invalid compact block length.");
    }
    private int EdgeCount(BinaryReader reader)
    {
        ulong count = columns ? reader.ReadUInt16() : DatasetIndexFormat.ReadUInt(reader);
        if (count > 256) throw new InvalidDataException("Invalid compact edge count.");
        return (int)count;
    }
    internal int Find(byte[] key)
    {
        int lo = 0, hi = Count - 1;
        while (lo <= hi)
        {
            int mid = (lo + hi) / 2, compare = keys.AsSpan(mid * 35, 35).SequenceCompareTo(key);
            if (compare == 0) return mid;
            if (compare < 0) lo = mid + 1; else hi = mid - 1;
        }
        return -1;
    }
    internal DatasetIndexRecord At(int i)
    {
        if (i < 0 || i >= Count) throw new ArgumentOutOfRangeException(nameof(i));
        using var stream = new MemoryStream(raw, false); using var reader = new BinaryReader(stream);
        stream.Position = countsAt[i]; var counts = DatasetIndexFormat.ReadCounts(reader, false);
        stream.Position = edgesAt[i]; int count = EdgeCount(reader);
        var edges = new List<(ushort, DatasetCounts)>(count);
        for (int e = 0; e < count; e++)
        {
            ushort move = reader.ReadUInt16();
            edges.Add(((ushort)(move & 0x7fff), (move & 0x8000) != 0 ? counts : DatasetIndexFormat.ReadCounts(reader, false)));
        }
        return new(keys.AsSpan(i * 35, 35).ToArray(), counts, edges);
    }
}
