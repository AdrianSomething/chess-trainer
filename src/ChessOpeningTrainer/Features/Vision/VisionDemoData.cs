namespace ChessOpeningTrainer;

/// <summary>Explicit, deterministic display fixture. Never saved as player statistics.</summary>
public static class VisionDemoData
{
    public static VisionStatistics.Data Create(DateTime throughDate, int seed = 160400)
    {
        const int days = 400;
        var result = new VisionStatistics.Data();
        var random = new Random(seed);
        var activity = Enumerable.Range(0, days).Select(day => day % 87 is >= 65 and < 77 ? 0d :
            Math.Max(.05, 1.5 + 1.2 * Math.Sin(day / 16d) + .9 * Math.Sin(day / 5.3))).ToArray();
        double scale = 2 / activity.Average();
        int sinceBreak = 100;
        for (int day = 0; day < days; day++)
        {
            if (activity[day] == 0) { sinceBreak = 0; continue; }
            sinceBreak++;
            double relapse = Math.Exp(-sinceBreak / 7d);
            double learning = day / (days - 1d);
            for (int mode = 0; mode < 8; mode++)
            {
                double expected = activity[day] * scale + (random.NextDouble() - .5) * 1.4;
                int count = Math.Clamp((int)Math.Floor(expected) + (random.NextDouble() < expected - Math.Floor(expected) ? 1 : 0), 0, 10);
                for (int n = 0; n < count; n++)
                {
                    int mistakes = Math.Max(0, (int)Math.Round(8 - 6 * learning + 4 * relapse +
                        (mode >= 5 ? (mode - 5) * 1.4 : mode == 1 ? 1.5 : 0) + (random.NextDouble() - .5) * 4));
                    double rate = Math.Max(.15, .65 + 1.6 * learning - .45 * relapse + (random.NextDouble() - .5) * .3 - (mode >= 5 ? .2 * (mode - 5) : 0));
                    DateTime local = throughDate.Date.AddDays(day - days + 1).AddHours(9 + n).AddMinutes(random.Next(60));
                    var run = new VisionStatistics.Run
                    {
                        Id = $"demo-{seed}-{day}-{mode}-{n}", Mode = mode, Errors = mistakes, Rate = rate,
                        CompletedUtc = DateTime.SpecifyKind(local, DateTimeKind.Local).ToUniversalTime()
                    };
                    for (int error = 0; error < mistakes; error++)
                    {
                        (char piece, string pattern) = Error(mode, learning, random);
                        string key = $"{mode}:{piece} · {pattern}";
                        run.Missed[key] = run.Missed.GetValueOrDefault(key) + 1;
                        result.Missed[key] = result.Missed.GetValueOrDefault(key) + 1;
                    }
                    result.Runs.Add(run);
                    if (!result.Records.TryGetValue(mode, out var best) || mistakes < best.Errors || mistakes == best.Errors && rate > best.Rate)
                        result.Records[mode] = new() { Errors = mistakes, Rate = rate };
                }
            }
        }
        return result;
    }
    private static (char Piece, string Pattern) Error(int mode, double learning, Random random)
    {
        char piece = "PNBRQK"[random.Next(6)];
        if (mode >= 5) return (piece, random.Next(3) switch { 0 => "Figur nicht wiederaufgebaut", 1 => "Falscher Figurentyp", _ => "Zusätzliche Figur auf leerem Feld" });
        if (mode == 3) return (piece, random.Next(3) == 0 ? "Illegales Zielfeld" : "Zielfeld bleibt angegriffen");
        if (mode == 4) return (piece, "Schlechterer Vergleichszug");
        if (mode == 1) return (piece, random.NextDouble() < .25 + .4 * learning ? "Freiziehen übersehen" : "Umpositionierung übersehen");
        if (random.NextDouble() < .12) return (piece, "Nicht vorhandenen Angriff markiert");
        if (random.NextDouble() < .1 + .3 * learning) return ('Q', "Röntgenlinie übersehen");
        return piece switch { 'N' => (piece, "Springersprung"), 'P' => (piece, "Bauernangriff"), 'K' => (piece, "Königsnähe"), _ => (piece, "Linienangriff") };
    }
}
