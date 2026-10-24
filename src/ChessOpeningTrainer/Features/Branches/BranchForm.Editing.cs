namespace ChessOpeningTrainer;

public sealed partial class BranchForm
{
    private Position? editBase, editPosition;
    private Move? editLastMove;
    private Func<Position, Move?>? incomingMoveFor;
    private readonly List<string> editMoves = [];
    private int? editSelected;
    private int editVersion;
    private CancellationTokenSource? editSearch;
    private readonly Label draftInfo = new() { Width = 360, Height = 68, ForeColor = UiPalette.Gainsboro };
    private readonly ScrollNumber editDepth = new() { Minimum = 4, Maximum = 30, Value = 16, Width = 80 };
    private readonly ScrollNumber editEasy = new() { Minimum = -20, Maximum = 0, DecimalPlaces = 2, Increment = .25m, Value = -2, Width = 80 };
    private readonly ScrollNumber editLoss = new() { Maximum = 20, DecimalPlaces = 2, Increment = .25m, Value = 2, Width = 80 };
    private readonly CheckedListBox draftReplies = new() { Width = 360, Height = 100, CheckOnClick = true,
        BackColor = UiPalette.FromArgb(43, 49, 55), ForeColor = UiPalette.Gainsboro };
    private readonly List<IncomingPath> deleteIncoming = [];
    private readonly FlowLayoutPanel draftResetRow = new() { Width = 366, Height = 36, WrapContents = false, Visible = false };
    private readonly FlowLayoutPanel deleteRow = new() { Width = 366, Height = 38, WrapContents = false, Visible = false };
    private readonly FlowLayoutPanel engineSettings = new() { Width = 370, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
        FlowDirection = FlowDirection.TopDown, WrapContents = false, Visible = false, MaximumSize = new(376, 0) };
    private Button? undoDraft;
    private sealed record IncomingPath(Position Before, string Uci, string Description) { public override string ToString() => Description; }
    private Button? confirmDraft, bestDraft, generateDraft, deleteNode, deleteWholeNode;
    private sealed record DraftReply(string Uci, string San, double Score) { public override string ToString() => $"{San}  ·  {Score:+0.00;-0.00;0.00}"; }
    private bool DraftDirty => editMoves.Count > 0 || draftReplies.Items.Count > 0;

