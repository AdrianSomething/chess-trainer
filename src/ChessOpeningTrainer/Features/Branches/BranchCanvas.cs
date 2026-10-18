using System.Drawing.Drawing2D;

namespace ChessOpeningTrainer;
public sealed class BranchCanvas : Control
{
    private readonly BranchCanvasCache renderCache = new();
    public List<BranchVertex> Vertices { get; } = [];
    public IReadOnlyDictionary<string, int> AnalysisExits { get; set; } = new Dictionary<string, int>();
    public IReadOnlyDictionary<string, IReadOnlySet<string>> AnalysisCaptions { get; set; } = new Dictionary<string, IReadOnlySet<string>>();
    public bool AnalysisMode { get; set; }
    internal IReadOnlySet<string> PositionKeys(BranchVertex vertex) => renderCache.Keys(vertex).Normalized;
    public bool HideSubbranchColors { get; set; }
    public Color AnalysisMarkerColor { get; set; } = Color.FromArgb(230, 72, 72);

    public event Action<Position>? Selected;
    public event Action<BranchVertex>? VertexSelected;
    public event Action? SelectionCleared;
    private bool explicitSelectionCleared;
    public event Action<BranchVertex, Point>? NodeMenuRequested;
    public event Action<BranchVertex>? ToggleVariants;
    public event Action<BranchVertex>? ToggleSubbranch;
    private string? selectedKey;
    public void MarkSelected(BranchVertex vertex)
    {
        selectedKey = OpeningStore.PositionKey(vertex.Position);
        explicitSelectionCleared = false;
        Invalidate();
    }
    private readonly ProgressStore progress;
    private readonly string? highlight;
    private PointF pan = new(35, 35);
    private float zoom = 1;
    public (float Zoom, PointF Pan) Camera
    {
        get => (zoom, pan);
        set
        {
            zoom = value.Zoom;
            pan = value.Pan;
            Invalidate();
        }
    }

    public bool HasVisibleNodes => Vertices.Any(v => ClientRectangle.IntersectsWith(Rectangle.Ceiling(new RectangleF(pan.X + (v.Location.X - v.Width / 2) * zoom, pan.Y + (v.Location.Y - v.Height / 2) * zoom, v.Width * zoom, v.Height * zoom))));

    private Point mouseDown, lastMouse;
    private bool dragging;
    public BranchCanvas(OpeningDefinition opening, ProgressStore progress, Position? current, bool groupVariations = true, bool hideHuman = false, bool hideSubbranches = false, IReadOnlySet<string>? collapsed = null, IReadOnlySet<string>? compact = null, bool? betaLayout = null, double? columnSpacing = null, bool fastLayout = false, bool crossingPostPass = true)
    {
        this.progress = progress;
        highlight = current is null ? null : BranchPositionIdentity.Key(current);
        DoubleBuffered = true;
        BackColor = UiPalette.FromArgb(29, 33, 38);
        Dock = DockStyle.Fill;
        var map = new Dictionary<string, BranchVertex>();
        var seen = new HashSet<OpeningNode>();
        var nodeVertices = new Dictionary<OpeningNode, BranchVertex>();
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
            if (!nodeVertices.TryGetValue(w.Node, out var vertex) && !map.TryGetValue(key, out vertex))
            {
                vertex = new()
                {
                    Id = Vertices.Count,
                    Position = w.P.Clone(),
                    Depth = w.Depth,
                    IsLearnerMove = own
                };
                map[key] = vertex;
                Vertices.Add(vertex);
            }

            nodeVertices[w.Node] = vertex;
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
            foreach (var d in w.Node.Moves)
            {
                if (hideHuman && d.HumanUnfriendly)
                    continue;
                var p = w.P.Clone();
                var m = MoveGenerator.GenerateLegal(p).First(m => m.Uci == d.Uci);
                string san = SanNotation.Format(p, m);
                string? learningKey = p.WhiteToMove == opening.LearnsWhite ? (opening.SchemaVersion == 2 ? OpeningStore.NodeProgressKey(opening, w.Node, d.Uci) : w.Path + "/" + d.Uci) : null;
                p.Apply(m);
                q.Enqueue((d.Next, p, w.Depth + 1, w.Path + "/" + d.Uci, vertex, san, learningKey, d.HumanUnfriendly));
            }
        }

