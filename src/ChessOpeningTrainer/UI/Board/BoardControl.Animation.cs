using System.Drawing.Drawing2D;
using ChessMove = ChessOpeningTrainer.Move;

namespace ChessOpeningTrainer;
public partial class BoardControl
{
    private readonly int animationThread = Environment.CurrentManagedThreadId;
    private volatile bool animationHandleDestroyed;
    private CancellationTokenSource animationLifetime = new();
    private bool animationDisposed;
    internal bool AnimationAvailable => !IsDisposed && !Disposing && !animationHandleDestroyed;

    protected override void OnHandleCreated(EventArgs e)
    {
        if (animationLifetime.IsCancellationRequested)
        {
            animationLifetime.Dispose();
            animationLifetime = new();
        }
        animationHandleDestroyed = false;
        base.OnHandleCreated(e);
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        // Handle destruction precedes the disposed bit while a parent closes.
        // A pending frame must be cancelled, not mistaken for a fresh off-thread start.
        animationHandleDestroyed = true;
        if (!animationDisposed) animationLifetime.Cancel();
        animationId++;
        animated.Clear();
        if (!Disposing && !IsDisposed) turnTimer.Stop();
        base.OnHandleDestroyed(e);
    }

    public void StopAnimation()
    {
        if (!AnimationAvailable) return;
        if (Environment.CurrentManagedThreadId != animationThread)
        {
            if (!IsHandleCreated)
            {
                if (!AnimationAvailable) return;
                throw new InvalidOperationException("Board animation must start on the board's UI thread.");
            }
            try { Invoke((Action)StopAnimation); }
            catch (Exception ex) when ((ex is ObjectDisposedException or InvalidOperationException) && !AnimationAvailable) { }
            return;
        }
        animationId++;
        animated.Clear();
        Invalidate();
    }

    public Task AnimateAsync(Position before, ChessMove move, Position after, int milliseconds = 250)
    {
        List<(char Piece, int From, int To)> pieces = [(before.Squares[move.From], move.From, move.To)];
        if (move.IsCastle)
        {
            int rookFrom = move.To > move.From ? move.From + 3 : move.From - 4;
            int rookTo = move.To > move.From ? move.From + 1 : move.From - 1;
            pieces.Add((before.Squares[rookFrom], rookFrom, rookTo));
        }

        return AnimatePiecesAsync(after, pieces, milliseconds);
    }

    private async Task AnimatePiecesAsync(Position target, List<(char Piece, int From, int To)> pieces, int milliseconds)
    {
        int id = -1;
        await OnAnimationThread(() =>
        {
            // Pin continuations to this control's actual owner, not an ambient
            // SynchronizationContext left behind by a previously closed dialog.
            if (!IsHandleCreated) _ = Handle;
            StartTurnTransition(target.WhiteToMove, milliseconds);
            milliseconds = (int)Math.Round(milliseconds * Math.Clamp(Preferences.Current.AnimationPercent, 0, 200) / 100d);
            id = ++animationId;
            Position = target;
            turnDuration = milliseconds;
            animationFraction = 0;
            animated = pieces;
        }).ConfigureAwait(false);
        if (id < 0) return;

        var timer = System.Diagnostics.Stopwatch.StartNew();
        while (timer.ElapsedMilliseconds < milliseconds && AnimationAvailable)
        {
            bool active = false;
            await OnAnimationThread(() =>
            {
                if (id != animationId) return;
                active = true;
                animationFraction = Math.Clamp(timer.Elapsed.TotalMilliseconds / milliseconds, 0, 1);
                Invalidate();
            }).ConfigureAwait(false);
            if (!active) return;
            await Task.Delay(15).ConfigureAwait(false);
        }

        await OnAnimationThread(() =>
        {
            if (id != animationId) return;
            animated.Clear();
            Invalidate();
        }).ConfigureAwait(false);
    }

    internal async Task OnAnimationThread(Action update)
    {
        if (!AnimationAvailable) return;
        if (Environment.CurrentManagedThreadId == animationThread) { update(); return; }
        if (!IsHandleCreated)
        {
            if (!AnimationAvailable) return;
            throw new InvalidOperationException("Board animation must start on the board's UI thread.");
        }
        CancellationToken handleToken = default;
        try
        {
            // Closing a window can discard a posted native message. Tie the
            // awaiter to the HWND lifetime so a cancelled replay cannot hang.
            handleToken = animationLifetime.Token;
            await this.InvokeAsync(() => { if (!handleToken.IsCancellationRequested && AnimationAvailable) update(); }, handleToken).ConfigureAwait(false);
        }
        catch (Exception ex) when ((ex is ObjectDisposedException or InvalidOperationException or OperationCanceledException) && (!AnimationAvailable || handleToken.IsCancellationRequested)) { }
    }
}