    private void BuildEditingControls(FlowLayoutPanel panel)
    {
        var settingsToggle = new Button { Text = "▸ " + L.T("Engine-Einstellungen"), Width = 360, Height = 27, FlatStyle = FlatStyle.Flat, BackColor = UiPalette.FromArgb(43, 49, 55), ForeColor = UiPalette.Gainsboro, Margin = new(0, 1, 0, 1), TextAlign = ContentAlignment.MiddleLeft };
        settingsToggle.FlatAppearance.BorderSize = 0;
        panel.Controls.Add(settingsToggle); panel.Controls.Add(engineSettings);
        settingsToggle.Click += (_, _) => { engineSettings.Visible = !engineSettings.Visible; settingsToggle.Text = (engineSettings.Visible ? "▾ " : "▸ ") + L.T("Engine-Einstellungen"); };
        void Row(string label, Control input)
        {
            var row = new FlowLayoutPanel { Width = 370, Height = 32 };
            row.Controls.AddRange([new Label { Text = label, Width = 245, Height = 28, ForeColor = UiPalette.Gainsboro }, input]); engineSettings.Controls.Add(row);
        }
        Row("Engine-Suchtiefe", editDepth); Row("Leichtigkeitsgrenze (Gegnersicht)", editEasy); Row("Maximaler gegnerischer Verlust", editLoss);
        var tips = new ToolTip { AutoPopDelay = 15000 };
        L.SetToolTip(tips, editDepth, "Suchtiefe für den besten Zug und alle Gegnerantworten. Die Suche arbeitet im Hintergrund; höchstens 15 Sekunden je Auftrag.");
        L.SetToolTip(tips, editEasy, "Gegnerantworten unter dieser Bewertung aus Gegnersicht sind zu leicht und werden nicht vorgeschlagen.");
        L.SetToolTip(tips, editLoss, "Eine Antwort darf höchstens so viele Bauerneinheiten schlechter als der beste Gegnerzug sein.");
        FormClosed += (_, _) => tips.Dispose();
        Button Add(string title, Action action)
        {
            var button = new Button { Text = title, Width = 360, Height = 32 };
            button.Click += (_, _) => action(); panel.Controls.Add(button); return button;
        }
        bestDraft = Add("Bester Enginezug spielen", () => _ = SearchDraft(false));
        generateDraft = Add("Gegenantworten vorbereiten", () => _ = SearchDraft(true));
        panel.Controls.Add(draftReplies); panel.Controls.Add(draftInfo);
        undoDraft = new Button { Text = "<", Width = 40, Height = 30, AccessibleName = "Letzten Entwurfzug zurücknehmen" };
        var reset = new Button { Text = "Zugfolge zurücksetzen", Width = 308, Height = 30 };
        undoDraft.Click += (_, _) => UndoDraft();
        reset.Click += (_, _) => { if (editBase is not null) { var p = editBase.Clone(); ResetDraft(p); PaintDraft(null); } };
        draftResetRow.Controls.AddRange([undoDraft, reset]); panel.Controls.Add(draftResetRow);
        confirmDraft = Add("Knoten hinzufügen", ConfirmDraft);
        if (deleteNode is not null) { deleteRow.Controls.Add(deleteNode); deleteNode.Width = 360; deleteNode.Height = 32; }
        if (deleteWholeNode is not null) { deleteRow.Controls.Add(deleteWholeNode); deleteWholeNode.Height = 32; }
        panel.Controls.Add(deleteRow);
        board.SquareClicked += square =>
        {
            if (!IsEditingMode || editPosition is null || modeSelection?.Invoke() is null) return;
            if (editSelected is int from && TryEditMove(from, square, true)) return;
            if (editPosition.Squares[square] != '\0' && char.IsUpper(editPosition.Squares[square]) == editPosition.WhiteToMove)
            {
                editSelected = square; board.SelectedSquare = square;
                board.CandidateMoves = MoveGenerator.GenerateLegal(editPosition).Where(m => m.From == square).ToArray(); board.Invalidate();
            }
            else { editSelected = null; BoardPresentation.ClearSelection(board); }
        };
        board.SquareDragged += (from, to) => { if (IsEditingMode) TryEditMove(from, to, false); };
        board.BlankClicked += () => { if (IsEditingMode) { editSelected = null; BoardPresentation.ClearSelection(board); } };
    }

    private void ResetDraft(Position position)
    {
        CancelEditSearch(); editVersion++;
        editBase = position.Clone(); editPosition = position.Clone(); editMoves.Clear(); draftReplies.Items.Clear(); editSelected = null;
        editLastMove = incomingMoveFor?.Invoke(position);
        UpdateDraft(); UpdateDeletePaths(); board.InputEnabled = modeSelection?.Invoke() is not null;
    }

    private bool TryEditMove(int from, int to, bool animate)
    {
        if (editPosition is null || modeSelection?.Invoke() is null) return false;
        var choices = MoveGenerator.GenerateLegal(editPosition).Where(m => m.From == from && m.To == to).ToArray();
        if (choices.Length == 0) return false;
        var move = choices.Length == 1 ? choices[0] : PromotionPicker.Choose(this, choices.ToList(), editPosition.WhiteToMove);
        if (move is null) return false;
        ApplyDraftMove(move, animate); return true;
    }

    private void ApplyDraftMove(Move move, bool animate)
    {
        if (editPosition is null) return;
        CancelEditSearch(); editVersion++; draftReplies.Items.Clear();
        var before = editPosition.Clone(); editPosition.Apply(move); editMoves.Add(move.Uci);
        editSelected = null; editLastMove = move; BoardPresentation.ClearSelection(board); AppSounds.PlayMove(before, move, editPosition);
        PaintDraft(move); UpdateDraft();
        if (animate) _ = AnimateDraft(before, move, editPosition.Clone());
    }

