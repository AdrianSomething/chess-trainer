using System.Drawing.Drawing2D;

namespace ChessOpeningTrainer;

public sealed partial class OpeningGeneratorForm
{
    private readonly GeneratorWorkQueue workQueue;
    private GeneratorJob? selectedJob;
    private GeneratorJob? draft;
    private bool closingQueue;
    private Button? cancelJobButton;
    private GeneratorPreviewForm? queuePreview;
    private readonly Button newJob = new() { Text = "Weiteren Auftrag erstellen" }, previousJob = new() { Text = "<" }, nextJob = new() { Text = ">" };
    private readonly Button resumeAll = new() { Text = "Mit allen angehaltenen Aufträgen fortfahren" };
    private readonly Button clearCompleted = new() { Text = "Abgeschlossene Aufträge entfernen" };
    private readonly Button clearJob = new() { Text = "Auftrag entfernen" }, viewJob = new() { Text = "Branch anzeigen" }, learnJob = new() { Text = "Lernen" };
    private readonly CheckBox pauseAllAtLimit = new() { Text = "Beim Kartenlimit alle Aufträge anhalten", AutoSize = true };
    private readonly System.Windows.Forms.Timer queueClock = new() { Interval = 150 };
    private void InitializeQueue(Panel footer)
    {
        boardHost.Controls.AddRange([newJob, previousJob, nextJob, resumeAll, clearCompleted]);
        newJob.Click += (_, _) =>
        {
            if (draft is not null) return;
            draft = CaptureDraft(); selectedJob = null; RefreshQueueUi(); board.Invalidate();
        };
        previousJob.Click += (_, _) => SwitchJob(-1);
        nextJob.Click += (_, _) => SwitchJob(1);
        resumeAll.Click += (_, _) => workQueue.ResumeAll();
        clearCompleted.Click += (_, _) => { foreach (var job in workQueue.Jobs.Where(j => j.Finished)) workQueue.RemoveFinished(job); SelectRemainingJob(); };
        clearJob.Click += (_, _) => { if (selectedJob is { } job && workQueue.RemoveFinished(job)) SelectRemainingJob(); };
        viewJob.Click += (_, _) =>
        {
            if (selectedJob?.Result is { } result)
                Application.OpenForms.OfType<MainForm>().FirstOrDefault(f => f.IsHomeWindow)?.OpenBranch(result);
        };
        learnJob.Click += (_, _) =>
        {
            if (selectedJob?.Result is { } result)
                Application.OpenForms.OfType<MainForm>().FirstOrDefault(f => f.IsHomeWindow)?.OpenLearning(result);
        };
        footer.Controls.AddRange([clearJob, viewJob, learnJob]);
        clearJob.SetBounds(12, 4, 190, 32); viewJob.SetBounds(208, 4, 190, 32); learnJob.SetBounds(404, 4, 193, 32);
        L.SetToolTip(tips, clearJob, "Entfernt nur den Auftrag aus dieser Ansicht. Das gespeicherte Opening bleibt erhalten.");
        var preview = new MenuIconButton("graph-preview", "") { AccessibleName = "Wachsenden Opening-Branch anzeigen", Width = 36, Height = 32, Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
        L.SetToolTip(tips, preview, "Wachsenden Opening-Branch anzeigen");
        footer.Controls.Add(preview); preview.SetBounds(footer.Width - 48, footer.Height - 36, 36, 32);
        preview.BringToFront();
        preview.Click += (_, _) =>
        {
            if (queuePreview is null || queuePreview.IsDisposed) queuePreview = new GeneratorPreviewForm(workQueue, followJobs: true);
            queuePreview.Show(); queuePreview.Activate();
        };
        queueClock.Tick += (_, _) => { RefreshQueueUi(); if (selectedJob is not null) board.Invalidate(); };
        board.ForegroundPaint += PaintJobStatus;
        workQueue.Completed += SaveJob;
        FormClosed += (_, _) => workQueue.Completed -= SaveJob;
        FormClosing += async (_, e) =>
        {
            if (closingQueue || !workQueue.Jobs.Any(j => !j.Finished)) return;
            e.Cancel = true;
            if (L.Message(this, "Laufende Aufträge abbrechen und vorhandene Teilbäume speichern?", "Generator schließen", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            closingQueue = true; Enabled = false;
            await workQueue.DisposeAsync();
            Close();
        };
        queueClock.Start();
        RefreshQueueUi();
    }
    private void LayoutQueueNavigation(int size)
    {
        int x = 52, y = size + 65, width = Math.Max(240, size), third = width / 3;
        newJob.SetBounds(x, y, third - 4, 36); previousJob.SetBounds(x + third, y, third - 4, 36); nextJob.SetBounds(x + 2 * third, y, width - 2 * third, 36);
        resumeAll.SetBounds(x, y + 42, width, 36);
        clearCompleted.SetBounds(x, y + 84, width, 32);
    }
    private GeneratorJob CaptureDraft(bool validate = false)
    {
        var options = Options(); var setup = SetupMoves();
        long predicted = cachedEstimate is not null && cachedEstimateKey == EstimateKey(options, setup) ? cachedEstimate.Estimate : options.MaxPositions;
        return new(name.Text, position, setup, options, pauseAllAtLimit.Checked, predicted, timingHistory.SecondsPerCard(GeneratorTimingHistory.Configuration(options)), validate);
    }
    private void SubmitSelectedJob()
    {
        if (selectedJob is not null) { workQueue.Prioritize(selectedJob); RefreshQueueUi(); return; }
        try
        {
            var job = CaptureDraft(validate: true); draft?.Cancellation.Dispose(); draft = null; selectedJob = job;
            workQueue.Add(job); RefreshQueueUi();
        }
        catch (ArgumentException ex) { L.Message(this, ex.Message, L.T("Opening-Branch-Generator")); }
    }
    private void SwitchJob(int delta)
    {
        var jobs = workQueue.Jobs;
        if (jobs.Count == 0 && draft is null) return;
        if (selectedJob is null) { draft?.Cancellation.Dispose(); draft = CaptureDraft(); }
        int index = selectedJob is null ? jobs.Count : jobs.ToList().IndexOf(selectedJob);
        index = Math.Clamp(index + delta, 0, jobs.Count - (draft is null ? 1 : 0));
        selectedJob = index < jobs.Count ? jobs[index] : null;
        RestoreJob(selectedJob ?? draft!); RefreshQueueUi(); board.Invalidate();
    }
    private void SelectRemainingJob()
    {
        if (selectedJob is not null && !workQueue.Jobs.Contains(selectedJob))
        {
            selectedJob = workQueue.Jobs.FirstOrDefault();
            if ((selectedJob ?? draft) is { } remaining) RestoreJob(remaining);
        }
        RefreshQueueUi(); board.Invalidate();
    }
    private void RestoreJob(GeneratorJob job)
    {
        // Do not nest a whole-window WM_SETREDRAW batch around SectionEditor:
        // its own native redraw batch otherwise restores obsolete child bounds.
        controls.SuspendLayout();
        try
        {
        updatingName = true; name.Text = job.Name; updatingName = false; nameEdited = true;
        var o = job.Options;
        color.SelectedIndex = o.LearnWhite ? 1 : 0;
        min.Value = o.MinDepth; cards.Value = o.MaxPositions;
        workers.Value = o.Workers; threads.Value = Math.Clamp(o.Threads / o.Workers, (int)threads.Minimum, (int)threads.Maximum);
        sectionCount.Value = Math.Max(1, o.Sections.Count); sections.SetValues(o.Sections);
        merge.Checked = o.MergeTranspositions; human.Checked = o.HumanFilter; humanDepth.Value = o.HumanDepth;
        pauseAllAtLimit.Checked = job.PauseAllAtLimit;
        }
        finally
        {
            controls.ResumeLayout(true);
            controls.PerformLayout(); controls.Invalidate(true);
        }
        undo.Clear(); position = Position.FromFen(Position.InitialFen);
        foreach (string uci in job.Setup) { undo.Push(position.Clone()); position.Apply(MoveGenerator.GenerateLegal(position).Single(m => m.Uci == uci)); }
        position = job.Start.Clone();
        RefreshPosition();
    }
    private void RefreshQueueUi()
    {
        if (IsDisposed) return;
        if (operation is not null)
        {
            newJob.Enabled = previousJob.Enabled = nextJob.Enabled = false;
            if (cancelJobButton is not null) { cancelJobButton.Visible = true; cancelJobButton.Text = L.T("Abbrechen"); }
            return;
        }
        newJob.Enabled = previousJob.Enabled = nextJob.Enabled = true;
        var jobs = workQueue.Jobs; bool submitted = selectedJob is not null;
        foreach (Control input in controls.Controls) input.Enabled = !submitted;
        board.InputEnabled = !submitted && !moving;
        newJob.Visible = jobs.Count > 0 && submitted && draft is null;
        int index = submitted ? jobs.ToList().IndexOf(selectedJob!) : jobs.Count;
        previousJob.Visible = index > 0; nextJob.Visible = submitted && (index < jobs.Count - 1 || draft is not null);
        resumeAll.Visible = jobs.Any(j => j.State == GeneratorJobState.AwaitingApproval);
        clearCompleted.Visible = jobs.Any(j => j.Finished);
        clearJob.Visible = submitted && selectedJob!.Finished;
        viewJob.Visible = learnJob.Visible = clearJob.Visible && selectedJob!.Result is not null;
        generate.Visible = !submitted || selectedJob!.State == GeneratorJobState.Queued;
        generate.Enabled = true;
        generate.Text = L.T(submitted ? "Auftrag jetzt bearbeiten" : jobs.Any(j => !j.Finished) ? "Auftrag der Warteschlange hinzufügen" : "Auftrag erstellen – Lernkarten generieren");
        estimate.Visible = !submitted || selectedJob!.State == GeneratorJobState.AwaitingApproval;
        estimate.Enabled = true;
        estimate.Text = selectedJob?.State == GeneratorJobState.AwaitingApproval ? L.T($"Mit Auftrag fortfahren (Kartenzahl: {selectedJob.Graph?.Cards ?? 0})") : L.T("Kartenzahl neu schätzen");
        if (cancelJobButton is not null)
        {
            cancelJobButton.Visible = submitted && !selectedJob!.Finished;
            cancelJobButton.Text = L.T((selectedJob?.Graph?.Cards ?? 0) == 0 ? "Auftrag verwerfen" : "Abbrechen / Teilbaum behalten");
        }
        if (submitted) status.Text = L.T(selectedJob!.Name + "\n" + selectedJob.Message);
    }
    private void SaveJob(GeneratorJob job)
    {
        try
        {
            if (job.Result is not { } result || !OpeningStore.LearnerMoves(result).Any()) return;
            Directory.CreateDirectory(directory);
            string file = Path.Combine(directory, result.Id + ".json");
            OpeningStore.Save(result, file); result.FilePath = file;
            lock (timingHistory) timingHistory.Record(GeneratorTimingHistory.Configuration(job.Options), job.Forecast.CompletedCards, job.Forecast.Elapsed);
            job.Message = L.T("Opening gespeichert") + ": " + result.Name;
            if (IsHandleCreated && !IsDisposed) BeginInvoke((Action)(() => { if (!IsDisposed) Generated?.Invoke(result); }));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { job.State = GeneratorJobState.Failed; job.Message = ex.Message; }
    }
    private void PaintJobStatus(object? sender, PaintEventArgs e)
    {
        if (selectedJob is not { } job) return;
        string text = workQueue.Resting && !job.Finished ? L.T("Generator-Abkühlpause") : L.T(job.State switch
        {
            GeneratorJobState.Completed => "Auftrag abgeschlossen", GeneratorJobState.Running => "Auftrag in Bearbeitung",
            GeneratorJobState.AwaitingApproval => "Auftrag wartet auf Bestätigung", GeneratorJobState.Failed => "Auftrag fehlgeschlagen",
            GeneratorJobState.Cancelled => "Auftrag abgebrochen", _ => "Auftrag in Warteschlange"
        });
        using var dim = new SolidBrush(Color.FromArgb(155, 25, 28, 31));
        e.Graphics.FillRectangle(dim, board.ClientRectangle);
        using var font = new Font(Font.FontFamily, Math.Clamp(board.Width / 24f, 22, 34), FontStyle.Bold);
        using var brush = new SolidBrush(Color.FromArgb(235, Color.White));
        using var shadow = new SolidBrush(Color.FromArgb(150, Color.Black));
        var bounds = new RectangleF(2, board.Height / 2f - 65, board.Width - 28, 110);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        e.Graphics.DrawString(text, font, shadow, new RectangleF(bounds.X + 1, bounds.Y + 1, bounds.Width, bounds.Height), format);
        e.Graphics.DrawString(text, font, brush, bounds, format);
        if (job.State == GeneratorJobState.Running)
        {
            using var pen = new Pen(Color.FromArgb(185, Color.White), 3);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.DrawArc(pen, bounds.X + bounds.Width / 2 - 22, bounds.Bottom + 4, 44, 44, Environment.TickCount64 % 1000 * .36f, 260);
        }
    }
}
