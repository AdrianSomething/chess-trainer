namespace ChessOpeningTrainer;

public sealed record CoverageGap(string Fen, string Move, string Sequence, long Games, double Gain);
public sealed record CoverageReport(double? Score, int ObservedPositions, int UnknownPositions, IReadOnlyList<CoverageGap> Gaps);

/// <summary>A descriptive repertoire coverage index, not a playing-strength estimate.</summary>
public static class OpeningCoverage
{
    public static CoverageReport Calculate(OpeningDefinition opening,
        Func<Position, IReadOnlyList<(string Move, DatasetCounts Counts)>> children, CancellationToken token)
    {
        var positions = GraphOperations.Positions(opening);
        // Older generated repertoires explicitly label their manually supplied prefix.
        var start = positions[0];
        var seenPrefix = new HashSet<OpeningNode>();
        while (seenPrefix.Add(start.Node) && start.Node.Moves.Count == 1 &&
            start.Node.Moves[0].Explanation == "Manuell vorgegebene Startsequenz")
        {
            var next = positions.FirstOrDefault(w => w.Node == start.Node.Moves[0].Next);
            if (next.Node is null) break;
            start = next;
        }
        if (opening.GenerationStartFen is { } fen)
        {
            string key = OpeningStore.PositionKey(Position.FromFen(fen));
            var explicitStart = positions.FirstOrDefault(w => OpeningStore.PositionKey(w.Position) == key);
            if (explicitStart.Node is not null) start = explicitStart;
        }
        var reachable = new HashSet<OpeningNode>();
        var pending = new Queue<(OpeningNode Node, int Depth)>(); pending.Enqueue((start.Node, 0));
        var depths = new Dictionary<OpeningNode, int>();
        while (pending.TryDequeue(out var w))
        {
            token.ThrowIfCancellationRequested();
            if (!reachable.Add(w.Node)) continue;
            depths[w.Node] = w.Depth;
            foreach (var move in w.Node.Moves) pending.Enqueue((move.Next, w.Depth + 1));
        }
        double total = 0, covered = 0; int observed = 0, unknown = 0;
        var gaps = new List<(CoverageGap Gap, double Weight)>();
        var identities = new HashSet<string>();
        foreach (var w in positions.Where(w => reachable.Contains(w.Node) && w.Position.WhiteToMove != opening.LearnsWhite))
        {
            token.ThrowIfCancellationRequested();
            if (!identities.Add(OpeningStore.PositionKey(w.Position))) continue;
            var replies = children(w.Position).Where(e => e.Counts.Total > 0).ToArray();
            if (replies.Length == 0) { unknown++; continue; }
            observed++;
            double discount = 1d / (1 + depths[w.Node] / 2);
            var included = w.Node.Moves.Select(m => m.Uci).ToHashSet();
            foreach (var reply in replies)
            {
                double weight = reply.Counts.Total * discount; total += weight;
                if (included.Contains(reply.Move)) covered += weight;
                else
                {
                    var legal = MoveGenerator.GenerateLegal(w.Position).FirstOrDefault(m => m.Uci == reply.Move);
                    if (legal is null) continue;
                    var p = Position.FromFen(opening.StartFen ?? Position.InitialFen); var san = new List<string>();
                    foreach (string u in w.Path.Append(reply.Move))
                    { var m = MoveGenerator.GenerateLegal(p).First(m => m.Uci == u); san.Add(SanNotation.Format(p, m)); p.Apply(m); }
                    gaps.Add((new(w.Position.ToFen(), reply.Move, string.Join(" ", san), reply.Counts.Total, 0), weight));
                }
            }
        }
        return new(total > 0 ? 100 * covered / total : null, observed, unknown,
            gaps.OrderByDescending(g => g.Weight).Select(g => g.Gap with { Gain = total > 0 ? 100 * g.Weight / total : 0 }).ToArray());
    }
}
