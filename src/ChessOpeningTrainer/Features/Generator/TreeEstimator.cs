namespace ChessOpeningTrainer;
public static class TreeEstimator
{
    // Knuth-style random root-to-leaf paths, including the real stopping rules.
    // Unlike extrapolating a shallow layer, this samples through the full horizon.
    // It estimates an unmerged tree; graph transpositions can reduce the final count.
    public static async Task<TreeEstimate> Estimate(StockfishEngine engine, Position start, GeneratorOptions options, IReadOnlyList<string> setup, CancellationToken token, int seconds = 35)
    {
        options.Validate();
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(token);
        budget.CancelAfter(TimeSpan.FromSeconds(seconds));
        await using var pool = new EnginePool(engine, Math.Min(4, options.Workers), Math.Max(1, options.Threads / options.Workers), budget.Token);
        var cache = new Dictionary<string, Task<EngineResult>>();
        var results = new List<double>();
        int prefix = setup.Select((_, i) => (i % 2 == 0) == options.LearnWhite).Count(b => b);
        async Task<double> Sample()
        {
            var p = start.Clone();
            double count = prefix, weight = 1;
            var seen = new HashSet<string>();
            for (int depth = 0; depth <= options.MaxDepth; depth++)
            {
                budget.Token.ThrowIfCancellationRequested();
                bool learner = p.WhiteToMove == options.LearnWhite;
                var effective = options.AtDepth(depth);
                if (learner && depth >= options.MaxDepth || !seen.Add(OpeningStore.PositionKey(p)) || MoveGenerator.GameState(p, new Dictionary<string, int>())is not ("" or "Schach"))
                    break;
                int searchDepth = Math.Min(effective.SearchDepth, 8);
                string key = p.ToFen() + "|" + searchDepth;
                Task<EngineResult> task;
                lock (cache)
                {
                    if (!cache.TryGetValue(key, out task!))
                        cache[key] = task = pool.Search(p, searchDepth);
                }

                var r = await task;
                // Still one answer/card at a learner node, not one branch per
                // eligible alternative. Sample the same own-move variation rule.
                List<EngineLine> choices = learner ? [OpeningGenerator.ChooseLearner(p, r.Lines, effective, _ => false)] : OpeningGenerator.Plausible(r, effective, effective.TwoStages && depth >= effective.MiddleDepth ? effective.LateReplyLoss : null);
                int complex = effective.TwoStages && depth >= effective.MiddleDepth ? effective.LateComplexThreshold : effective.ComplexThreshold;
                if (!learner && depth >= options.MinDepth && choices.Count >= complex && depth < options.MaxDepth)
                    choices = choices.Take(effective.RetainBest).Where(l => (p.WhiteToMove ? OpeningGenerator.Score(l) : -OpeningGenerator.Score(l)) >= effective.RetainMinEval).ToList();
                if (choices.Count == 0 || !learner && depth >= options.MaxDepth)
                    break;
                if (learner)
                    count += weight;
                else
                    weight *= choices.Count;
                string? u = choices[Random.Shared.Next(choices.Count)].BestMove;
                p.Apply(MoveGenerator.GenerateLegal(p).First(m => m.Uci == u));
            }

            return count;
        }

        var samples = Enumerable.Range(0, 12).Select(async _ =>
        {
            try
            {
                double value = await Sample();
                lock (results)
                    results.Add(value);
            }
            catch (OperationCanceledException)
            {
            }
        }).ToArray();
        // Cancellation drains queued work before waiting for every path.
        var all = Task.WhenAll(samples);
        var timeout = Task.Delay(Timeout.Infinite, budget.Token);
        if (await Task.WhenAny(all, timeout) != all)
            await pool.DisposeAsync();
        await all;
        token.ThrowIfCancellationRequested();
        long Bound(double n) => (long)Math.Clamp(n, 0, long.MaxValue / 2d);
        return results.Count == 0 ? new(0, 0, 0, 0, true) : new(results.Count, Bound(results.Average()), Bound(results.Min()), Bound(results.Max()), budget.IsCancellationRequested);
    }
}
