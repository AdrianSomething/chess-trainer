using System.Windows.Media;

namespace ChessOpeningTrainer;

/// <summary>One cached audio file, confined to the audio dispatcher. A failed native
/// media session is replaced on demand, rather than poisoning the sound pack.</summary>
internal sealed class SoundVoice : IDisposable
{
    private readonly string path;
    private readonly Action ready;
    private readonly Action<string> reportError;
    private MediaPlayer? player;
    private TaskCompletionSource? completion;
    private bool opened, failed, requested, countedReady, disposed;
    private DateTime started;
    private TimeSpan previousPosition;
    private int recoveryAttempts;
    private double requestedVolume;
    internal int OutputGeneration { get; private set; }
    internal bool IsPlaying => requested;

    internal SoundVoice(string path, Action ready, Action<string> reportError)
    {
        this.path = path;
        this.ready = ready;
        this.reportError = reportError;
        Open();
    }

    private void Open()
    {
        player?.Close();
        opened = failed = false;
        var output = player = new MediaPlayer { Volume = 0 };
        OutputGeneration++;
        output.MediaOpened += (_, _) =>
        {
            if (player != output || disposed) return;
            opened = true;
            if (!countedReady) { countedReady = true; ready(); }
            if (requested) Start(requestedVolume);
            else { output.Pause(); output.Position = TimeSpan.Zero; }
        };
        output.MediaFailed += (_, e) =>
        {
            if (player == output && !disposed) InvalidateOutput(e.ErrorException.Message);
        };
        output.MediaEnded += (_, _) =>
        {
            if (player != output || !requested || disposed) return;
            // An end notification queued by a previous playback can arrive after
            // Stop/seek/Play. Never mute the newly started sound in that callback.
            if (output.NaturalDuration.HasTimeSpan &&
                output.Position + TimeSpan.FromMilliseconds(35) < output.NaturalDuration.TimeSpan) return;
            requested = false;
            completion?.TrySetResult(); completion = null;
        };
        output.Open(new Uri(path));
    }

    internal Task Play(double volume)
    {
        completion?.TrySetResult(); // Replaying the same event supersedes its waiter.
        completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var result = completion.Task;
        recoveryAttempts = 0;
        try
        {
            if (failed || player is null) Open();
            requested = true;
            Start(volume);
        }
        catch (Exception e) { InvalidateOutput(e.Message); }
        return result;
    }

    private void Start(double volume)
    {
        requestedVolume = volume;
        started = DateTime.UtcNow;
        previousPosition = TimeSpan.Zero;
        // Open is asynchronous. Stop/seek before MediaOpened can invalidate a
        // slow native decoder under CPU load; start exactly when it is ready.
        if (!opened) return;
        player!.Stop();
        player.Position = TimeSpan.Zero;
        player.Volume = volume;
        player.Play();
    }

    internal void SetVolume(double volume) { requestedVolume = volume; if (player is not null) player.Volume = requested ? volume : 0; }

    // Native media can also stall silently after a device/session change. Check
    // only active sounds, retry at most once per event, and never poll while idle.
    internal void CheckPlayback(double volume)
    {
        if (!requested || disposed || DateTime.UtcNow - started < TimeSpan.FromMilliseconds(650)) return;
        if (!opened && DateTime.UtcNow - started < TimeSpan.FromSeconds(5)) return;
        try
        {
            if (opened && player!.NaturalDuration.HasTimeSpan && player.Position >= player.NaturalDuration.TimeSpan - TimeSpan.FromMilliseconds(35))
            {
                requested = false; completion?.TrySetResult(); completion = null; return;
            }
            if (opened && player!.Position > previousPosition)
            {
                previousPosition = player.Position; started = DateTime.UtcNow; return;
            }
            if (recoveryAttempts++ == 0)
            {
                Open(); requested = true; Start(volume);
            }
            else InvalidateOutput("Audiowiedergabe antwortet nicht; erneuter Versuch beim nächsten Klang.");
        }
        catch (Exception e) { InvalidateOutput(e.Message); }
    }

    internal void InvalidateOutput(string message)
    {
        failed = true; requested = false;
        reportError(message);
        completion?.TrySetResult(); completion = null;
    }

    internal void Stop()
    {
        requested = false;
        completion?.TrySetResult(); completion = null;
        try { player?.Stop(); if (player is not null) player.Volume = 0; }
        catch (Exception e) { InvalidateOutput(e.Message); }
    }

    public void Dispose()
    {
        disposed = true; Stop(); player?.Close(); player = null;
    }
}
