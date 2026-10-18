namespace ChessOpeningTrainer;

public sealed record VisionErrorCategory(string Key, char Piece, string Pattern);
public sealed record VisionTimelineSample(DateTime Date, int RunCount, int TotalErrors, double Rate, IReadOnlyDictionary<string, int> Counts);

/// <summary>Control-free aggregation; filtering never changes the all-errors denominator.</summary>
public static class VisionTimelineData
{
    public const string LegacyKey = "Auswahlfehler ohne Detailprotokoll (vor V16)";
    public const string PieceOrder = "PNBRQK";
    public static string CategoryKey(string key) => key.Contains(':') ? key.Split(':', 2)[1] : key;
    public static VisionErrorCategory[] Categories(IEnumerable<VisionStatistics.Run> source)
    {
        var runs = source.ToArray();
        var keys = runs.SelectMany(r => r.Missed.Where(p => p.Value > 0).Select(p => p.Key)).Select(CategoryKey).Distinct().ToList();
        if (runs.Any(r => r.Errors > r.Missed.Values.Sum()) && !keys.Contains(LegacyKey)) keys.Add(LegacyKey);
        return keys.Select(key =>
        {
            bool piece = key.Length > 1 && PieceOrder.Contains(key[0]) && key[1] is ' ' or '·';
            return new VisionErrorCategory(key, piece ? key[0] : '\0', piece ? key[1..].TrimStart(' ', '·') : key);
        }).OrderBy(c => c.Piece == '\0' ? 6 : PieceOrder.IndexOf(c.Piece)).ThenBy(c => c.Pattern, StringComparer.Ordinal).ToArray();
    }
    public static DateTime Bucket(DateTime localDate, int period)
    {
        DateTime day = localDate.Date;
        return period switch
        {
            3 => new(day.Year, day.Month, 1),
            2 => day.AddDays(-((int)day.DayOfWeek + 6) % 7),
            1 => day.AddDays(-(((day - new DateTime(2000, 1, 1)).Days % 3 + 3) % 3)),
            _ => day
        };
    }
    public static VisionTimelineSample[] Build(VisionStatistics.Run[] source, int period, DateTime? from = null, DateTime? through = null)
    {
        if (source.Length == 0) return [];
        var groups = source.GroupBy(r => Bucket(r.CompletedUtc.ToLocalTime(), period)).ToDictionary(g => g.Key, g => g.ToArray());
        DateTime first = Bucket(from ?? groups.Keys.Min(), period), last = Bucket(through ?? groups.Keys.Max(), period);
        var result = new List<VisionTimelineSample>();
        for (DateTime date = first; date <= last && result.Count < 2000;)
        {
            var runs = groups.GetValueOrDefault(date, []);
            var counts = runs.SelectMany(r => r.Missed).GroupBy(p => CategoryKey(p.Key)).ToDictionary(g => g.Key, g => g.Sum(p => p.Value));
            int unclassified = runs.Sum(r => Math.Max(0, r.Errors - r.Missed.Values.Sum()));
            if (unclassified > 0) counts[LegacyKey] = unclassified;
            int total = Math.Max(runs.Sum(r => r.Errors), counts.Values.Sum());
            result.Add(new(date, runs.Length, total, runs.Length == 0 ? double.NaN : runs.Average(r => r.Rate), counts));
            if (date >= last) break;
            date = period switch { 3 => date.AddMonths(1), 2 => date.AddDays(7), 1 => date.AddDays(3), _ => date.AddDays(1) };
        }
        return result.ToArray();
    }
    public static double? Value(VisionTimelineSample sample, int metric, string? category = null)
    {
        int errors = category is null ? sample.TotalErrors : sample.Counts.GetValueOrDefault(category);
        return metric switch
        {
            0 => sample.TotalErrors == 0 ? null : 100d * errors / sample.TotalErrors,
            1 => sample.RunCount == 0 ? null : (double)errors / sample.RunCount,
            2 => sample.RunCount,
            3 => sample.RunCount == 0 ? null : sample.Rate,
            4 => sample.RunCount == 0 ? null : errors,
            _ => null
        };
    }
}
