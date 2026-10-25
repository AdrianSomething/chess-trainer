namespace ChessOpeningTrainer;

internal sealed record DatasetIndexRecord(byte[] Key, DatasetCounts Counts,
    List<(ushort Move, DatasetCounts Counts)> Edges);

/// <summary>Sorted prefix-coded position blocks. Counts use unsigned varints;
/// Zstandard compresses each independently addressable block. No PGN metadata.</summary>
internal static class DatasetIndexFormat
{
    public static readonly byte[] Magic = "COTDSI03"u8.ToArray();
    public const int BlockRecords = 256;
    internal sealed record Block(byte[] FirstKey, long Offset, int Length, long FirstOrdinal = 0, int Records = 0);


    public static byte[] Encode(IEnumerable<DatasetIndexRecord> records)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        byte[] previous = new byte[35];
        foreach (var record in records)
        {
            int prefix = 0;
            while (prefix < Math.Min(previous.Length, record.Key.Length) && previous[prefix] == record.Key[prefix])
                prefix++;
            writer.Write((byte)prefix);
            writer.Write(record.Key.AsSpan(prefix));
            previous = record.Key;
            WriteCounts(writer, record.Counts);
            WriteUInt(writer, (ulong)record.Edges.Count);
            foreach (var edge in record.Edges)
            {
                bool shared = edge.Counts == record.Counts;
                writer.Write((ushort)(edge.Move | (shared ? 0x8000 : 0)));
                if (!shared)
                    WriteCounts(writer, edge.Counts);
            }
        }
        return stream.ToArray();
    }

    public static List<DatasetIndexRecord> Decode(byte[] bytes, int version = 3)
    {
        using var stream = new MemoryStream(bytes);
        using var reader = new BinaryReader(stream);
        var records = new List<DatasetIndexRecord>();
        byte[] previous = new byte[35];
        while (stream.Position < stream.Length)
        {
            int prefix = reader.ReadByte();
            const int length = 35;
            if (prefix > 35 || records.Count >= (version >= 4 ? DatasetCompactFormat.MaxRecords : BlockRecords))
                throw new InvalidDataException("Invalid index block.");
            byte[] key = new byte[length];
            previous.AsSpan(0, prefix).CopyTo(key);
            stream.ReadExactly(key.AsSpan(prefix));
            previous = key;
            var counts = ReadCounts(reader, version == 2);
            ulong edgeCount = ReadUInt(reader);
            if (edgeCount > 256)
                throw new InvalidDataException("Invalid edge count.");
            var edges = new List<(ushort, DatasetCounts)>();
            for (ulong i = 0; i < edgeCount; i++)
            {
                ushort move = reader.ReadUInt16();
                bool shared = version >= 3 && (move & 0x8000) != 0;
                edges.Add(((ushort)(move & 0x7fff), shared ? counts : ReadCounts(reader, version == 2)));
            }
            records.Add(new(key, counts, edges));
        }
        return records;
    }

    internal static void WriteCounts(BinaryWriter writer, DatasetCounts c)
    {
        long[] values = [c.White, c.Draw, c.Black, c.Unknown];
        if (values.Count(n => n != 0) <= 1 && c.Total <= long.MaxValue / 8)
        {
            int kind = Math.Max(0, Array.FindIndex(values, n => n != 0));
            WriteUInt(writer, ((ulong)c.Total << 3) | (uint)kind);
            return;
        }
        WriteUInt(writer, 4);
        WriteUInt(writer, (ulong)c.White);
        WriteUInt(writer, (ulong)c.Draw);
        WriteUInt(writer, (ulong)c.Black);
        WriteUInt(writer, (ulong)c.Unknown);
    }
    internal static DatasetCounts ReadCounts(BinaryReader reader, bool legacy)
    {
        if (!legacy)
        {
            ulong tag = ReadUInt(reader);
            if ((tag & 7) < 4)
            {
                long[] values = new long[4];
                values[(int)(tag & 7)] = checked((long)(tag >> 3));
                return new(values[0], values[1], values[2], values[3]);
            }
            if (tag != 4)
                throw new InvalidDataException("Invalid result code.");
        }
        return new(checked((long)ReadUInt(reader)), checked((long)ReadUInt(reader)), checked((long)ReadUInt(reader)), checked((long)ReadUInt(reader)));
    }
    private static void WriteUInt(BinaryWriter writer, ulong value)
    {
        while (value >= 128)
        {
            writer.Write((byte)(value | 128));
            value >>= 7;
        }
        writer.Write((byte)value);
    }
    internal static ulong ReadUInt(BinaryReader reader)
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
