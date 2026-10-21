namespace ChessOpeningTrainer;

/// <summary>Copies a selected position's descendants and one shortest approach path.
/// No source nodes or learning identities are reused by the new repertoire.</summary>
public static class BranchExtraction
{
    public static OpeningDefinition Extract(OpeningDefinition source, Position selected, string name)
    {
        var positions = GraphOperations.Positions(source);
        var selectedKey = OpeningStore.PositionKey(selected);
        var target = positions.FirstOrDefault(w => OpeningStore.PositionKey(w.Position) == selectedKey);
        if (target.Node is null) throw new ArgumentException("Selected position is not in this opening.");
        int serial = 0;
        var copies = new Dictionary<OpeningNode, OpeningNode>();
        OpeningNode Copy(OpeningNode node)
        {
            if (copies.TryGetValue(node, out var existing)) return existing;
            var clone = new OpeningNode { Id = "n" + serial++, Fen = node.Fen, Comment = node.Comment,
                IsSubbranch = node.IsSubbranch, SubbranchColor = node.SubbranchColor };
            copies[node] = clone;
            foreach (var move in node.Moves) clone.Moves.Add(CopyMove(move, Copy(move.Next)));
            return clone;
        }
        var root = Copy(target.Node);
        var prefixes = new List<(OpeningNode Node, OpeningMove Move)>();
        var parent = source.Tree;
        foreach (string uci in target.Path)
        {
            var move = parent.Moves.Single(m => m.Uci == uci);
            prefixes.Add((parent, move)); parent = move.Next;
        }
        for (int i = prefixes.Count - 1; i >= 0; i--)
        {
            var (node, move) = prefixes[i];
            root = new OpeningNode { Id = "n" + serial++, Fen = node.Fen, Comment = node.Comment,
                Moves = [CopyMove(move, root)] };
        }
        var result = new OpeningDefinition { SchemaVersion = 2, Id = "extracted_" + Guid.NewGuid().ToString("N"),
            Name = name, Perspective = source.Perspective, StartFen = source.StartFen,
            Source = source.Source, Description = source.Description, Tree = root, RootNodeId = root.Id };
        result.Nodes = GraphOperations.Positions(result).ToDictionary(w => w.Node.Id!, w => w.Node);
        OpeningStore.Validate(result);
        return result;
    }
    private static OpeningMove CopyMove(OpeningMove move, OpeningNode next) => new()
    { Uci = move.Uci, Name = move.Name, Explanation = move.Explanation, HumanUnfriendly = move.HumanUnfriendly,
        Next = next, NextId = next.Id };
}
