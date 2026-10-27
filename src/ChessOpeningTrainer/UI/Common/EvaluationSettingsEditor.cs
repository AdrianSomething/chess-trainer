namespace ChessOpeningTrainer;

internal static class EvaluationSettingsEditor
{
    public static Control Create()
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new(20), FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
        panel.Controls.Add(EngineSelectionEditor.Create());
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
        CancellationTokenSource? measurement = null;
        var exact = new CheckBox { Text = "Bis zur Zieltiefe messen (ohne Zeitlimit)", Checked = true, AutoSize = true };
        var tips = new ToolTip { AutoPopDelay = 25000 };
        L.SetToolTip(tips, exact, "Aktiv: alle eingestellten Suchtiefen tatsächlich messen, auch wenn dies lange dauert. Inaktiv: die zeitbegrenzte Hintergrundanalyse messen. Erneutes Drücken bricht nur die Messung ab.");
        panel.Disposed += (_, _) => { lifetime.Cancel(); measurement?.Cancel(); tips.Dispose(); };
        measure.Click += async (_, _) =>
        {
            if (measurement is not null) { measurement.Cancel(); return; }
            using var run = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token); measurement = run;
            measure.Text = L.T("Messung abbrechen"); exact.Enabled = false;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var reports = new Dictionary<int, string>();
            try
            {
                var example = Position.FromFen("rnbqkbnr/pppp1ppp/8/4p3/4P3/5N2/PPPP1PPP/RNBQKB1R b KQkq - 1 2");
                result.Text = L.T("Messung läuft …");
                var engine = new StockfishEngine(AppContext.BaseDirectory);
                if (exact.Checked)
                {
                    var depths = new[] { settings.QuickEvaluationDepth, settings.LightEvaluationDepth, settings.HeavyEvaluationDepth }.Take(settings.EvaluationWorkers).ToArray();
                    var updates = new Progress<(int Index, int Depth, bool Done)>(value =>
                    {
                        if (panel.IsDisposed || run.IsCancellationRequested) return;
                        reports[value.Index] = L.T($"Ziel {depths[value.Index]} · erreicht {value.Depth} · {watch.Elapsed.TotalSeconds:0.000} s") + (value.Done ? " ✓" : " …");
                        result.Text = string.Join("\n", reports.OrderBy(x => x.Key).Select(x => x.Value));
                    });
                    await Task.WhenAll(depths.Select(async (depth, index) =>
                    {
                        await using var pool = new EnginePool(engine, 1, index == 2 ? Math.Clamp(Environment.ProcessorCount - 2, 1, 6) : 1, run.Token);
                        var r = await pool.Search(example, depth, 3, run.Token, d => ((IProgress<(int,int,bool)>)updates).Report((index,d,false)));
                        if (r.Error is not null) throw new IOException(r.Error);
                        ((IProgress<(int,int,bool)>)updates).Report((index,r.Lines.FirstOrDefault()?.Depth ?? 0,true));
                    }));
                }
                else
                {
                    var final = await AnalysisSearch.Run(engine, example, 3, r =>
                    {
                        reports[reports.Count] = L.T($"Nach {watch.Elapsed.TotalSeconds:0.000} s: Tiefe {r.Lines.FirstOrDefault()?.Depth ?? 0}");
                        result.Text = string.Join("\n", reports.Values);
                    }, run.Token);
                    if (final.Error is not null) throw new IOException(final.Error);
                }
                if (!panel.IsDisposed) result.Text += "\n" + L.T($"Abgeschlossen nach {watch.Elapsed.TotalSeconds:0.00} s.");
            }
            catch (OperationCanceledException) { if (!panel.IsDisposed) result.Text += "\n" + L.T("Messung abgebrochen."); }
            catch (Exception ex) when (ex is IOException or InvalidOperationException) { if (!panel.IsDisposed) result.Text = ex.Message; }
            finally { measurement = null; if (!panel.IsDisposed) { measure.Text = L.T("Laufzeit am Beispiel messen"); exact.Enabled = true; } }
        };
        panel.Controls.Add(exact);
        panel.Controls.AddRange([measure, result]);
        return panel;
    }
}
