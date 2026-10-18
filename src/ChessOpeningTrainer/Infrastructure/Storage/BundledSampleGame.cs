namespace ChessOpeningTrainer;

/// <summary>Seeds an explicitly synthetic game once, only for a new public-release profile.</summary>
internal static class BundledSampleGame
{
    public static void Initialize(string directory)
    {
        // An empty saved library is intentional: deleting the sample must persist.
        if (File.Exists(Path.Combine(directory, "personal-games.json"))) return;
        string sample = Path.Combine(AppContext.BaseDirectory, "Samples", "demo-game.pgn");
        if (!File.Exists(sample)) return;
        new PersonalGameStore(directory).Import(File.ReadAllText(sample));
    }
}