        var parents = Vertices.ToDictionary(v => v, _ => new List<BranchVertex>());
        foreach (var parent in Vertices)
            foreach (var child in parent.Children) parents[child].Add(parent);
        var degrees = parents.ToDictionary(p => p.Key, p => p.Value.Count);
        var ready = new Queue<BranchVertex>(Vertices.Where(v => degrees[v] == 0));
        int visited = 0;
        while (ready.TryDequeue(out var v))
        {
            visited++;
            foreach (var child in v.Children)
                if (--degrees[child] == 0)
                    ready.Enqueue(child);
        }

        if (visited != Vertices.Count && groupVariations)
        {
            using var safe = new BranchCanvas(opening, progress, current, false, hideHuman, hideSubbranches, collapsed, compact, betaLayout, columnSpacing, fastLayout, crossingPostPass);
            Vertices.Clear();
            Vertices.AddRange(safe.Vertices);
            return;
        }

        foreach (var v in Vertices.Where(v => !v.IsLearnerMove))
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

        foreach (var layer in Vertices.GroupBy(Layer))
        {
            float y = 0;
            foreach (var v in layer.OrderBy(v => v.Children.Count == 0 ? 1 : 0))
            {
                v.Location = new(layer.Key * 292.5f, y + v.Height / 2);
                y += v.Height + 22;
            }
        }

        bool improved = betaLayout ?? !Preferences.Current.BranchLegacyPositioning;
        if (!fastLayout && !improved)
        {
            // Retained legacy placement: keep its ordering/projection unchanged.
            for (int pass = 0; pass < 11; pass++)
                foreach (var layer in (pass % 2 == 0 ? Vertices.GroupBy(Layer).OrderByDescending(g => g.Key) : Vertices.GroupBy(Layer).OrderBy(g => g.Key)))
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
                foreach (var layer in (pass % 2 == 0 ? Vertices.GroupBy(Layer).OrderByDescending(g => g.Key) : Vertices.GroupBy(Layer).OrderBy(g => g.Key)))
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
        else if (!fastLayout)
            BranchBetaLayout.Apply(Vertices, layers, parents);
        if (crossingPostPass && !fastLayout) BranchCrossingReduction.Apply(Vertices);

        double spacing = columnSpacing ?? Preferences.Current.BranchColumnSpacing;
        float factor = (float)BranchLayoutOptions.ClampSpacing(spacing);
        foreach (var v in Vertices) v.Location = new(v.Location.X * factor, v.Location.Y);

        var owners = new Dictionary<BranchVertex, BranchVertex>();
        Color[] outlines = [Color.Goldenrod, Color.CornflowerBlue, Color.MediumSeaGreen, Color.Coral, Color.Turquoise];
        var ownerColors = new Dictionary<BranchVertex, Color>();
        foreach (var root in Vertices.Where(v => v.Nodes.Any(n => n.IsSubbranch)).OrderBy(Layer).ThenBy(v => v.Id))
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

        float minY = Vertices.Min(v => v.Location.Y);
        foreach (var v in Vertices)
            v.Location = new(v.Location.X, v.Location.Y - minY);
    }

    // A live color edit does not rebuild/re-layout the graph or move its camera.
    public void PreviewSubbranchColor(BranchVertex root, Color color)
    {
        foreach (var vertex in Vertices.Where(v => v.SubbranchRoot == root)) vertex.SubbranchColor = color;
        Invalidate();
    }

    public void Fit()
    {
        if (Vertices.Count == 0)
            return;
        float width = Vertices.Max(v => v.Location.X) + 145, height = Vertices.Max(v => v.Location.Y + v.Height / 2) + 60;
        zoom = Math.Clamp(Math.Min((Width - 70) / width, (Height - 70) / height), .035f, 1.1f);
        pan = new(35, 35);
        Invalidate();
    }

    public void FocusCurrent()
    {
        var v = Vertices.FirstOrDefault(v => highlight is not null && renderCache.Keys(v).Normalized.Contains(highlight)) ?? Vertices.FirstOrDefault();
        if (v is null)
            return;
        zoom = 1;
        pan = new((highlight is null ? 65 : Width / 2f) - v.Location.X, Height / 2f - v.Location.Y);
        MarkSelected(v);
        Selected?.Invoke(v.Position.Clone());
        VertexSelected?.Invoke(v);
        Invalidate();
    }

