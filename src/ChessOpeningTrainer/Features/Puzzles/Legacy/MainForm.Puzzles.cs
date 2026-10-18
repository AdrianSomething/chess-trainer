using ChessMove = ChessOpeningTrainer.Move;

namespace ChessOpeningTrainer;
public sealed partial class MainForm
{
    private void StartPuzzle()
    {
        InvalidateWork();
        explorationHistory.Clear();
        mode = "Rätsel";
        opening = null;
        home.Visible = false;
        game.Visible = true;
        title.Text = "Rätsel · Taktik und Endspiel";
        puzzle = puzzles.Next(difficulty.SelectedItem?.ToString() ?? "Alle");
        reset.Visible = false;
        if (puzzle is null)
        {
            question = false;
            review = false;
            node = null;
            inTree = false;
            position = Position.FromFen(Position.InitialFen);
            snapshots.Clear();
            variations.Clear();
            history.Items.Clear();
            regenIndex = -1;
            Feedback("Für diesen Schwierigkeitsfilter fehlt derzeit ein Rätsel. Wähle auf der Startseite „Alle“ oder warte auf Nachschub. " + puzzles.LastError, null);
            LayoutGame();
            RefreshBoard();
            _ = puzzles.FillAsync(lifetime.Token);
            return;
        }

        position = Position.FromFen(puzzle.Fen);
        humanWhite = position.WhiteToMove;
        node = null;
        inTree = false;
        path = "";
        puzzlePly = 0;
        question = true;
        failed = false;
        review = false;
        snapshots.Clear();
        repetitions.Clear();
        AddSnapshot("Rätselstellung");
        regenIndex = -1;
        variations.Clear();
        ClearSelection();
        board.MarkedSquares.Clear();
        board.Arrows.Clear();
        Feedback(puzzle.Theme + " · Finde die beste Fortsetzung. Mehrere eigene Züge sind nötig.", null);
        LayoutGame();
        RefreshBoard();
        _ = puzzles.FillAsync(lifetime.Token);
    }

    private async Task MatePuzzleMove(ChessMove move, bool animate, bool revealed = false)
    {
        if (puzzle?.MateGoal is not int goal)
            return;
        int version = revision;
        int remaining = goal - puzzlePly / 2;
        var route = puzzle.PreparedMateRoutes?.GetValueOrDefault(MatePreparation.Key(position, remaining))?.FirstOrDefault(r => r.Move == move.Uci);
        if (route is null)
        {
            failed = true;
            Feedback("✕ Dieser Zug erzwingt kein Matt innerhalb des Zuglimits.", false);
            RefreshBoard();
            return;
        }

        if (!revealed)
            AppSounds.Play("success");
        await Play(move.Uci, animate);
        if (version != revision)
            return;
        puzzlePly++;
        if (Terminal())
        {
            FinishPuzzle();
            return;
        }

        var defense = MoveGenerator.GenerateLegal(position).FirstOrDefault(m => m.Uci == route.Defense);
        if (defense is null)
        {
            Feedback("Keine gültige Verteidigung geliefert.", false);
            RefreshBoard();
            return;
        }

        await Play(defense.Uci);
        if (version != revision)
            return;
        puzzlePly++;
        Feedback("✓ Gültiger Mattweg · noch höchstens " + (goal - puzzlePly / 2) + " eigene Züge.", true);
        RefreshBoard();
    }

    private void FinishPuzzle()
    {
        question = false;
        ShowSuggestionsAfterAnswer();
        if (puzzle is not null)
            puzzles.Complete(puzzle.Id);
        Feedback("✓ Rätsel gelöst!", true);
        RefreshBoard();
        Analyze();
        _ = puzzles.FillAsync(lifetime.Token);
    }

    private async Task PuzzleMove(string uci, bool animate = true, bool revealed = false)
    {
        int version = revision;
        if (puzzle is null)
            return;
        if (!revealed)
            AppSounds.Play("success");
        await Play(uci, animate);
        if (version != revision)
            return;
        puzzlePly++;
        if (puzzlePly < puzzle.Moves.Count)
        {
            await Play(puzzle.Moves[puzzlePly]);
            if (version != revision)
                return;
            puzzlePly++;
        }

        if (puzzlePly >= puzzle.Moves.Count)
        {
            question = false;
            ShowSuggestionsAfterAnswer();
            puzzles.Complete(puzzle.Id);
            Feedback(Terminal() ? "✓  Rätsel gelöst! Die Schlussstellung ist erreicht." : "✓  Rätsel gelöst! Du kannst die Enginevarianten jetzt anklicken.", true);
            RefreshBoard();
            Analyze();
            _ = puzzles.FillAsync(lifetime.Token);
        }
        else
        {
            Feedback("✓  Richtig. Finde den nächsten Zug.", true);
            RefreshBoard();
        }
    }
}
