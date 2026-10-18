using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChessOpeningTrainer;
public sealed class ProgressData
{
    public Dictionary<string, string> BranchPriority { get; set; } = [];
    public bool GroupSimilarReplies { get; set; }
    public Dictionary<string, ProgressRecord> Moves { get; set; } = new();
    public long LearningTurns { get; set; }
}
