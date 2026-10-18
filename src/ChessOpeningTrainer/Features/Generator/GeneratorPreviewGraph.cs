namespace ChessOpeningTrainer;

/// <summary>Display-only projection. The actual opening keeps every saved opponent counter.</summary>
public static class GeneratorPreviewGraph
{
    public static GeneratorGraphSnapshot Visible(GeneratorGraphSnapshot snapshot)
    {
        var nodes = snapshot.Nodes.Where(n => !n.IsTerminalCounter).ToArray();
        var visible = nodes.Select(n => n.Id).ToHashSet();
        return snapshot with { Nodes = nodes, Edges = snapshot.Edges.Where(e => visible.Contains(e.From) && visible.Contains(e.To)).ToArray() };
    }
    public static HashSet<string> Ends(GeneratorGraphSnapshot visible)
    {
        var outgoing = visible.Edges.Select(e => e.From).ToHashSet();
        return visible.Nodes.Where(n => n.IsLearnerMove && n.Completed && !outgoing.Contains(n.Id)).Select(n => n.Id).ToHashSet();
    }
}
