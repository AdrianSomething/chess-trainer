namespace ChessOpeningTrainer;

/// <summary>One window-owned serial game queue. Each game retains its parallel engine workers.</summary>
public sealed class GameAnalysisQueue : IDisposable
{
    private sealed class Job(PersonalGame game, CancellationToken lifetime, Task? prepared, Action? cancelPrepared)
    {
        public PersonalGame Game = game;
        public CancellationTokenSource Cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime);
        public TaskCompletionSource Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task? Prepared = prepared;
        public Action? CancelPrepared = cancelPrepared;
        public bool Running;
        public string Progress = "In Warteschlange";
    }
    private readonly object sync = new();
    private readonly Queue<Job> queue = [];
    private readonly Dictionary<string,Job> entries = [];
    private readonly Dictionary<string,string> messages = [];
    private readonly Func<PersonalGame,IProgress<string>,CancellationToken,Task> analyze;
    private readonly CancellationTokenSource lifetime = new();
    private bool pumping, disposed;
    public event Action<string>? Changed;
    public GameAnalysisQueue(Func<PersonalGame,IProgress<string>,CancellationToken,Task> analyze) => this.analyze = analyze;
    public bool IsPending(string id) { lock(sync) return entries.TryGetValue(id,out var j) && !j.Cancellation.IsCancellationRequested; }
    public bool IsRunning(string id) { lock(sync) return entries.TryGetValue(id,out var j) && j.Running && !j.Cancellation.IsCancellationRequested; }
    public int PendingCount { get {lock(sync) return entries.Values.Count(j=>!j.Cancellation.IsCancellationRequested);} }
    public string Describe(string id) { lock(sync) return entries.TryGetValue(id,out var j) ? j.Progress : messages.GetValueOrDefault(id,""); }
    public Task Enqueue(PersonalGame game,Task? prepared=null,Action? cancelPrepared=null)
    {
        Job job; bool start;
        lock(sync)
        {
            ObjectDisposedException.ThrowIf(disposed,this);
            if(entries.TryGetValue(game.Id,out var old) && !old.Cancellation.IsCancellationRequested) return old.Completion.Task;
            job=new(game,lifetime.Token,prepared,cancelPrepared); entries[game.Id]=job; queue.Enqueue(job);
            start=!pumping; pumping=true;
        }
        // One dedicated coordinator exists only while this queue has work. It
        // must not wait behind engine/generator work in the shared thread pool.
        // Engine searches themselves retain their bounded asynchronous pools.
        Changed?.Invoke(game.Id);
        if(start) _=Task.Factory.StartNew(Pump,CancellationToken.None,TaskCreationOptions.LongRunning,TaskScheduler.Default);
        return job.Completion.Task;
    }
    public void Cancel(string id)
    {
        Action? cancelPrepared=null;
        lock(sync) if(entries.TryGetValue(id,out var job)) { job.Cancellation.Cancel(); job.Progress="Abgebrochen / aus Warteschlange entfernt"; cancelPrepared=job.CancelPrepared; }
        try { cancelPrepared?.Invoke(); } catch(ObjectDisposedException) { }
        Changed?.Invoke(id);
    }
    private void Pump()
    {
        while(true)
        {
            Job job;
            lock(sync)
            {
                if(!queue.TryDequeue(out job!)) { pumping=false; return; }
                job.Running=true; job.Progress="Analyse läuft …";
            }
            Changed?.Invoke(job.Game.Id);
            try
            {
                var token=job.Cancellation.Token; token.ThrowIfCancellationRequested();
                if(job.Prepared is not null) WaitPrepared(job.Prepared,token);
                if(!job.Game.HasCompleteAnalysis)
                    analyze(job.Game,new ProgressRelay(s=>{ lock(sync) job.Progress=s; Changed?.Invoke(job.Game.Id); }),token).GetAwaiter().GetResult();
                lock(sync) job.Progress="Analyse gespeichert.";
            }
            catch(OperationCanceledException) { lock(sync) job.Progress="Analyse abgebrochen."; }
            catch(Exception ex) { lock(sync) job.Progress="Analyse nicht abgeschlossen: "+ex.Message; }
            finally
            {
                lock(sync)
                {
                    messages[job.Game.Id]=job.Progress;
                    if(entries.GetValueOrDefault(job.Game.Id)==job) entries.Remove(job.Game.Id);
                }
                job.Cancellation.Dispose(); job.Completion.TrySetResult(); Changed?.Invoke(job.Game.Id);
            }
        }
    }
    private static void WaitPrepared(Task prepared,CancellationToken token)
    {
        // Waiting on the original task signals this dedicated coordinator
        // directly. A WaitAsync proxy would need a shared-pool continuation
        // even when the local game's result is already ready.
        try { prepared.Wait(token); }
        catch(AggregateException) { prepared.GetAwaiter().GetResult(); throw; }
        prepared.GetAwaiter().GetResult();
    }
    private sealed class ProgressRelay(Action<string> report):IProgress<string> { public void Report(string value)=>report(value); }
    public void Dispose() { lock(sync) { if(disposed)return; disposed=true; lifetime.Cancel(); } }
}
