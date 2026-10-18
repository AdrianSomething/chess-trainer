using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChessOpeningTrainer;
public sealed class OpeningMove
{
    public bool HumanUnfriendly { get; set; }
    public string Uci { get; set; } = "";
    public string? Name { get; set; }
    public string? Explanation { get; set; }
    public OpeningNode Next { get; set; } = new();
    public string? NextId { get; set; }
}
