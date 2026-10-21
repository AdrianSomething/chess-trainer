using System.Diagnostics;
using System.Threading.Channels;

namespace ChessOpeningTrainer;
// A bounded set of persistent UCI processes. The queue is shared; a free worker
// immediately takes the next independent position without restarting Stockfish.
public sealed class EnginePool : IAsyncDisposable
{
    private sealed record Job(Position Position, int Depth, TaskCompletionSource<EngineResult> Completion, int Multi, CancellationToken Cancellation, Action<int>? DepthProgress);
    private readonly Channel<Job> jobs = Channel.CreateUnbounded<Job>(new() { SingleWriter = false });
    private readonly CancellationTokenSource lifetime;
    private readonly Task[] workers;
    private int active;
    private int disposed;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<int, byte> processIds = [];
    public int ActiveSearches => Volatile.Read(ref active);
    public IReadOnlyCollection<int> ProcessIds => processIds.Keys.ToArray();

    public EnginePool(StockfishEngine engine, int count, int threads, CancellationToken token)
    {
        lifetime = CancellationTokenSource.CreateLinkedTokenSource(token);
        workers = Enumerable.Range(0, engine.Budget is null ? count : 1).Select(_ => Task.Run(() => Worker(engine, threads))).ToArray();
    }

    public Task<EngineResult> Search(Position p, int depth, int multi = 0, CancellationToken cancellationToken = default, Action<int>? depthProgress = null)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<EngineResult>(cancellationToken);
        var completion = new TaskCompletionSource<EngineResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!jobs.Writer.TryWrite(new(p.Clone(), depth, completion, multi, cancellationToken, depthProgress)))
            completion.SetException(new IOException("Enginepool geschlossen."));
        return completion.Task;
    }

    private async Task Worker(StockfishEngine engine, int threads)
    {
        IDisposable? slot = null;
        Process? process = null;
        CancellationTokenRegistration registration = default;
        var token = lifetime.Token;
        async Task Send(string command)
        {
            await process!.StandardInput.WriteLineAsync(command);
            await process.StandardInput.FlushAsync();
        }

        async Task Wait(string expected)
        {
            while (await process!.StandardOutput.ReadLineAsync(token)is string line)
                if (line == expected)
                    return;
            throw new IOException("Engine beendet.");
        }

        void Stop()
        {
            registration.Dispose();
            if (process is null)
                return;
            try
            {
                processIds.TryRemove(process.Id, out _);
                if (!process.HasExited)
                    process.Kill(true);
            }
            catch
            {
            }

            process.Dispose();
            process = null;
        }

        async Task Start()
        {
            if (engine.Path is null)
                throw new IOException("Stockfish fehlt");
            process = new()
            {
                StartInfo = new(engine.Path)
                {
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            // Keep the UI/audio responsive when many background engines compete.
            try { process.PriorityClass = ProcessPriorityClass.BelowNormal; }
            catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException) { }
            processIds[process.Id] = 0;
            registration = token.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                        process.Kill(true);
                }
                catch
                {
                }
            });
            await Send("uci");
            await Wait("uciok");
            await Send("setoption name Threads value " + Math.Clamp(threads, 1, Environment.ProcessorCount));
            await Send("setoption name Hash value 256");
            await Send("isready");
            await Wait("readyok");
        }

        async Task<EngineResult> Analyze(Position p, int depth, int multi, string? move = null, Action<int>? depthProgress = null)
        {
            await Send("setoption name MultiPV value " + multi);
            await Send("isready");
            await Wait("readyok");
            await Send("position fen " + p.ToFen());
            await Send("go depth " + depth + (move is null ? "" : " searchmoves " + move));
            var lines = new Dictionary<int, EngineLine>();
            string? best = null;
            int reportedDepth = 0;
            while (await process!.StandardOutput.ReadLineAsync(token)is string line)
            {
                if (line.StartsWith("bestmove "))
                {
                    best = line.Split(' ')[1];
                    break;
                }

                if (!line.StartsWith("info ") || !line.Contains(" pv "))
                    continue;
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                int Value(string key, int fallback = 0)
                {
                    int i = Array.IndexOf(parts, key);
                    return i >= 0 && i + 1 < parts.Length && int.TryParse(parts[i + 1], out int value) ? value : fallback;
                }

                int pi = Array.IndexOf(parts, "pv"), si = Array.IndexOf(parts, "score");
                if (si < 0 || pi < 0 || si + 2 >= parts.Length || pi + 1 >= parts.Length || !int.TryParse(parts[si + 2], out int score))
                    continue;
                int rank = Value("multipv", 1);
                int reachedDepth = Value("depth");
                if (reachedDepth > reportedDepth)
                {
                    reportedDepth = reachedDepth;
                    depthProgress?.Invoke(reachedDepth);
                }
                lines[rank] = new()
                {
                    Rank = rank,
                    Depth = reachedDepth,
                    BestMove = parts[pi + 1],
                    Pv = parts[(pi + 1)..].ToList(),
                    ScoreCentipawns = parts[si + 1] == "cp" ? score : null,
                    Mate = parts[si + 1] == "mate" ? score : null
                };
            }

            token.ThrowIfCancellationRequested();
            if (best is null)
                throw new IOException("Keine Engineantwort");
            var first = lines.GetValueOrDefault(1);
            return new()
            {
                BestMove = best,
                ScoreCentipawns = first?.ScoreCentipawns,
                Mate = first?.Mate,
                Lines = lines.Values.OrderBy(l => l.Rank).ToList()
            };
        }

        try
        {
            try
            {
                if (engine.Budget is null) await Start();
            }
            catch when (!token.IsCancellationRequested)
            {
                Stop();
            }

            await foreach (var job in jobs.Reader.ReadAllAsync(token))
            {
                // Obsolete quick previews must not delay a newer board position.
                if (job.Cancellation.IsCancellationRequested)
                {
                    job.Completion.TrySetCanceled(job.Cancellation);
                    continue;
                }

                Interlocked.Increment(ref active);
                try
                {
                    // A waiting pool must never occupy a generation slot. Otherwise
                    // idle pools can starve every other puzzle source indefinitely.
                    if (engine.Budget is not null)
                    {
                        using var waiting = CancellationTokenSource.CreateLinkedTokenSource(token, job.Cancellation);
                        slot = await engine.Budget.Acquire(waiting.Token);
                        threads = engine.Budget.Threads;
                    }
                    if (process is null)
                        await Start();
                    var legal = MoveGenerator.GenerateLegal(job.Position);
                    if (legal.Count == 0)
                    {
                        job.Completion.TrySetResult(new());
                        continue;
                    }

                    if (job.Multi > 0)
                    {
                        job.Completion.TrySetResult(await Analyze(job.Position, job.Depth, Math.Min(job.Multi, legal.Count), depthProgress: job.DepthProgress));
                        continue;
                    }

                    var r = await Analyze(job.Position, job.Depth, legal.Count, depthProgress: job.DepthProgress);
                    var lines = r.Lines.Where(l => l.BestMove is not null).ToDictionary(l => l.BestMove!);
                    foreach (var move in legal)
                        if (!lines.TryGetValue(move.Uci, out var line) || line.Depth < job.Depth && line.Mate is null)
                        {
                            var one = await Analyze(job.Position, job.Depth, 1, move.Uci, job.DepthProgress);
                            var checkedLine = one.Lines.FirstOrDefault();
                            if (checkedLine is null || checkedLine.Depth < job.Depth && checkedLine.Mate is null)
                                throw new IOException("Unvollständige Engine-Tiefensuche.");
                            lines[move.Uci] = checkedLine;
                        }

                    var sorted = lines.Values.OrderByDescending(OpeningGenerator.Score).ToList();
                    job.Completion.TrySetResult(new() { BestMove = sorted[0].BestMove, ScoreCentipawns = sorted[0].ScoreCentipawns, Mate = sorted[0].Mate, Lines = sorted });
                }
                catch (OperationCanceledException)
                {
                    job.Completion.TrySetCanceled(token);
                }
                catch (Exception ex)
                {
                    job.Completion.TrySetException(ex);
                    Stop();
                }
                finally
                {
                    if (engine.Budget is not null)
                    {
                        Stop();
                        slot?.Dispose();
                        slot = null;
                    }
                    Interlocked.Decrement(ref active);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception)when (token.IsCancellationRequested)
        {
        // Killing a starting UCI process can surface a closed pipe instead of OCE.
        // Cancellation is expected here; genuine non-cancellation faults still propagate.
        }
        finally
        {
            Stop();
            slot?.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
            return;
        lifetime.Cancel();
        jobs.Writer.TryComplete();
        while (jobs.Reader.TryRead(out var job))
            job.Completion.TrySetCanceled(lifetime.Token);
        // Shutdown can be synchronously awaited by the entry point. Never marshal
        // this infrastructure-only continuation back onto the blocked UI thread.
        await Task.WhenAll(workers).ConfigureAwait(false);
        lifetime.Dispose();
    }
}
