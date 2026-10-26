using ChessMove = ChessOpeningTrainer.Move;

namespace ChessOpeningTrainer;
public sealed partial class MainForm
{
    private bool CanExploreCounter => mode == "Lernen" && !question && !automaticEngine.Checked && position.WhiteToMove != humanWhite;
    private void ClickSquare(int square)
    {
        if (busy && !waitingReply || (!review && (Terminal() || mode == "Rätsel" && puzzle is null || position.WhiteToMove != humanWhite && !waitingReply && !CanExploreCounter)))
            return;
        if (selected is int from)
        {
            if (from == square)
            {
                ClearSelection();
                return;
            }

            if (TryMove(from, square))
                return;
        }

        if (Display.Squares[square] != '\0' && char.IsUpper(Display.Squares[square]) == (waitingReply ? humanWhite : Display.WhiteToMove))
        {
            selected = square;
            board.SelectedSquare = square;
            board.CandidateMoves = waitingReply ? Premoves.Candidates(Display, square, humanWhite) : MoveGenerator.GenerateLegal(Display).Where(m => m.From == square).ToList();
            board.Invalidate();
        }
        else
            ClearSelection();
    }

    private void DragSquare(int from, int to)
    {
        draggingMove = true;
        if (waitingReply || !busy && (review || !Terminal() && !(mode == "Rätsel" && puzzle is null) && (position.WhiteToMove == humanWhite || CanExploreCounter)))
            TryMove(from, to);
        draggingMove = false;
        ClearSelection();
    }

    private bool TryMove(int from, int to)
    {
        if (waitingReply && !review)
        {
            premove = Premoves.Candidates(Display, from, humanWhite).FirstOrDefault(m => m.To == to);
            board.QueuedMove = premove;
            ClearSelection();
            return premove is not null;
        }

        var options = MoveGenerator.GenerateLegal(Display).Where(m => m.From == from && m.To == to).ToList();
        if (options.Count == 0)
            return false;
        var move = options[0];
        if (options.Count > 1)
        {
            var chosen = board.ChoosePromotion(this, options, Display.WhiteToMove);
            if (chosen is null)
                return true;
            move = chosen;
        }

        ClearSelection();
        UserMove(move);
        return true;
    }

    private void ClearSelection()
    {
        selected = null;
        BoardPresentation.ClearSelection(board);
    }

    private async void UserMove(ChessMove move)
    {
        bool animate = !draggingMove;
        int version = revision;
        if (CanExploreCounter && !review)
        {
            // A manually chosen counter becomes the actual continuation.
            awaitingContinuation = false;
            board.Arrows.Clear();
        }
        if (review)
        {
            var before = Display.Clone();
            var after = before.Clone();
            after.Apply(move);
            AppSounds.PlayMove(before, move, after);
            PreviewBoard(after);
            if (animate)
            {
                busy = true;
                RefreshBoard();
                await board.AnimateAsync(before, move, after);
                if (version == revision)
                {
                    busy = false;
                    RefreshBoard();
                }
            }

            return;
        }

        if (mode == "Rätsel" && question && puzzle?.MateGoal is int)
        {
            await MatePuzzleMove(move, animate);
            return;
        }

        string? expected = mode == "Rätsel" && question ? puzzle?.Moves.ElementAtOrDefault(puzzlePly) : node?.Moves.FirstOrDefault()?.Uci;
        bool test = question || (mode == "Freeplay" && inTree && node?.Moves.Count > 0);
        if (test && move.Uci != expected)
        {
            if (question && mode == "Lernen" && !failed && targetKey is not null)
                foreach (var t in currentGroup)
                    progress.MarkWrong(t.Key);
            failed = true;
            Feedback("✕ Falsch", false);
            RefreshBoard();
            return;
        }

        if (mode == "Rätsel" && question)
        {
            await PuzzleMove(move.Uci, animate);
            return;
        }

        if (question && mode == "Lernen")
        {
            if (!failed && targetKey is not null)
                progress.MarkGroupCorrect(currentGroup.Select(t => t.Key));
            AppSounds.Play("success");
            question = false;
            lessonAnswerIndex = snapshots.Count; history.LessonIndices.Add(lessonAnswerIndex);
            awaitingContinuation = !automaticEngine.Checked;
            ShowSuggestionsAfterAnswer();
            Feedback("✓ Richtig · " + SanNotation.Format(position, move), true);
        }
        else
            Feedback("Dein Zug: " + SanNotation.Format(position, move), null);
        await Play(move.Uci, animate);
        if (version == revision)
            Advance();
    }

