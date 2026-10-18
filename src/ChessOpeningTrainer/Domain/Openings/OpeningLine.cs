using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChessOpeningTrainer;
public sealed class OpeningLine
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<string> Moves { get; set; } = [];
}
