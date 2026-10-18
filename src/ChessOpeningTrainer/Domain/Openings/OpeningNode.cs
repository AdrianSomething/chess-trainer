using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChessOpeningTrainer;
public sealed class OpeningNode
{
    public bool IsSubbranch { get; set; }
    public string? SubbranchColor { get; set; }
    public string? Id { get; set; }
    public string? Fen { get; set; }
    public string? Comment { get; set; }
    public List<OpeningMove> Moves { get; set; } = [];
}
