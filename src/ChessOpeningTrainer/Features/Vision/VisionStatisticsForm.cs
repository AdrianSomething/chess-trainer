namespace ChessOpeningTrainer;

public sealed class VisionStatisticsForm : LocalizedForm
{
    public VisionStatisticsForm(VisionStatistics.Data data)
    {
        Text = "Schachblick · Statistik"; Size = new(1150, 800); MinimumSize = new(880, 650);
        BackColor = UiPalette.FromArgb(31, 35, 39); ForeColor = UiPalette.Gainsboro;
        var tabs = new ThemedTabControl { Dock = DockStyle.Fill };
        var range = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 42, Padding = new(10, 5, 0, 0), WrapContents = false };
        var from = new ThemedDatePicker { Value = DateTime.Today.AddDays(-399) };
        var through = new ThemedDatePicker { Value = DateTime.Today };
        range.Controls.AddRange([new Label { Text = "Zeitraum", AutoSize = true, Margin = new(0, 5, 8, 0) }, from,
            new Label { Text = "bis", AutoSize = true, Margin = new(8, 5, 8, 0) }, through]);
        Controls.AddRange([tabs, range]);
        VisionStatistics.Data? demoData = null;
        var updates = new List<Action>();
        string[] names = ["Angreifer & Verteidiger", "Angriffe im nächsten Zug", "Gegnerische Angriffe", "Figur retten", "Zugvergleich", "Figuren merken"];
        for (int mode = 0; mode < names.Length; mode++)
        {
            int baseMode = mode;
            var page = new TabPage(names[mode]) { BackColor = BackColor };
            var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 74, Padding = new(10, 4, 0, 0), WrapContents = true };
            var period = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            period.Items.AddRange(["Pro Tag", "Pro 3 Tage", "Pro Woche", "Pro Monat"]); period.SelectedIndex = 0;
            var metric = new ComboBox { Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };
            metric.Items.AddRange(["Durchgänge", "Geschwindigkeit (Figuren/s)", "Gesamte Fehler", "Fehler pro Durchgang", "Fehlerzusammensetzung (%)"]); metric.SelectedIndex = 0;
            int[] metricOrder = [2, 3, 4, 1, 0];
            var level = new ComboBox { Width = 120, DropDownStyle = ComboBoxStyle.DropDownList, Visible = mode == 5 };
            level.Items.AddRange(["Leicht", "Mittel", "Schwer"]); level.SelectedIndex = 0;
            var graph = new CheckBox { Text = "Liniengraph", AutoSize = true, Margin = new(8, 5, 3, 3) };
            var connect = new CheckBox { Text = "Balken verbinden", AutoSize = true, Margin = new(8, 5, 3, 3) };
            var sum = new CheckBox { Text = "Summenlinie", AutoSize = true, Checked = true, Margin = new(8, 5, 3, 3) };
            var resetSelection = new Button { Text = "Alle Fehlertypen", AutoSize = true, Height = 28 };
            toolbar.Controls.AddRange([period, metric, level, graph, connect, sum, resetSelection]);
            var totals = new Label { Dock = DockStyle.Top, Height = 96, Padding = new(10) };
            var chart = new VisionTimelineChart { Dock = DockStyle.Fill, ForeColor = ForeColor, BackColor = BackColor };
            void Update()
            {
                int selected = baseMode + (baseMode == 5 ? level.SelectedIndex : 0);
                var source = Preferences.Current.VisionStatisticsDemo ? demoData ??= VisionDemoData.Create(DateTime.Today) : data;
                DateTime begin = from.Value.Date, end = through.Value.Date;
                var runs = source.Runs.Where(r => r.Mode == selected && r.CompletedUtc.ToLocalTime().Date >= begin && r.CompletedUtc.ToLocalTime().Date <= end).ToArray();
                int allRuns = source.Runs.Count(r => r.Mode == selected);
                totals.Text = $"Zeitraum: {runs.Length} Durchgänge · {runs.Sum(r => r.Errors)} Fehler · " +
                    $"Ø {(runs.Length == 0 ? 0 : runs.Average(r => r.Errors)):0.00} Fehler/Durchgang · Ø {(runs.Length == 0 ? 0 : runs.Average(r => r.Rate)):0.00} Figuren/s\n" +
                    $"Gesamte datierte Durchgänge: {allRuns}. " +
                    (Preferences.Current.VisionStatisticsDemo ? "DEMO – ausschließlich synthetische Testdaten; echte Ergebnisse bleiben unverändert." :
                        source.Records.TryGetValue(selected, out var best) ? $"Rekord: {best.Errors} Fehler · {best.Rate:0.00}/s. Ältere Summen besitzen kein Datum." : "Noch kein Rekord.");
                chart.Runs = runs; chart.Period = period.SelectedIndex; chart.Metric = metricOrder[metric.SelectedIndex];
                chart.ColorCategories = VisionTimelineData.Categories(source.Runs.Where(r => r.Mode == selected));
                bool errorsMetric = chart.Metric is 0 or 1 or 4;
                connect.Enabled = !graph.Checked && errorsMetric;
                sum.Visible = graph.Checked && errorsMetric;
                sum.Enabled = chart.CanShowSum;
                resetSelection.Visible = errorsMetric;
                if (errorsMetric) totals.Text += "\nLegende: Fehlertypen per Klick ein-/ausblenden. Prozentwerte beziehen sich immer auf sämtliche Fehler.";
                chart.GraphMode = graph.Checked; chart.ConnectBars = connect.Checked && !graph.Checked; chart.ShowSum = sum.Checked;
                chart.FromDate = begin; chart.ThroughDate = end; chart.Invalidate();
            }
            updates.Add(Update);
            period.SelectedIndexChanged += (_, _) => Update(); metric.SelectedIndexChanged += (_, _) => Update(); level.SelectedIndexChanged += (_, _) => Update();
            graph.CheckedChanged += (_, _) => Update(); connect.CheckedChanged += (_, _) => Update(); sum.CheckedChanged += (_, _) => Update();
            resetSelection.Click += (_, _) => chart.SelectAllCategories(); chart.SelectionChanged += Update;
            page.Controls.AddRange([chart, totals, toolbar]); tabs.TabPages.Add(page); Update();
        }
        void UpdateAll() { foreach (var update in updates) update(); }
        from.ValueChanged += (_, _) => UpdateAll(); through.ValueChanged += (_, _) => UpdateAll();
        Preferences.Changed += UpdateAll;
        Disposed += (_, _) => Preferences.Changed -= UpdateAll;
    }
}
