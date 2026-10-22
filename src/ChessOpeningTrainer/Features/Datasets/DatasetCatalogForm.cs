namespace ChessOpeningTrainer;

public sealed class DatasetCatalogForm : LocalizedForm
{
    private readonly IDatasetDownloadSource source;
    private readonly CancellationTokenSource lifetime = new();
    private readonly ListView list = new() { Dock = DockStyle.Fill, View = View.Details, CheckBoxes = true, FullRowSelect = true, HideSelection = false };
    private readonly Label totals = new() { Dock = DockStyle.Bottom, Height = 38 };
    public IReadOnlyList<DatasetDownloadOption> Selection { get; private set; } = [];
    public DatasetCatalogForm(IDatasetDownloadSource source)
    {
        this.source = source;
        Text = "Lichess-Datenbank";
        Size = new(750, 650);
        MinimumSize = new(600, 400);
        BackColor = UiPalette.FromArgb(31, 35, 39);
        ForeColor = UiPalette.Gainsboro;
        list.BackColor = UiPalette.FromArgb(43, 49, 55);
        list.ForeColor = ForeColor;
        list.Columns.Add(L.T("Zeitraum"), 240);
        list.Columns.Add(L.T("Downloadgröße (ca.)"), 185);
        list.Columns.Add(L.T("Partien"), 180);
        var download = DatasetControls.Button("Von Lichess herunterladen", () => { Selection = list.CheckedItems.Cast<ListViewItem>().Select(i => (DatasetDownloadOption)i.Tag!).ToArray(); if (Selection.Count > 0) { DialogResult = DialogResult.OK; Close(); } });
        download.Dock = DockStyle.Bottom;
        Controls.AddRange([list, totals, download]);
        list.ItemChecked += (_, _) => { var items = list.CheckedItems.Cast<ListViewItem>().Select(i => (DatasetDownloadOption)i.Tag!).ToArray(); totals.Text = $"{items.Length} · {items.Sum(i => i.Bytes) / 1e9:0.000} GB · {items.Sum(i => i.Games):N0} " + L.T("Partien"); download.Enabled = items.Length > 0; };
        Shown += async (_, _) => await LoadCatalog();
        FormClosed += (_, _) => lifetime.Cancel();
    }
    private async Task LoadCatalog()
    {
        totals.Text = L.T("Lichess-Katalog wird geladen …");
        try
        {
            var options = await source.List(lifetime.Token);
            if (IsDisposed)
                return;
            list.BeginUpdate();
            foreach (var option in options)
            {
                var item = new ListViewItem([option.Name, $"{option.Bytes / 1e9:0.000} GB", option.Games.ToString("N0")]) { Tag = option };
                list.Items.Add(item);
            }
            list.EndUpdate();
            totals.Text = L.T("Mehrere Monate können ausgewählt werden. Standard-Schach, gewertete Partien, CC0.");
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { if (!IsDisposed) totals.Text = ex.Message; }
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            lifetime.Cancel();
            lifetime.Dispose();
        }
        base.Dispose(disposing);
    }
}
