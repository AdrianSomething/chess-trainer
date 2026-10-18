namespace ChessOpeningTrainer;

internal static class PuzzleSupplySettingsEditor
{
    public static Control Create()
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new(20), FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
        var settings = Preferences.Current;
        var defaults = new UserPreferences();
        var defaultValues = new Queue<int>([defaults.TacticStockPerLane, defaults.TacticGenerationWorkers, defaults.TacticGenerationThreads]);
        void Number(string title, int value, int max, Action<int> set)
        {
            panel.Controls.Add(new Label { Text = title, AutoSize = true, Margin = new(0, 16, 0, 4) });
            var input = new ScrollNumber { Minimum = 1, Maximum = max, Value = Math.Clamp(value, 1, max), Width = 150 };
            input.ValueChanged += (_, _) => { set((int)input.Value); Preferences.Save(); };
            panel.Controls.Add(SettingsReset.Wrap(input, defaultValues.Dequeue()));
        }
        Number("Rätselvorrat je Typ und Schwierigkeit", settings.TacticStockPerLane, 50, v => settings.TacticStockPerLane = v);
        Number("Maximale parallele Rätsel-Engines", settings.TacticGenerationWorkers, 8, v => settings.TacticGenerationWorkers = v);
        Number("Threads je Rätsel-Engine", settings.TacticGenerationThreads, 8, v => settings.TacticGenerationThreads = v);
        panel.Controls.Add(new Label { Text = "Animation beim Verlust eines Herzens", AutoSize = true, Margin = new(0, 16, 0, 4) });
        var animation = new ComboBox { Width = 350, DropDownStyle = ComboBoxStyle.DropDownList };
        animation.Items.AddRange(["Vergrößern und ausblenden", "Zerbrechen und herunterfallen"]);
        animation.SelectedIndex = Math.Clamp(settings.HeartLossAnimation, 0, 1);
        animation.SelectedIndexChanged += (_, _) => { settings.HeartLossAnimation = animation.SelectedIndex; Preferences.Save(); };
        panel.Controls.Add(SettingsReset.Wrap(animation, new UserPreferences().HeartLossAnimation));
        panel.Controls.Add(new Label { Text = "Gemeinsames Enginebudget für Taktik, Endspiel, Mittelspiel und Openingfehler. Nachschub läuft ab App-Start; gelöste und übersprungene Aufgaben werden nicht erneut ausgegeben. KPK- und freie Matttechnik-Stellungen entstehen direkt. Neue Filter und große Vorräte benötigen beim ersten Vorbereiten Rechenzeit. Änderungen gelten ab dem nächsten Suchlauf; laufende Arbeiter werden nicht gewaltsam beendet.", AutoSize = true, MaximumSize = new(470, 0), Margin = new(0, 20, 0, 0) });
        return panel;
    }
}