    public bool SelectPosition(Position position, bool center = false)
    {
        string key = BranchPositionIdentity.Key(position);
        var vertex = Vertices.FirstOrDefault(v => renderCache.Keys(v).Normalized.Contains(key));
        if (vertex is null) return false;
        if (center) pan = new(Width / 2f - vertex.Location.X * zoom, Height / 2f - vertex.Location.Y * zoom);
        MarkSelected(vertex); VertexSelected?.Invoke(vertex); Selected?.Invoke(position.Clone());
        return true;
    }

    private Color ColorFor(BranchVertex v)
    {
        var keys = v.IsLearnerMove ? v.ProgressKeys : v.InheritedKeys;
        string status = "unknown";
        if (keys.Count > 0)
        {
            var states = keys.Select(progress.Status).ToArray();
            if (keys.All(k => progress.Data.BranchPriority.GetValueOrDefault(k) == "Ausgeschlossen"))
                status = "excluded";
            else if (keys.Any(k => progress.Data.BranchPriority.GetValueOrDefault(k) == "Später"))
                status = "later";
            else if (states.All(s => s == "Gelernt"))
                status = "learned";
            else if (states.Contains("Zu Wiederholen"))
                status = "due";
            else if (states.Any(s => s == "Angeschaut" || s == "Gelernt"))
                status = "seen";
        }

        return UiPalette.BranchNodeColor(status, v.IsLearnerMove);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TranslateTransform(pan.X, pan.Y);
        g.ScaleTransform(zoom, zoom);
        using var edge = new Pen(UiPalette.FromArgb(87, 106, 125), 1.3f / zoom);
        foreach (var v in Vertices)
            foreach (var child in v.Children)
                g.DrawBezier(edge, new PointF(v.Location.X + v.Width / 2, v.Location.Y), new PointF(v.Location.X + 98, v.Location.Y), new PointF(child.Location.X - 98, child.Location.Y), new PointF(child.Location.X - child.Width / 2, child.Location.Y));
        Color normalText = UiPalette.LightBackground ? UiPalette.ButtonText : Color.White;
        using var text = new SolidBrush(normalText);
        using var font = new Font("Segoe UI", 8);
        using var countFont = new Font(font, FontStyle.Bold);
        foreach (var v in Vertices)
        {
            using var brush = new SolidBrush(ColorFor(v));
            g.FillRectangle(brush, v.Location.X - v.Width / 2, v.Location.Y - v.Height / 2, v.Width, v.Height);
            void Handle(RectangleF rect, bool horizontal, bool closed)
            {
                g.FillRectangle(brush, rect);
                float x = rect.X + rect.Width / 2, y = rect.Y + rect.Height / 2;
                using var line = new Pen(normalText, 1.5f);
                if (horizontal)
                    g.DrawLines(line, closed ? new PointF[] { new(x - 2, y - 4), new(x + 2, y), new(x - 2, y + 4) } : [new(x + 2, y - 4), new(x - 2, y), new(x + 2, y + 4)]);
                else
                    g.DrawLines(line, closed ? new PointF[] { new(x - 4, y - 2), new(x, y + 2), new(x + 4, y - 2) } : [new(x - 4, y + 2), new(x, y - 2), new(x + 4, y + 2)]);
            }
            if (v.CanCompact) Handle(v.VariantHandle, false, v.Compact);
            if (v.Nodes.Any(n => n.IsSubbranch)) Handle(v.SubbranchHandle, true, v.Collapsed);

            if (!explicitSelectionCleared && (selectedKey is not null ? renderCache.Keys(v).Exact.Contains(selectedKey)
                : highlight is not null && renderCache.Keys(v).Normalized.Contains(highlight)))
            {
                using var ring = new Pen(UiPalette.Orange, 3);
                using var outlinePath = v.Outline(4);
                g.DrawPath(ring, outlinePath);
            }

            if (!HideSubbranchColors && v.SubbranchColor is Color outline)
            {
                using var sub = new Pen(outline, 2);
                using var outlinePath = v.Outline();
                g.DrawPath(sub, outlinePath);
            }

            if (AnalysisMode)
            {
                int count = renderCache.Overlay(v, AnalysisExits, AnalysisCaptions).Count;
                if (count > 0)
                {
                    using var matched = new Pen(AnalysisMarkerColor, 2.4f);
                    using var matchPath = v.Outline(6);
                    g.DrawPath(matched, matchPath);
                    if (count > 1)
                        g.DrawString(count.ToString(), countFont, text, v.Location.X + v.Width / 2 + 12, v.Location.Y - v.Height / 2 - 14);
                }
            }

            if (zoom > .25)
            {
                var chunks = (v.Incoming.Count == 0 ? new[]
                {
                    "Start"
                }

                : v.Compact && v.CanCompact ? new[]
                {
                    v.Incoming.First() + "  +" + (v.Incoming.Count - 1)
                }

                : v.Incoming.ToArray()).Chunk(2).ToList();
                for (int row = 0; row < chunks.Count; row++)
                {
                    var labels = chunks[row];
                    float total = labels.Sum(a => g.MeasureString(a, font).Width) + (labels.Length - 1) * 8, x = v.Location.X - total / 2;
                    foreach (string label in labels)
                    {
                        string caption = v.Compact && v.CanCompact ? v.Incoming.First() : label;
                        using var ink = new SolidBrush(CaptionColorFor(v, caption));
                        g.DrawString(label, font, ink, x, v.Location.Y - v.Height / 2 + 2 + row * 14);
                        x += g.MeasureString(label, font).Width + 8;
                    }
                }
            }
        }
    }

