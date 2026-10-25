using System.Diagnostics;

namespace ChessOpeningTrainer;

/// <summary>Operation-owned pause gate. Cancellation and keeping a partial result
/// are distinct actions; elapsed work time excludes pauses.</summary>
public sealed class DatasetOperation : IDisposable
{
    private readonly ManualResetEventSlim running = new(true);
    private readonly Stopwatch elapsed = Stopwatch.StartNew();
    private readonly object sync = new();
    private int stopAndSave;
    private long lastDone, lastTotal;
    private double lastSeconds, secondsPerUnit;
    private readonly CancellationTokenSource stopTransfer = new();
    public CancellationToken StopTransferToken => stopTransfer.Token;
    public long PeakWorkspaceBytes
    {
        get; private set;
    }
    public long ObserveWorkspace(string path)
    {
        long bytes = 0;
        foreach (string file in new[] { path, path + "-journal", path + "-wal", path + "-shm" })
            if (File.Exists(file))
                bytes += new FileInfo(file).Length;
        PeakWorkspaceBytes = Math.Max(PeakWorkspaceBytes, bytes);
        return bytes;
    }
    internal long ObserveFiles(IEnumerable<string> paths)
    {
        long bytes = paths.Where(File.Exists).Sum(p => new FileInfo(p).Length);
        PeakWorkspaceBytes = Math.Max(PeakWorkspaceBytes, bytes);
        return bytes;
    }
    public bool PreserveCheckpoint
    {
        get; set;
    }
    public bool Paused => !running.IsSet;
    internal double ActiveSeconds { get { lock (sync) return elapsed.Elapsed.TotalSeconds; } }
    public bool SaveRequested => Volatile.Read(ref stopAndSave) != 0;
    public void TogglePause()
    {
        lock (sync)
        {
            if (running.IsSet)
            {
                running.Reset();
                elapsed.Stop();
            }
            else
            {
                elapsed.Start();
                running.Set();
            }
        }
    }
    public void StopAndSave()
    {
        Interlocked.Exchange(ref stopAndSave, 1);
        running.Set();
        stopTransfer.Cancel();
    }
    public void Wait(CancellationToken token)
    {
        running.Wait(token);
        token.ThrowIfCancellationRequested();
    }
    public string Timing(long done, long total)
    {
        double seconds, remainingSeconds;
        lock (sync)
        {
            seconds = elapsed.Elapsed.TotalSeconds;
            if (total != lastTotal || done < lastDone)
            {
                lastDone = 0; lastSeconds = 0; secondsPerUnit = 0;
            }
            if (done > lastDone && seconds - lastSeconds >= .5)
            {
                double recent = (seconds - lastSeconds) / (done - lastDone);
                secondsPerUnit = secondsPerUnit == 0 ? recent : .65 * secondsPerUnit + .35 * recent;
                lastDone = done; lastSeconds = seconds; lastTotal = total;
            }
            remainingSeconds = Math.Max(0, total - done) * (secondsPerUnit > 0 ? secondsPerUnit : seconds / Math.Max(1, done));
        }
        static string Duration(double value)
        {
            var time = TimeSpan.FromSeconds(Math.Min(8640000, value));
            return $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}";
        }
        string remaining = done > 0 && total > done ? Duration(remainingSeconds) : done >= total && total > 0 ? "00:00:00" : "…";
        return L.T("Vergangen") + ": " + Duration(seconds) + " · " + L.T("Verbleibend") + ": " + remaining;
    }
    public void Dispose()
    {
        running.Dispose();
        stopTransfer.Dispose();
    }
}
