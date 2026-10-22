using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using ZstdSharp;

namespace ChessOpeningTrainer;

/// <summary>Streaming game boundaries; comments and variations never enter the index.</summary>
public static class DatasetPgn
{
    private static readonly Regex tagPattern = new("(?m)^[ \\t]*\\[(\\w+)\\s+\"((?:\\\\.|[^\"])*)\"[ \\t]*\\][ \\t]*\\r?$", RegexOptions.Compiled);
    public static IEnumerable<string> Read(string path, CancellationToken token)
    {
        using var file = File.OpenRead(path);
        if (path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            using var zip = new ZipArchive(file, ZipArchiveMode.Read);
            foreach (var entry in zip.Entries.Where(e => e.FullName.EndsWith(".pgn", StringComparison.OrdinalIgnoreCase)))
            {
                using var stream = entry.Open();
                foreach (var text in ReadText(stream, token))
                    yield return text;
            }
        }
        else
        {
            using Stream stream = path.EndsWith(".zst", StringComparison.OrdinalIgnoreCase) ? new DecompressionStream(file) :
                path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase) ? new GZipStream(file, CompressionMode.Decompress) : file;
            foreach (var text in ReadText(stream, token))
                yield return text;
        }
    }
    private static IEnumerable<string> ReadText(Stream stream, CancellationToken token)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, true, 65536, leaveOpen: true);
        var text = new StringBuilder();
        bool moves = false;
        while (reader.ReadLine() is { } line)
        {
            token.ThrowIfCancellationRequested();
            bool tag = line.TrimStart().StartsWith('[');
            if (tag && moves)
            {
                yield return text.ToString();
                text.Clear();
                moves = false;
            }
            text.AppendLine(line);
            if (!tag && !string.IsNullOrWhiteSpace(line))
                moves = true;
            if (text.Length > 4_000_000)
                throw new InvalidDataException("PGN game exceeds the 4 MB safety limit.");
        }
        if (moves)
            yield return text.ToString();
    }
    public static DatasetGame Parse(string text)
    {
        var tags = tagPattern.Matches(text)
            .ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);
        if (tags.GetValueOrDefault("Variant", "Standard") is not ("Standard" or "Chess"))
            throw new InvalidDataException("Not standard chess.");
        string fen = tags.GetValueOrDefault("FEN", Position.InitialFen);
        var final = Position.FromFen(fen);
        if (final.Squares.Count(c => c == 'K') != 1 || final.Squares.Count(c => c == 'k') != 1)
            throw new InvalidDataException("Standard chess requires one king per side.");
        string[] moves = MainlineMoves.Parse(tagPattern.Replace(text, ""), final);
        if (moves.Length == 0)
            throw new InvalidDataException("Empty game.");
        int Elo(string key) => int.TryParse(tags.GetValueOrDefault(key), out int value) ? value : -1;
        var clock = tags.GetValueOrDefault("TimeControl", "-").Split('+');
        int seconds = clock.Length == 2 && int.TryParse(clock[0], out int s) ? s : -1;
        int increment = clock.Length == 2 && int.TryParse(clock[1], out int i) ? i : -1;
        string ending = tags.GetValueOrDefault("Termination", "Unknown");
        if (ending is not ("Time forfeit" or "Normal" or "Abandoned" or "Rules infraction"))
            ending = "Unknown";
        if (ending == "Normal" && MoveGenerator.GameState(final, new Dictionary<string, int>()) is not ("" or "Schach"))
            ending = "Final position";
        string result = tags.GetValueOrDefault("Result", "*");
        if (result is not ("1-0" or "0-1" or "1/2-1/2"))
            result = "*";
        return new(fen, moves, result, Elo("WhiteElo"), Elo("BlackElo"), seconds, increment, ending);
    }
}
