using System.Text.Json;

namespace ChessOpeningTrainer;

/// <summary>One window owns mutable catalog operations. Readers keep the last complete index.</summary>
public sealed class DatasetForm : LocalizedForm
{
    private readonly DatasetStore store;
    private readonly StockfishEngine engine;
    private readonly Panel body = new() { Dock = DockStyle.Fill };
    private readonly DatasetActivityLabel status = new() { Dock = DockStyle.Bottom, Height = 100, Padding = new(32, 6, 12, 4) };
    private enum WorkKind { Index, Import, Download, Filter }
    private WorkKind workKind;
    private readonly Button cancel;
    private readonly Button pause;
    private readonly Button savePartial;
    private readonly FlowLayoutPanel operationButtons = new() { Dock = DockStyle.Bottom, Height = 50, Visible = false };
    private DatasetOperation? job;
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
        cancel = DatasetControls.Button("Abbrechen", () => { if (job is not null) job.PreserveCheckpoint = false; operation?.Cancel(); }, 155);
        pause = DatasetControls.Button("Pause", () => { job?.TogglePause(); pause!.Text = L.T(job?.Paused == true ? "Fortsetzen" : "Pause"); }, 155);
        savePartial = DatasetControls.Button("Stoppen und speichern", () => job?.StopAndSave(), 225);
        operationButtons.Controls.AddRange([pause, cancel, savePartial]);
        Controls.AddRange([body, operationButtons, status]);
        _ = new InputFocusCommit(this, includeTextBoxes: true);
        Render();
        forecastDelay.Tick += async (_, _) => { forecastDelay.Stop(); await UpdateForecast(); };
        Shown += (_, _) => forecastDelay.Start();
        FormClosing += (_, e) =>
        {
            if (operation is null)
                return;
            e.Cancel = true;
            string question = workKind switch
            {
                WorkKind.Import => "Import speichern und Fenster schließen? Du kannst ihn später mit „Import fortsetzen“ fortführen.",
                WorkKind.Download => "Download und Import anhalten und schließen? Der Fortschritt bleibt zum späteren Fortsetzen gespeichert.",
                WorkKind.Filter => "Zuschneiden abbrechen und schließen? Der unvollständige neue Datensatz wird verworfen; das Original bleibt erhalten.",
                _ => "Baumerstellung abbrechen und schließen? Der bisherige Baum und deine Datensätze bleiben erhalten. Der Neubau muss später neu gestartet werden."
            };
            if (closeAfter || L.Message(this, question, "Datensätze", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                closeAfter = true;
                if (job is not null)
                    job.PreserveCheckpoint = true;
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
        var heading = new Panel { Dock = DockStyle.Top, Height = 65 };
        heading.Controls.Add(new Label { Text = L.T("Datensätze"), Font = new Font(Font.FontFamily, 24, FontStyle.Bold), AutoSize = true, Left = 16, Top = 14 });
        var settings = new MenuIconButton("settings") { Width = 38, Height = 38, Top = 10, AccessibleName = L.T("Einstellungen") };
        settings.Click += (_, _) => SettingsForm.OpenDatasets();
        heading.Controls.Add(settings);
        heading.Resize += (_, _) => settings.Left = heading.Width - 50;
        var content = new Panel { Dock = DockStyle.Fill };
        body.Controls.AddRange([content, heading]);
        void OpenFolder() => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(store.DirectoryPath) { UseShellExecute = true });
        if (store.Manifest.Entries.Count == 0 && DatasetImportJob.PendingSource(store) is null && !File.Exists(QueuePath))
        {
            var hub = DatasetControls.Column();
            var download = new ModeMenuButton("dataset-download", L.T("Von Lichess herunterladen"));
            var import = new ModeMenuButton("dataset-import", L.T("Datensätze importieren"));
            download.Click += (_, _) => ChooseDownload();
            import.Click += (_, _) => ChooseImport();
            hub.Controls.AddRange([download, import]);
            content.Controls.Add(hub);
            hub.SizeChanged += (_, _) => ModeHubLayout.Center(hub);
            ModeHubLayout.Center(hub);
            return;
        }
        var side = DatasetControls.Column();
        side.Dock = DockStyle.Left;
        side.Width = 280;
        side.Controls.Add(DatasetControls.Button("Von Lichess herunterladen", ChooseDownload, 245));
        side.Controls.Add(DatasetControls.Button("Datensätze importieren", ChooseImport, 245));
        side.Controls.Add(DatasetControls.Button("Datensatzordner öffnen", OpenFolder, 245));
        if (DatasetImportJob.PendingSource(store) is string pending)
        {
            side.Controls.Add(DatasetControls.Button("Import fortsetzen", () => ImportFiles([pending]), 245));
            side.Controls.Add(DatasetControls.Button("Import verwerfen", () => { DatasetImportJob.Discard(store); Render(); }, 245));
        }
        if (File.Exists(QueuePath) || File.Exists(DatasetDownloader.Journal(store.DirectoryPath)))
            side.Controls.Add(DatasetControls.Button("Download fortsetzen", () => _ = Run(DownloadQueue, true, WorkKind.Download), 245));
        if (store.HasIndex)
        {
            viewTreeButton = DatasetControls.Button((store.IsStale ? "(alt) " : "") + "Datensatzbaum anzeigen", ShowGraph, 245);
            side.Controls.Add(viewTreeButton);
        }
        var cutoffRow = new Panel { Width = 250, Height = 46, Margin = new(4, 25, 4, 4) };
        cutoffRow.Controls.Add(new Label { Text = L.T("Minimum Partien pro Knoten"), Left = 0, Top = 7, Width = 165, Height = 36 });
        var threshold = new ScrollNumber { Minimum = 0, Maximum = 1000000, Value = Math.Clamp(store.Manifest.StopAtGames, 0, 1000000), Width = 80, Left = 168, Top = 5, EmptyDefaultValue = 0 };
        threshold.EnterCommitted = _ => side.Focus();
        L.SetToolTip(tips, threshold, "Die Stellung und ihre exakten Ergebniszahlen bleiben erhalten. Bei höchstens dieser Partienzahl entfallen ihre weiteren Züge. 0 erhält alle Tiefen; beispielsweise 5 spart seltene Fortsetzungen aus.");
        threshold.ValueChanged += (_, _) => { store.Manifest.StopAtGames = (int)threshold.Value; store.Save(); ActiveChanged(); };
        cutoffRow.Controls.Add(threshold);
        side.Controls.Add(cutoffRow);
        forecastLabel = new Label { Width = 245, Height = 155, Text = L.T("Dateigröße wird geschätzt …"), Margin = new(4, 15, 4, 4) };
        side.Controls.Add(forecastLabel);
        var right = new Panel { Dock = DockStyle.Fill };
        var list = new DarkScrollPanel { Dock = DockStyle.Fill };
        right.Controls.Add(list);
        content.Controls.AddRange([right, side]);
        buildButton = DatasetControls.Button("Aktive Datensätze zu Branch komprimieren", () => _ = Run(async (progress, token) => { await Task.Run(() => DatasetIndex.Build(store, Preferences.Current.Datasets, progress, token, job), token); }), 560);
        buildButton.Dock = DockStyle.Bottom;
        right.Controls.Add(buildButton);
        buildButton.BringToFront();
        RefreshBuild();
        int y = 8;
        foreach (var entry in store.Manifest.Entries.OrderByDescending(e => e.Bookmarked).ThenBy(e => e.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            long bytes = File.Exists(store.PathFor(entry)) ? new FileInfo(store.PathFor(entry)).Length : 0;
            var card = new DatasetCard(entry, bytes, store.Save, ActiveChanged, Render, () => Trim(entry), () =>
            {
                if (L.Message(this, "Diesen importierten Datensatz endgültig löschen? Die ursprüngliche Importdatei bleibt erhalten.", entry.Name, MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    store.Delete(entry);
                    Changed?.Invoke();
                    Render();
                }
            })
            {
                Left = 8,
                Top = y
            };
            y += 137;
            list.Controls.Add(card);
        }
        list.Resize += (_, _) => { foreach (Control card in list.Controls) card.Width = Math.Max(400, list.ClientSize.Width - 30); };
        foreach (Control card in list.Controls)
            card.Width = Math.Max(400, list.ClientSize.Width - 30);
        if (IsHandleCreated)
            forecastDelay.Start();
    }
    private void ActiveChanged()
    {
        RefreshBuild();
        Changed?.Invoke();
        forecast?.Cancel();
        forecastDelay.Stop();
        forecastDelay.Start();
    }
    private void RefreshBuild()
    {
        if (buildButton is { IsDisposed: false })
            buildButton.Visible = store.IsStale && store.Manifest.Entries.Any(e => e.Active);
        if (viewTreeButton is { IsDisposed: false })
            viewTreeButton.Text = (store.IsStale ? L.T("(alt) ") : "") + L.T("Datensatzbaum anzeigen");
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
            if (store.HasIndex && !store.IsStale)
            {
                target.Text = L.T("Baumgröße (tatsächlich): ") + $"{new FileInfo(store.IndexPath(store.Manifest.IndexFile!)).Length / 1e6:0.000} MB\n" +
                    L.T("Arbeitsraum beim Aufbau (Maximum): ") + (store.Manifest.IndexPeakWorkspaceBytes is long peak ? $"{peak / 1e6:0.000} MB" : L.T("Bei diesem älteren Baum nicht erfasst."));
                return;
            }
            long? measured = await Task.Run(() => DatasetPruningForecast.Measure(store, cts.Token), cts.Token);
            var estimate = await Task.Run(() => DatasetForecast.Estimate(store.DirectoryPath, active, cutoff, Preferences.Current.Datasets, cts.Token), cts.Token);
            if (IsDisposed || target.IsDisposed || cts.IsCancellationRequested)
                return;
            string size = Math.Round(estimate.LowerBytes / 1e6, 1) == Math.Round(estimate.UpperBytes / 1e6, 1) ? $"{estimate.UpperBytes / 1e6:0.0} MB" : $"{estimate.LowerBytes / 1e6:0.0}–{estimate.UpperBytes / 1e6:0.0} MB";
            target.Text = (measured is long exact ? L.T("Neuer Baum (vollständig gemessen): ") + $"{exact / 1e6:0.000} MB" :
                estimate.PruningUnknown ? L.T("Die Größe nach dem Ausdünnen benötigt vollständige Häufigkeiten. Nach einem Aufbau kann ein höheres Minimum exakt vermessen werden.") : L.T("Baum: ca. ") + size) +
                "\n" + L.T("Temporärer Arbeitsraum: ca. ") + $"{estimate.TemporaryBytes / 1e6:0.0} MB";
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
        ImportFiles(dialog.FileNames);
    }
    private void ImportFiles(string[] paths) => _ = Run(async (progress, token) => { foreach (string path in paths) { var entry = await Task.Run(() => store.Import(path, Preferences.Current.Datasets, progress, token, job), token); store.Register(entry); if (job!.SaveRequested) break; } }, true, WorkKind.Import);
    private void ChooseDownload()
    {
        if (operation is not null)
            return;
        var dialog = new DatasetCatalogForm(new LichessDatasetSource());
        dialog.FormClosed += (_, _) =>
        {
            if (IsDisposed || dialog.Selection.Count == 0 || operation is not null)
                return;
            var queue = ReadQueue();
            queue.AddRange(dialog.Selection.Where(o => !queue.Any(q => q.Url == o.Url)));
            SaveQueue(queue);
            _ = Run(DownloadQueue, true, WorkKind.Download);
        };
        dialog.Show();
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
        SaveQueue(queue);
        var all = queue.ToArray();
        using var producerStop = CancellationTokenSource.CreateLinkedTokenSource(token, job!.StopTransferToken);
        var channel = System.Threading.Channels.Channel.CreateBounded<(DatasetDownloadOption Option, string Path)>(1);
        int downloaded = 0, processed = 0;
        string downloadText = "", importText = "";
        object sync = new();
        void Report()
        {
            lock (sync)
                progress.Report(L.T("Heruntergeladen") + $": {downloaded}/{all.Length} · " + L.T("Verarbeitet") + $": {processed}/{all.Length}\n" +
                downloadText + "\n" + importText + "\n" + L.T("Gesamt") + " · " + job!.Timing(all.Take(processed).Sum(o => o.Games), all.Sum(o => o.Games)));
        }
        var producer = Task.Run(async () =>
        {
            Exception? failure = null;
            try
            {
                foreach (var option in all)
                {
                    job!.Wait(producerStop.Token);
                    if (job.SaveRequested)
                        break;
                    var downloadJob = pending?.Option.Url == option.Url ? pending : new DatasetDownloadJob { Option = option, Sha256 = await new LichessDatasetSource().Checksum(option, producerStop.Token) };
                    var downloadProgress = new DatasetInlineProgress(s => { lock (sync) downloadText = L.T("Download") + " · " + s; Report(); });
                    string path = await DatasetDownloader.Download(store.DirectoryPath, downloadJob, downloadProgress, producerStop.Token, operation: job);
                    lock (sync)
                    {
                        downloaded++;
                        downloadText = L.T("Download abgeschlossen") + " · " + option.Name;
                    }
                    Report();
                    await channel.Writer.WriteAsync((option, path), producerStop.Token);
                }
            }
            catch (OperationCanceledException) when (job!.SaveRequested) { }
            catch (Exception ex) { failure = ex; }
            finally { channel.Writer.TryComplete(failure); }
        });
        try
        {
            await foreach (var item in channel.Reader.ReadAllAsync(token))
            {
                if (job!.SaveRequested)
                    break;
                var importProgress = new DatasetInlineProgress(s => { lock (sync) importText = L.T("Import") + " · " + s; Report(); });
                var entry = await Task.Run(() => store.Import(item.Path, Preferences.Current.Datasets, importProgress, token, job), token);
                store.Register(entry);
                queue.RemoveAll(o => o.Url == item.Option.Url);
                SaveQueue(queue);
                File.Delete(item.Path);
                lock (sync)
                    processed++;
                Report();
                if (job!.SaveRequested)
                    break;
            }
            if (queue.Count == 0)
                File.Delete(QueuePath);
        }
        finally
        {
            producerStop.Cancel();
            await producer;
            if (token.IsCancellationRequested && job?.PreserveCheckpoint != true)
            {
                DatasetImportJob.Discard(store);
                foreach (var option in queue)
                {
                    string name = Path.GetFileName(option.Url.AbsolutePath);
                    if (!name.EndsWith(".pgn.zst", StringComparison.OrdinalIgnoreCase))
                        continue;
                    string owned = Path.Combine(store.DirectoryPath, name);
                    File.Delete(owned);
                    File.Delete(owned + ".part");
                }
                File.Delete(QueuePath);
                File.Delete(journal);
            }
        }
    }
    private void Trim(DatasetEntry entry)
    {
        if (operation is not null)
            return;
        var dialog = new DatasetTrimForm(entry, engine);
        dialog.FormClosed += (_, _) =>
        {
            if (IsDisposed || dialog.DialogResult != DialogResult.OK || operation is not null)
                return;
            var filter = dialog.Filter;
            bool replace = dialog.ReplaceOriginal;
            _ = Run(async (progress, token) => { var result = await Task.Run(() => store.Trim(entry, filter, Preferences.Current.Datasets, progress, token, job), token); result.Active = replace && entry.Active; store.Register(result); if (replace) store.Delete(entry); }, true, WorkKind.Filter);
        };
        dialog.Show();
    }
    private async Task Run(Func<IProgress<string>, CancellationToken, Task> action, bool canSave = false, WorkKind kind = WorkKind.Index)
    {
        if (operation is not null)
            return;
        using var cts = new CancellationTokenSource();
        using var state = new DatasetOperation();
        job = state;
        operation = cts;
        workKind = kind;
        status.Busy = true;
        body.Enabled = false;
        operationButtons.Visible = true;
        savePartial.Visible = canSave;
        pause.Text = L.T("Pause");
        forecast?.Cancel();
        forecastDelay.Stop();
        status.Text = L.T("Vorgang läuft …");
        try
        {
            await action(new Progress<string>(s => { if (!IsDisposed) status.Text = s; }), cts.Token);
            status.Text = L.T(state.SaveRequested ? "Gestoppt. Bereits verarbeitete Partien und Downloads wurden gespeichert." : "Abgeschlossen.");
        }
        catch (OperationCanceledException) { status.Text = L.T("Angehalten. Der bisherige Datensatzbaum bleibt erhalten."); }
        catch (Exception ex) { status.Text = L.T("Fehler: ") + ex.GetBaseException().Message; }
        finally { operation = null; job = null; if (!IsDisposed) { status.Busy = false; body.Enabled = true; operationButtons.Visible = false; Changed?.Invoke(); Render(); if (closeAfter) Close(); } }
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
