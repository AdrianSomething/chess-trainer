using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ChessOpeningTrainer;

/// <summary>Private, bounded timing samples. Never stores opening positions or personal game data.</summary>
public sealed class GeneratorTimingHistory
{
    public sealed record Sample(string Configuration, int Cards, double Seconds, DateTime CompletedUtc);
    private readonly string file;
    public GeneratorTimingHistory(string directory) => file = Path.Combine(directory, "generator-timings.json");
    public static string Configuration(GeneratorOptions options) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(options))));
    private List<Sample> Read()
    {
        try { return File.Exists(file) ? JsonSerializer.Deserialize<List<Sample>>(File.ReadAllText(file)) ?? [] : []; }
        catch (IOException) { return []; }
        catch (JsonException) { return []; }
        catch (UnauthorizedAccessException) { return []; }
    }
    public double? SecondsPerCard(string configuration)
    {
        var samples = Read().Where(s => s.Configuration == configuration && s.Cards > 0 && s.Seconds > 0).TakeLast(8).OrderBy(s => s.Seconds / s.Cards).ToArray();
        return samples.Length == 0 ? null : samples[samples.Length / 2].Seconds / samples[samples.Length / 2].Cards;
    }
    public void Record(string configuration, int cards, TimeSpan duration)
    {
        if (cards < 1 || duration <= TimeSpan.Zero) return;
        var samples = Read();
        samples.Add(new(configuration, cards, duration.TotalSeconds, DateTime.UtcNow));
        samples = samples.TakeLast(40).ToList();
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file + ".tmp", JsonSerializer.Serialize(samples));
        File.Move(file + ".tmp", file, true);
    }
}
