using System.Text.Json;
namespace ChessOpeningTrainer;

public sealed class VisionScores
{
    private readonly string file;
    private Dictionary<int, double> scores = [];
    public VisionScores(string directory)
    {
        file = Path.Combine(directory, "vision-scores.json");
        if (File.Exists(file))
            scores = JsonSerializer.Deserialize<Dictionary<int, double>>(File.ReadAllText(file)) ?? [];
    }
    public double Best(int mode) => scores.GetValueOrDefault(mode);
    public void Record(int mode, double score)
    {
        scores[mode] = Math.Max(Best(mode), score);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file + ".tmp", JsonSerializer.Serialize(scores));
        File.Move(file + ".tmp", file, true);
    }
}
