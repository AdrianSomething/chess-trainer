namespace ChessOpeningTrainer;

public sealed record BranchEditResult(OpeningDefinition Opening, Position SelectedPosition, int AddedNodes, bool ExistingPosition, bool CycleSkipped, int ConflictingReplies);
public sealed class BranchReplyConflictException(int count) : InvalidOperationException("Unterschiedliche eigene Antworten führen zur selben Stellung.")
{
    public int Count { get; } = count;
}

/// <summary>Pure, copy-on-write graph edits. Saving and confirmation belong to the branch UI.</summary>
public static class BranchGraphEdit
{
    public static BranchEditResult Insert(OpeningDefinition source, Position start, IEnumerable<IReadOnlyList<string>> lines, bool replaceOwnReply = false, bool acceptMergedReplyConflict = false)
    {
        var (copy, conflicts) = CanonicalCopy(source);
        if (conflicts > 0 && !acceptMergedReplyConflict) throw new BranchReplyConflictException(conflicts);
        var positions = GraphOperations.Positions(copy);
        var map = positions.ToDictionary(w => OpeningStore.PositionKey(w.Position), w => w.Node);
        string startKey = OpeningStore.PositionKey(start);
        if (!map.TryGetValue(startKey, out var root)) throw new InvalidOperationException("Die Ausgangsstellung gehört nicht zum Opening.");
        int added = 0; bool existing = false, cycle = false;
        var selected = start.Clone();
        foreach (var line in lines)
        {
            var p = start.Clone(); var node = root;
            foreach (string uci in line)
            {
                var move = MoveGenerator.GenerateLegal(p).FirstOrDefault(m => m.Uci == uci)
                    ?? throw new InvalidOperationException("Illegaler Bearbeitungszug: " + uci);
                bool replace = p.WhiteToMove == copy.LearnsWhite && node.Moves.Any(m => m.Uci != uci);
                if (replace)
                {
                    if (!replaceOwnReply) throw new InvalidOperationException("In dieser Stellung existiert bereits eine andere eigene Antwort.");
                }
                p.Apply(move); selected = p.Clone();
                string key = OpeningStore.PositionKey(p);
                existing = map.TryGetValue(key, out var next);
                if (!existing)
                {
                    next = new OpeningNode { Id = "edit_" + Guid.NewGuid().ToString("N"), Fen = p.ToFen() };
                    map[key] = next; added++;
                }
                var edge = node.Moves.FirstOrDefault(m => m.Uci == uci);
                if (edge is null && Reaches(next!, node)) { cycle = true; break; }
                if (replace) node.Moves.Clear();
                if (edge is null)
                {
                    // A repeated position is selected but cannot create a cycle
                    // in the persisted acyclic opening format.
                    node.Moves.Add(new OpeningMove { Uci = uci, Next = next!, NextId = next!.Id });
                }
                node = next!;
            }
        }
        RefreshNodes(copy); OpeningStore.Validate(copy);
        return new(copy, selected, added, existing, cycle, conflicts);
    }

    public static OpeningDefinition DeletePosition(OpeningDefinition source, Position target, bool acceptMergedReplyConflict = false)
        => DeletePositions(source, [target], acceptMergedReplyConflict);

    public static OpeningDefinition DeletePositions(OpeningDefinition source, IEnumerable<Position> targets, bool acceptMergedReplyConflict = false)
    {
        var (copy, conflicts) = CanonicalCopy(source);
        if (conflicts > 0 && !acceptMergedReplyConflict) throw new BranchReplyConflictException(conflicts);
        var keys = targets.Select(OpeningStore.PositionKey).ToHashSet();
        if (keys.Contains(OpeningStore.PositionKey(Position.FromFen(copy.StartFen ?? Position.InitialFen))))
            throw new InvalidOperationException("Die Anfangsstellung kann nicht gelöscht werden.");
        foreach (var w in GraphOperations.Positions(copy))
            w.Node.Moves.RemoveAll(m => keys.Contains(OpeningStore.PositionKey(Position.FromFen(m.Next.Fen!))));
        RefreshNodes(copy); OpeningStore.Validate(copy); return copy;
    }

