namespace ChessOpeningTrainer;

// Owned by the app's puzzle supply, never shared with playing/analysis engines.
public sealed class GenerationBudget
{
    private readonly object sync = new();
    private readonly Func<int> workerLimit, threadLimit;
    private int active;
    public GenerationBudget(Func<int>? workers = null, Func<int>? threads = null)
    {
        workerLimit = workers ?? (() => Preferences.Current.TacticGenerationWorkers);
        threadLimit = threads ?? (() => Preferences.Current.TacticGenerationThreads);
    }
    public int Workers => Math.Clamp(workerLimit(), 1, 8);
    public int Active { get { lock (sync) return active; } }
    public async Task<IDisposable> Acquire(CancellationToken token)
    {
        while (true)
        {
            token.ThrowIfCancellationRequested();
            lock (sync)
                if (active < Workers)
                {
                    active++;
                    return new Lease(this);
                }
            await Task.Delay(40, token);
        }
    }
    private sealed class Lease(GenerationBudget owner) : IDisposable
    {
        private int disposed;
        public void Dispose() { if (Interlocked.Exchange(ref disposed, 1) == 0) lock (owner.sync) owner.active--; }
    }
    public int Threads => Math.Clamp(threadLimit(), 1, 8);
}
