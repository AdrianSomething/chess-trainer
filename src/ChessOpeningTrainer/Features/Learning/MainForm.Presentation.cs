using ChessMove = ChessOpeningTrainer.Move;

namespace ChessOpeningTrainer;
public sealed partial class MainForm
{
    private void LayoutGame()
    {
        bool showStats = mode == "Lernen" && showDatasetStats.Checked && !question;
        int size = Math.Max(mode == "Lernen" ? 300 : 360, Math.Min(ClientSize.Height - (mode is "Lernen" or "Freeplay" ? 160 : 115) - (showStats ? 44 : 0), ClientSize.Width - 470));
        board.SetBounds(68, 92, size, size);
        title.Width = Math.Max(300, ClientSize.Width - 90);
        evaluation.SetBounds(16, 92, 44, size);
        datasetStats.SetBounds(68, board.Bottom + 2, size, 42);
        next.SetBounds(16, board.Bottom + 10 + (showStats ? 44 : 0), size + 120 - 68, 40);
        reveal.Bounds = next.Bounds;
        int x = size + 90, w = ClientSize.Width - x - 20;
        book.SetBounds(x, 92, w, 48);
        feedback.SetBounds(x, 145, w, 80);
        lessonProgress.SetBounds(x, 48, w, 42);
        cardVariations.SetBounds(x, 118, w, 24);
        int rows = (int)Math.Ceiling(actions.Controls.Cast<Control>().Count(c => c.Visible) / (double)Math.Max(1, w / 172));
        branch.SetBounds(x, 235, w, 36);
        automaticEngine.SetBounds(x, branch.Bottom + 8, w, 26);
        int actionsTop = automaticEngine.Visible ? automaticEngine.Bottom + 4 : branch.Bottom + 8;
        actions.SetBounds(x, actionsTop, w, rows * 44);
        opponentSettings?.SetBounds(x, actions.Bottom + 2, w, 64);
        int top = actions.Bottom + 72;
        int returnSpace = mode is "Lernen" or "Freeplay" ? 88 : 44;
        int variantHeight = Math.Clamp(ClientSize.Height - returnSpace - top - 148, 66, 126);
        suggestions.Location = new Point(x, top);
        showDatasetStats.Visible = mode == "Lernen";
        showDatasetStats.SetBounds(x + Math.Max(0, w - 125), top, 125, 24);
        suggestions.AutoSize = false; suggestions.Width = Math.Max(100, w - (showDatasetStats.Visible ? 130 : 0));
        variantCaption.SetBounds(x, top + 28, w, 23);
        variations.SetBounds(x, top + 52, w, variantHeight);
        int historyTop = suggestions.Visible ? top + (variations.Visible ? 58 + variantHeight : 56) : top;
        historyCaption.SetBounds(x, historyTop, w, 23);
        history.SetBounds(x, historyTop + 26, w, Math.Max(0, ClientSize.Height - historyTop - 34 - returnSpace));
        int returnY = ClientSize.Height - (mode is "Lernen" or "Freeplay" ? 90 : 48);
        resume.SetBounds(x, returnY, w, 38);
        lessonReturn.SetBounds(x, returnY, w, 38);
        reset.SetBounds(x, returnY, w, 38);
        exploreBack.SetBounds(x, ClientSize.Height - 48, w, 38);
        freeFromHere.Bounds = exploreBack.Bounds;
    }

    private void Feedback(string message, bool? success)
    {
        if (success == false)
            AppSounds.Play("error");
        feedback.Text = message;
        feedback.BackColor = success == true ? UiPalette.FromArgb(35, 100, 65) : success == false ? UiPalette.FromArgb(145, 48, 48) : UiPalette.FromArgb(44, 51, 57);
    }

