namespace ChessOpeningTrainer;

/// <summary>Shared asynchronous position lookup. Owns its reader and bounded cache;
/// never starts engines, blocks board input, or loads the entire index.</summary>
public sealed class DatasetFrequencyBinding : IDisposable
{
    private readonly BoardControl board;
    private readonly DatasetResultsStrip strip;
    private readonly Func<bool> enabled;
    private readonly string directory;
    private readonly object sync = new();
    private DatasetIndex? reader;
    private string? indexPath, lastFen;
    private DateTime manifestStamp;
    private bool disposed;
    private int revision;
    public DatasetFrequencyBinding(BoardControl board, DatasetResultsStrip strip, Func<bool> enabled, string? dataDirectory = null)
    {
        this.board = board;
        this.strip = strip;
        this.enabled = enabled;
        directory = dataDirectory ?? Preferences.DataDirectory;
        board.PositionChanged += Refresh;
        board.Disposed += BoardDisposed;
    }
    private void BoardDisposed(object? sender, EventArgs e) => Dispose();
    public async void Refresh()
    {
        if (disposed || strip.IsDisposed)
            return;
        bool visible = enabled();
        strip.Visible = visible;
        if (!visible || board.Position is null)
        {
            lastFen = null;
            ++revision;
            return;
        }
        string manifest = Path.Combine(directory, "DataSets", "manifest.json");
        DateTime stamp = File.GetLastWriteTimeUtc(manifest);
        string fen = board.Position.ToFen();
        if (lastFen == fen && manifestStamp == stamp)
            return;
        lastFen = fen;
        int request = ++revision;
        var position = board.Position.Clone();
        strip.Counts = default;
        try
        {
            var counts = await Task.Run(() =>
            {
                lock (sync)
                {
                    if (disposed)
                        return default;
                    if (manifestStamp != stamp || reader is null)
                    {
                        manifestStamp = stamp;
                        var store = File.Exists(manifest) ? new DatasetStore(directory) : null;
                        string? next = store?.HasIndex == true ? store.IndexPath(store.Manifest.IndexFile!) : null;
                        if (next != indexPath || reader is null)
                        {
                            reader?.Dispose();
                            reader = null;
                            indexPath = next;
                            if (next is not null)
                                reader = new(next);
                        }
                    }
                    return reader?.Counts(position) ?? default;
                }
            });
            if (!disposed && !strip.IsDisposed && request == revision)
                strip.Counts = counts;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            if (!strip.IsDisposed && request == revision)
                strip.Counts = default;
        }
    }
    public void Dispose()
    {
        board.PositionChanged -= Refresh;
        board.Disposed -= BoardDisposed;
        lock (sync)
        {
            disposed = true;
            reader?.Dispose();
            reader = null;
        }
    }
}
