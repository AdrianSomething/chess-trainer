using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChessOpeningTrainer;
public sealed class OpeningDefinition
{
    public int SchemaVersion { get; set; } = 1;
    public string Id { get; set; } = "";
    public string Name { get; set; } = "Unbenanntes Opening";
    public string Perspective { get; set; } = "white";
    public string Description { get; set; } = "";
    public string? Source { get; set; }
    public string? StartFen { get; set; }
    public OpeningNode Tree { get; set; } = new();
    public string? RootNodeId { get; set; }
    public Dictionary<string, OpeningNode>? Nodes { get; set; }
    // `lines` is an author-friendly shorthand. It is expanded into Tree while loading.
    public List<OpeningLine>? Lines { get; set; }

    [JsonIgnore]
    public bool LearnsWhite => !Perspective.Equals("black", StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public string ColorName => LearnsWhite ? "Weiß" : "Schwarz";

    [JsonIgnore]
    public string FilePath { get; set; } = "";
}