    private async Task AnimateDraft(Position before, Move move, Position after)
    {
        try { await board.AnimateAsync(before, move, after); }
        catch (Exception ex) when (IsDisposed || Disposing) { _ = ex; }
    }

    private void PaintDraft(Move? move)
    {
        if (editPosition is null) return;
        BoardPresentation.Preview(board, editPosition, !opening.LearnsWhite);
        board.HintMove = move ?? editLastMove; board.InputEnabled = IsEditingMode && modeSelection?.Invoke() is not null;
        evaluation.WhiteAtBottom = opening.LearnsWhite;
        presenter.Show(editPosition, true, false, true, MoveGenerator.GameState(editPosition, new Dictionary<string, int>()));
    }

    private void UpdateDraft()
    {
        if (confirmDraft is null) return;
        var p = editBase?.Clone(); var notation = new List<string>();
        if (p is not null) foreach (string uci in editMoves)
        {
            var move = MoveGenerator.GenerateLegal(p).First(m => m.Uci == uci);
            notation.Add($"{p.Fullmove}{(p.WhiteToMove ? "." : "…")} {SanNotation.Format(p, move)}"); p.Apply(move);
        }
        draftInfo.Text = string.Join("  ", notation);
        if (draftReplies.Items.Count > 0) draftInfo.Text += $"\n{draftReplies.Items.Count} Gegnerantworten vorbereitet · Auswahl mit Häkchen.";
        draftInfo.Visible = DraftDirty;
        draftInfo.Height = Math.Max(30, TextRenderer.MeasureText(draftInfo.Text, draftInfo.Font, new Size(350, 1000), TextFormatFlags.WordBreak).Height + 8);
        draftReplies.Visible = draftReplies.Items.Count > 0;
        draftReplies.Height = Math.Max(28, draftReplies.Items.Count * draftReplies.ItemHeight + 5);
        draftResetRow.Visible = DraftDirty; undoDraft!.Enabled = editMoves.Count > 0;
        confirmDraft.Visible = confirmDraft.Enabled = DraftDirty;
        bestDraft!.Enabled = editPosition is not null;
        generateDraft!.Enabled = editPosition is not null && editPosition.WhiteToMove != opening.LearnsWhite;
        UpdateDeleteControls();
        if (editingExplore is not null) editingExplore.Visible = editingExplore.Enabled = !DraftDirty && modeSelection?.Invoke()?.IsLearnerMove == true;
        UpdateEditorFooter();
    }

    private void UndoDraft()
    {
        if (editBase is null || editMoves.Count == 0) return;
        CancelEditSearch(); editVersion++; board.StopAnimation(); editMoves.RemoveAt(editMoves.Count - 1); draftReplies.Items.Clear();
        editPosition = editBase.Clone(); Move? last = null;
        foreach (string uci in editMoves) { last = MoveGenerator.GenerateLegal(editPosition).First(m => m.Uci == uci); editPosition.Apply(last); }
        editSelected = null; editLastMove = last ?? incomingMoveFor?.Invoke(editPosition); PaintDraft(editLastMove); UpdateDraft(); UpdateDeletePaths();
    }

    private void CancelEditSearch()
    {
        editSearch?.Cancel(); editSearch?.Dispose(); editSearch = null;
    }

