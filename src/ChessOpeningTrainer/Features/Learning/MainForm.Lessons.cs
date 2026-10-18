using ChessMove = ChessOpeningTrainer.Move;

namespace ChessOpeningTrainer;
public sealed partial class MainForm
{
    private void ShowSuggestionsAfterAnswer()
    {
        if (suggestionPreference == false)
            return;
        settingSuggestions = true;
        suggestions.Checked = true;
        settingSuggestions = false;
    }

    private void Start(OpeningDefinition item, string newMode) => StartFocused(item, newMode, null);
    private void StartFocused(OpeningDefinition item, string newMode, List<OpeningStore.LearningTarget>? focused)
    {
        InvalidateWork();
        opening = item;
        mode = newMode;
        puzzle = null;
        humanWhite = item.LearnsWhite;
        targets.Clear();
        targets.AddRange(OpeningStore.LearningTargets(item));
        branchLessons.Clear();
        if (focused is not null)
            foreach (var target in focused.Where(t => progress.Data.BranchPriority.GetValueOrDefault(t.Key) != "Ausgeschlossen"))
                branchLessons.Enqueue(target);
        home.Visible = false;
        game.Visible = true;
        title.Text = item.Name + " · " + mode;
        ResetPosition();
        LayoutGame();
        if (mode == "Lernen")
            NextQuestion();
        else
        {
            question = false;
            Feedback("Spiele dein Repertoire.", null);
            Advance();
        }
    }

    private void ResetPosition()
    {
        position = Position.FromFen(opening?.StartFen ?? Position.InitialFen);
        node = opening?.Tree;
        path = opening?.Id ?? "";
        inTree = node is not null;
        question = false;
        failed = false;
        review = false;
        awaitingContinuation = false;
        targetKey = null;
        lessonPositionIndex = lessonAnswerIndex = -1; history.LessonIndices.Clear();
        selected = null;
        snapshots.Clear();
        repetitions.Clear();
        AddSnapshot("Startstellung");
        regenIndex = -1;
        board.MarkedSquares.Clear();
        board.Arrows.Clear();
    }

    private async void NextQuestion()
    {
        if (opening is null)
            return;
        bool switchingVariant = forcedTarget is not null, previousFailure = failed;
        var previousGroup = currentGroup;
        var previous = snapshots.ToList();
        if (review)
        {
            int viewed = previous.FindLastIndex(s => s.Position.ToFen() == Display.ToFen());
            if (viewed >= 0)
                previous = previous.Take(viewed + 1).ToList();
            else
            {
                foreach (var ancestor in explorationHistory)
                {
                    int match = previous.FindLastIndex(s => s.Position.ToFen() == ancestor.ToFen());
                    if (match >= 0)
                    {
                        previous = previous.Take(match + 1).ToList();
                        break;
                    }
                }
            }
        }

        InvalidateWork();
        int version = revision;
        var eligible = targets.Where(t => progress.Eligible(t.Key)).ToList();
        // Targets are in depth-first order: finish a new branch before its siblings.
        if (targets.Any(t => progress.Data.BranchPriority.GetValueOrDefault(t.Key)is not ("Später" or "Ausgeschlossen") && progress.Status(t.Key) != "Gelernt"))
            eligible = eligible.Where(t => progress.Data.BranchPriority.GetValueOrDefault(t.Key) != "Später").ToList();
        var target = forcedTarget ?? (branchLessons.TryDequeue(out var focused) ? focused : null) ?? eligible.FirstOrDefault(t => progress.Status(t.Key) == "Unbekannt") ?? eligible.OrderBy(t => t.Key == targetKey ? 1 : 0).ThenBy(t => progress.Status(t.Key) == "Zu Wiederholen" ? 0 : 1).ThenBy(t => progress.Get(t.Key).LastAttemptUtc).FirstOrDefault();
        forcedTarget = null;
        if (target is null)
        {
            question = false;
            next.Visible = false;
            Feedback("Für heute ist alles erledigt. Neue Wiederholungen werden mit Abstand fällig.", true);
            Analyze();
            RefreshBoard();
            return;
        }

        ResetPosition();
        explorationHistory.Clear();
        currentGroup = switchingVariant ? previousGroup : progress.Data.GroupSimilarReplies ? eligible.Where(t => LearningGroups.Signature(t) == LearningGroups.Signature(target)).ToList() : [target];
        if (!currentGroup.Contains(target))
            currentGroup.Add(target);
        updatingCardVariations = true;
        cardVariations.Items.Clear();
        foreach (var t in currentGroup)
            cardVariations.Items.Add("Gegenvariante " + LearningGroups.VariantName(opening, t) + " · gleiche Antwort");
        cardVariations.SelectedIndex = currentGroup.IndexOf(target);
        updatingCardVariations = false;
        foreach (var d in target.Prefix)
            Apply(d.Uci, d, false);
        targetKey = target.Key;
        lessonPositionIndex = snapshots.Count - 1;
        question = true;
        failed = switchingVariant && previousFailure;
        Feedback("Dein Lernzug", null);
        evaluation.Visible = false;
        variations.Clear();
        RefreshBoard();
        int common = -1;
        for (int i = 0; i < Math.Min(previous.Count, snapshots.Count); i++)
        {
            if (previous[i].Position.ToFen() != snapshots[i].Position.ToFen())
                break;
            common = i;
        }

        if (common < 0)
            common = 0;
        busy = true;
        board.InputEnabled = false;
        for (int i = common + 1; i < snapshots.Count && version == revision; i++)
        {
            int animationCount = snapshots.Count - common - 1;
            if (animationCount > 1 && i == snapshots.Count - 1 && Preferences.Current.AnimationPercent > 0)
                await Task.Delay(200);
            if (version != revision || IsDisposed) return;
            var before = snapshots[i - 1].Position;
            var after = snapshots[i].Position;
            var move = MoveGenerator.GenerateLegal(before).First(m =>
            {
                var p = before.Clone();
                p.Apply(m);
                return p.ToFen() == after.ToFen();
            });
            AppSounds.PlayMove(before, move, after);
            await board.AnimateAsync(before, move, after, HistoryPlayback.DurationMilliseconds(i - common - 1, animationCount, false));
        }

        if (version == revision)
        {
            busy = false;
            RefreshBoard();
        }
    }

