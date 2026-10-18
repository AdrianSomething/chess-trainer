namespace ChessOpeningTrainer;

public sealed partial class PuzzleSessionForm
{
    private readonly HistoryPlayback historyPlayback;

    private Task PreviewHistoryAsync(int target, HistoryNavigationKind kind)
    {
        if (target < 0 || target >= snapshots.Count || loading || waitingReply || busy && !historyPlayback.IsPlaying)
            return Task.CompletedTask;
        return historyPlayback.NavigateAsync(snapshots.ToList(), target, kind, (p, _) =>
        {
            review = p.ToFen() == position.ToFen() ? null : p;
            busy = historyPlayback.IsPlaying;
            Clear();
            RefreshBoard();
            if (!question) Analyze();
        });
    }
}
