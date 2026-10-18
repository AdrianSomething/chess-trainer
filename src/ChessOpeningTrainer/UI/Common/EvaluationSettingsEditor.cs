namespace ChessOpeningTrainer;

internal static class EvaluationSettingsEditor
{
    public static Control Create()
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new(20), FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
        var settings = Preferences.Current;
        var defaults = new UserPreferences();
        var defaultValues = new Queue<int>([defaults.EvaluationWorkers, defaults.QuickEvaluationDepth, defaults.LightEvaluationDepth, defaults.HeavyEvaluationDepth]);
        void Number(string label, int value, int max, Action<int> set)
        {
            panel.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new(0, 15, 0, 4) });
            var input = new ScrollNumber { Minimum = 1, Maximum = max, Value = Math.Clamp(value, 1, max), Width = 160 };
            input.ValueChanged += (_, _) => { set((int)input.Value); Preferences.Save(); };
            panel.Controls.Add(SettingsReset.Wrap(input, defaultValues.Dequeue()));
        }
        Number("Gleichzeitige Bewertungsengines", settings.EvaluationWorkers, 3, n => settings.EvaluationWorkers = n);
        Number("Suchtiefe: sofortige Bewertung", settings.QuickEvaluationDepth, 12, n => settings.QuickEvaluationDepth = n);
        Number("Suchtiefe: leichte Bewertung", settings.LightEvaluationDepth, 30, n => settings.LightEvaluationDepth = n);
        Number("Suchtiefe: starke Bewertung", settings.HeavyEvaluationDepth, 40, n => settings.HeavyEvaluationDepth = n);
        panel.Controls.Add(new Label { Text = "1 = sofortig; 2 = zusätzlich leicht; 3 = zusätzlich stark. Änderungen gelten ab der nächsten neuen Analyse. Zeitlimits bleiben bestehen; die erreichte Tiefe kann niedriger sein.", MaximumSize = new(460, 0), AutoSize = true, Margin = new(0, 20, 0, 0) });
        var measure = new Button { Text = "Laufzeit am Beispiel messen", AutoSize = true };
        var result = new Label { AutoSize = true, MaximumSize = new(460, 0), Text = "Realer Test auf diesem PC: Stellung nach 1. e4 e5 2. Sf3. Die Dauer hängt von Stellung, Tiefe und CPU-Auslastung ab." };
        var lifetime = new CancellationTokenSource();
        panel.Disposed += (_, _) => lifetime.Cancel();
        measure.Click += async (_, _) =>
        {
            measure.Enabled = false;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var reports = new List<string>();
            try
            {
                var example = Position.FromFen("rnbqkbnr/pppp1ppp/8/4p3/4P3/5N2/PPPP1PPP/RNBQKB1R b KQkq - 1 2");
                result.Text = "Messung läuft …";
                var final = await AnalysisSearch.Run(new StockfishEngine(AppContext.BaseDirectory), example, 3, r =>
                {
                    reports.Add($"Nach {watch.Elapsed.TotalSeconds:0.000} s: Tiefe {r.Lines.FirstOrDefault()?.Depth ?? 0}");
                    result.Text = string.Join("\n", reports);
                }, lifetime.Token);
                if (!panel.IsDisposed) result.Text += "\n" + (final.Error ?? $"Abgeschlossen nach {watch.Elapsed.TotalSeconds:0.00} s.");
            }
            catch (OperationCanceledException) { }
            finally { if (!panel.IsDisposed) measure.Enabled = true; }
        };
        panel.Controls.AddRange([measure, result]);
        return panel;
    }
}
