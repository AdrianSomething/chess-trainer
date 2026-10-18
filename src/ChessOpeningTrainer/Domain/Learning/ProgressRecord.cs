using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChessOpeningTrainer;
public sealed class ProgressRecord
{
    public int CountedStreak { get; set; }
    public bool WasSeen { get; set; }
    public DateTime LastAttemptUtc { get; set; }
    public DateTime LastCountedSuccessUtc { get; set; }
    public long LastCountedSuccessTurn { get; set; }
    public DateTime LearnedUtc { get; set; }
    public long LearnedAtTurn { get; set; }
}
