namespace ChessOpeningTrainer;

public sealed partial class MainForm
{
    private int lessonPositionIndex = -1, lessonAnswerIndex = -1;
    private readonly Button lessonReturn = Button("Zur Lernstellung");
    private bool HideUnansweredLines => mode == "Lernen" && question;
    private int LearningReturnIndex => question || lessonAnswerIndex < 0 ? lessonPositionIndex : lessonAnswerIndex;
    private int DisplayedHistoryIndex => snapshots.FindLastIndex(s => s.Position.ToFen() == Display.ToFen());
    private bool CanResetCurrentLessonPosition => mode == "Lernen" && !question && review && lessonAnswerIndex >= 0 &&
        DisplayedHistoryIndex >= lessonAnswerIndex && DisplayedHistoryIndex < snapshots.Count - 1;

    private void SelectDisplayedHistory()
    {
        listUpdating = true;
        try { history.SelectedIndex = DisplayedHistoryIndex; }
        finally { listUpdating = false; }
    }

    private void ResetCurrentLessonPosition()
    {
        if (CanResetCurrentLessonPosition) ContinueAtDisplayedPosition();
    }

    private Position NearestOpeningPosition()
    {
        if (opening is null) return Display;
        var known = GraphOperations.Positions(opening).Select(w => BranchPositionIdentity.Key(w.Position)).ToHashSet();
        foreach (var p in new[] {Display}.Concat(explorationHistory))
        {
            if (known.Contains(BranchPositionIdentity.Key(p))) return p.Clone();
            int anchor = snapshots.FindLastIndex(s => s.Position.ToFen() == p.ToFen());
            if (anchor >= 0)
            {
                foreach (var s in snapshots.Take(anchor+1).Reverse())
                    if (known.Contains(BranchPositionIdentity.Key(s.Position))) return s.Position.Clone();
            }
        }
        foreach (var s in snapshots.AsEnumerable().Reverse())
            if (known.Contains(BranchPositionIdentity.Key(s.Position))) return s.Position.Clone();
        return Position.FromFen(opening.StartFen ?? Position.InitialFen);
    }

    private void PreviewVariation(IReadOnlyList<Position> route)
    {
        if (route.Count == 0 || busy && !historyPlayback.IsPlaying) return;
        if (Display.ToFen() != route[0].ToFen()) explorationHistory.Push(Display.Clone());
        foreach (var p in route.Take(route.Count-1)) explorationHistory.Push(p.Clone());
        PreviewBoard(route[^1], remember:false);
    }

    private void ContinueFreeplayHere()
    {
        if (mode != "Freeplay") return;
        ContinueAtDisplayedPosition();
    }

    private void ContinueAtDisplayedPosition()
    {
        var desired = Display.Clone();
        int anchor = snapshots.FindLastIndex(s => s.Position.ToFen() == desired.ToFen());
        var extension = new List<Position>();
        if (anchor < 0)
        {
            var route = explorationHistory.Reverse().Append(desired).ToArray();
            for (int i = route.Length - 2; i >= 0 && anchor < 0; i--)
            {
                anchor = snapshots.FindLastIndex(s => s.Position.ToFen() == route[i].ToFen());
                if (anchor >= 0) extension.AddRange(route.Skip(i + 1));
            }
        }
        if (anchor < 0) return; // Never replace live history with an unconnected FEN.
        var check = snapshots[anchor].Position.Clone(); var moves = new List<string>();
        foreach (var after in extension)
        {
            var move = MoveGenerator.GenerateLegal(check).FirstOrDefault(m => { var p = check.Clone(); p.Apply(m); return p.ToFen() == after.ToFen(); });
            if (move is null) return;
            moves.Add(move.Uci); check = after;
        }
        InvalidateWork();
        var snapshot = snapshots[anchor]; position = snapshot.Position.Clone(); node = snapshot.Node; path = snapshot.Path; inTree = snapshot.InTree;
        snapshots.RemoveRange(anchor + 1, snapshots.Count - anchor - 1);
        history.LessonIndices.RemoveWhere(i => i >= snapshots.Count);
        review = question = awaitingContinuation = false; explored = null; explorationHistory.Clear();
        repetitions.Clear(); foreach (var s in snapshots) repetitions[s.Position.RepetitionKey()] = repetitions.GetValueOrDefault(s.Position.RepetitionKey()) + 1;
        foreach (string uci in moves) Apply(uci, paint: false);
        listUpdating = true; history.Items.Clear(); foreach (var s in snapshots) history.Items.Add(s.Text);
        history.SetPositions(snapshots.Select(s => s.Position)); history.SelectedIndex = snapshots.Count - 1; listUpdating = false;
        ClearSelection(); RefreshBoard(); Advance();
    }
}
