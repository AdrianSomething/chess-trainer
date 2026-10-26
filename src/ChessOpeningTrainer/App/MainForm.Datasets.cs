namespace ChessOpeningTrainer;

public sealed partial class MainForm
{
    private DatasetForm? datasetWindow;
    private void OpenDatasets()
    {
        if (datasetWindow is null || datasetWindow.IsDisposed)
        {
            try
            {
                datasetWindow = new(dataDir, engine);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException or InvalidDataException)
            {
                L.Message(this, "Datensatzkatalog konnte nicht gelesen werden: " + ex.Message, "Datensätze");
                return;
            }
            datasetWindow.Changed += () => { if (!IsDisposed && home.Visible) AddDatasetWarning(home); };
        }
        datasetWindow.Show();
        if (datasetWindow.WindowState == FormWindowState.Minimized)
            datasetWindow.WindowState = FormWindowState.Normal;
        datasetWindow.Activate();
    }
    private void AddDatasetWarning(Panel header)
    {
        foreach (var old in home.Controls.OfType<Label>().Where(l => l.Name == "dataset-stale-warning").ToArray()) old.Dispose();
        try
        {
            if (!new DatasetStore(dataDir).IsStale)
                return;
            var warning = new Label { Dock = DockStyle.Bottom, Height = 48, ForeColor = UiPalette.Orange, Padding = new(12, 4, 12, 4), Text = "Warnung: Datensatzbaum nicht aktuell. Wende deine Einstellungen im Datensatzmodus an, indem du die aktiven Datensätze neu komprimierst." };
            home.Controls.Add(warning);
            warning.Name = "dataset-stale-warning";
            warning.BringToFront();
        }
        catch (Exception ex) when (ex is IOException or System.Text.Json.JsonException or UnauthorizedAccessException or InvalidDataException)
        {
            header.Controls.Add(new Label { Text = L.T("Datensatzkatalog konnte nicht gelesen werden: ") + ex.Message, Dock = DockStyle.Bottom, Height = 24 });
        }
    }
}
