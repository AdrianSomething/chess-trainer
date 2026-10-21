namespace ChessOpeningTrainer;

public sealed class BranchSettingsEditor : UserControl
{
    public BranchSettingsEditor()
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new(20), FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
        var legacy = new CheckBox { Text = "Bisherige Branchpositionierung verwenden", AutoSize = true, Checked = Preferences.Current.BranchLegacyPositioning };
        legacy.CheckedChanged += (_, _) => { Preferences.Current.BranchLegacyPositioning = legacy.Checked; Preferences.Save(); };
        panel.Controls.AddRange([SettingsReset.Wrap(legacy, new UserPreferences().BranchLegacyPositioning), new Label { Text = "Standardmäßig ordnet die verbesserte Positionierung Geschwister gemeinsam, zentriert Abzweigungen und bevorzugt horizontale Gegnerzug–Antwort-Verbindungen. Zusätzlich werden Kreuzungen nur bei einer globalen Verbesserung reduziert. Die bisherige Grundpositionierung bleibt optional verfügbar. Offene Branchfenster übernehmen die Änderung sofort.", AutoSize = true, MaximumSize = new(465, 0), Margin = new(0, 15, 0, 0) }]);
        panel.Controls.Add(GeneratorRestSettings.Create());
        Controls.Add(panel);
    }
}
