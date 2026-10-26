namespace ChessOpeningTrainer;

/// <summary>Merge-phase progress, independent of the preceding game scan and pauses.</summary>
internal sealed class DatasetMergeProgress(DatasetOperation operation, int runCount)
{
    internal int Passes { get; } = CountPasses(runCount);
    private double started;
    internal static int CountPasses(int runs)
    {
        int passes = 0;
        while (runs > 1) { runs = (runs + 15) / 16; passes++; }
        return passes;
    }
    internal void StartPass() => started = operation.ActiveSeconds;
    internal string Describe(int pass, int group, int groups, long consumed, long total)
    {
        double seconds = Math.Max(0, operation.ActiveSeconds - started);
        // Later passes may coalesce additional duplicates. Until those pass sizes
        // are known, use the current input size and throughput as a conservative ETA.
        double workLeft = Math.Max(0, total - consumed) + (double)total * (Passes - pass);
        // Opening the compressed readers is startup cost, not per-record work.
        // Do not extrapolate the first one or two entries into hours of waiting.
        string remaining = workLeft == 0 ? "00:00:00"
            : consumed >= Math.Min(total, 1024) && seconds >= .25
                ? Duration(seconds * workLeft / consumed) : "…";
        return L.T("Sortierblöcke zusammenführen") + $": {L.T("Durchlauf")} {pass}/{Passes} · {L.T("Blockgruppe")} {group}/{groups}"
            + $" · {consumed:N0}/{total:N0} {L.T("Einträge")} · {L.T("Verbleibend")} ≈ {remaining}";
    }
    private static string Duration(double seconds)
    {
        var t = TimeSpan.FromSeconds(Math.Clamp(seconds, 0, 8640000));
        return $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";
    }
}
