namespace ChessOpeningTrainer;

/// <summary>CPU-only graph preparation; safe to run off the UI thread. No Controls or HWNDs.</summary>
public static class BranchGraphBuilder
{
    public static List<BranchVertex> Build(OpeningDefinition opening, bool groupVariations = true, bool hideHuman = false, bool hideSubbranches = false, IReadOnlySet<string>? collapsed = null, IReadOnlySet<string>? compact = null, bool? betaLayout = null, double? columnSpacing = null, bool fastLayout = false, bool crossingPostPass = true, bool? globalRefinement = null)
    {
        var vertices = new List<BranchVertex>();
        var map = new Dictionary<string, BranchVertex>();
        var seen = new HashSet<OpeningNode>();
        var nodevertices = new Dictionary<OpeningNode, BranchVertex>();
        var q = new Queue<(OpeningNode Node, Position P, int Depth, string Path, BranchVertex? Parent, string San, string? LearningKey, bool Human)>();
        q.Enqueue((opening.Tree, Position.FromFen(opening.StartFen ?? Position.InitialFen), 0, opening.Id, null, "", null, false));
        while (q.TryDequeue(out var w))
        {
            var key = opening.SchemaVersion == 2 ? w.Node.Id! : w.Depth + "|" + OpeningStore.PositionKey(w.P);
            bool own = w.Depth > 0 && w.P.WhiteToMove != opening.LearnsWhite;
            if (groupVariations && w.Parent is not null && !own && w.Node.Moves.Count == 1)
                key = "group|" + w.Parent.Id + "|" + w.Node.Moves[0].Uci;
            if (groupVariations && w.Parent?.Alternatives.Count > 1 && own)
                key = "answer|" + w.Parent.Id + "|" + w.San.TrimEnd('+', '#');
            if (w.Parent is not null && !own && w.Node.Moves.Count == 0)
                key = "endgroup|" + w.Parent.Id;
            if (!nodevertices.TryGetValue(w.Node, out var vertex) && !map.TryGetValue(key, out vertex))
            {
                vertex = new()
                {
                    Id = vertices.Count,
                    Position = w.P.Clone(),
                    Depth = w.Depth,
                    IsLearnerMove = own
                };
                map[key] = vertex;
                vertices.Add(vertex);
            }

            nodevertices[w.Node] = vertex;
            w.Parent?.Children.Add(vertex);
            if (w.Parent is not null)
                w.Parent.EdgeLabels[vertex] = w.San;
            if (w.San != "")
                vertex.Incoming.Add(w.San);
            if (vertex.Alternatives.All(p => p.ToFen() != w.P.ToFen()))
            {
                string label = w.San == "" ? "Start" : w.San;
                if (w.Parent?.Alternatives.Count > 1)
                {
                    string uci = w.Path.Split('/')[^1];
                    for (int i = 0; i < w.Parent.Alternatives.Count; i++)
                    {
                        var before = w.Parent.Alternatives[i].Clone();
                        var move = MoveGenerator.GenerateLegal(before).FirstOrDefault(m => m.Uci == uci);
                        if (move is null)
                            continue;
                        before.Apply(move);
                        if (before.ToFen() == w.P.ToFen())
                        {
                            label = w.Parent.AlternativeNames[i] + " → " + w.San;
                            break;
                        }
                    }
                }

                vertex.Alternatives.Add(w.P.Clone());
                vertex.AlternativeNames.Add(label);
            }

            vertex.Compact |= compact?.Contains(OpeningStore.PositionKey(w.P)) == true;
            vertex.Collapsed |= hideSubbranches || collapsed?.Contains(OpeningStore.PositionKey(w.P)) == true;
            vertex.HumanUnfriendly |= w.Human;
            vertex.Nodes.Add(w.Node);
            if (w.Human)
                vertex.HumanCaptions.Add(w.San);
            if (w.LearningKey is not null)
                vertex.ProgressKeys.Add(w.LearningKey);
            if (!own && w.Parent is not null)
                vertex.InheritedKeys.UnionWith(w.Parent.ProgressKeys);
            if (!seen.Add(w.Node) || hideSubbranches && w.Node.IsSubbranch && w.Depth > 0 || collapsed?.Contains(OpeningStore.PositionKey(w.P)) == true)
                continue;
            var legalMoves = MoveGenerator.GenerateLegal(w.P).ToDictionary(m => m.Uci);
            foreach (var d in w.Node.Moves)
            {
                if (hideHuman && d.HumanUnfriendly)
                    continue;
                var p = w.P.Clone();
                var m = legalMoves[d.Uci];
                string san = SanNotation.Format(p, m);
                string? learningKey = p.WhiteToMove == opening.LearnsWhite ? (opening.SchemaVersion == 2 ? OpeningStore.NodeProgressKey(opening, w.Node, d.Uci) : w.Path + "/" + d.Uci) : null;
                p.Apply(m);
                q.Enqueue((d.Next, p, w.Depth + 1, w.Path + "/" + d.Uci, vertex, san, learningKey, d.HumanUnfriendly));
            }
        }

        var parents = vertices.ToDictionary(v => v, _ => new List<BranchVertex>());
        foreach (var parent in vertices)
            foreach (var child in parent.Children) parents[child].Add(parent);
        var degrees = parents.ToDictionary(p => p.Key, p => p.Value.Count);
        var ready = new Queue<BranchVertex>(vertices.Where(v => degrees[v] == 0));
        int visited = 0;
        while (ready.TryDequeue(out var v))
        {
            visited++;
            foreach (var child in v.Children)
                if (--degrees[child] == 0)
                    ready.Enqueue(child);
        }

        if (visited != vertices.Count && groupVariations)
        {
            return Build(opening, false, hideHuman, hideSubbranches, collapsed, compact, betaLayout, columnSpacing, fastLayout, crossingPostPass, globalRefinement);
        }

        foreach (var v in vertices.Where(v => !v.IsLearnerMove))
            foreach (var parent in parents[v])
                v.InheritedKeys.UnionWith(parent.ProgressKeys);
        var layers = new Dictionary<BranchVertex, int>();
        int Layer(BranchVertex v)
        {
            if (layers.TryGetValue(v, out int value))
                return value;
            layers[v] = 0;
            return layers[v] = parents[v].Count == 0 ? 0 : parents[v].Max(Layer) + 1;
        }

        foreach (var layer in vertices.GroupBy(Layer))
        {
            float y = 0;
            foreach (var v in layer.OrderBy(v => v.Children.Count == 0 ? 1 : 0))
            {
                v.Location = new(layer.Key * 292.5f, y + v.Height / 2);
                y += v.Height + 22;
            }
        }

        bool improved = betaLayout ?? !Preferences.Current.BranchLegacyPositioning;
        bool refine = globalRefinement ?? Preferences.Current.BranchGlobalRefinementBeta;
        string layoutKey = BranchLayoutCache.Key(vertices, improved, fastLayout, crossingPostPass) + (refine ? ":global-beta-2-aligned" : "");
        bool cachedLayout = BranchLayoutCache.Apply(layoutKey, vertices);
        if (!cachedLayout && !fastLayout && !improved)
        {
            // Retained legacy placement: keep its ordering/projection unchanged.
            for (int pass = 0; pass < 11; pass++)
                foreach (var layer in (pass % 2 == 0 ? vertices.GroupBy(Layer).OrderByDescending(g => g.Key) : vertices.GroupBy(Layer).OrderBy(g => g.Key)))
                {
                    float Desired(BranchVertex v)
                    {
                        var related = pass % 2 == 0 ? v.Children.ToList() : parents[v];
                        return related.Count == 0 ? v.Location.Y : related.Average(n => n.Location.Y);
                    }

                    float last = float.NegativeInfinity;
                    foreach (var v in layer.OrderBy(Desired).ThenBy(v => v.Children.Count == 0 ? 1 : 0))
                    {
                        float y = Math.Max(Desired(v), last + v.Height / 2 + 20);
                        v.Location = new(v.Location.X, y);
                        last = y + v.Height / 2;
                    }
                }

            // Symmetric non-overlap projection rather than packing every layer
            // against its top edge.
            for (int pass = 0; pass < 12; pass++)
                foreach (var layer in (pass % 2 == 0 ? vertices.GroupBy(Layer).OrderByDescending(g => g.Key) : vertices.GroupBy(Layer).OrderBy(g => g.Key)))
                {
                    var order = layer.OrderBy(v => v.Location.Y).ToList();
                    var wanted = order.Select(v =>
                    {
                        var family = pass % 2 == 0 ? v.Children.ToList() : parents[v];
                        return family.Count == 0 ? v.Location.Y : family.Average(n => n.Location.Y);
                    }).ToArray();
                    var ys = wanted.ToArray();
                    for (int i = 1; i < ys.Length; i++)
                        ys[i] = Math.Max(ys[i], ys[i - 1] + (order[i - 1].Height + order[i].Height) / 2 + 30);
                    float shift = ys.Average() - wanted.Average();
                    for (int i = 0; i < ys.Length; i++)
                        order[i].Location = new(order[i].Location.X, ys[i] - shift);
                }
        }
        else if (!cachedLayout && !fastLayout)
            BranchBetaLayout.Apply(vertices, layers, parents);
        if (!cachedLayout)
        {
            if (crossingPostPass && !fastLayout) BranchCrossingReduction.Apply(vertices);
            if (refine && !fastLayout) { BranchGlobalRefinement.Apply(vertices); BranchAlignmentPass.Apply(vertices); }
            BranchLayoutCache.Store(layoutKey, vertices);
        }

        double spacing = columnSpacing ?? Preferences.Current.BranchColumnSpacing;
        float factor = (float)BranchLayoutOptions.ClampSpacing(spacing);
        foreach (var v in vertices) v.Location = new(v.Location.X * factor, v.Location.Y);

        var owners = new Dictionary<BranchVertex, BranchVertex>();
        Color[] outlines = [Color.Goldenrod, Color.CornflowerBlue, Color.MediumSeaGreen, Color.Coral, Color.Turquoise];
        var ownerColors = new Dictionary<BranchVertex, Color>();
        foreach (var root in vertices.Where(v => v.Nodes.Any(n => n.IsSubbranch)).OrderBy(Layer).ThenBy(v => v.Id))
        {
            int colorIndex = (OpeningStore.PositionKey(root.Position).GetHashCode() & int.MaxValue) % outlines.Length;
            if (owners.TryGetValue(root, out var outer) && ownerColors[outer] == outlines[colorIndex]) colorIndex = (colorIndex + 1) % outlines.Length;
            string? customColor = root.Nodes.FirstOrDefault(n => n.IsSubbranch && n.SubbranchColor is not null)?.SubbranchColor;
            ownerColors[root] = string.IsNullOrWhiteSpace(customColor) ? outlines[colorIndex] : Skins.Color(customColor, outlines[colorIndex]);
            var pending = new Stack<BranchVertex>();
            var visitedBranch = new HashSet<BranchVertex>();
            pending.Push(root);
            while (pending.TryPop(out var node))
            {
                if (!visitedBranch.Add(node)) continue;
                owners[node] = root;
                foreach (var child in node.Children) pending.Push(child);
            }
        }
        foreach (var (node, root) in owners) { node.SubbranchColor = ownerColors[root]; node.SubbranchRoot = root; }

        float minY = vertices.Min(v => v.Location.Y);
        foreach (var v in vertices)
            v.Location = new(v.Location.X, v.Location.Y - minY);
        return vertices;
    }
}
