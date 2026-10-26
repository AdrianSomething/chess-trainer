namespace ChessOpeningTrainer;

public sealed class BranchSettingsEditor : UserControl
{
    public BranchSettingsEditor()
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new(20), FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
        string[] keys = ["sugiyama", "legacy", "alternative"];
        var layout = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 420 };
        layout.Items.AddRange([L.T("Sugiyama (Standard)"), L.T("Legacy (bisheriges Standardlayout)"), L.T("Alternative: stärkere Kreuzungsreduktion")]);
        layout.SelectedIndex = Math.Max(0, Array.IndexOf(keys, Preferences.Current.BranchLayout));
        layout.SelectedIndexChanged += (_, _) => { if (layout.SelectedIndex >= 0) { Preferences.Current.BranchLayout = keys[layout.SelectedIndex]; Preferences.Save(); } };
        panel.Controls.Add(new Label { Text = L.T("Branchlayout"), AutoSize = true });
        panel.Controls.Add(SettingsReset.Wrap(layout, 0));
        panel.Controls.Add(new Label { Text = L.T("Sugiyama ordnet den Graphen in Schichten und reduziert Kreuzungen. Legacy behält das bisherige Standardlayout; Alternative verwendet die stärkere Kreuzungsreduktion. Offene Branchfenster übernehmen Änderungen."), AutoSize = true, MaximumSize = new(465, 0), Margin = new(0, 15, 0, 0) });
        panel.Controls.Add(GeneratorRestSettings.Create());
        Controls.Add(panel);
    }
}
