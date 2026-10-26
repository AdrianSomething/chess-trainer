using System.IO.MemoryMappedFiles;

namespace ChessOpeningTrainer;

/// <summary>Exact graph reachability with one disk-backed bit per position and
/// sequential frontier files. Cycles terminate and transpositions share a bit.</summary>
internal static class DatasetStreamingPrune
{
    internal static void Write(string full, string output, string workspace, IEnumerable<string> roots, int cutoff,
        DatasetOperation operation, CancellationToken token, Action report)
    {
        string bitsPath = Path.Combine(workspace, "reachable.bits");
        string[] frontier = [Path.Combine(workspace, "frontier-a.bin"), Path.Combine(workspace, "frontier-b.bin")];
        try
        {
            using var index = new DatasetIndex(full);
            using var bitsFile = new FileStream(bitsPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
            long bytes = Math.Max(1, (index.PositionCount + 7) / 8); bitsFile.SetLength(bytes);
            using var mapped = MemoryMappedFile.CreateFromFile(bitsFile, null, bytes, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true);
            using var bits = mapped.CreateViewAccessor(0, bytes, MemoryMappedFileAccess.ReadWrite);
            bool Mark(long id)
            {
                long offset = id >> 3; byte mask = (byte)(1 << (int)(id & 7)), value = bits.ReadByte(offset);
                if ((value & mask) != 0) return false;
                bits.Write(offset, (byte)(value | mask)); return true;
            }
            using (var writer = new BinaryWriter(File.Create(frontier[0])))
                foreach (string root in roots)
                    if (index.FindKey(DatasetPositionKey.Expand(Convert.FromHexString(root)), out long id) is not null && Mark(id)) writer.Write(id);
            int side = 0; long reportAt = 0;
            void Check()
            {
                operation.Wait(token);
                if (Environment.TickCount64 - reportAt > 750) { reportAt = Environment.TickCount64; report(); }
            }
            while (new FileInfo(frontier[side]).Length > 0)
            {
                using (var input = new BinaryReader(File.OpenRead(frontier[side])))
                using (var next = new BinaryWriter(File.Create(frontier[1 - side])))
                    while (input.BaseStream.Position < input.BaseStream.Length)
                    {
                        Check();
                        var record = index.At(input.ReadInt64());
                        if (record.Counts.Total <= cutoff) continue;
                        var position = DatasetPositionKey.FromExpanded(record.Key);
                        var legal = MoveGenerator.GenerateLegal(position).ToDictionary(m => m.Uci);
                        foreach (var edge in record.Edges)
                        {
                            var child = position.Clone(); child.Apply(legal[PackedMove.Decode(edge.Move)]);
                            if (index.FindKey(DatasetIndex.Key(child), out long id) is not null && Mark(id)) next.Write(id);
                        }
                    }
                report(); File.Delete(frontier[side]); side = 1 - side;
            }
            IEnumerable<DatasetIndexRecord> Kept()
            {
                long id = 0;
                foreach (var record in index.Records(token))
                {
                    Check();
                    bool keep = (bits.ReadByte(id >> 3) & (1 << (int)(id & 7))) != 0; id++;
                    if (keep) yield return record.Counts.Total <= cutoff ? record with { Edges = [] } : record;
                }
            }
            DatasetCompactFormat.Write(output, Kept(), cutoff, token); report();
        }
        finally { File.Delete(bitsPath); foreach (string p in frontier) File.Delete(p); }
    }
}
