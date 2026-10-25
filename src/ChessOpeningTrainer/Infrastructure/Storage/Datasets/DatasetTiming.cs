namespace ChessOpeningTrainer;

/// <summary>Independent progress clock anchored to an operation's pause-aware time.</summary>
internal sealed class DatasetTiming(Func<double> clock, double origin)
{
    private readonly object sync = new();
    private long lastDone, lastTotal;
    private double lastSeconds, secondsPerUnit;
    public string Describe(long done, long total)
    {
        double seconds, remainingSeconds;
        lock (sync)
        {
            seconds = Math.Max(0, clock() - origin);
            if (total != lastTotal || done < lastDone)
            {
                lastDone = 0; lastSeconds = 0; secondsPerUnit = 0;
            }
            if (done > lastDone && seconds - lastSeconds >= .5)
            {
                double recent = (seconds - lastSeconds) / (done - lastDone);
                secondsPerUnit = secondsPerUnit == 0 ? recent : .65 * secondsPerUnit + .35 * recent;
                lastDone = done; lastSeconds = seconds; lastTotal = total;
            }
            remainingSeconds = Math.Max(0, total - done) * (secondsPerUnit > 0 ? secondsPerUnit : seconds / Math.Max(1, done));
        }
        static string Duration(double value)
        {
            var time = TimeSpan.FromSeconds(Math.Min(8640000, value));
            return $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}";
        }
        string remaining = done > 0 && total > done ? Duration(remainingSeconds) : done >= total && total > 0 ? "00:00:00" : "…";
        return L.T("Vergangen") + ": " + Duration(seconds) + " · " + L.T("Verbleibend") + ": " + remaining;
    }
}

