using ZstdSharp;

namespace ChessOpeningTrainer;

/// <summary>Version 4: large independently searchable blocks; choose the smaller
/// of row/prefix coding and columnar key/count/edge coding for each block.</summary>
internal static class DatasetCompactFormat
{
    internal static readonly byte[] Magic = "COTDSI04"u8.ToArray();
    internal const int MaxRecords = 4096;
    internal static void Write(string path, IEnumerable<DatasetIndexRecord> records, int cutoff, CancellationToken token)
    {
        using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        using var output = new BinaryWriter(file);
        output.Write(Magic); output.Write(0L); output.Write(cutoff); output.Write(0L);
        var directory = new List<DatasetIndexFormat.Block>();
        using var compressor = new Compressor(6);
        long count = 0;
        foreach (var group in Groups(records))
        {
            token.ThrowIfCancellationRequested();
            byte[] rows = compressor.Wrap(DatasetIndexFormat.Encode(group)).ToArray();
            byte[] columns = compressor.Wrap(EncodeColumns(group)).ToArray();
            bool column = columns.Length < rows.Length;
            byte[] payload = column ? columns : rows;
            directory.Add(new(group[0].Key, file.Position, payload.Length + 1, count, group.Count));
            output.Write((byte)(column ? 1 : 0)); output.Write(payload);
            count += group.Count;
        }
        long offset = file.Position;
        output.Write(directory.Count);
        foreach (var b in directory)
        {
            output.Write(b.FirstKey); output.Write(b.Offset); output.Write(b.Length);
            output.Write(b.FirstOrdinal); output.Write(b.Records);
        }
        file.Position = 8; output.Write(offset); output.Write(cutoff); output.Write(count);
        output.Flush(); file.Flush(true);
    }
    private static IEnumerable<List<DatasetIndexRecord>> Groups(IEnumerable<DatasetIndexRecord> records)
    {
        var group = new List<DatasetIndexRecord>(); int upperBytes = 0;
        foreach (var record in records)
        {
            int size = 80 + record.Edges.Count * 42;
            if (group.Count >= MaxRecords || upperBytes + size > 2_000_000)
            { yield return group; group = []; upperBytes = 0; }
            group.Add(record); upperBytes += size;
        }
        if (group.Count > 0) yield return group;
    }
    private static byte[] EncodeColumns(List<DatasetIndexRecord> records)
    {
        using var stream = new MemoryStream(); using var w = new BinaryWriter(stream);
        w.Write((ushort)records.Count);
        byte[] previous = new byte[35];
        foreach (var r in records)
        {
            int prefix = 0; while (prefix < 35 && previous[prefix] == r.Key[prefix]) prefix++;
            w.Write((byte)prefix); w.Write(r.Key.AsSpan(prefix)); previous = r.Key;
        }
        foreach (var r in records) DatasetIndexFormat.WriteCounts(w, r.Counts);
        foreach (var r in records)
        {
            w.Write((ushort)r.Edges.Count);
            foreach (var e in r.Edges)
            {
                bool shared = e.Counts == r.Counts;
                w.Write((ushort)(e.Move | (shared ? 0x8000 : 0)));
                if (!shared) DatasetIndexFormat.WriteCounts(w, e.Counts);
            }
        }
        return stream.ToArray();
    }
}
