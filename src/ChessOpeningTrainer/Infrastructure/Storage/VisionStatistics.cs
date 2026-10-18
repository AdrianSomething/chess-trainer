using System.Text.Json;
namespace ChessOpeningTrainer;

public sealed class VisionStatistics
{
    public sealed class Result { public int Errors { get; set; } public double Rate { get; set; } }
    public sealed class Run
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public DateTime CompletedUtc { get; set; } = DateTime.UtcNow;
        public int Mode { get; set; }
        public int Errors { get; set; }
        public double Rate { get; set; }
        public Dictionary<string, int> Missed { get; set; } = [];
    }
    public sealed class Data
    {
        public Dictionary<int, Result> Records { get; set; } = [];
        public Dictionary<string, int> Missed { get; set; } = [];
        public List<Run> Runs { get; set; } = [];
    }
    private readonly string file;
    private Dictionary<string, int> savedMisses;
    private readonly Dictionary<string, int> runMisses = [];
    public void BeginRun() => runMisses.Clear();
    public Data Values { get; }
    public VisionStatistics(string directory)
    {
        file = Path.Combine(directory, "vision-statistics.json");
        Values = File.Exists(file) ? JsonSerializer.Deserialize<Data>(File.ReadAllText(file)) ?? new() : new();
        savedMisses = new(Values.Missed);
    }
    public void Miss(int mode, char piece) => Miss(mode, piece, "Übersehen");
    public void Miss(int mode, char piece, string pattern)
    {
        string key = mode + ":" + char.ToUpperInvariant(piece) + " · " + pattern;
        Values.Missed[key] = Values.Missed.GetValueOrDefault(key) + 1;
        runMisses[key] = runMisses.GetValueOrDefault(key) + 1;
    }
    public void Record(int mode, int errors, double rate)
    {
        Values.Runs.Add(new() { Mode = mode, Errors = errors, Rate = rate, Missed = new(runMisses) });
        runMisses.Clear();
        if (!Values.Records.TryGetValue(mode, out var old) || errors < old.Errors || errors == old.Errors && rate > old.Rate)
            Values.Records[mode] = new() { Errors = errors, Rate = rate };
        Save();
    }
    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        // Several training windows can coexist. Merge their independent mode
        // records and only this instance's newly counted mistakes before saving.
        var disk = File.Exists(file) ? JsonSerializer.Deserialize<Data>(File.ReadAllText(file)) ?? new() : new();
        foreach (var (mode, candidate) in Values.Records)
            if (!disk.Records.TryGetValue(mode, out var previous) || candidate.Errors < previous.Errors || candidate.Errors == previous.Errors && candidate.Rate > previous.Rate)
                disk.Records[mode] = candidate;
        foreach (var (key, count) in Values.Missed)
            disk.Missed[key] = disk.Missed.GetValueOrDefault(key) + Math.Max(0, count - savedMisses.GetValueOrDefault(key));
        Values.Records = disk.Records;
        Values.Missed = disk.Missed;
        Values.Runs = disk.Runs.Concat(Values.Runs).DistinctBy(r => r.Id).OrderBy(r => r.CompletedUtc).ToList();
        File.WriteAllText(file + ".tmp", JsonSerializer.Serialize(Values));
        File.Move(file + ".tmp", file, true);
        savedMisses = new(Values.Missed);
    }
}
