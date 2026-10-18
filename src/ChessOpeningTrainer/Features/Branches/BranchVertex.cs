using System.Drawing.Drawing2D;

namespace ChessOpeningTrainer;
public sealed class BranchVertex
{
    public int Id { get; init; }
    public bool IsLearnerMove { get; init; }
    public bool HumanUnfriendly { get; set; }
    public HashSet<string> HumanCaptions { get; } = [];
    public HashSet<OpeningNode> Nodes { get; } = [];
    public HashSet<string> InheritedKeys { get; } = [];
    public required Position Position { get; init; }
    public PointF Location { get; set; }
    public int Depth { get; init; }
    public HashSet<string> ProgressKeys { get; } = [];
    public HashSet<BranchVertex> Children { get; } = [];
    public Dictionary<BranchVertex, string> EdgeLabels { get; } = [];
    public HashSet<string> Incoming { get; } = [];
    public List<Position> Alternatives { get; } = [];
    public List<string> AlternativeNames { get; } = [];
    public string Caption => Incoming.Count == 0 ? "Start" : string.Join("\n", Incoming.Chunk(2).Select(group => string.Join(" / ", group)));
    public bool Compact { get; set; }
    public bool Collapsed { get; set; }
    public Color? SubbranchColor { get; set; }
    public BranchVertex? SubbranchRoot { get; set; }
    public bool CanCompact => Incoming.Count > 2;
    public RectangleF VariantHandle => new(Location.X - Width / 2, Location.Y + Height / 2, Width, 9);
    public RectangleF SubbranchHandle => new(Location.X + Width / 2, Location.Y - Height / 2, 10, Height);
    public GraphicsPath Outline(float margin = 0)
    {
        float left = Location.X - Width / 2 - margin, top = Location.Y - Height / 2 - margin;
        float right = Location.X + Width / 2 + margin, bottom = Location.Y + Height / 2 + margin;
        float extraRight = Nodes.Any(n => n.IsSubbranch) ? 10 : 0, extraBottom = CanCompact ? 9 : 0;
        var path = new GraphicsPath();
        path.AddPolygon(new PointF[] { new(left, top), new(right + extraRight, top), new(right + extraRight, bottom),
            new(right, bottom), new(right, bottom + extraBottom), new(left, bottom + extraBottom) });
        return path;
    }
    public float Height => Compact && CanCompact ? 26 : Math.Max(26, ((Incoming.Count + 1) / 2) * 14 + 4);
    public float Width => Math.Clamp(Caption.Split('\n').Max(l => l.Length) * 6 + 16, 48, 120);
}
