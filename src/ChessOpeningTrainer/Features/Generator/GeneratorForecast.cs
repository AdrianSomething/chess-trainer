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
        if (Finished) { progress = 1; elapsed.Stop(); return; }
        // A prefilled start sequence is not work already done by the engine.
        // The previous node/pending ratio could incorrectly report 30% here.
        long completed = Math.Max(0, snapshot.Cards - prefixCards);
        long pendingLowerBound = Math.Max(1, (snapshot.Pending + 1L) / 2);
        EstimatedCards = Math.Max(EstimatedCards, snapshot.Cards + pendingLowerBound);
        double fraction = completed / (double)Math.Max(1, EstimatedCards - prefixCards);
        progress = Math.Max(progress, Math.Min(.98, fraction));
    }
    public void Stop() { Stopped = true; elapsed.Stop(); }

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
