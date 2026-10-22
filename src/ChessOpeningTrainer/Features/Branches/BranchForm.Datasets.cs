namespace ChessOpeningTrainer;

public sealed partial class BranchForm
{
    private readonly DatasetResultsStrip datasetResults = new();
    private string? datasetIndexPath;
    private int datasetQuery;
    private readonly object datasetReaderLock = new();
    private DatasetIndex? datasetReader;
    private string? datasetReaderPath;
    private void InitializeDatasetStats(Panel right)
    {
        datasetResults.SetBounds(12, 390, 385, 65);
        datasetResults.Visible = false;
        right.Controls.Add(datasetResults);
        void RefreshIndex()
        {
            try
            {
                var store = new DatasetStore(originalProgress.DataDirectory);
                datasetIndexPath = store.HasIndex ? store.IndexPath(store.Manifest.IndexFile!) : null;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException or InvalidDataException) { datasetIndexPath = null; }
            if (board.Position is { } position)
                UpdateDatasetStats(position);
        }
        Activated += (_, _) => RefreshIndex();
        RefreshIndex();
        FormClosed += (_, _) => { lock (datasetReaderLock) { datasetReader?.Dispose(); datasetReader = null; } };
    }
    private async void UpdateDatasetStats(Position position)
    {
        int revision = ++datasetQuery;
        string? path = datasetIndexPath;
        datasetResults.Visible = branchMode == BranchMode.Learning && path is not null;
        LayoutModeSidebar();
        if (!datasetResults.Visible || path is null)
            return;
        var snapshot = position.Clone();
        datasetResults.Counts = default;
        try
        {
            var counts = await Task.Run(() =>
            {
                lock (datasetReaderLock)
                {
                    if (IsDisposed || Disposing)
                        return default;
                    if (datasetReader is null || datasetReaderPath != path)
                    {
                        datasetReader?.Dispose();
                        datasetReader = new DatasetIndex(path);
                        datasetReaderPath = path;
                    }
                    return datasetReader.Counts(snapshot);
                }
            });
            if (!IsDisposed && revision == datasetQuery)
                datasetResults.Counts = counts;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or Microsoft.Data.Sqlite.SqliteException) { if (!IsDisposed && revision == datasetQuery) datasetResults.Visible = false; }
    }
}
