namespace ChessOpeningTrainer;

public sealed partial class MainForm
{
    // Restore a tutorial checkpoint synchronously, without replaying earlier
    // user input, animations or writes to the real learning profile.
    private void RestoreTutorialLesson(OpeningDefinition item, OpeningStore.LearningTarget target, bool answered)
    {
        InvalidateWork();
        opening = item; mode = "Lernen"; puzzle = null; humanWhite = item.LearnsWhite;
        automaticEngine.Checked = false;
        targets.Clear(); targets.AddRange(OpeningStore.LearningTargets(item));
        branchLessons.Clear(); forcedTarget = null;
        home.Visible = false; game.Visible = true; title.Text = item.Name + " · " + mode;
        ResetPosition(); explorationHistory.Clear(); currentGroup = [target];
        updatingCardVariations = true;
        cardVariations.Items.Clear(); cardVariations.Items.Add("Gegenvariante " + LearningGroups.VariantName(item, target) + " · gleiche Antwort");
        cardVariations.SelectedIndex = 0; updatingCardVariations = false;
        foreach (var move in target.Prefix) Apply(move.Uci, move, false);
        targetKey = target.Key; lessonPositionIndex = snapshots.Count - 1;
        question = !answered; busy = false;
        board.HintMove = null; variations.Clear();
        if (answered && node?.Moves.FirstOrDefault() is { } answer)
        {
            lessonAnswerIndex = snapshots.Count; history.LessonIndices.Add(lessonAnswerIndex);
            Apply(answer.Uci, answer, false); awaitingContinuation = true;
            ShowSuggestionsAfterAnswer(); Feedback("Lösung", null);
        }
        else { evaluation.Visible = false; Feedback("Dein Lernzug", null); }
        LayoutGame(); RefreshBoard();
        if (answered) Analyze();
    }
}
