namespace ChessOpeningTrainer;

public sealed partial class BranchForm
{
    private enum BranchMode { Learning, Editing, Analysis }
    private BranchMode branchMode;
    private bool IsEditingMode => branchMode == BranchMode.Editing;
    private readonly ComboBox modeSelector = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 190, AccessibleName = "Branchmodus" };
    private FlowLayoutPanel? editPanel, gamesPanel;
    private Control[] learningControls = [];
    private readonly Dictionary<Control, Rectangle> sidebarBounds = [];
    private Panel? rightSidebar;
    private Button? editingExplore;
    private Control? exploreParent;
    private Rectangle exploreBounds;
    private ComboBox? modeAlternatives;
    private Func<BranchVertex?>? modeSelection;
    private Action? rebuildGraph;
    private bool restoringDraftSelection;
    private bool layingOutSidebar;

    private void InitializeModes(FlowLayoutPanel top, Panel right, FlowLayoutPanel settings, ComboBox alternatives, Func<BranchVertex?> selection, Action rebuild, Action<OpeningDefinition, ProgressStore> switchData)
    {
        rightSidebar = right; modeAlternatives = alternatives; modeSelection = selection; rebuildGraph = rebuild; setBranchData = switchData;
        foreach (Control control in right.Controls) sidebarBounds[control] = control.Bounds;
        editingExplore = right.Controls.OfType<Button>().FirstOrDefault(b => b.Text == L.T("Tiefer erforschen"));
        exploreParent = editingExplore?.Parent; exploreBounds = editingExplore?.Bounds ?? Rectangle.Empty;
        learningControls = right.Controls.Cast<Control>().Where(c => c != board && c != evaluation && c != info && c != alternatives && c.Text != L.T("Stellungsvarianten") && c != deleteNode && c != deleteWholeNode && c != editingExplore).ToArray();
        editPanel = new FlowLayoutPanel { Left = 12, Width = 385, FlowDirection = FlowDirection.TopDown, WrapContents = false,
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MaximumSize = new(385, 0), Visible = false };
        gamesPanel = new FlowLayoutPanel { Left = 12, Width = 385, FlowDirection = FlowDirection.TopDown, WrapContents = false,
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MaximumSize = new(385, 0), Visible = false };
        right.Controls.AddRange([editPanel, gamesPanel]);
        BuildEditingControls(editPanel); BuildAnalysisControls(gamesPanel); InitializeEditSessionFooter();
        modeSelector.Items.AddRange(["Lernmodus", "Bearbeitungsmodus", "Analysemodus"]); modeSelector.SelectedIndex = 0;
        top.Controls.Add(modeSelector); top.Controls.SetChildIndex(modeSelector, 0);
        InitializeCoverage(top);
        settings.Controls.Add(hideSubbranchColors);
        hideSubbranchColors.CheckedChanged += (_, _) => ApplyModeCanvas();
        modeSelector.SelectionChangeCommitted += (_, _) => ChangeBranchMode((BranchMode)modeSelector.SelectedIndex);
        right.Resize += (_, _) => LayoutModeSidebar();
        void RefreshSidebarAppearance()
        {
            if (IsDisposed || Disposing) return;
            if (InvokeRequired) { try { BeginInvoke((Action)RefreshSidebarAppearance); } catch (InvalidOperationException) { } return; }
            LayoutModeSidebar();
        }
        Preferences.Changed += RefreshSidebarAppearance;
        FormClosed += (_, _) => Preferences.Changed -= RefreshSidebarAppearance;
        FormClosed += (_, _) => CancelEditSearch();
        FormClosing += (_, e) =>
        {
            if (editSession is null) return;
            try { editSession.MarkChanged(); if (DraftDirty || editSession.Changed) editSession.SaveCheckpoint(editBase?.ToFen(), editMoves); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
            { L.Message(this, "Zwischenstand konnte nicht gespeichert werden: " + ex.Message, "Bearbeitung"); e.Cancel = true; }
        };
        UpdateModeSelection();
    }

    private void ChangeBranchMode(BranchMode mode)
    {
        if (mode == branchMode) return;
        bool needsGraphRebuild = IsEditingMode || mode == BranchMode.Editing;
        if (IsEditingMode && editSession is not null)
        {
            editSession.MarkChanged();
            if ((DraftDirty || editSession.Changed) && L.Message(this, "Bearbeitung ohne Übernehmen verlassen? Der Zwischenstand bleibt separat gespeichert.", "Bearbeitung wechseln", MessageBoxButtons.YesNo) != DialogResult.Yes)
            { modeSelector.SelectedIndex = (int)branchMode; return; }
            EndEditorSession(keepCheckpoint: true);
        }
        CancelEditSearch(); branchMode = mode; board.StopAnimation();
        RefreshCoverage();
        if (IsEditingMode) BeginEditorSession();
        foreach (var control in learningControls) control.Visible = mode == BranchMode.Learning;
        editPanel!.Visible = IsEditingMode; gamesPanel!.Visible = mode == BranchMode.Analysis;
        editFooter.Visible = IsEditingMode;
        if (editingExplore is not null)
        {
            if (IsEditingMode) { editPanel.Controls.Add(editingExplore); editingExplore.Size = new(360,32); }
            else if (exploreParent is not null) { exploreParent.Controls.Add(editingExplore); editingExplore.Bounds = exploreBounds; }
            editingExplore.Visible = mode != BranchMode.Analysis;
        }
        if (mode == BranchMode.Analysis) LoadGameMatches();
        // Learning and analysis display exactly the same graph. Reusing it also
        // retains its camera and cached position identities; only editor changes
        // need a new layout or a restored original graph.
        if (needsGraphRebuild) rebuildGraph?.Invoke();
        if (board.Position is not null) ModePositionChanged(board.Position);
        RestoreEditorDraft(); UpdateModeSelection(); ApplyModeCanvas();
    }

    private void ModePositionChanged(Position position)
    {
        UpdateDatasetStats(position);
        if (IsEditingMode)
        {
            if (restoringDraftSelection) return;
            if (DraftDirty && editBase is not null)
            {
                if (BranchPositionIdentity.Key(position) == BranchPositionIdentity.Key(editBase)) { PaintDraft(null); return; }
                if (!ConfirmDiscardDraft())
                {
                    restoringDraftSelection = true;
                    try { SelectEditedPosition(editBase); PaintDraft(null); }
                    finally { restoringDraftSelection = false; }
                    return;
                }
            }
            ResetDraft(position);
        }
        else board.InputEnabled = false;
    }
    private bool ConfirmDiscardDraft() => L.Message(this, "Unbestätigte Zugfolge verwerfen? Hinzugefügte Knoten bleiben in der Arbeitskopie erhalten.", "Bearbeitung wechseln", MessageBoxButtons.YesNo) == DialogResult.Yes;

    private void UpdateModeSelection()
    {
        if (editPanel is null) return;
        var selected = modeSelection?.Invoke();
        rightSidebar!.Visible = selected is not null;
        if (IsEditingMode)
        {
            board.InputEnabled = selected is not null;
            UpdateDeleteControls();
            if (editingExplore is not null) editingExplore.Visible = !DraftDirty && selected?.IsLearnerMove == true;
        }
        if (branchMode == BranchMode.Analysis) UpdateGameList();
        LayoutModeSidebar(); UpdateEditorFooter();
    }

    private void LayoutModeSidebar()
    {
        if (layingOutSidebar || rightSidebar is null || editPanel is null || gamesPanel is null) return;
        layingOutSidebar = true;
        try
        {
        int shift = Preferences.Current.DebugInformation ? 0 : -65;
        bool showDataset = branchMode == BranchMode.Learning && datasetIndexPath is not null;
        datasetResults.Visible = showDataset;
        datasetResults.Top = 390 + (Preferences.Current.DebugInformation ? 65 : 0) + rightSidebar.AutoScrollPosition.Y;
        if (showDataset) shift += 47;
        info.Visible = Preferences.Current.DebugInformation;
        foreach (var (control, original) in sidebarBounds)
            if (control.Parent == rightSidebar && original.Top >= 450) control.Top = original.Top + shift + rightSidebar.AutoScrollPosition.Y;
        editPanel.Top = gamesPanel.Top = (modeAlternatives?.Bottom ?? 515 + shift) + 8;
        int width = Math.Min(360, Math.Max(120, rightSidebar.ClientSize.Width - 55));
        board.Width = board.Height = width; evaluation.Height = width;
        if (analysisColor is not null && sidebarBounds.Keys.FirstOrDefault(c => c.AccessibleName == "Subbranchfarbe") is { } color)
            analysisColor.Bounds = color.Bounds;
        }
        finally { layingOutSidebar = false; }
    }

    private void ApplyModeCanvas()
    {
        canvas.AnalysisMode = branchMode == BranchMode.Analysis;
        canvas.HideSubbranchColors = hideSubbranchColors.Checked;
        canvas.AnalysisMarkerColor = analysisMarkerColor;
        canvas.AnalysisExits = gameMatchIndex.Counts;
        canvas.AnalysisCaptions = gameCaptions;
        if (analysisColor is not null) analysisColor.Visible = canvas.AnalysisMode;
        canvas.Invalidate();
    }
}