    private Color CaptionColorFor(BranchVertex vertex, string caption)
    {
        if (AnalysisMode && renderCache.Overlay(vertex, AnalysisExits, AnalysisCaptions).Captions.Contains(caption))
            return AnalysisMarkerColor;
        return vertex.HumanCaptions.Contains(caption) ? UiPalette.FromArgb(225, 155, 255) : UiPalette.LightBackground ? UiPalette.ButtonText : Color.White;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Right)
        {
            float x = (e.X - pan.X) / zoom, y = (e.Y - pan.Y) / zoom;
            var hit = Vertices.FirstOrDefault(v => Math.Abs(v.Location.X - x) <= v.Width / 2 && Math.Abs(v.Location.Y - y) <= v.Height / 2);
            if (hit is not null)
            {
                MarkSelected(hit);
                VertexSelected?.Invoke(hit);
                NodeMenuRequested?.Invoke(hit, e.Location);
            }
        }
        if (e.Button == MouseButtons.Left)
        {
            mouseDown = lastMouse = e.Location;
            dragging = false;
            Capture = true;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (e.Button == MouseButtons.Left && Capture)
        {
            if (Math.Abs(e.X - mouseDown.X) + Math.Abs(e.Y - mouseDown.Y) > 4)
                dragging = true;
            if (dragging)
            {
                pan.X += e.X - lastMouse.X;
                pan.Y += e.Y - lastMouse.Y;
                Invalidate();
            }

            lastMouse = e.Location;
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        Capture = false;
        if (e.Button == MouseButtons.Left && !dragging)
        {
            var x = (e.X - pan.X) / zoom;
            var y = (e.Y - pan.Y) / zoom;
            var point = new PointF(x, y);
            var variants = Vertices.FirstOrDefault(v => v.CanCompact && v.VariantHandle.Contains(point));
            if (variants is not null) { ToggleVariants?.Invoke(variants); return; }
            var sub = Vertices.FirstOrDefault(v => v.Nodes.Any(n => n.IsSubbranch) && v.SubbranchHandle.Contains(point));
            if (sub is not null) { ToggleSubbranch?.Invoke(sub); return; }
            var nearest = Vertices.FirstOrDefault(v => Math.Abs(v.Location.X - x) < Math.Max(v.Width / 2, 8 / zoom) && Math.Abs(v.Location.Y - y) < Math.Max(v.Height / 2, 8 / zoom));
            if (nearest is not null)
            {
                MarkSelected(nearest);
                Selected?.Invoke(nearest.Position.Clone());
                VertexSelected?.Invoke(nearest);
            }
            else
            {
                selectedKey = null; explicitSelectionCleared = true;
                SelectionCleared?.Invoke(); Invalidate();
            }
        }
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        float z = Math.Clamp(zoom * (e.Delta > 0 ? 1.2f : 1 / 1.2f), .025f, 3);
        pan = new(e.X - (e.X - pan.X) * z / zoom, e.Y - (e.Y - pan.Y) * z / zoom);
        zoom = z;
        Invalidate();
    }
}
