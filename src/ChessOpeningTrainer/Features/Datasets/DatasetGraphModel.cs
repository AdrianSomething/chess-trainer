namespace ChessOpeningTrainer;

/// <summary>Small lazy projection of a disk-backed DAG. Repetition loops are displayed once.</summary>
public sealed class DatasetGraphModel
{
    public List<BranchVertex> Vertices { get; } = [];
    public Dictionary<BranchVertex, DatasetCounts> Counts { get; } = [];
    public Dictionary<BranchVertex, string[]> Paths { get; } = [];
    public Dictionary<(BranchVertex, BranchVertex), long> Frequencies { get; } = [];
    public static DatasetGraphModel Build(string indexPath, IReadOnlyDictionary<int, string> expanded, CancellationToken token)
    {
        using var index = new DatasetIndex(indexPath);
        var model = new DatasetGraphModel();
        float row = 0;
        BranchVertex Visit(Position position, string[] path, string caption, HashSet<string> ancestors, bool root = false)
        {
            token.ThrowIfCancellationRequested();
            var v = new BranchVertex { Id = model.Vertices.Count, Position = position, Depth = path.Length, IsLearnerMove = true };
            v.Incoming.Add(caption);
            model.Vertices.Add(v);
            model.Paths[v] = path;
            model.Counts[v] = index.Counts(position);
            string key = Convert.ToHexString(DatasetIndex.Key(position));
            bool repeated = ancestors.Contains(key);
            var children = repeated ? [] : index.Children(position);
            bool branch = root || children.Count > 1;
            bool open = !branch || expanded.GetValueOrDefault(path.Length) == string.Join(' ', path);
            if (children.Count > 0 && branch)
                v.Nodes.Add(new OpeningNode { IsSubbranch = true });
            v.Collapsed = !open;
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
                    var child = Visit(next, [.. path, uci], SanNotation.Format(position, move), nextAncestors);
                    v.Children.Add(child);
                    model.Frequencies[(v, child)] = count.Total;
                }
            }
            v.Location = new(path.Length * 180 + 65, v.Children.Count > 0 ? (v.Children.First().Location.Y + v.Children.Last().Location.Y) / 2 : row++ * 55 + 45);
            return v;
        }
        Visit(Position.FromFen(Position.InitialFen), [], L.T("Grundstellung"), [], true);
        return model;
    }
}
