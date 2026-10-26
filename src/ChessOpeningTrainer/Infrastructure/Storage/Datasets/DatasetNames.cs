namespace ChessOpeningTrainer;

public static class DatasetNames
{
    public static string FromFile(string path)
    {
        string name = Path.GetFileName(path);
        foreach (string suffix in new[] { ".zst", ".gz", ".zip", ".pgn" })
            if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                name = name[..^suffix.Length];
        return name;
    }
}
