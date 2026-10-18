using System.Threading.Channels;

namespace ChessOpeningTrainer;

/// <summary>Bounded continuous CPU preparation, cancelled with its owning training window.</summary>
public sealed class VisionBuffer<T> : IDisposable
{
    private readonly Channel<T> white = Channel.CreateBounded<T>(15), black = Channel.CreateBounded<T>(15);
    private readonly CancellationTokenSource lifetime;
    private int nextSide, disposed;
    public VisionBuffer(Func<bool, CancellationToken, T> generate, Func<T, string> key, CancellationToken token)
    {
        lifetime = CancellationTokenSource.CreateLinkedTokenSource(token);
        var ct = lifetime.Token;
        _ = Task.Run(async () =>
        {
            var used = new HashSet<string>();
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    bool side = white.Reader.Count <= black.Reader.Count;
                    var queue = side ? white : black;
                    if (queue.Reader.Count >= 15) { await Task.Delay(100, ct); continue; }
                    T item;
                    do { ct.ThrowIfCancellationRequested(); item = generate(side, ct); } while (!used.Add(key(item)));
                    await queue.Writer.WriteAsync(item, ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
            catch (Exception ex) { white.Writer.TryComplete(ex); black.Writer.TryComplete(ex); }
            finally { white.Writer.TryComplete(); black.Writer.TryComplete(); }
        }, CancellationToken.None);
    }
    public Task<T> Next(CancellationToken token, bool? desiredSide = null)
    {
        bool side = desiredSide ?? Interlocked.Increment(ref nextSide) % 2 == 1;
        return (side ? white : black).Reader.ReadAsync(token).AsTask();
    }
    public void Dispose() { if (Interlocked.Exchange(ref disposed, 1) == 0) { lifetime.Cancel(); lifetime.Dispose(); } }
}
