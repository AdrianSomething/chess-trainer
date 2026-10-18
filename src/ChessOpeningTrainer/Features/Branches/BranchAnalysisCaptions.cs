namespace ChessOpeningTrainer;

/// <summary>Incoming SANs actually played at the matched positions, not every label of a bundled node.</summary>
public static class BranchAnalysisCaptions
{
    public static IReadOnlyDictionary<string, IReadOnlySet<string>> Build(IEnumerable<BranchGameExit> matches)
    {
        var result = new Dictionary<string, HashSet<string>>();
        foreach (var match in matches)
        {
            string? san = match.IncomingSan;
            if (san is null)
            {
                // Compatibility for callers constructing matches directly.
                try
                {
                    if (match.PositionIndex == 0) san = "Start";
                    else
                    {
                        var p = match.Game.Positions()[match.PositionIndex - 1];
                        var move = MoveGenerator.GenerateLegal(p).First(m => m.Uci == match.Game.Moves[match.PositionIndex - 1]);
                        san = SanNotation.Format(p, move);
                    }
                }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or FormatException) { continue; }
            }
            string key = BranchPositionIdentity.Key(match.LastTheoryPosition);
            if (!result.TryGetValue(key, out var tokens)) result[key] = tokens = [];
            tokens.Add(san);
        }
        return result.ToDictionary(p => p.Key, p => (IReadOnlySet<string>)p.Value);
    }
}
