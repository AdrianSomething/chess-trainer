using System.Text.Json;

namespace ChessOpeningTrainer;

/// <summary>One window owns mutable catalog operations. Readers keep the last complete index.</summary>
public sealed class DatasetForm : LocalizedForm
{
    private readonly DatasetStore store;
    private readonly StockfishEngine engine;
    private readonly Panel body = new() { Dock = DockStyle.Fill };
    private readonly Label status = new() { Dock = DockStyle.Bottom, Height = 48, Padding = new(12, 6, 12, 4) };
    private readonly Button cancel;
    private CancellationTokenSource? operation;
    private bool closeAfter;
    private readonly ToolTip tips = new();
    private CancellationTokenSource? forecast;
    private Label? forecastLabel;
    private Button? buildButton;
    private Button? viewTreeButton;
    private readonly System.Windows.Forms.Timer forecastDelay = new() { Interval = 350 };
    private string QueuePath => Path.Combine(store.DirectoryPath, "download-queue.json");
    public event Action? Changed;
    public DatasetForm(string dataDirectory, StockfishEngine engine)
    {
        store = new(dataDirectory);
        this.engine = engine;
        Text = "Datensätze";
        Size = new(1120, 790);
        MinimumSize = new(870, 600);
        BackColor = UiPalette.FromArgb(31, 35, 39);
        ForeColor = UiPalette.Gainsboro;
        cancel = DatasetControls.Button("Vorgang anhalten", () => operation?.Cancel());
        cancel.Dock = DockStyle.Bottom;
        cancel.Visible = false;
        Controls.AddRange([body, cancel, status]);
        Render();
        forecastDelay.Tick += async (_, _) => { forecastDelay.Stop(); await UpdateForecast(); };
        Shown += (_, _) => forecastDelay.Start();
        FormClosing += (_, e) =>
        {
            if (operation is null)
                return;
            e.Cancel = true;
            if (closeAfter || L.Message(this, "Vorgang anhalten und schließen? Downloads können später fortgesetzt werden; unvollständige Imports und Indizes werden verworfen.", "Datensätze", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                closeAfter = true;
                operation.Cancel();
            }
        };
    }
    private void ClearBody()
    {
        foreach (Control c in body.Controls.Cast<Control>().ToArray())
            c.Dispose();
    }
    private void Render()
    {
        forecast?.Cancel();
        forecastDelay.Stop();
        forecastLabel = null;
        viewTreeButton = null;
        buildButton = null;
        ClearBody();
        var side = DatasetControls.Column();
        side.Dock = DockStyle.Left;
        side.Width = 260;
        side.Controls.Add(DatasetControls.Button("Von Lichess herunterladen", ChooseDownload));
        side.Controls.Add(DatasetControls.Button("Datensätze importieren", ChooseImport));
        side.Controls.Add(DatasetControls.Button("Einstellungen", SettingsForm.OpenDatasets));
        if (File.Exists(QueuePath) || File.Exists(DatasetDownloader.Journal(store.DirectoryPath)))
            side.Controls.Add(DatasetControls.Button("Download fortsetzen", () => _ = Run(DownloadQueue)));
        if (store.HasIndex)
        {
            viewTreeButton = DatasetControls.Button((store.IsStale ? "(alt) " : "") + "Datensatzbaum anzeigen", ShowGraph);
            side.Controls.Add(viewTreeButton);
        }
        if (store.Manifest.Entries.Count == 0)
        {
            side.Dock = DockStyle.None;
            body.Controls.Add(side);
            void Center()
            {
                side.SetBounds(Math.Max(0, (body.Width - 260) / 2), Math.Max(0, (body.Height - 250) / 2), 260, 250);
            }
            body.Resize += (_, _) => { if (!side.IsDisposed) Center(); };
            Center();
            return;
        }
        var right = new Panel { Dock = DockStyle.Fill };
        var list = DatasetControls.Column();
        right.Controls.Add(list);
        body.Controls.Add(right);
        body.Controls.Add(side);
        var thresholdLabel = new Label { Text = "Fortsetzungen beenden bei höchstens … Partien (0 = aus)", Width = 230, Height = 50 };
        var threshold = new ScrollNumber { Minimum = 0, Maximum = 1000000, Value = Math.Clamp(store.Manifest.StopAtGames, 0, 1000000), Width = 230, EmptyDefaultValue = 0 };
        threshold.EnterCommitted = _ => side.Focus();
        L.SetToolTip(tips, threshold, "Die Stellung und ihre exakten Ergebniszahlen bleiben erhalten. Bei höchstens dieser Partienzahl entfallen ihre weiteren Züge. 0 erhält alle Tiefen; beispielsweise 5 spart seltene Fortsetzungen aus.");
        threshold.ValueChanged += (_, _) => { store.Manifest.StopAtGames = (int)threshold.Value; store.Save(); RefreshBuild(); Changed?.Invoke(); forecast?.Cancel(); forecastDelay.Stop(); forecastDelay.Start(); };
        side.Controls.AddRange([thresholdLabel, threshold]);
        forecastLabel = new Label { Width = 230, Height = 140, Text = "Dateigröße wird geschätzt …", Margin = new(4, 15, 4, 4) };
        side.Controls.Add(forecastLabel);
        var build = DatasetControls.Button("Aktive Datensätze zu Branch komprimieren", () => _ = Run(async (progress, token) => { await Task.Run(() => DatasetIndex.Build(store, Preferences.Current.Datasets, progress, token), token); }), 560);
        buildButton = build;
        build.Dock = DockStyle.Bottom;
        build.Visible = store.IsStale && store.Manifest.Entries.Any(e => e.Active);
        right.Controls.Add(build);
        build.BringToFront();
        foreach (var entry in store.Manifest.Entries)
        {
            var card = new Panel { Width = Math.Max(520, body.Width - 300), Height = 125, BackColor = UiPalette.FromArgb(43, 49, 55), Margin = new(4, 5, 4, 8) };
            var name = new TextBox { Text = entry.Name, Left = 12, Top = 10, Width = card.Width - 155, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            void SaveName()
            {
                string text = name.Text.Trim();
                if (text.Length > 0 && text != entry.Name)
                {
                    entry.Name = text;
                    store.Save();
                }
            }
            name.Leave += (_, _) => SaveName();
            name.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { SaveName(); card.Focus(); e.SuppressKeyPress = true; } };
            var active = new CheckBox { Text = "Aktiver Datensatz", Checked = entry.Active, Left = 12, Top = 84, Width = 250 };
            active.CheckedChanged += (_, _) => { entry.Active = active.Checked; store.Save(); RefreshBuild(); Changed?.Invoke(); forecast?.Cancel(); forecastDelay.Stop(); forecastDelay.Start(); };
            long bytes = File.Exists(store.PathFor(entry)) ? new FileInfo(store.PathFor(entry)).Length : 0;
            var info = new Label { Text = $"{entry.Games:N0} " + L.T("Partien") + $" · {bytes / 1e6:0.0} MB · {entry.Skipped:N0} " + L.T("übersprungen"), Left = 12, Top = 50, Width = card.Width - 160, Height = 30 };
            var delete = DatasetControls.Button("Löschen", () => { if (L.Message(this, "Diesen importierten Datensatz endgültig löschen? Die ursprüngliche Importdatei bleibt erhalten.", entry.Name, MessageBoxButtons.YesNo) == DialogResult.Yes) { store.Delete(entry); Changed?.Invoke(); Render(); } }, 120);
            delete.SetBounds(card.Width - 135, 10, 120, 40);
            delete.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            var trim = DatasetControls.Button("Zuschneiden", () => Trim(entry), 120);
            trim.SetBounds(card.Width - 135, 60, 120, 40);
            trim.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            card.Controls.AddRange([name, info, active, delete, trim]);
            list.Controls.Add(card);
        }
        list.Resize += (_, _) => { foreach (Control card in list.Controls) card.Width = Math.Max(520, list.ClientSize.Width - 35); };
        if (IsHandleCreated)
            forecastDelay.Start();
    }
    private void RefreshBuild()
    {
        if (buildButton is { IsDisposed: false })
            buildButton.Visible = store.IsStale && store.Manifest.Entries.Any(e => e.Active);
        if (viewTreeButton is { IsDisposed: false })
            viewTreeButton.Text = (store.IsStale ? "(alt) " : "") + "Datensatzbaum anzeigen";
    }
    private async Task UpdateForecast()
    {
        forecast?.Cancel();
        var cts = new CancellationTokenSource();
        forecast = cts;
        var target = forecastLabel;
        if (target is null || target.IsDisposed)
        {
            forecast = null;
            cts.Dispose();
            return;
        }
        var active = store.Manifest.Entries.Where(e => e.Active).ToArray();
        int cutoff = store.Manifest.StopAtGames;
        target.Text = L.T("Dateigröße wird geschätzt …");
        try
        {
            var estimate = await Task.Run(() => DatasetForecast.Estimate(store.DirectoryPath, active, cutoff, Preferences.Current.Datasets, cts.Token), cts.Token);
            if (IsDisposed || target.IsDisposed || cts.IsCancellationRequested)
                return;
            target.Text = L.T($"Baum: ca. {estimate.LowerBytes / 1e6:0.0}–{estimate.UpperBytes / 1e6:0.0} MB\nTemporärer Arbeitsraum: ca. {estimate.TemporaryBytes / 1e6:0.0} MB\nStichprobe: {estimate.SampleGames} Partien. Seltene Verläufe machen diese Schätzung unsicher.");
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { if (!target.IsDisposed) target.Text = L.T("Schätzung nicht verfügbar: ") + ex.GetBaseException().Message; }
        finally { if (forecast == cts) forecast = null; cts.Dispose(); }
    }
    private void ChooseImport()
    {
        using var dialog = new OpenFileDialog { Multiselect = true, Filter = "Chess datasets (*.pgn;*.zst;*.gz;*.zip)|*.pgn;*.zst;*.gz;*.zip" };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        string[] paths = dialog.FileNames;
        _ = Run(async (progress, token) => { foreach (string path in paths) { var entry = await Task.Run(() => store.Import(path, Preferences.Current.Datasets, progress, token), token); store.Register(entry); } });
    }
    private void ChooseDownload()
    {
        using var dialog = new DatasetCatalogForm(new LichessDatasetSource());
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        var queue = ReadQueue();
        queue.AddRange(dialog.Selection.Where(o => !queue.Any(q => q.Url == o.Url)));
        SaveQueue(queue);
        _ = Run(DownloadQueue);
    }
    private List<DatasetDownloadOption> ReadQueue() => File.Exists(QueuePath) ? JsonSerializer.Deserialize<List<DatasetDownloadOption>>(File.ReadAllText(QueuePath)) ?? [] : [];
    private void SaveQueue(List<DatasetDownloadOption> queue)
    {
        File.WriteAllText(QueuePath + ".tmp", JsonSerializer.Serialize(queue));
        File.Move(QueuePath + ".tmp", QueuePath, true);
    }
    private async Task DownloadQueue(IProgress<string> progress, CancellationToken token)
    {
        var queue = ReadQueue();
        string journal = DatasetDownloader.Journal(store.DirectoryPath);
        var pending = File.Exists(journal) ? JsonSerializer.Deserialize<DatasetDownloadJob>(File.ReadAllText(journal)) : null;
        if (pending is not null && !queue.Any(o => o.Url == pending.Option.Url))
            queue.Insert(0, pending.Option);
        while (queue.Count > 0)
        {
            token.ThrowIfCancellationRequested();
            var option = queue[0];
            var job = pending?.Option.Url == option.Url ? pending : new DatasetDownloadJob { Option = option, Sha256 = await new LichessDatasetSource().Checksum(option, token) };
            string path = await DatasetDownloader.Download(store.DirectoryPath, job, progress, token);
            var entry = await Task.Run(() => store.Import(path, Preferences.Current.Datasets, progress, token), token);
            store.Register(entry);
            queue.RemoveAt(0);
            SaveQueue(queue);
            pending = null;
            // Only this owned, verified download is removed, never a user import.
            File.Delete(path);
        }
        File.Delete(QueuePath);
    }
    private void Trim(DatasetEntry entry)
    {
        using var dialog = new DatasetTrimForm(entry, engine);
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        var filter = dialog.Filter;
        bool replace = dialog.ReplaceOriginal;
        _ = Run(async (progress, token) => { var result = await Task.Run(() => store.Trim(entry, filter, Preferences.Current.Datasets, progress, token), token); store.Register(result); if (replace) store.Delete(entry); });
    }
    private async Task Run(Func<IProgress<string>, CancellationToken, Task> action)
    {
        if (operation is not null)
            return;
        using var cts = new CancellationTokenSource();
        operation = cts;
        body.Enabled = false;
        cancel.Visible = true;
        forecast?.Cancel();
        forecastDelay.Stop();
        status.Text = L.T("Vorgang läuft …");
        try
        {
            await action(new Progress<string>(s => { if (!IsDisposed) status.Text = s; }), cts.Token);
            status.Text = L.T("Abgeschlossen.");
        }
        catch (OperationCanceledException) { status.Text = L.T("Angehalten. Der bisherige Datensatzbaum bleibt erhalten."); }
        catch (Exception ex) { status.Text = L.T("Fehler: ") + ex.GetBaseException().Message; }
        finally { operation = null; if (!IsDisposed) { body.Enabled = true; cancel.Visible = false; Changed?.Invoke(); Render(); if (closeAfter) Close(); } }
    }
    private void ShowGraph()
    {
        forecast?.Cancel();
        forecastDelay.Stop();
        ClearBody();
        var view = new DatasetGraphView(store.IndexPath(store.Manifest.IndexFile!), engine) { Dock = DockStyle.Fill };
        view.BackRequested += Render;
        body.Controls.Add(view);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            forecastDelay.Dispose();
            forecast?.Cancel();
            operation?.Cancel();
            tips.Dispose();
        }
        base.Dispose(disposing);
    }
}