    private async void Reveal()
    {
        int version = revision;
        if (mode == "Rätsel" && question && puzzle?.MateGoal is int)
        {
            var route = puzzle.PreparedMateRoutes?.GetValueOrDefault(MatePreparation.Key(position, puzzle.MateGoal.Value - puzzlePly / 2))?.FirstOrDefault();
            var m = MoveGenerator.GenerateLegal(position).FirstOrDefault(m => m.Uci == route?.Move);
            if (m is not null)
                await MatePuzzleMove(m, true, true);
            return;
        }

        if (mode == "Rätsel" && question)
        {
            failed = true;
            var uci = puzzle?.Moves.ElementAtOrDefault(puzzlePly);
            if (uci is not null)
                await PuzzleMove(uci, true, true);
            return;
        }

        var d = node?.Moves.FirstOrDefault();
        if (d is null)
            return;
        if (question && mode == "Lernen" && !failed && targetKey is not null)
            foreach (var t in currentGroup)
                progress.MarkWrong(t.Key);
        failed = true;
        question = false;
        ShowSuggestionsAfterAnswer();
        Feedback("Lösung · " + SanNotation.Format(position, Legal(d.Uci)), null);
        awaitingContinuation = mode == "Lernen" && !automaticEngine.Checked;
        if (mode == "Lernen") { lessonAnswerIndex = snapshots.Count; history.LessonIndices.Add(lessonAnswerIndex); }
        await Play(d.Uci);
        if (version == revision)
            Advance();
    }

    private void Advance()
    {
        RefreshBoard();
        if (awaitingContinuation)
        {
            board.Arrows.Clear();
            foreach (var d in node?.Moves ?? [])
            {
                var m = Legal(d.Uci);
                board.Arrows.Add(new BoardArrow(m.From, m.To));
            }

            board.Invalidate();
            Analyze();
            return;
        }

        if (Terminal())
        {
            Analyze();
            return;
        }

        if (position.WhiteToMove != humanWhite)
        {
            // An unanswered card's prefix is still prepared normally. Once
            // answered, either both sides are manual or the opponent is automatic.
            if (mode != "Lernen" || automaticEngine.Checked || question)
                _ = OpponentMove();
            else Analyze();
            return;
        }

        if (!question)
            Analyze();
    }
}
