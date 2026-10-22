using ZstdSharp;

namespace ChessOpeningTrainer;

internal sealed record DatasetIndexRecord(byte[] Key, DatasetCounts Counts,
    List<(ushort Move, DatasetCounts Counts)> Edges);

/// <summary>Sorted prefix-coded position blocks. Counts use unsigned varints;
/// Zstandard compresses each independently addressable block. No PGN metadata.</summary>
internal static class DatasetIndexFormat
{
    public static readonly byte[] Magic = "COTDSI02"u8.ToArray();
    public const int BlockRecords = 256;
    internal sealed record Block(byte[] FirstKey, long Offset, int Length);

    public static void Write(string path, IEnumerable<DatasetIndexRecord> records, int cutoff, CancellationToken token)
    {
        using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var output = new BinaryWriter(file);
        output.Write(Magic);
        output.Write(0L);
        output.Write(cutoff);
        output.Write(0L);
        var directory = new List<Block>();
        long count = 0;
        using var compressor = new Compressor(6);
        foreach (var group in records.Chunk(BlockRecords))
        {
            token.ThrowIfCancellationRequested();
            byte[] bytes = compressor.Wrap(Encode(group)).ToArray();
            directory.Add(new(group[0].Key, file.Position, bytes.Length));
            output.Write(bytes);
            count += group.Length;
        }
        long directoryOffset = file.Position;
        output.Write(directory.Count);
        foreach (var block in directory)
        {
            output.Write(block.FirstKey);
            output.Write(block.Offset);
            output.Write(block.Length);
        }
        file.Position = Magic.Length;
        output.Write(directoryOffset);
        output.Write(cutoff);
        output.Write(count);
        output.Flush();
        file.Flush(true);
    }

    public static byte[] Encode(IEnumerable<DatasetIndexRecord> records)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        byte[] previous = new byte[35];
        foreach (var record in records)
        {
            int prefix = 0;
            while (prefix < 35 && previous[prefix] == record.Key[prefix])
                prefix++;
            writer.Write((byte)prefix);
            writer.Write(record.Key.AsSpan(prefix));
            previous = record.Key;
            WriteCounts(writer, record.Counts);
            WriteUInt(writer, (ulong)record.Edges.Count);
            foreach (var edge in record.Edges)
            {
                writer.Write(edge.Move);
                WriteCounts(writer, edge.Counts);
            }
        }
        return stream.ToArray();
    }

    public static List<DatasetIndexRecord> Decode(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var reader = new BinaryReader(stream);
        var records = new List<DatasetIndexRecord>();
        byte[] previous = new byte[35];
        while (stream.Position < stream.Length)
        {
            int prefix = reader.ReadByte();
            if (prefix > 35 || records.Count >= BlockRecords)
                throw new InvalidDataException("Invalid index block.");
            byte[] key = previous.ToArray();
            stream.ReadExactly(key.AsSpan(prefix));
            previous = key;
            var counts = ReadCounts(reader);
            ulong edgeCount = ReadUInt(reader);
            if (edgeCount > 256)
                throw new InvalidDataException("Invalid edge count.");
            var edges = new List<(ushort, DatasetCounts)>();
            for (ulong i = 0; i < edgeCount; i++)
                edges.Add((reader.ReadUInt16(), ReadCounts(reader)));
            records.Add(new(key, counts, edges));
        }
        return records;
    }

    private static void WriteCounts(BinaryWriter writer, DatasetCounts c)
    {
        WriteUInt(writer, (ulong)c.White);
        WriteUInt(writer, (ulong)c.Draw);
        WriteUInt(writer, (ulong)c.Black);
        WriteUInt(writer, (ulong)c.Unknown);
    }
    private static DatasetCounts ReadCounts(BinaryReader reader) => new(checked((long)ReadUInt(reader)), checked((long)ReadUInt(reader)), checked((long)ReadUInt(reader)), checked((long)ReadUInt(reader)));
    private static void WriteUInt(BinaryWriter writer, ulong value)
    {
        while (value >= 128)
        {
            writer.Write((byte)(value | 128));
            value >>= 7;
        }
        writer.Write((byte)value);
    }
    private static ulong ReadUInt(BinaryReader reader)
    {
        ulong value = 0;
        for (int shift = 0; shift < 63; shift += 7)
        {
            byte b = reader.ReadByte();
            value |= (ulong)(b & 127) << shift;
            if ((b & 128) == 0)
                return value;
        }
        throw new InvalidDataException("Invalid index counter.");
    }
}
