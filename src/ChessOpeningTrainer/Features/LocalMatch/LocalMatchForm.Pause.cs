namespace ChessOpeningTrainer;

public sealed partial class LocalMatchForm
{
    private readonly PauseOverlay pauseOverlay = new();
    // Instance-owned seam lets smoke tests exercise a modal wait without showing dialogs.
    private Func<DialogResult>? leaveConfirmation = null;
    private void SetPaused(bool value)
    {
        paused = value;
        last = watch.ElapsedMilliseconds;
        pauseOverlay.SetPaused(running && paused);
        if (review)
        {
            // Pausing a live clock must not replace the historical board being viewed.
            board.InputEnabled = false;
            refreshActions?.Invoke();
            board.Invalidate();
        }
        else RefreshBoard();
    }
    private bool ConfirmLeave()
    {
        if (!running || game?.Moves.Count is not > 0) return true;
        Tick();
        if (!running) return true;
        bool previous = paused;
        SetPaused(true);
        try
        {
            return (leaveConfirmation?.Invoke() ?? L.Message(this, "Laufende Partie verlassen?", "Zurück", MessageBoxButtons.YesNo)) == DialogResult.Yes;
        }
        finally { if (running) SetPaused(previous); }
    }
}
