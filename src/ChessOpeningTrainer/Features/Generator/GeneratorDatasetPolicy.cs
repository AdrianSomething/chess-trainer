namespace ChessOpeningTrainer;

/// <summary>Frequency evidence only changes opponent branches. Missing/undersampled positions fall back to engine rules.</summary>
public sealed class GeneratorDatasetPolicy : IDisposable
{
    private readonly DatasetIndex? index;
    public GeneratorDatasetPolicy(GeneratorOptions options)
    {
        bool enabled = options.MinimumCommonReplies > 0 || options.StopBelowGames > 0 || options.Sections.Any(s => s.MinimumCommonReplies > 0 || s.StopBelowGames > 0);
        if (enabled)
        {
            if (options.DatasetIndexFile is null || !File.Exists(options.DatasetIndexFile))
                throw new InvalidOperationException(L.T("Für diese Datensatzfilter muss zuerst ein Datensatzbaum erstellt werden."));
            index = new DatasetIndex(options.DatasetIndexFile);
        }
    }
    public (HashSet<string> Continue, bool Stop) Decide(Position position, GeneratorOptions effective)
    {
        if (index is null || position.WhiteToMove == effective.LearnWhite) return ([], false);
        long games = index.Counts(position).Total;
        if (games < Math.Max(1, effective.MinimumDatasetGames)) return ([], false);
        bool stop = effective.StopBelowGames > 0 && games < effective.StopBelowGames;
        var common = index.Children(position).Where(e => e.Counts.Total > 0).Take(Math.Min(effective.MinimumCommonReplies, effective.ComplexThreshold)).Select(e => e.Move).ToHashSet();
        return (common, stop);
    }
    public void Dispose() => index?.Dispose();
}
