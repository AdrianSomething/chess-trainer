using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChessOpeningTrainer;
public sealed class ProgressStore
{
    private readonly string _path;
    public string DataDirectory => Path.GetDirectoryName(Path.GetFullPath(_path))!;
    public ProgressData Data { get; private set; } = new();

    public ProgressStore(string dataFolder)
    {
        Directory.CreateDirectory(dataFolder);
        _path = Path.Combine(dataFolder, "progress.json");
        try
        {
            if (File.Exists(_path))
                Data = JsonSerializer.Deserialize<ProgressData>(File.ReadAllText(_path), OpeningStore.Json) ?? new();
        }
        catch
        {
            Data = new();
        }
    }

    public void Save() => File.WriteAllText(_path, JsonSerializer.Serialize(Data, OpeningStore.Json));
    public void ResetOpening(OpeningDefinition opening)
    {
        foreach (var key in OpeningStore.LearnerMoves(opening).Select(x => x.Key).ToList())
            Data.Moves.Remove(key);
        Save();
    }

    public ProgressRecord Get(string key)
    {
        if (!Data.Moves.TryGetValue(key, out var record))
            Data.Moves[key] = record = new();
        return record;
    }

    public void MarkWrong(string key)
    {
        var r = Get(key);
        r.WasSeen = true;
        r.CountedStreak = 0;
        r.LastAttemptUtc = DateTime.UtcNow;
        Save();
    }

    public bool Eligible(string key)
    {
        if (Data.BranchPriority.GetValueOrDefault(key) == "Ausgeschlossen")
            return false;
        var r = Get(key);
        if (Status(key) == "Gelernt")
            return false;
        if (Status(key) == "Zu Wiederholen")
            return true;
        return r.LastCountedSuccessTurn == 0 || Data.LearningTurns + 1 - r.LastCountedSuccessTurn >= 3 || DateTime.UtcNow - r.LastCountedSuccessUtc >= TimeSpan.FromHours(12);
    }

    public bool MarkCorrect(string key)
    {
        var r = Get(key);
        var due = Status(key) == "Zu Wiederholen";
        r.WasSeen = true;
        var now = DateTime.UtcNow;
        var turn = ++Data.LearningTurns;
        var sufficientlySpaced = due || r.LastCountedSuccessTurn == 0 || turn - r.LastCountedSuccessTurn >= 3 || now - r.LastCountedSuccessUtc >= TimeSpan.FromHours(12);
        if (sufficientlySpaced)
        {
            r.CountedStreak++;
            r.LastCountedSuccessUtc = now;
            r.LastCountedSuccessTurn = turn;
            if (r.CountedStreak >= 3)
            {
                r.LearnedUtc = now;
                r.LearnedAtTurn = turn;
            }
        }

        r.LastAttemptUtc = now;
        Save();
        return sufficientlySpaced;
    }

    public void MarkGroupCorrect(IEnumerable<string> keys)
    {
        long before = Data.LearningTurns;
        foreach (var key in keys)
        {
            Data.LearningTurns = before;
            MarkCorrect(key);
        }

        Save();
    }

    public string Status(string key)
    {
        var r = Get(key);
        if (!r.WasSeen)
            return "Unbekannt";
        if (r.CountedStreak >= 3)
        {
            var dueByTime = DateTime.UtcNow - r.LearnedUtc >= TimeSpan.FromDays(7);
            var dueByTurns = Data.LearningTurns - r.LearnedAtTurn >= 20;
            return dueByTime || dueByTurns ? "Zu Wiederholen" : "Gelernt";
        }

        return "Angeschaut";
    }

    public Dictionary<string, int> Counts(OpeningDefinition opening) => OpeningStore.LearnerMoves(opening).GroupBy(x => Status(x.Key)).ToDictionary(x => x.Key, x => x.Count());
}
