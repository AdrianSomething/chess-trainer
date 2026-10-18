namespace ChessOpeningTrainer;

/// <summary>
/// Per-canvas snapshots: graph positions do not change when panning, selecting or
/// previewing a color. In particular, never regenerate legal en-passant moves in Paint.
/// A replacement match dictionary invalidates overlays, not position identities.
/// </summary>
internal sealed class BranchCanvasCache
{
    internal sealed record Identity(HashSet<string> Exact, HashSet<string> Normalized);
    internal sealed record Annotation(int Count, HashSet<string> Captions);
    private readonly Dictionary<BranchVertex, Identity> identities = [];
    private readonly Dictionary<BranchVertex, Annotation> annotations = [];
    private IReadOnlyDictionary<string, int>? counts;
    private IReadOnlyDictionary<string, IReadOnlySet<string>>? captions;

    public Identity Keys(BranchVertex vertex)
    {
        if (identities.TryGetValue(vertex, out var cached)) return cached;
        IReadOnlyList<Position> positions = vertex.Alternatives.Count > 0 ? vertex.Alternatives : [vertex.Position];
        cached = new(positions.Select(OpeningStore.PositionKey).ToHashSet(), positions.Select(BranchPositionIdentity.Key).ToHashSet());
        identities.Add(vertex, cached); return cached;
    }

    public Annotation Overlay(BranchVertex vertex, IReadOnlyDictionary<string, int> currentCounts,
        IReadOnlyDictionary<string, IReadOnlySet<string>> currentCaptions)
    {
        if (!ReferenceEquals(counts, currentCounts) || !ReferenceEquals(captions, currentCaptions))
        {
            counts = currentCounts; captions = currentCaptions; annotations.Clear();
        }
        if (annotations.TryGetValue(vertex, out var cached)) return cached;
        int count = 0; var labels = new HashSet<string>();
        foreach (string key in Keys(vertex).Normalized)
        {
            count += currentCounts.GetValueOrDefault(key);
            if (currentCaptions.TryGetValue(key, out var played)) labels.UnionWith(played);
        }
        cached = new(count, labels); annotations.Add(vertex, cached); return cached;
    }
}
