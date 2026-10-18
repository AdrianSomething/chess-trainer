namespace ChessOpeningTrainer;
public static class GameAnalysisSettingsEditor
{
    public static Control Create()
    {
        var layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new(14),
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true
        };
        var tips = new ToolTip
        {
            AutoPopDelay = 20000
        };
        layout.Disposed += (_, _) => tips.Dispose();
        var a = Preferences.Current.GameAnalysis;
        var defaults = new GameAnalysisOptions();
        var defaultValues = new Queue<decimal>([defaults.Workers, defaults.Threads, defaults.Depth, (decimal)defaults.Blunder, (decimal)defaults.Mistake, (decimal)defaults.OpportunityTolerance, defaults.MaxOpportunityMoves, (decimal)defaults.EdgeDamping]);
        void Field(string name, decimal value, decimal min, decimal max, int decimals, Action<decimal> set, string description)
        {
            var row = new FlowLayoutPanel
            {
                Width = 500,
                Height = 62
            };
            var label = new Label
            {
                Text = name,
                Width = 330,
                Height = 58
            };
            var input = new ScrollNumber
            {
                Minimum = min,
                Maximum = max,
                DecimalPlaces = decimals,
                Value = Math.Clamp(value, min, max),
                Increment = decimals == 0 ? 1 : .05m,
                Width = 130
            };
            L.SetToolTip(tips, label, description);
            L.SetToolTip(tips, input, description);
            input.ValueChanged += (_, _) =>
            {
                set(input.Value);
                Preferences.Save();
            };
            row.Controls.AddRange([label, SettingsReset.Wrap(input, defaultValues.Dequeue())]);
            layout.Controls.Add(row);
        }

        Field("Parallele Analyse-Arbeiter", a.Workers, 1, 32, 0, v => a.Workers = (int)v, "Persistente Stockfish-Prozesse für gleichzeitig untersuchte Stellungen. Je Arbeiter bis zu 256 MB Hashspeicher.");
        Field("Threads je Analyse-Arbeiter", a.Threads, 1, 32, 0, v => a.Threads = (int)v, "CPU-Suchthreads einer Engine. Gesamtbudget = Arbeiter × Threads; nicht fest zwei.");
        Field("Engine-Suchtiefe", a.Depth, 5, 24, 0, v => a.Depth = (int)v, "Alle legalen Antworten werden bis zu dieser Tiefe untersucht. Kein kurzes Zeitlimit; höhere Werte brauchen erheblich länger.");
        Field("Patzer: gewichtete Verschlechterung", (decimal)a.Blunder, -20, -.1m, 2, v => a.Blunder = (double)v, "Negativer Schwellenwert. Muss negativer als der Schlecht-Schwellenwert sein.");
        Field("Schlecht: gewichtete Verschlechterung", (decimal)a.Mistake, -10, -.05m, 2, v => a.Mistake = (double)v, "Zwischen diesem Schwellenwert und dem besten Zug gilt der Zug als Gut.");
        Field("Verpasste Chance: Bewertungstoleranz", (decimal)a.OpportunityTolerance, 0, 5, 2, v => a.OpportunityTolerance = (double)v, "Nähe zur Stellung vor dem gegnerischen Patzer und Toleranz für gleichwertige gute Antworten, in Bauern.");
        Field("Chance: höchstens gute Antworten", a.MaxOpportunityMoves, 1, 30, 0, v => a.MaxOpportunityMoves = (int)v, "Nur wenn höchstens so viele Züge den neuen Vorteil erhalten, wird eine Rückkehr zum alten Niveau als verpasste Chance gewertet.");
        Field("Dämpfung bei klaren Gewinn-/Verluststellungen", (decimal)a.EdgeDamping, 0, 1, 2, v => a.EdgeDamping = (double)v, "Gewichteter Verlust = Rohverlust / (1 + Dämpfung × Randwert²). Randwert ist die kleinere absolute Bewertung, nur bei gleichem Vorzeichen. 0 schaltet die Dämpfung aus.");
        return layout;
    }
}
