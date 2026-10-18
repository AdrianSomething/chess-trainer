namespace ChessOpeningTrainer;

public sealed partial class VisionForm
{
    private readonly PauseOverlay pauseOverlay = new() { CoverContent = true, Dock = DockStyle.Fill };
    // Instance-owned modal seam; tests do not need to accept real message boxes.
    private Func<DialogResult>? leaveConfirmation = null;
    private void SetTrainingPaused(bool value)
    {
        paused = value;
        if (board.Parent is Control body) body.Enabled = !value;
        board.Answering = !value && !instruction && !feedbackPending && exercise is not null;
        pauseOverlay.SetPaused(value);
        pause.Text = value ? "Fortsetzen" : "Pause";
        if (value) clock.Stop(); else if (board.Answering) clock.Start();
    }
    private bool ConfirmLeaveTraining()
    {
        if (menu.Visible || instruction || rounds >= 30 || exercise is null) return true;
        bool previous = paused, wasAnswering = board.Answering, wasRunning = clock.IsRunning;
        bool leaving = false;
        var previousExercise = exercise;
        SetTrainingPaused(true);
        try
        {
            leaving = (leaveConfirmation?.Invoke() ?? L.Message(this, "Laufenden Durchgang wirklich verlassen?", "Training verlassen", MessageBoxButtons.YesNo)) == DialogResult.Yes;
            return leaving;
        }
        finally
        {
            if (!IsDisposed && !leaving)
            {
                SetTrainingPaused(previous);
                if (ReferenceEquals(exercise, previousExercise))
                {
                    board.Answering = wasAnswering;
                    if (!wasRunning) clock.Stop();
                }
            }
        }
    }
}
