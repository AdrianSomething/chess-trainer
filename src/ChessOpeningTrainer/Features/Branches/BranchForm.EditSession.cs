namespace ChessOpeningTrainer;

public sealed partial class BranchForm
{
    private readonly Panel graphArea = new() { Dock = DockStyle.Fill };
    private readonly Panel editFooter = new() { Dock = DockStyle.Bottom, Height = 50, Visible = false };
    private readonly FlowLayoutPanel editFooterButtons = new() { AutoSize = true, WrapContents = false };
    private Button? commitEditing, cancelEditing;
    private BranchEditSession? editSession;
    private CancellationTokenSource? featureCancellation;
    private Action<OpeningDefinition, ProgressStore>? setBranchData;

    private void InitializeEditSessionFooter()
    {
        commitEditing = new Button { Text = "Bearbeitung bestätigen", Width = 200, Height = 36 };
        cancelEditing = new Button { Text = "Bearbeitung abbrechen", Width = 200, Height = 36 };
        editFooterButtons.Controls.AddRange([commitEditing, cancelEditing]); editFooter.Controls.Add(editFooterButtons);
        graphArea.Controls.Add(editFooter); editFooter.BringToFront();
        void Center()
        {
            commitEditing.Width = cancelEditing.Width = Math.Clamp((editFooter.ClientSize.Width - 20) / 2, 110, 200);
            editFooterButtons.Location = new(Math.Max(0, (editFooter.ClientSize.Width - editFooterButtons.PreferredSize.Width) / 2), 5);
        }
        editFooter.Resize += (_, _) => Center(); Center();
        commitEditing.Click += (_, _) => CommitEditorSession();
        cancelEditing.Click += (_, _) => AbortEditorSession();
        FormClosed += (_, _) => StopEditorFeatures();
    }

    private void BeginEditorSession()
    {
        editSession = new BranchEditSession(originalOpening, originalProgress);
        featureCancellation = CancellationTokenSource.CreateLinkedTokenSource(analysis.Token);
        if (editSession.CanRestore && L.Message(this, "Separat gespeicherte Bearbeitung wieder aufnehmen? Das Original wird erst beim Bestätigen verändert.", "Zwischenstand", MessageBoxButtons.YesNo) == DialogResult.Yes)
            editSession.Restore();
        setBranchData!(editSession.Working, editSession.Progress);
    }
    private void RestoreEditorDraft()
    {
        if (editSession?.RestoredDraftFen is not string fen || editSession.RestoredDraftMoves.Count == 0) return;
        var start = Position.FromFen(fen);
        if (!canvas.SelectPosition(start)) return;
        foreach (string uci in editSession.RestoredDraftMoves)
        {
            var move = MoveGenerator.GenerateLegal(editPosition!).FirstOrDefault(m => m.Uci == uci);
            if (move is null) break;
            ApplyDraftMove(move, false);
        }
    }
    private void EndEditorSession(bool keepCheckpoint)
    {
        if (editSession is null) return;
        StopEditorFeatures();
        if (keepCheckpoint && (editSession.Changed || DraftDirty)) editSession.SaveCheckpoint(editBase?.ToFen(), editMoves);
        else editSession.Discard();
        editSession = null; editMoves.Clear(); draftReplies.Items.Clear(); editSelected = null;
        setBranchData!(originalOpening, originalProgress);
    }
    private void CommitEditorSession()
    {
        if (editSession is null) return;
        try
        {
            if (DraftDirty) ConfirmDraft();
            if (DraftDirty) return;
            editSession.MarkChanged();
            if (editSession.Changed) editSession.Commit(); else editSession.Discard();
            string? checkpointWarning = editSession.CheckpointWarning;
            StopEditorFeatures();
            editSession = null; setBranchData!(originalOpening, originalProgress);
            modeSelector.SelectedIndex = 0; ChangeBranchMode(BranchMode.Learning);
            if (checkpointWarning is not null) L.Message(this, "Änderungen übernommen. Der Zwischenstand konnte nicht abgeschlossen werden: " + checkpointWarning, "Bearbeitung gespeichert");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or InvalidDataException)
        { L.Message(this, ex.Message, "Bearbeitung speichern"); }
    }
    private void StopEditorFeatures()
    { featureCancellation?.Cancel(); featureCancellation?.Dispose(); featureCancellation = null; }
    private void AbortEditorSession()
    {
        try { EndEditorSession(keepCheckpoint: false); modeSelector.SelectedIndex = 0; ChangeBranchMode(BranchMode.Learning); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { L.Message(this, ex.Message, "Bearbeitung"); }
    }
    private void CheckpointGraphChange()
    {
        if (!IsEditingMode || editSession is null) return;
        try { editSession.MarkChanged(); if (editSession.Changed) editSession.SaveCheckpoint(editBase?.ToFen(), editMoves); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException) { info.Text = "Zwischenstand konnte nicht gespeichert werden: " + ex.Message; }
        UpdateEditorFooter();
    }
    private void UpdateEditorFooter()
    {
        if (commitEditing is not null) commitEditing.Enabled = editSession is not null && (DraftDirty || editSession.Changed);
    }
}
