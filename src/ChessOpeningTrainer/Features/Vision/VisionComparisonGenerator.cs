namespace ChessOpeningTrainer;

public sealed record VisionComparison(Position Before, Move[] Moves, int BetterIndex, double Gap, double[]? ScoresWhite = null);

// No Controls: generation and confidence requirements can be tested independently.
public sealed class VisionComparisonGenerator(StockfishEngine engine)
{
    public async Task<VisionComparison> Generate(bool white, CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromMinutes(2));
        while (true)
        {
            var before = (await Task.Run(() => VisionRules.Generate(0, timeout.Token, white), timeout.Token)).Position;
            var quick = await engine.AnalyzeAsync(before.ToFen(), 10, 1, 1, 64, timeout.Token, 300);
            if (quick.Error is not null) throw new IOException(quick.Error);
            if (quick.Mate is not null || Math.Abs((quick.ScoreCentipawns ?? 10000) / 100d) > 3) continue;
            var search = await engine.AnalyzeAsync(before.ToFen(), 24, 6, 2, 128, timeout.Token, 6000);
            if (search.Error is not null) throw new IOException(search.Error);
            var legal = MoveGenerator.GenerateLegal(before);
            var candidates = search.Lines.Where(l => l.Depth >= 18 && l.Mate is null && l.BestMove is not null && Math.Abs(OpeningGenerator.Score(l)) <= 3)
                .Where(l => legal.Any(m => m.Uci == l.BestMove && VisionComparisonQuality.ImmediateGain(before, m) <= 1)).OrderByDescending(OpeningGenerator.Score).ToList();
            if (candidates.Count < 2) continue;
            var pairs = (from better in candidates from other in candidates where OpeningGenerator.Score(better) - OpeningGenerator.Score(other) >= .5
                select (Best: better, Worse: other)).OrderBy(pair => Math.Max(
                    VisionComparisonQuality.ImmediateGain(before, legal.First(m => m.Uci == pair.Best.BestMove)),
                    VisionComparisonQuality.ImmediateGain(before, legal.First(m => m.Uci == pair.Worse.BestMove)))).ThenBy(_ => Random.Shared.Next()).ToArray();
            if (pairs.Length == 0) continue;
            var (best, worse) = pairs[0];
            int correct = Random.Shared.Next(2);
            Move[] moves = correct == 0 ? [legal.First(m => m.Uci == best.BestMove), legal.First(m => m.Uci == worse.BestMove)] : [legal.First(m => m.Uci == worse.BestMove), legal.First(m => m.Uci == best.BestMove)];
            double whiteFactor = before.WhiteToMove ? 1 : -1;
            double[] scores = correct == 0 ? [OpeningGenerator.Score(best) * whiteFactor, OpeningGenerator.Score(worse) * whiteFactor]
                : [OpeningGenerator.Score(worse) * whiteFactor, OpeningGenerator.Score(best) * whiteFactor];
            return new(before, moves, correct, OpeningGenerator.Score(best) - OpeningGenerator.Score(worse), scores);
        }
    }
}