    private void RefreshBoard()
    {
        lessonProgress.Visible = mode == "Lernen" && opening is not null;
        if (opening is not null)
        {
            lessonProgress.Counts = progress.Counts(opening);
            lessonProgress.Invalidate();
        }

        variations.Flip = !humanWhite;
        board.Position = Display;
        datasetBinding?.Refresh();
        if (!review && snapshots.Count > 1 && board.SelectedSquare is null)
        {
            var before = snapshots[^2].Position;
            board.HintMove = MoveGenerator.GenerateLegal(before).FirstOrDefault(m =>
            {
                var after = before.Clone(); after.Apply(m);
                return after.ToFen() == Display.ToFen();
            });
        }
        board.PremoveSide = waitingReply ? humanWhite : null;
        board.Flip = !humanWhite;
        board.InputEnabled = (!busy || waitingReply) && (review || !Terminal() && !(mode == "Rätsel" && puzzle is null) && (position.WhiteToMove == humanWhite || waitingReply || CanExploreCounter));
        board.Invalidate();
        evaluation.WhiteAtBottom = humanWhite;
        evaluation.Invalidate();
        var state = MoveGenerator.GameState(Display, review ? new Dictionary<string, int>() : repetitions);
        board.Outcome = state;
        book.Text = mode == "Rätsel" ? "Rätsel · " + puzzle?.Origin + " · " + puzzle?.Difficulty + " · " + (question ? "Lösen" : "Gelöst") : inTree ? (node?.Moves.Count == 0 ? "Ende dieser Buchvariante" : "Im Opening-Spielbaum") : "Außerhalb des Baums · Stockfish";
        next.Text = mode == "Rätsel" ? "Nächstes Rätsel  ↵" : question ? "Lösung zeigen - Leertaste" : "Nächste Karte - Leertaste";
        automaticEngine.Visible = mode == "Lernen";
        freeFromHere.Visible = mode == "Freeplay" && review;
        freeFromHere.Enabled = !busy;
        cardVariations.Visible = mode == "Lernen" && question && currentGroup.Count > 1;
        cardVariations.Enabled = !busy;
        next.Visible = mode == "Rätsel" || mode == "Lernen" && (question || branchLessons.Count > 0 || targets.Any(t => progress.Eligible(t.Key)));
        next.Enabled = !question || !busy;
        retry.Visible = failed && question;
        reveal.Visible = mode != "Lernen" && !review && !busy && position.WhiteToMove == humanWhite && (question || mode == "Freeplay" && inTree && node?.Moves.Count > 0);
        regenerate.Visible = regenIndex >= 0 && !question && !review;
        regenerate.Enabled = !busy;
        reset.Visible = mode == "Freeplay" && !review;
        reset.Enabled = !busy;
        resume.Visible = review && !(mode == "Lernen" && question);
        lessonReturn.Visible = mode == "Lernen" && (question || !review) && LearningReturnIndex >= 0 && LearningReturnIndex < snapshots.Count && Display.ToFen() != snapshots[LearningReturnIndex].Position.ToFen();
        exploreBack.Visible = CanResetCurrentLessonPosition;
        exploreBack.Enabled = !busy;
        branch.Visible = opening is not null;
        branch.Enabled = !busy;
        suggestions.Visible = (!question || review) && !HideUnansweredLines;
        evaluation.Visible = !question || review;
        variantCaption.Visible = (!question || review) && !HideUnansweredLines;
        variations.Visible = (!question || review) && suggestions.Checked && !HideUnansweredLines;
        if (review)
            book.Text = "Freie Analyse · beide Seiten spielbar · Training bleibt unverändert";
        LayoutGame();
    }

    private void PreviewBoard(Position p, bool remember = true)
    {
        if (busy && !historyPlayback.IsPlaying)
            return;
        if (historyPlayback.IsPlaying) { historyPlayback.Cancel(); busy = false; }
        if (remember && Display.ToFen() != p.ToFen())
            explorationHistory.Push(Display.Clone());
        explored = p.Clone();
        review = p.ToFen() != position.ToFen();
        SelectDisplayedHistory();
        ClearSelection();
        RefreshBoard();
        Analyze();
    }

    private void Analyze(Position? source = null)
    {
        if (IsDisposed || question && !review)
            return;
        presenter ??= new AnalysisPresenter(engine, evaluation, variations);
        var p = source ?? Display;
        presenter.Show(p, true, suggestions.Checked && !HideUnansweredLines,
            review || mode == "Lernen" && !automaticEngine.Checked || p.WhiteToMove == humanWhite,
            MoveGenerator.GameState(p, review ? new Dictionary<string, int>() : repetitions));
    }
}