    public static OpeningDefinition DeleteTransition(OpeningDefinition source, Position before, string uci, bool acceptMergedReplyConflict = false)
    {
        var (copy, conflicts) = CanonicalCopy(source);
        if (conflicts > 0 && !acceptMergedReplyConflict) throw new BranchReplyConflictException(conflicts);
        var node = GraphOperations.Positions(copy).FirstOrDefault(w => OpeningStore.PositionKey(w.Position) == OpeningStore.PositionKey(before)).Node;
        if (node is null || node.Moves.RemoveAll(m => m.Uci == uci) == 0)
            throw new InvalidOperationException("Der ausgewählte Zugweg wurde nicht gefunden.");
        RefreshNodes(copy); OpeningStore.Validate(copy); return copy;
    }

    public static void Apply(OpeningDefinition target, OpeningDefinition edited)
    {
        target.SchemaVersion = edited.SchemaVersion; target.Tree = edited.Tree;
        target.RootNodeId = edited.RootNodeId; target.Nodes = edited.Nodes; target.Lines = null;
    }

    public static IReadOnlyDictionary<string, string> ProgressMapping(OpeningDefinition old, OpeningDefinition edited)
    {
        string Signature(OpeningDefinition o, OpeningStore.LearningTarget t)
        {
            var p = Position.FromFen(o.StartFen ?? Position.InitialFen);
            foreach (var edge in t.Prefix) p.Apply(MoveGenerator.GenerateLegal(p).First(m => m.Uci == edge.Uci));
            return OpeningStore.PositionKey(p) + "|" + t.Expected.Uci;
        }
        var next = OpeningStore.LearningTargets(edited).GroupBy(t => Signature(edited, t)).ToDictionary(g => g.Key, g => g.First().Key);
        return OpeningStore.LearningTargets(old).Where(t => next.ContainsKey(Signature(old, t)))
            .ToDictionary(t => t.Key, t => next[Signature(old, t)]);
    }

    private static (OpeningDefinition Copy, int Conflicts) CanonicalCopy(OpeningDefinition source)
    {
        var all = GraphOperations.Positions(source);
        var nodes = new Dictionary<string, OpeningNode>(); var ids = new HashSet<string>();
        foreach (var w in all)
        {
            string key = OpeningStore.PositionKey(w.Position);
            if (nodes.ContainsKey(key)) continue;
            string id = w.Node.Id ?? "edit_" + Guid.NewGuid().ToString("N");
            if (!ids.Add(id)) { id = "edit_" + Guid.NewGuid().ToString("N"); ids.Add(id); }
            nodes[key] = new OpeningNode { Id = id, Fen = w.Position.ToFen(), Comment = w.Node.Comment,
                IsSubbranch = w.Node.IsSubbranch, SubbranchColor = w.Node.SubbranchColor };
        }
        int conflicts = 0;
        foreach (var w in all)
        {
            var node = nodes[OpeningStore.PositionKey(w.Position)];
            foreach (var edge in w.Node.Moves)
            {
                if (node.Moves.Any(m => m.Uci == edge.Uci)) continue;
                if (w.Position.WhiteToMove == source.LearnsWhite && node.Moves.Count > 0) { conflicts++; continue; }
                var p = w.Position.Clone(); p.Apply(MoveGenerator.GenerateLegal(p).First(m => m.Uci == edge.Uci));
                var next = nodes[OpeningStore.PositionKey(p)];
                if (Reaches(next, node)) continue;
                node.Moves.Add(new OpeningMove { Uci = edge.Uci, Next = next, NextId = next.Id,
                    Name = edge.Name, Explanation = edge.Explanation, HumanUnfriendly = edge.HumanUnfriendly });
            }
        }
        var copy = new OpeningDefinition { SchemaVersion = 2, Id = source.Id, Name = source.Name, Perspective = source.Perspective,
            StartFen = source.StartFen, Description = source.Description, Source = source.Source, FilePath = source.FilePath,
            Tree = nodes[OpeningStore.PositionKey(Position.FromFen(source.StartFen ?? Position.InitialFen))] };
        RefreshNodes(copy); return (copy, conflicts);
    }

    private static void RefreshNodes(OpeningDefinition o)
    {
        o.RootNodeId = o.Tree.Id;
        o.Nodes = GraphOperations.Positions(o).ToDictionary(w => w.Node.Id!, w => w.Node);
    }

    private static bool Reaches(OpeningNode start, OpeningNode target)
    {
        var seen = new HashSet<OpeningNode>(); var pending = new Stack<OpeningNode>(); pending.Push(start);
        while (pending.TryPop(out var node))
        {
            if (node == target) return true;
            if (seen.Add(node)) foreach (var edge in node.Moves) pending.Push(edge.Next);
        }
        return false;
    }
}
