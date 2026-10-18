namespace ChessOpeningTrainer;

/// <summary>Persisted Schachblick options, shared by the hub and training windows.</summary>
public sealed class VisionSettingsEditor : UserControl
{
    public VisionSettingsEditor()
    {
        var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new(18), AutoScroll = true };
        var indirect = new CheckBox { Text = "Indirekte Verteidiger (Röntgenlinien) mitzählen", AutoSize = true, Checked = Preferences.Current.VisionIndirectDefenders, Margin = new(0, 8, 0, 8) };
        var description = new Label { Text = "Gleichgerichtete Läufer, Türme oder Damen hinter einer verteidigenden Figur zählen ebenfalls als Verteidiger. Diese Option gilt für Angreifer & Verteidiger und Gegnerische Angriffe.", AutoSize = true, MaximumSize = new(470, 0), Margin = new(0, 0, 0, 22) };
        var row = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Margin = new(0), Padding = new(0, 3, 0, 8) };
        row.Controls.Add(new Label { Text = "Figuren merken", AutoSize = true, Margin = new(0, 6, 12, 0) });
        var difficulty = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        difficulty.Items.AddRange(["Leicht (4–8 Figuren)", "Mittel (9–16 Figuren)", "Schwer (17–32 Figuren)"]);
        difficulty.SelectedIndex = Math.Clamp(Preferences.Current.VisionMemoryDifficulty, 0, 2);
        row.Controls.Add(SettingsReset.Wrap(difficulty, new UserPreferences().VisionMemoryDifficulty));
        indirect.CheckedChanged += (_, _) => { Preferences.Current.VisionIndirectDefenders = indirect.Checked; Preferences.Save(); };
        difficulty.SelectedIndexChanged += (_, _) => { Preferences.Current.VisionMemoryDifficulty = difficulty.SelectedIndex; Preferences.Save(); };
        layout.Controls.AddRange([SettingsReset.Wrap(indirect, new UserPreferences().VisionIndirectDefenders), description, row]);
        void GenerationSetting(string label, int value, int standard, Action<int> save, string help)
        {
            var generationRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new(0, 8, 0, 3) };
            var input = new NumericUpDown { Minimum = 1, Maximum = 8, Value = Math.Clamp(value, 1, 8), Width = 78 };
            generationRow.Controls.Add(new Label { Text = label, AutoSize = true, Width = 250, Margin = new(0, 5, 12, 0) });
            generationRow.Controls.Add(SettingsReset.Wrap(input, standard)); layout.Controls.Add(generationRow);
            input.ValueChanged += (_, _) => { save((int)input.Value); Preferences.Save(); };
            var tip = new ToolTip(); L.SetToolTip(tip, input, help); input.Disposed += (_, _) => tip.Dispose();
        }
        GenerationSetting("Schachblick: parallele Engines", Preferences.Current.VisionGenerationWorkers, new UserPreferences().VisionGenerationWorkers,
            value => Preferences.Current.VisionGenerationWorkers = value, "Maximale gleichzeitige Engine-Suchen für den Zugvergleich-Vorrat. Unabhängig von Taktikrätseln und Partieanalysen.");
        GenerationSetting("Threads je Schachblick-Engine", Preferences.Current.VisionGenerationThreads, new UserPreferences().VisionGenerationThreads,
            value => Preferences.Current.VisionGenerationThreads = value, "CPU-Threads pro Schachblick-Engine. Mehr Threads beschleunigen eine einzelne Suche, verwenden aber mehr CPU. Änderungen gelten für die nächsten Suchläufe.");
        var demo = new CheckBox { Text = "Demo: 400 Tage Testdaten (nicht gespeichert)", AutoSize = true,
            Checked = Preferences.Current.VisionStatisticsDemo, Margin = new(0, 24, 0, 8) };
        demo.CheckedChanged += (_, _) => { Preferences.Current.VisionStatisticsDemo = demo.Checked; Preferences.Save(); };
        layout.Controls.Add(SettingsReset.Wrap(demo, false));
        var demoTip = new ToolTip();
        L.SetToolTip(demoTip, demo, "Eigener Demo-Datensatz: Aktivitätsphasen, Pausen und Lernkurven für 400 Tage. Er wird nie in deine Statistik geschrieben.");
        Disposed += (_, _) => demoTip.Dispose();
        Controls.Add(layout);
    }
}
