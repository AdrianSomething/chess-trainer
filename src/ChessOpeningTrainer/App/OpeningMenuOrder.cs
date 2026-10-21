namespace ChessOpeningTrainer;

internal static class OpeningMenuOrder
{
    public static IEnumerable<OpeningDefinition> Sort(IEnumerable<OpeningDefinition> openings, IReadOnlySet<string> bookmarks) =>
        openings.OrderByDescending(o => bookmarks.Contains(o.Id))
            .ThenBy(o => o.Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenByDescending(o => File.Exists(o.FilePath) ? File.GetCreationTimeUtc(o.FilePath) : DateTime.MinValue)
            .ThenBy(o => o.Id, StringComparer.Ordinal);
}
