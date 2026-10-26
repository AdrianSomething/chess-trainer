using System.Diagnostics;
using System.Text.Json;

namespace ChessOpeningTrainer;

public enum GeneratorJobState { Queued, Running, AwaitingApproval, Completed, Cancelled, Failed }

/// <summary>Job-owned settings and immutable UI snapshots. Original opening JSON is unchanged.</summary>
public sealed class GeneratorJob
{
    public string Name { get; }
    public Position Start { get; }
    public string[] Setup { get; }
    public GeneratorOptions Options { get; }
    public bool PauseAllAtLimit { get; }
    public GeneratorJobState State { get; internal set; } = GeneratorJobState.Queued;
    public GeneratorForecast Forecast { get; }
    public GeneratorGraphSnapshot? Graph { get; internal set; }
    public GeneratorSearchDepth? SearchDepth { get; internal set; }
    public string Message { get; internal set; } = "";
    public OpeningDefinition? Result { get; internal set; }
    internal CancellationTokenSource Cancellation { get; } = new();
    internal TaskCompletionSource<bool>? Approval;
    internal int Active;
    public bool Finished => State is GeneratorJobState.Completed or GeneratorJobState.Cancelled or GeneratorJobState.Failed;
    public GeneratorJob(string name, Position start, IReadOnlyList<string> setup, GeneratorOptions options, bool pauseAll, long expected, double? secondsPerCard = null, bool validate = true)
    {
        Name = name; Start = start.Clone(); Setup = setup.ToArray(); PauseAllAtLimit = pauseAll;
        Options = JsonSerializer.Deserialize<GeneratorOptions>(JsonSerializer.Serialize(options))!;
        Options.DatasetIndexFile = options.DatasetIndexFile;
        if (validate) Options.Validate();
        Forecast = new(expected, setup.Select((_, i) => (i % 2 == 0) == options.LearnWhite).Count(b => b), secondsPerCard);
        Forecast.SetWorking(false);
    }
}

public interface IOpeningSearch
{
    Task<EngineResult> Search(Position position, int depth, Action<int>? progress, CancellationToken token);
}

