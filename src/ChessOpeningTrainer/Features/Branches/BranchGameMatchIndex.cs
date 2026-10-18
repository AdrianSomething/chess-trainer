namespace ChessOpeningTrainer;

/// <summary>Window-owned lookup built once per player/library refresh, not once per node click.</summary>
internal sealed class BranchGameMatchIndex
{
    private readonly Dictionary<string, List<(BranchGameExit Match, int Rank)>> positions = [];
    public IReadOnlyDictionary<string, int> Counts { get; }

    public BranchGameMatchIndex(IEnumerable<BranchGameExit> matches)
    {
        int rank = 0;
        foreach (var match in matches.OrderByDescending(m => m.Game.Tags.GetValueOrDefault("Date")).ThenByDescending(m => m.Game.ImportedUtc))
        {
            string key = BranchPositionIdentity.Key(match.LastTheoryPosition);
            if (!positions.TryGetValue(key, out var list)) positions.Add(key, list = []);
            list.Add((match, rank++));
        }
        Counts = positions.ToDictionary(pair => pair.Key, pair => pair.Value.Count);
    }

    public IEnumerable<BranchGameExit> Find(IEnumerable<string> keys) => keys.Distinct()
        .Where(positions.ContainsKey).SelectMany(key => positions[key]).OrderBy(item => item.Rank).Select(item => item.Match);
}
