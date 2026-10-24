namespace ChessOpeningTrainer;

/// <summary>Temporary adjacency lives inside its position row: no second B-tree
/// containing a full parent key and SQLite row header for every outgoing move.</summary>
internal static class DatasetEdgePayload
{
    internal static byte[] Encode(IEnumerable<(ushort Move, DatasetCounts Counts)> edges)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        var ordered = edges.OrderBy(e => e.Move).ToArray();
        writer.Write((ushort)ordered.Length);
        foreach (var edge in ordered)
        {
            writer.Write(edge.Move);
            DatasetIndexFormat.WriteCounts(writer, edge.Counts);
        }
        return stream.ToArray();
    }
    internal static List<(ushort Move, DatasetCounts Counts)> Decode(byte[] bytes)
    {
        using var reader = new BinaryReader(new MemoryStream(bytes));
        var result = new List<(ushort, DatasetCounts)>();
        int count = reader.ReadUInt16();
        for (int i = 0; i < count; i++)
            result.Add((reader.ReadUInt16(), DatasetIndexFormat.ReadCounts(reader, false)));
        return result;
    }
    internal static byte[] Merge(byte[] first, byte[] second)
    {
        var counts = Decode(first).ToDictionary(e => e.Move, e => e.Counts);
        foreach (var edge in Decode(second))
        {
            var previous = counts.GetValueOrDefault(edge.Move);
            counts[edge.Move] = new(previous.White + edge.Counts.White, previous.Draw + edge.Counts.Draw,
                previous.Black + edge.Counts.Black, previous.Unknown + edge.Counts.Unknown);
        }
        return Encode(counts.Select(e => (e.Key, e.Value)));
    }
}