    private ChessMove Legal(string uci) => MoveGenerator.GenerateLegal(position).First(m => m.Uci == uci);
    private void Apply(string uci, OpeningMove? definition = null, bool paint = true)
    {
        analysis.Cancel();
        variations.Clear();
        evaluation.Score = null;
        evaluation.TextValue = "…";
        var m = Legal(uci);
        string text = $"{position.Fullmove}{(position.WhiteToMove ? "." : "...")} {SanNotation.Format(position, m)}";
        definition ??= inTree ? node?.Moves.FirstOrDefault(d => d.Uci == uci) : null;
        if (definition is not null)
        {
            node = definition.Next;
            path += "/" + uci;
        }
        else
        {
            inTree = false;
            node = null;
        }

        position.Apply(m);
        AddSnapshot(text);
        if (paint)
            RefreshBoard();
    }

    private async Task Play(string uci, bool animate = true)
    {
        int version = revision;
        var before = position.Clone();
        var move = Legal(uci);
        bool wasBusy = busy;
        var after = before.Clone();
        after.Apply(move);
        AppSounds.PlayMove(before, move, after);
        Apply(uci);
        if (animate)
        {
            busy = true;
            RefreshBoard();
            await board.AnimateAsync(before, move, position.Clone());
            if (version == revision)
            {
                busy = wasBusy;
                RefreshBoard();
            }
        }
    }

    private void AddSnapshot(string text)
    {
        snapshots.Add(new(position.Clone(), node, path, inTree, text));
        var key = position.RepetitionKey();
        repetitions[key] = repetitions.GetValueOrDefault(key) + 1;
        listUpdating = true;
        history.Items.Clear();
        foreach (var s in snapshots)
            history.Items.Add(s.Text);
        history.SetPositions(snapshots.Select(s => s.Position));
        history.SelectedIndex = snapshots.Count - 1;
        listUpdating = false;
    }

    private async Task OpponentMove(bool regenerateMove = false)
    {
        if (waitingReply)
            return;
        waitingReply = true;
        busy = true;
        RefreshBoard();
        int version = revision;
        var fen = position.ToFen();
        string? uci;
        if (inTree && node?.Moves.Count > 0)
        {
            var choices = node.Moves.Where(m => !regenerateMove || m.Uci != lastOpponent).ToList();
            if (choices.Count == 0)
                choices = node.Moves;
            regenIndex = node.Moves.Count > 1 ? snapshots.Count - 1 : -1;
            uci = choices[Random.Shared.Next(choices.Count)].Uci;
            lastOpponent = uci;
        }
        else
        {
            regenIndex = -1;
            inTree = false;
            var result = await EngineOpponentMove.Choose(engine, position.Clone(), (int)learningDepth.Value, (int)learningIntelligence.Value, opponent.Token);
            if (version != revision || position.ToFen() != fen || IsDisposed)
                return;
            uci = result.Move?.Uci;
            if (uci is null || !MoveGenerator.GenerateLegal(position).Any(m => m.Uci == uci))
            {
                waitingReply = false;
                busy = false;
                Feedback("Engine konnte keinen Zug liefern: " + result.Error, false);
                RefreshBoard();
                return;
            }
        }

        await Play(uci);
        if (version != revision)
            return;
        waitingReply = false;
        busy = false;
        var queued = premove;
        premove = null;
        board.QueuedMove = null;
        RefreshBoard();
        if (queued is not null)
        {
            var legal = MoveGenerator.GenerateLegal(position).FirstOrDefault(m => m.Uci == queued.Uci);
            if (legal is not null)
            {
                draggingMove = true;
                UserMove(legal);
                draggingMove = false;
                return;
            }
        }

        if (!question)
            Analyze();
    }

    private void Regenerate()
    {
        if (busy || regenIndex < 0 || regenIndex >= snapshots.Count)
            return;
        InvalidateWork();
        var s = snapshots[regenIndex];
        position = s.Position.Clone();
        node = s.Node;
        path = s.Path;
        inTree = s.InTree;
        snapshots.RemoveRange(regenIndex + 1, snapshots.Count - regenIndex - 1);
        repetitions.Clear();
        foreach (var snap in snapshots)
        {
            var key = snap.Position.RepetitionKey();
            repetitions[key] = repetitions.GetValueOrDefault(key) + 1;
        }

        _ = OpponentMove(true);
    }

    private bool Terminal()
    {
        var state = MoveGenerator.GameState(position, repetitions);
        return state != "" && state != "Schach";
    }
}
