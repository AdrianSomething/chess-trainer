using System.Diagnostics;

namespace ChessOpeningTrainer;

/// <summary>One prediction shared by the estimate action and the read-only progress view.</summary>
public sealed class GeneratorForecast
{
    private readonly Stopwatch elapsed = Stopwatch.StartNew();
    private readonly int prefixCards;
    private readonly double? historicalSecondsPerCard;
    private double progress;
    public long EstimatedCards { get; private set; }
    public TimeSpan Elapsed => elapsed.Elapsed;
    public double Progress => progress;
    public int CompletedCards { get; private set; }
    public bool Finished { get; private set; }
    public bool Stopped { get; private set; }

    public GeneratorForecast(long estimatedCards, int prefixCards = 0, double? historicalSecondsPerCard = null)
    {
        this.prefixCards = prefixCards;
        this.historicalSecondsPerCard = historicalSecondsPerCard;
        EstimatedCards = Math.Max(prefixCards + 1L, estimatedCards);
    }

    public void Update(GeneratorGraphSnapshot snapshot)
    {
        // UI progress notifications can still be queued when generation returns.
        // Never let an older queued frame reopen a completed timing sample.
        if (Finished || Stopped || snapshot.Cards < CompletedCards) return;
        CompletedCards = snapshot.Cards;
        Finished = snapshot.Finished;
        Stopped = snapshot.Stopped;
        if (Stopped) elapsed.Stop();
        if (Finished) { EstimatedCards = CompletedCards; progress = 1; elapsed.Stop(); return; }
        // A prefilled start sequence is not work already done by the engine.
        // The previous node/pending ratio could incorrectly report 30% here.
        long completed = Math.Max(0, snapshot.Cards - prefixCards);
        long pendingLowerBound = Math.Max(1, (snapshot.Pending + 1L) / 2);
        long lowerBound = snapshot.Cards + pendingLowerBound;
        if (completed >= 8 && snapshot.Nodes.Count(n => n.Completed) >= 12)
        {
            // Learn continuation/termination rates from processed nodes at each
            // depth. Unlike a maximum-only estimate this can correct an overly
            // broad initial forecast downwards when filters close many branches.
            var useful = snapshot.Nodes.Where(n => !n.IsTerminalCounter).ToArray();
            var ids = useful.Select(n => n.Id).ToHashSet();
            var outgoing = snapshot.Edges.Where(e => ids.Contains(e.To)).GroupBy(e => e.From).ToDictionary(g => g.Key, g => g.Count());
            var cohorts = useful.Where(n => n.Completed).GroupBy(n => n.Depth).ToDictionary(g => g.Key, g => g.ToArray());
            var rates = cohorts.ToDictionary(g => g.Key, g => g.Value.Average(n => (double)outgoing.GetValueOrDefault(n.Id)));
            // For unseen deeper layers, use the latest observed layer of the
            // same side. A broad early layer must not override a later cutoff.
            double Rate(int depth)
            {
                if (rates.TryGetValue(depth, out double exact)) return exact;
                int latest = rates.Keys.Where(d => d < depth && (d & 1) == (depth & 1)).DefaultIfEmpty(-1).Max();
                return latest >= 0 ? rates[latest] : 1;
            }
            double FutureCards(int depth)
            {
                double population = 1, cards = 0;
                for (int d = depth; d < snapshot.MaxDepth && population > .01; d++)
                {
                    population *= Rate(d);
                    cards = Math.Min(1_000_000_000, cards + population * .5);
                    if (cards >= 1_000_000_000) break;
                }
                return cards;
            }
            var frontier = useful.Where(n => !n.Completed).ToArray();
            double projected = snapshot.Cards + frontier.Sum(n => FutureCards(n.Depth));
            long observed = (long)Math.Min(1_000_000_000, Math.Max(lowerBound, projected));
            // React strongly when a sampled frontier layer is mostly closed.
            // Keep some smoothing for small/noisy cohorts, not an obsolete
            // exponential estimate that survives until the final notification.
            bool cutoff = frontier.Any(n => cohorts.TryGetValue(n.Depth, out var cohort) && cohort.Length >= 4 && rates[n.Depth] < .5);
            double weight = cutoff ? .9 : .5;
            EstimatedCards = Math.Max(lowerBound, (long)Math.Round((1 - weight) * EstimatedCards + weight * observed));
        }
        else EstimatedCards = Math.Max(EstimatedCards, lowerBound);
        double fraction = completed / (double)Math.Max(1, EstimatedCards - prefixCards);
        progress = Math.Max(progress, Math.Min(.98, fraction));
    }
    public void Stop() { Stopped = true; elapsed.Stop(); }
    public void SetWorking(bool active)
    {
        if (active && !Stopped && !Finished) elapsed.Start(); else elapsed.Stop();
    }

    public TimeSpan? Remaining
    {
        get
        {
            if (Finished) return TimeSpan.Zero;
            if (Stopped) return null;
            int completed = Math.Max(0, CompletedCards - prefixCards);
            double? secondsPerCard = historicalSecondsPerCard;
            if (completed >= 3 && elapsed.Elapsed.TotalSeconds >= 5)
            {
                double observed = elapsed.Elapsed.TotalSeconds / completed;
                secondsPerCard = secondsPerCard is double previous ? .35 * previous + .65 * observed : observed;
            }
            if (secondsPerCard is null) return null;
            double seconds = Math.Max(0, EstimatedCards - CompletedCards) * secondsPerCard.Value;
            return TimeSpan.FromSeconds(Math.Min(TimeSpan.MaxValue.TotalSeconds - 1, seconds));
        }
    }
}