    private async Task SearchDraft(bool replies)
    {
        if (!IsEditingMode || editPosition is null || replies && editPosition.WhiteToMove == opening.LearnsWhite) return;
        var p = editPosition.Clone(); var legal = MoveGenerator.GenerateLegal(p);
        if (legal.Count == 0) { draftInfo.Text = "In dieser Stellung gibt es keinen legalen Zug."; return; }
        CancelEditSearch(); editSearch = CancellationTokenSource.CreateLinkedTokenSource(analysis.Token);
        int version = editVersion; var token = editSearch.Token;
        bestDraft!.Enabled = generateDraft!.Enabled = false; draftInfo.Visible = true; draftInfo.Text = "Engine sucht im Hintergrund …";
        try
        {
            var result = await engine.AnalyzeAsync(p.ToFen(), (int)editDepth.Value, replies ? legal.Count : 1, 2, 128, token, 15000);
            await board.OnAnimationThread(() =>
            {
                if (version != editVersion || token.IsCancellationRequested || !IsEditingMode) return;
                if (result.Error is not null) { draftInfo.Text = result.Error; return; }
                if (!replies)
                {
                    var move = legal.FirstOrDefault(m => m.Uci == result.BestMove);
                    if (move is not null) ApplyDraftMove(move, true); else draftInfo.Text = "Engine lieferte keinen legalen Zug.";
                    return;
                }
                draftReplies.Items.Clear();
                var options = new GeneratorOptions { EasyThreshold = (double)editEasy.Value, MaxReplyLoss = (double)editLoss.Value };
                foreach (var line in OpeningGenerator.Plausible(result, options).Where(l => legal.Any(m => m.Uci == l.BestMove)).DistinctBy(l => l.BestMove))
                {
                    var move = legal.First(m => m.Uci == line.BestMove);
                    draftReplies.Items.Add(new DraftReply(move.Uci, SanNotation.Format(p, move), OpeningGenerator.Score(line)), true);
                }
                UpdateDraft();
                if (draftReplies.Items.Count == 0) { draftInfo.Visible = true; draftInfo.Text = "Keine Antwort erfüllt diese Grenzen. Das Opening wurde nicht verändert."; }
            });
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { await board.OnAnimationThread(() => { if (version == editVersion) draftInfo.Text = ex.Message; }); }
        finally { await board.OnAnimationThread(() => { if (version == editVersion && IsEditingMode) { bestDraft!.Enabled = true; generateDraft!.Enabled = editPosition?.WhiteToMove != opening.LearnsWhite; } }); }
    }

    private void ConfirmDraft()
    {
        if (editBase is null || !DraftDirty) return;
        try
        {
            List<IReadOnlyList<string>> lines = draftReplies.Items.Count > 0
                ? draftReplies.CheckedItems.Cast<DraftReply>().Select(r => (IReadOnlyList<string>)[..editMoves, r.Uci]).ToList()
                : [(IReadOnlyList<string>)editMoves.ToArray()];
            if (lines.Count == 0) { draftInfo.Text = "Mindestens eine Gegnerantwort auswählen."; return; }
            BranchEditResult result; bool replace = false, merge = false;
            while (true)
            {
                try { result = BranchGraphEdit.Insert(opening, editBase, lines, replace, merge); break; }
                catch (BranchReplyConflictException ex)
                { if (!ConfirmReplyMerge(ex.Count)) return; merge = true; }
                catch (InvalidOperationException ex) when (ex.Message.Contains("andere eigene Antwort"))
                {
                    if (L.Message(this, "Die vorhandene eigene Antwort und ihre ausschließlich daran hängenden Folgezüge ersetzen? Gemeinsam erreichbare Stellungen bleiben erhalten.", "Zugfolge bestätigen", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
                    replace = true;
                }
            }
            PersistGraph(result.Opening);
            editMoves.Clear(); draftReplies.Items.Clear(); editSelected = null;
            rebuildGraph?.Invoke(); SelectEditedPosition(result.SelectedPosition);
            info.Text = result.CycleSkipped ? "Stellung bereits vorhanden; zyklischer Rückverweis nicht gespeichert." : result.ExistingPosition ? "Arbeitskopie · vorhandene Stellung ausgewählt, kein doppelter Knoten." : $"Arbeitskopie · {result.AddedNodes} neue Knoten.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or InvalidDataException) { draftInfo.Text = ex.Message; }
    }

    private void PersistGraph(OpeningDefinition edited)
    {
        if (!IsEditingMode || editSession is null) throw new InvalidOperationException("Keine aktive Bearbeitungssitzung.");
        if (string.IsNullOrWhiteSpace(opening.FilePath)) throw new InvalidOperationException("Für dieses Opening fehlt der Dateipfad.");
        foreach (var (oldKey, newKey) in BranchGraphEdit.ProgressMapping(opening, edited))
        {
            editSession?.TrackProgressTransfer(oldKey, newKey);
            if (progress.Data.Moves.TryGetValue(oldKey, out var record) && (!progress.Data.Moves.TryGetValue(newKey, out var current) || record.CountedStreak > current.CountedStreak)) progress.Data.Moves[newKey] = record;
            if (progress.Data.BranchPriority.TryGetValue(oldKey, out var priority) && !progress.Data.BranchPriority.ContainsKey(newKey)) progress.Data.BranchPriority[newKey] = priority;
        }
        // This store and file belong to the private edit session, never to the original opening.
        progress.Save();
        BranchGraphEdit.Apply(opening, edited);
        editSession?.MarkChanged();
    }

    internal void InstallDeleteAction(BranchFeatureContext context)
    {
        deleteNode = context.AddButton("Knoten löschen", 900); deleteNode.Visible = false;
        deleteNode.Click += (_, _) => DeleteSelection();
    }

    internal void InstallDeleteWholeNodeAction(BranchFeatureContext context)
    {
        deleteWholeNode = context.AddButton("Gesamten Knoten löschen", 936); deleteWholeNode.Visible = false;
        deleteWholeNode.Click += (_, _) => DeleteWholeSelection();
    }

    private void UpdateDeleteControls()
    {
        var selected = modeSelection?.Invoke();
        bool active = IsEditingMode && !DraftDirty && selected is { Depth: > 0 };
        bool variants = selected?.Alternatives.Select(OpeningStore.PositionKey).Distinct().Count() > 1;
        deleteRow.Visible = active;
        if (deleteNode is not null)
        {
            deleteNode.Visible = deleteNode.Enabled = active;
            deleteNode.Width = variants ? 174 : 360;
        }
        if (deleteWholeNode is not null)
        {
            deleteWholeNode.Visible = deleteWholeNode.Enabled = active && variants;
            deleteWholeNode.Width = 174;
        }
        UpdateDeleteLabel();
    }

    private void UpdateDeletePaths()
    {
        deleteIncoming.Clear();
        if (modeSelection?.Invoke() is { Depth: > 0 } vertex)
        {
            int index = Math.Clamp(modeAlternatives?.SelectedIndex ?? 0, 0, vertex.Alternatives.Count - 1);
            string target = OpeningStore.PositionKey(vertex.Alternatives[index]);
            foreach (var w in GraphOperations.Positions(opening))
                foreach (var edge in w.Node.Moves)
                {
                    var move = MoveGenerator.GenerateLegal(w.Position).First(m => m.Uci == edge.Uci);
                    var after = w.Position.Clone(); after.Apply(move);
                    if (OpeningStore.PositionKey(after) != target) continue;
                    string path = w.Path.Count == 0 ? "Start" : string.Join(" ", w.Path.TakeLast(3));
                    deleteIncoming.Add(new IncomingPath(w.Position, edge.Uci, $"{SanNotation.Format(w.Position, move)} ({edge.Uci}) · nach {path}"));
                }
        }
        UpdateDeleteLabel();
    }

    private void UpdateDeleteLabel()
    {
        if (deleteNode is null) return;
        deleteNode.Text = modeSelection?.Invoke()?.Alternatives.Select(OpeningStore.PositionKey).Distinct().Count() > 1 ? "Stellungsvariante löschen" : "Knoten löschen";
    }

    private void AppendModeMenu(ContextMenuStrip menu)
    {
        if (!IsEditingMode) return;
        menu.Items.Add(new ToolStripSeparator());
        var item = menu.Items.Add(deleteNode?.Text ?? "Knoten löschen");
        item.Enabled = !DraftDirty && modeSelection?.Invoke() is { Depth: > 0 };
        item.Click += (_, _) => DeleteSelection();
        if (modeSelection?.Invoke()?.Alternatives.Select(OpeningStore.PositionKey).Distinct().Count() > 1)
        {
            var whole = menu.Items.Add(L.T("Gesamten Knoten löschen")); whole.Enabled = item.Enabled;
            whole.Click += (_, _) => DeleteWholeSelection();
        }
        UpdateDeletePaths();
        if (deleteIncoming.Count > 1)
        {
            var paths = new ToolStripMenuItem(L.T("Einzelnen Zugweg löschen")) { Enabled = item.Enabled };
            foreach (var path in deleteIncoming)
            {
                var choice = paths.DropDownItems.Add(path.Description);
                choice.Click += (_, _) => DeleteSelectionCore(path, false);
            }
            menu.Items.Add(paths);
        }
    }

    private void DeleteSelection() => DeleteSelectionCore(null, false);
    private void DeleteWholeSelection() => DeleteSelectionCore(null, true);

    private void DeleteSelectionCore(IncomingPath? selectedPath, bool wholeNode)
    {
        if (!IsEditingMode || editSession is null) return;
        var vertex = modeSelection?.Invoke();
        if (vertex is null || vertex.Depth == 0 || DraftDirty) return;
        int selected = Math.Clamp(modeAlternatives?.SelectedIndex ?? 0, 0, vertex.Alternatives.Count - 1);
        var target = vertex.Alternatives[selected];
        string title = wholeNode ? "Gesamten Knoten löschen" : selectedPath is null ? deleteNode?.Text ?? "Knoten löschen" : "Zugweg löschen";
        if (L.Message(this, title + "? Nur noch von dort erreichbare Folgeknoten verschwinden ebenfalls; andere Transpositionswege bleiben erhalten.", "Bestätigen", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        try
        {
            IReadOnlyList<Position> targets = wholeNode ? vertex.Alternatives : [target];
            var targetKeys = targets.Select(OpeningStore.PositionKey).ToHashSet();
            var incoming = GraphOperations.Positions(opening).SelectMany(w => w.Node.Moves.Select(edge =>
            {
                var move = MoveGenerator.GenerateLegal(w.Position).First(m => m.Uci == edge.Uci);
                var after = w.Position.Clone(); after.Apply(move);
                return (Before: w.Position, Move: move, After: after, San: SanNotation.Format(w.Position, move));
            })).Where(w => targetKeys.Contains(OpeningStore.PositionKey(w.After))).ToList();
            var fallback = incoming.FirstOrDefault().Before ?? Position.FromFen(opening.StartFen ?? Position.InitialFen);
            OpeningDefinition edited;
            if (selectedPath is not null)
            {
                fallback = selectedPath.Before;
                try { edited = BranchGraphEdit.DeleteTransition(opening, selectedPath.Before, selectedPath.Uci); }
                catch (BranchReplyConflictException ex)
                { if (!ConfirmReplyMerge(ex.Count)) return; edited = BranchGraphEdit.DeleteTransition(opening, selectedPath.Before, selectedPath.Uci, true); }
            }
            else
            {
                try { edited = BranchGraphEdit.DeletePositions(opening, targets); }
                catch (BranchReplyConflictException ex)
                { if (!ConfirmReplyMerge(ex.Count)) return; edited = BranchGraphEdit.DeletePositions(opening, targets, true); }
            }
            PersistGraph(edited); rebuildGraph?.Invoke(); SelectEditedPosition(fallback); info.Text = "Arbeitskopie geändert · andere gemeinsam erreichbare Knoten bleiben erhalten. Erst Bearbeitung bestätigen speichert das Original.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or InvalidDataException) { info.Text = ex.Message; }
    }

    private bool ConfirmReplyMerge(int count) => L.Message(this, $"{count} unterschiedliche eigene Antworten erreichen dieselbe Stellung. Beim Zusammenführen hat die zuerst gespeicherte Antwort Vorrang. Bestätigen?", "Transpositionen zusammenführen", MessageBoxButtons.YesNo) == DialogResult.Yes;

    private void SelectEditedPosition(Position position)
    {
        if (!canvas.SelectPosition(position, true) || modeSelection?.Invoke() is not { } vertex || modeAlternatives is null) return;
        int alternative = vertex.Alternatives.FindIndex(p => OpeningStore.PositionKey(p) == OpeningStore.PositionKey(position));
        if (alternative >= 0) modeAlternatives.SelectedIndex = alternative;
    }
}
