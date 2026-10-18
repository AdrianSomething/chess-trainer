namespace ChessOpeningTrainer;

public sealed partial class MainForm
{
    private readonly HistoryPlayback historyPlayback;

    private async Task PreviewHistoryAsync(int target, HistoryNavigationKind kind)
    {
        if (target < 0 || target >= snapshots.Count || waitingReply || busy && !historyPlayback.IsPlaying)
            return;
        bool wasReview = review;
        int version = revision;
        await historyPlayback.NavigateAsync(snapshots.Select(s => s.Position).ToList(), target, kind, (p, _) =>
        {
            explored = p;
            review = p.ToFen() != position.ToFen();
            SelectDisplayedHistory();
            busy = historyPlayback.IsPlaying;
            ClearSelection();
            RefreshBoard();
            Analyze();
        });
        if (wasReview && version == revision && !IsDisposed)
            ResumeAutomaticPlayAfterReview();
    }

    private void ResumeAutomaticPlayAfterReview()
    {
        if (mode == "Lernen" && automaticEngine.Checked && !question && !review
            && !busy && !waitingReply && !historyPlayback.IsPlaying && position.WhiteToMove != humanWhite)
            Advance();
    }
}
