namespace ChessOpeningTrainer;

public sealed class DatasetSettings
{
    public int IndexWorkers { get; set; } = Math.Min(4, Environment.ProcessorCount);
    public int FilterWorkers { get; set; } = Math.Min(4, Environment.ProcessorCount);
    public int EstimateWorkers { get; set; } = 1;
    public int SampleSize { get; set; } = 1000;
}

public sealed record DatasetGame(string StartFen, string[] Moves, string Result, int WhiteElo, int BlackElo,
    int Seconds, int Increment, string Termination);

public sealed class DatasetFilter
{
    public int MinElo
    {
        get; set;
    }
    public int MaxElo { get; set; } = 3500;
    public HashSet<string> Results { get; set; } = ["1-0", "0-1", "1/2-1/2", "*"];
    public HashSet<string> Endings { get; set; } = ["Time forfeit", "Normal", "Final position", "Abandoned", "Rules infraction", "Unknown"];
    public bool IncludeUntimed { get; set; } = true;
    public int? MinSeconds
    {
        get; set;
    }
    public int? MaxSeconds
    {
        get; set;
    }
    public int? MinIncrement
    {
        get; set;
    }
    public int? MaxIncrement
    {
        get; set;
    }
    public long? Limit
    {
        get; set;
    }
    public string[] Prefix { get; set; } = [];
    public bool Matches(DatasetGame g) =>
        (MinElo == 0 || g.WhiteElo >= MinElo && g.BlackElo >= MinElo) &&
        (MaxElo >= 3500 || g.WhiteElo >= 0 && g.BlackElo >= 0 && g.WhiteElo <= MaxElo && g.BlackElo <= MaxElo) &&
        Results.Contains(g.Result) && Endings.Contains(g.Termination) &&
        (g.Seconds < 0 ? IncludeUntimed :
            (!MinSeconds.HasValue || g.Seconds >= MinSeconds) && (!MaxSeconds.HasValue || g.Seconds <= MaxSeconds) &&
            (!MinIncrement.HasValue || g.Increment >= MinIncrement) && (!MaxIncrement.HasValue || g.Increment <= MaxIncrement)) &&
        (Prefix.Length == 0 || g.StartFen == Position.InitialFen && g.Moves.Take(Prefix.Length).SequenceEqual(Prefix));
}

public readonly record struct DatasetCounts(long White, long Draw, long Black, long Unknown)
{
    public long Total => White + Draw + Black + Unknown;
    public static DatasetCounts From(string result) => result switch
    {
        "1-0" => new(1, 0, 0, 0),
        "0-1" => new(0, 0, 1, 0),
        "1/2-1/2" => new(0, 1, 0, 0),
        _ => new(0, 0, 0, 1)
    };
}

public sealed class DatasetEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Dataset";
    public bool Active { get; set; } = true;
    public long Games
    {
        get; set;
    }
    public long Skipped
    {
        get; set;
    }
    public long SourceBytes
    {
        get; set;
    }
    public string ContentHash { get; set; } = "";
    public List<DatasetGame> Sample { get; set; } = [];
}

public sealed class DatasetManifest
{
    public int StopAtGames
    {
        get; set;
    }
    public int Version { get; set; } = 1;
    public List<DatasetEntry> Entries { get; set; } = [];
    public string? IndexFile
    {
        get; set;
    }
    public string? IndexSignature
    {
        get; set;
    }
}
