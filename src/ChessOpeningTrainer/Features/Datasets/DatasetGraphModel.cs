namespace ChessOpeningTrainer;

/// <summary>Small lazy projection of a disk-backed DAG. Repetition loops are displayed once.</summary>
public sealed class DatasetGraphModel
{
    public List<BranchVertex> Vertices { get; } = [];
    public Dictionary<BranchVertex, DatasetCounts> Counts { get; } = [];
    public Dictionary<BranchVertex, string[]> Paths { get; } = [];
    public Dictionary<(BranchVertex, BranchVertex), long> Frequencies { get; } = [];
    public Dictionary<BranchVertex, (int Depth, string Path)> Expansion { get; } = [];
    public static DatasetGraphModel Build(string indexPath, IReadOnlyDictionary<int, string> expanded, CancellationToken token, bool compact = false, float spacing = 1)
    {
        using var index = new DatasetIndex(indexPath);
        var model = new DatasetGraphModel();
        float row = 0;
        var leadsToFork = new HashSet<string>();
        // A compact node represents an entire terminal forced line, not the
        // first five moves of a line that branches farther down the graph.
        List<(Position Position, string Uci, string San)>? TerminalTail(Position start, HashSet<string> ancestors)
        {
            var tail = new List<(Position, string, string)>();
            var visited = new HashSet<string>(ancestors);
            var ownKeys = new List<string>();
            var p = start;
            while (true)
            {
                token.ThrowIfCancellationRequested();
                string key = Convert.ToHexString(DatasetIndex.Key(p));
                if (!visited.Add(key)) return null;
                ownKeys.Add(key);
                var children = index.Children(p);
                if (children.Count == 0) return tail;
                if (children.Count != 1 || leadsToFork.Contains(key))
                {
                    leadsToFork.UnionWith(ownKeys);
                    return null;
                }
                var move = MoveGenerator.GenerateLegal(p).FirstOrDefault(m => m.Uci == children[0].Move);
                if (move is null) return null;
                var next = p.Clone(); next.Apply(move);
                tail.Add((next, move.Uci, SanNotation.Format(p, move)));
                p = next;
            }
        }
        BranchVertex Visit(Position position, string[] path, string caption, HashSet<string> ancestors, bool root = false, bool expandForced = false)
        {
            token.ThrowIfCancellationRequested();
            int startDepth = path.Length;
            string startPath = string.Join(' ', path);
            bool grouped = false;
            bool forcedAnchor = compact && !root && !expandForced && expanded.GetValueOrDefault(-startDepth - 1) == startPath;
            expandForced |= forcedAnchor;
            if (compact && !root && !expandForced && !ancestors.Contains(Convert.ToHexString(DatasetIndex.Key(position))))
            {
                var tail = TerminalTail(position, ancestors);
                if (tail is { Count: > 0 })
                {
                    caption += " " + string.Join(' ', tail.Take(4).Select(t => t.San)) + (tail.Count > 4 ? " …" : "");
                    path = [.. path, .. tail.Select(t => t.Uci)];
                    position = tail[^1].Position;
                    grouped = true;
                }
            }
            var v = new BranchVertex { Id = model.Vertices.Count, Position = position, Depth = path.Length, IsLearnerMove = true };
            if (grouped)
                v.DisplayWidth = Math.Clamp(caption.Length * 7 + 24, 140, 300);
            v.Incoming.Add(caption);
            model.Vertices.Add(v);
            model.Paths[v] = path;
            model.Counts[v] = index.Counts(position);
            string key = Convert.ToHexString(DatasetIndex.Key(position));
            bool repeated = ancestors.Contains(key);
            var children = repeated ? [] : index.Children(position);
            bool branch = root || children.Count > 1;
            bool open = !branch || expanded.GetValueOrDefault(path.Length) == string.Join(' ', path);
            if (children.Count > 0 && branch || grouped || forcedAnchor)
                v.Nodes.Add(new OpeningNode { IsSubbranch = true });
            v.Collapsed = grouped || !open;
            model.Expansion[v] = grouped || forcedAnchor ? (-startDepth - 1, startPath) : (path.Length, string.Join(' ', path));
            if (open && children.Count > 0)
            {
                var nextAncestors = new HashSet<string>(ancestors) { key };
                var legal = MoveGenerator.GenerateLegal(position);
                foreach (var (uci, count) in children)
                {
                    var move = legal.FirstOrDefault(m => m.Uci == uci);
                    if (move is null)
                        continue;
                    var next = position.Clone();
                    next.Apply(move);
                    var child = Visit(next, [.. path, uci], SanNotation.Format(position, move), nextAncestors, expandForced: expandForced && children.Count == 1);
                    v.Children.Add(child);
                    model.Frequencies[(v, child)] = count.Total;
                }
            }
            v.Location = new(path.Length * 180 + 65, v.Children.Count > 0 ? (v.Children.First().Location.Y + v.Children.Last().Location.Y) / 2 : row++ * 55 + 45);
            return v;
        }
        Visit(Position.FromFen(Position.InitialFen), [], L.T("Grundstellung"), [], true);
        void Place(BranchVertex vertex, float x)
        {
            vertex.Location = new(x, vertex.Location.Y);
            foreach (var child in vertex.Children)
                Place(child, x + vertex.Width / 2 + child.Width / 2 + 70 * spacing);
        }
        Place(model.Vertices[0], 70);
        return model;
    }
}
