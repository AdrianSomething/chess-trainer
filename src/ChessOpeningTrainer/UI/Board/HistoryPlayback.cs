namespace ChessOpeningTrainer;

public enum HistoryNavigationKind { Animated, Reverse, Immediate }

/// <summary>Recorded-board navigation shared by training and games; never changes their live game.</summary>
public sealed class HistoryPlayback
{
    private readonly BoardControl board;
    private int revision;
    public int CurrentIndex { get; private set; } = -1;
    public bool IsPlaying { get; private set; }

    public HistoryPlayback(BoardControl board)
    {
        this.board = board;
        board.HandleDestroyed += OnHandleDestroyed;
        board.Disposed += OnBoardDisposed;
    }

    private void OnHandleDestroyed(object? sender, EventArgs e)
    {
        revision++;
        IsPlaying = false;
    }

    private void OnBoardDisposed(object? sender, EventArgs e)
    {
        OnHandleDestroyed(sender, e);
        board.HandleDestroyed -= OnHandleDestroyed;
        board.Disposed -= OnBoardDisposed;
    }

    public void Cancel()
    {
        revision++;
        IsPlaying = false;
        board.StopAnimation();
    }

    public async Task NavigateAsync(IReadOnlyList<Position> positions, int target, HistoryNavigationKind kind,
        Action<Position, int> show, int? startIndex = null, Action<int>? assessedMove = null)
    {
        if (positions.Count == 0 || !board.AnimationAvailable) return;
        target = Math.Clamp(target, 0, positions.Count - 1);
        int start = startIndex ?? (IsPlaying ? CurrentIndex : FindDisplayed(positions));
        start = Math.Clamp(start, 0, positions.Count - 1);
        if (kind == HistoryNavigationKind.Animated && target < start) start = Math.Max(0, target - 1);
        int version = ++revision;
        board.StopAnimation();
        // Commit immediately: a repeated key advances from this destination,
        // snapping the incomplete preceding animation to its actual final board.
        if (IsPlaying) board.Position = positions[start].Clone();
        CurrentIndex = target;
        IsPlaying = kind != HistoryNavigationKind.Immediate && start != target;
        show(positions[target].Clone(), target);
        if (!IsPlaying) return;
        int direction = target > start ? 1 : -1, count = Math.Abs(target - start);
        try
        {
            for (int n = 0, source = start; n < count; n++, source += direction)
            {
                if (version != revision || !board.AnimationAvailable) return;
                if (count > 1 && n == count - 1 && Preferences.Current.AnimationPercent > 0)
                    await Task.Delay(200).ConfigureAwait(false);
                if (version != revision || !board.AnimationAvailable) return;
                int destination = source + direction;
                Position before = positions[Math.Min(source, destination)], after = positions[Math.Max(source, destination)];
                var move = FindMove(before, after, board.MoveProvider?.Invoke(before));
                Task animation = Task.CompletedTask;
                await board.OnAnimationThread(() =>
                {
                    if (version != revision || !board.AnimationAvailable) return;
                    show(positions[destination].Clone(), destination);
                    if (move is null) return;
                    AppSounds.PlayMove(before, move, after, royalKings: board.MoveProvider is null);
                    int duration = DurationMilliseconds(n, count, direction < 0);
                    animation = direction > 0 ? board.AnimateAsync(before, move, after, duration)
                        : board.AnimateReverseAsync(after, move, before, duration);
                    board.HintMove = move;
                    board.Invalidate();
                }).ConfigureAwait(false);
                await animation.ConfigureAwait(false);
                if (version != revision || !board.AnimationAvailable) return;
                int completed = destination;
                await board.OnAnimationThread(() =>
                {
                    if (version != revision || !board.AnimationAvailable) return;
                    // Restore the selected snapshot's own last-move marker/badge.
                    show(positions[completed].Clone(), completed);
                    assessedMove?.Invoke(completed);
                }).ConfigureAwait(false);
            }
        }
        finally
        {
            if (version == revision && board.AnimationAvailable)
                await board.OnAnimationThread(() =>
                {
                    if (version != revision || !board.AnimationAvailable) return;
                    IsPlaying = false;
                    show(positions[target].Clone(), target);
                }).ConfigureAwait(false);
        }
    }

    private int FindDisplayed(IReadOnlyList<Position> positions)
    {
        string? fen = board.Position?.ToFen();
        for (int i = positions.Count - 1; i >= 0; i--)
            if (positions[i].ToFen() == fen) return i;
        return Math.Max(0, CurrentIndex);
    }

    public static int DurationMilliseconds(int step, int count, bool reverse)
    {
        int forward = step == count - 1 ? 250 : Math.Max(60, (int)(180 * Math.Pow(.8, step)));
        return reverse ? forward / 2 : forward;
    }

    public static Move? FindMove(Position before, Position after, IEnumerable<Move>? candidates = null)
    {
        string fen = after.ToFen();
        return (candidates ?? MoveGenerator.GenerateLegal(before)).FirstOrDefault(move =>
        {
            var result = before.Clone(); result.Apply(move);
            return result.ToFen() == fen;
        });
    }
}