/// <summary>Priority is the list order. Idle workers borrow pending positions from
/// lower priority jobs; running searches are never interrupted for reprioritization.</summary>
public sealed class GeneratorWorkQueue : IAsyncDisposable
{
    private sealed record Request(GeneratorJob Job, Position Position, int Depth, Action<int>? Progress,
        CancellationToken Token, TaskCompletionSource<EngineResult> Result);
    private sealed class JobSearch(GeneratorWorkQueue queue, GeneratorJob job) : IOpeningSearch
    {
        public Task<EngineResult> Search(Position p, int d, Action<int>? progress, CancellationToken token) => queue.Search(job, p, d, progress, token);
    }
    private sealed class Reporter<T>(Action<T> report) : IProgress<T> { public void Report(T value) => report(value); }
    private readonly StockfishEngine engine;
    private readonly IOpeningSearch? searchOverride;
    private readonly object sync = new();
    private readonly List<GeneratorJob> jobs = [];
    private readonly List<GeneratorJob> retired = [];
    private readonly List<Request> pending = [];
    private readonly List<Task> runs = [];
    private readonly CancellationTokenSource lifetime = new();
    private readonly Task[] workers;
    private TaskCompletionSource wake = NewSignal();
    private readonly Stopwatch working = new();
    private readonly Stopwatch totalWorking = new();
    private DateTime? restingUntil;
    private bool disposed;
    public event Action? Changed;
    public event Action<GeneratorJob>? Completed;
    public IReadOnlyList<GeneratorJob> Jobs { get { lock (sync) return jobs.ToArray(); } }
    public bool Resting { get { lock (sync) return restingUntil is not null; } }
    public TimeSpan WorkElapsed { get { lock (sync) return totalWorking.Elapsed; } }
    public GeneratorWorkQueue(StockfishEngine engine, IOpeningSearch? search = null)
    {
        this.engine = engine;
        searchOverride = search;
        workers = Enumerable.Range(0, 16).Select(i => Task.Run(() => Worker(i))).ToArray();
    }
    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    private void Pulse() { var old = wake; wake = NewSignal(); old.TrySetResult(); }
    private void Notify() => Changed?.Invoke();
    public void Add(GeneratorJob job)
    {
        job.Options.Validate();
        lock (sync)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            jobs.Add(job); runs.Add(Task.Run(() => Run(job))); Pulse();
        }
        Notify();
    }
    public void Prioritize(GeneratorJob job)
    {
        lock (sync) { if (!jobs.Remove(job)) return; jobs.Insert(0, job); Pulse(); }
        Notify();
    }
    public void Resume(GeneratorJob job)
    {
        lock (sync)
        {
            if (job.State != GeneratorJobState.AwaitingApproval) return;
            job.State = GeneratorJobState.Queued; job.Approval?.TrySetResult(true); job.Approval = null; Pulse();
        }
        Notify();
    }
    public void ResumeAll() { foreach (var job in Jobs) Resume(job); }
    public bool RemoveFinished(GeneratorJob job)
    {
        lock (sync)
        {
            if (!job.Finished || !jobs.Remove(job)) return false;
            retired.Add(job); Pulse();
        }
        Notify(); return true;
    }
    public void Cancel(GeneratorJob job)
    {
        job.Cancellation.Cancel();
        lock (sync) { job.Approval?.TrySetResult(false); Pulse(); }
        Notify();
    }
    private Task<EngineResult> Search(GeneratorJob job, Position p, int depth, Action<int>? progress, CancellationToken token)
    {
        var result = new TaskCompletionSource<EngineResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (sync) { pending.Add(new(job, p.Clone(), depth, progress, token, result)); Pulse(); }
        return result.Task.WaitAsync(token);
    }
    private async Task Run(GeneratorJob job)
    {
        try
        {
            var result = await new OpeningGenerator(engine).GenerateAsync(job.Start, job.Name, job.Options, count =>
            {
                TaskCompletionSource<bool> approval;
                lock (sync)
                {
                    job.State = GeneratorJobState.AwaitingApproval;
                    job.Message = count.ToString(); job.Approval = approval = new(TaskCreationOptions.RunContinuationsAsynchronously); Pulse();
                }
                Notify(); return approval.Task.WaitAsync(job.Cancellation.Token);
            }, new Reporter<GeneratorProgress>(p => { job.Message = p.Message; Notify(); }), job.Cancellation.Token, job.Setup,
            new Reporter<GeneratorGraphSnapshot>(g => { job.Graph = g; job.Forecast.Update(g); Notify(); }),
            new Reporter<GeneratorSearchDepth>(d => { job.SearchDepth = d; Notify(); }), new JobSearch(this, job));
            job.Result = result;
            job.State = job.Cancellation.IsCancellationRequested ? GeneratorJobState.Cancelled : GeneratorJobState.Completed;
        }
        catch (OperationCanceledException) { job.State = GeneratorJobState.Cancelled; job.Forecast.Stop(); }
        catch (Exception ex) { job.State = GeneratorJobState.Failed; job.Message = ex.Message; job.Forecast.Stop(); }
        finally
        {
            lock (sync) { Pulse(); }
            Notify(); Completed?.Invoke(job);
        }
    }
    private bool CanSchedule()
    {
        foreach (var request in pending.Where(r => r.Token.IsCancellationRequested || r.Job.Finished).ToArray())
        { pending.Remove(request); request.Result.TrySetCanceled(); }
        if (jobs.Any(j => j.PauseAllAtLimit && j.State == GeneratorJobState.AwaitingApproval)) { working.Stop(); return false; }
        if (!Preferences.Current.GeneratorRestEnabled) { restingUntil = null; working.Reset(); return true; }
        if (restingUntil is DateTime until)
        {
            if (DateTime.UtcNow < until) return false;
            restingUntil = null; working.Restart();
        }
        if (working.Elapsed.TotalMinutes >= Math.Clamp(Preferences.Current.GeneratorWorkMinutes, 1, 1440))
        {
            if (jobs.Sum(j => j.Active) == 0) restingUntil = DateTime.UtcNow.AddMinutes(Math.Clamp(Preferences.Current.GeneratorRestMinutes, 1, 120));
            return false;
        }
        if (pending.Count > 0) working.Start(); else if (jobs.All(j => j.Active == 0)) working.Stop();
        return true;
    }
    private async Task Worker(int index)
    {
        EnginePool? pool = null; int previousThreads = 0;
        try
        {
            while (!lifetime.IsCancellationRequested)
            {
                Request? request = null; Task changed; int threads = 1; bool timedWait;
                lock (sync)
                {
                    changed = wake.Task;
                    timedWait = restingUntil is not null || Preferences.Current.GeneratorRestEnabled;
                    if (CanSchedule())
                    {
                        var first = jobs.FirstOrDefault(j => !j.Finished && j.State != GeneratorJobState.AwaitingApproval && !j.Cancellation.IsCancellationRequested);
                        int limit = first?.Options.Workers ?? 0;
                        threads = first is null ? 1 : Math.Max(1, first.Options.Threads / first.Options.Workers);
                        if (index < limit)
                            request = jobs.Where(j => !j.Finished && j.State != GeneratorJobState.AwaitingApproval && !j.Cancellation.IsCancellationRequested)
                                .Select(j => pending.FirstOrDefault(r => r.Job == j)).FirstOrDefault(r => r is not null);
                        if (request is not null)
                        {
                            foreach (var other in jobs.Where(j => j != request.Job && j.Active == 0 && j.State == GeneratorJobState.Running)) other.State = GeneratorJobState.Queued;
                            pending.Remove(request); request.Job.Active++; request.Job.State = GeneratorJobState.Running;
                            totalWorking.Start();
                            request.Job.Forecast.SetWorking(true);
                        }
                    }
                }
                if (request is null)
                {
                    if (timedWait) await Task.WhenAny(changed, Task.Delay(1000, lifetime.Token));
                    else await changed.WaitAsync(lifetime.Token);
                    continue;
                }
                Notify();
                try
                {
                    if (searchOverride is null && (pool is null || previousThreads != threads))
                    {
                        if (pool is not null) await pool.DisposeAsync();
                        pool = new(engine, 1, threads, lifetime.Token); previousThreads = threads;
                    }
                    var result = searchOverride is not null
                        ? await searchOverride.Search(request.Position, request.Depth, request.Progress, request.Token)
                        : await pool!.Search(request.Position, request.Depth, cancellationToken: request.Token, depthProgress: request.Progress);
                    request.Result.TrySetResult(result);
                }
                catch (OperationCanceledException) { request.Result.TrySetCanceled(); }
                catch (Exception ex) { request.Result.TrySetException(ex); }
                finally
                {
                    lock (sync)
                    {
                        request.Job.Active--;
                        if (request.Job.Active == 0) request.Job.Forecast.SetWorking(false);
                        if (jobs.Concat(retired).All(j => j.Active == 0)) totalWorking.Stop();
                        Pulse();
                    }
                    Notify();
                }
            }
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        finally { if (pool is not null) await pool.DisposeAsync(); }
    }
    public async ValueTask DisposeAsync()
    {
        lock (sync)
        {
            if (disposed) return; disposed = true;
            foreach (var job in jobs) job.Cancellation.Cancel();
            lifetime.Cancel(); Pulse();
        }
        await Task.WhenAll(workers).ConfigureAwait(false);
        await Task.WhenAll(runs).ConfigureAwait(false);
        foreach (var job in jobs.Concat(retired)) job.Cancellation.Dispose();
        lifetime.Dispose();
    }
}
