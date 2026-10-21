using System.Text.Json;

namespace ChessOpeningTrainer;

internal static class TutorialSampleGame
{
    public static PersonalGame Load(string directory)
    {
        using var resource = typeof(TutorialSampleGame).Assembly.GetManifestResourceStream("ChessOpeningTrainer.TutorialGame.json")
            ?? throw new InvalidOperationException("Bundled tutorial analysis is missing.");
        var sample = JsonSerializer.Deserialize<PersonalGame>(resource, OpeningStore.Json)!;
        if (!sample.HasCompleteAnalysis) throw new InvalidOperationException("Bundled tutorial analysis is incomplete.");
        var store = new PersonalGameStore(directory);
        var existing = store.Games.FirstOrDefault(g => g.Id == sample.Id && g.HasCompleteAnalysis);
        if (existing is not null) return existing;
        foreach (var old in store.Games.Where(g => g.Id == sample.Id)) store.Remove(old);
        store.Add(sample); return sample;
    }
}
