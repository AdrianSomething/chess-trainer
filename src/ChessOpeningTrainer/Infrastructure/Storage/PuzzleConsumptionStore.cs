using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ChessOpeningTrainer;

/// <summary>Durable position fingerprints, independent of IDs and bundled seed files.</summary>
public sealed class PuzzleConsumptionStore
{
    private readonly string file;
    private readonly HashSet<string> used;
    private readonly object sync = new();
    public string? Error { get; }

    public PuzzleConsumptionStore(string file)
    {
        this.file = file;
        // A corrupt ledger must not silently turn all old puzzles into new ones.
        used = [];
        try
        {
            if (File.Exists(file)) used = JsonSerializer.Deserialize<HashSet<string>>(File.ReadAllText(file)) ?? [];
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            Error = "Rätsel-Verbrauchshistorie konnte nicht gelesen werden: " + ex.Message;
        }
    }

    public static string Key(Puzzle puzzle) => Convert.ToHexString(SHA256.HashData(
        Encoding.UTF8.GetBytes(OpeningStore.PositionKey(Position.FromFen(puzzle.Fen)))));

    public bool Contains(Puzzle puzzle)
    {
        lock (sync) return Error is not null || used.Contains(Key(puzzle));
    }

    public void Record(IEnumerable<Puzzle> puzzles)
    {
        lock (sync)
        {
            if (Error is not null) throw new IOException(Error);
            var updated = new HashSet<string>(used);
            foreach (var puzzle in puzzles) updated.Add(Key(puzzle));
            if (updated.Count == used.Count) return;
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            string temporary = file + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(updated));
            File.Move(temporary, file, true);
            used.UnionWith(updated);
        }
    }
}
