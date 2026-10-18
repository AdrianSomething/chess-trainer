namespace ChessOpeningTrainer;
public static class BranchLearning
{
    public static List<OpeningStore.LearningTarget> Plan(OpeningDefinition opening, Position destination)
    {
        var queue = new Queue<(OpeningNode Node, Position P, List<OpeningMove> Prefix)>();
        queue.Enqueue((opening.Tree, Position.FromFen(opening.StartFen ?? Position.InitialFen), []));
        var seen = new HashSet<OpeningNode>();
        var result = new List<OpeningStore.LearningTarget>();
        var keys = new HashSet<string>();
        string Key(OpeningNode n, List<OpeningMove> prefix, OpeningMove move) => opening.SchemaVersion == 2 ? OpeningStore.NodeProgressKey(opening, n, move.Uci) : opening.Id + "/" + string.Join("/", prefix.Select(d => d.Uci).Append(move.Uci));
        void Add(OpeningNode n, Position p, List<OpeningMove> prefix, OpeningMove d)
        {
            if (p.WhiteToMove == opening.LearnsWhite)
            {
                var key = Key(n, prefix, d);
                if (keys.Add(key))
                    result.Add(new(key, [..prefix], d));
            }
        }

        while (queue.TryDequeue(out var w))
        {
            if (!seen.Add(w.Node))
                continue;
            if (OpeningStore.PositionKey(w.P) == OpeningStore.PositionKey(destination))
            {
                var p = Position.FromFen(opening.StartFen ?? Position.InitialFen);
                var node = opening.Tree;
                var prefix = new List<OpeningMove>();
                foreach (var d in w.Prefix)
                {
                    Add(node, p, prefix, d);
                    p.Apply(MoveGenerator.GenerateLegal(p).First(m => m.Uci == d.Uci));
                    node = d.Next;
                    prefix.Add(d);
                }

                var visited = new HashSet<OpeningNode>();
                void Walk(OpeningNode n, Position current, List<OpeningMove> path)
                {
                    if (!visited.Add(n))
                        return;
                    foreach (var d in n.Moves)
                    {
                        Add(n, current, path, d);
                        var after = current.Clone();
                        after.Apply(MoveGenerator.GenerateLegal(after).First(m => m.Uci == d.Uci));
                        Walk(d.Next, after, [..path, d]);
                    }
                }

                Walk(w.Node, w.P, w.Prefix);
                // Backtrack to the nearest ancestor with unvisited side branches.
                var ancestors = new List<(OpeningNode Node, Position P, List<OpeningMove> Prefix)>();
                p = Position.FromFen(opening.StartFen ?? Position.InitialFen);
                node = opening.Tree;
                prefix = [];
                foreach (var d in w.Prefix)
                {
                    ancestors.Add((node, p.Clone(), [..prefix]));
                    p.Apply(MoveGenerator.GenerateLegal(p).First(m => m.Uci == d.Uci));
                    node = d.Next;
                    prefix.Add(d);
                }

                foreach (var a in ancestors.AsEnumerable().Reverse())
                    Walk(a.Node, a.P, a.Prefix);
                return result;
            }

            foreach (var d in w.Node.Moves)
            {
                var after = w.P.Clone();
                after.Apply(MoveGenerator.GenerateLegal(after).First(m => m.Uci == d.Uci));
                queue.Enqueue((d.Next, after, [..w.Prefix, d]));
            }
        }

        return result;
    }
}
