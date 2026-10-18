using System.Text.Json;

namespace ChessOpeningTrainer;
public sealed class SkinDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Light { get; set; } = "#F0D9B5";
    public string Dark { get; set; } = "#B58863";
    public string WhitePiece { get; set; } = "#FAF9F2";
    public string BlackPiece { get; set; } = "#212327";
    public string WhiteOutline { get; set; } = "#41413E";
    public string BlackOutline { get; set; } = "#CFCAB9";
    public string PieceStyle { get; set; } = "silhouette";
    public Dictionary<string, string> Pieces { get; set; } = [];

    [System.Text.Json.Serialization.JsonIgnore]
    public string Directory { get; set; } = "";

    public override string ToString() => Name;
}
