namespace ChessOpeningTrainer;

public abstract partial class VisionChallengeForm
{
    private readonly PauseOverlay pauseOverlay = new() { CoverContent = true, Dock = DockStyle.Fill };
    private Func<DialogResult>? leaveConfirmation = null;
    internal void PauseFromHost() { if (Playing && !paused) SetTrainingPaused(true); }
    private void SetTrainingPaused(bool value)
    {
        paused = value; Content.Enabled = !value;
        pauseOverlay.SetPaused(value);
        if (value) Clock.Stop(); else if (Playing) { Clock.Start(); OnResumed(); }
        pause.IconName = value ? "play" : "pause"; pause.AccessibleName = value ? "Fortsetzen" : "Pause";
    }
    private bool ConfirmLeaveTraining()
    {
        if (introduction || Round >= 30) return true;
        bool previous = paused;
        bool leaving = false;
        SetTrainingPaused(true);
        try
        {
            leaving = (leaveConfirmation?.Invoke() ?? L.Message(this, "Laufenden Durchgang wirklich verlassen?", "Training verlassen", MessageBoxButtons.YesNo)) == DialogResult.Yes;
            return leaving;
        }
        // A preparation can finish behind the modal. Do not resume/reveal it on a confirmed exit.
        finally { if (!IsDisposed && !leaving) SetTrainingPaused(previous); }
    }
}
