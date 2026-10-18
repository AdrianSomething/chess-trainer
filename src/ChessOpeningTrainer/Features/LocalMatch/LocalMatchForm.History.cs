namespace ChessOpeningTrainer;

public sealed partial class LocalMatchForm
{
    private readonly HistoryPlayback historyPlayback;

    private Task PreviewHistoryAsync(int target, HistoryNavigationKind kind)
    {
        if (target < 0 || target >= positions.Count) return Task.CompletedTask;
        return historyPlayback.NavigateAsync(positions.ToList(), target, kind, (p, shown) =>
        {
            review = shown != positions.Count - 1;
            board.Position = p;
            board.HintMove = shown > 0 ? HistoryPlayback.FindMove(positions[shown - 1], p) : null;
            board.Outcome = MoveGenerator.GameState(p, review ? new Dictionary<string, int>() : repetitions);
            if (!review && !running && game?.Tags.GetValueOrDefault("Result") == "1/2-1/2") board.Outcome = "Remis";
            board.InputEnabled = running && !paused && !review && !historyPlayback.IsPlaying;
            board.Invalidate();
            refreshActions?.Invoke();
        });
    }
}
