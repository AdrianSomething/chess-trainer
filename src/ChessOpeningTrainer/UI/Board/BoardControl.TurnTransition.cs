namespace ChessOpeningTrainer;

public partial class BoardControl
{
    private readonly System.Windows.Forms.Timer turnTimer = new() { Interval = 15 };
    private readonly System.Diagnostics.Stopwatch turnClock = new();
    private double turnFrom, turnTarget, turnDuration;
    private bool turnInitialized;
    private double TurnWeight => !turnInitialized ? (displayedFen?.Split(' ')[1] == "w" ? 1 : 0)
        : turnFrom + (turnTarget - turnFrom) * Math.Clamp(turnClock.Elapsed.TotalMilliseconds / Math.Max(1, turnDuration), 0, 1);

    private void StartTurnTransition(bool white, int duration)
    {
        turnFrom = TurnWeight;
        turnTarget = white ? 1 : 0;
        turnDuration = duration * Math.Clamp(Preferences.Current.AnimationPercent, 0, 200) / 100d;
        if (!turnInitialized)
            turnTimer.Tick += (_, _) => { Invalidate(); if (turnClock.Elapsed.TotalMilliseconds >= turnDuration) turnTimer.Stop(); };
        turnInitialized = true;
        turnClock.Restart();
        turnTimer.Start();
    }
}
