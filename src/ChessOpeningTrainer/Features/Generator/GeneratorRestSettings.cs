namespace ChessOpeningTrainer;

internal static class GeneratorRestSettings
{
    public static Control Create()
    {
        var row = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new(0, 12, 0, 8) };
        var enabled = new CheckBox { AutoSize = true, Checked = Preferences.Current.GeneratorRestEnabled };
        var work = new ScrollNumber { Minimum = 1, Maximum = 1440, Value = Math.Clamp(Preferences.Current.GeneratorWorkMinutes, 1, 1440), Width = 62 };
        var rest = new ScrollNumber { Minimum = 1, Maximum = 120, Value = Math.Clamp(Preferences.Current.GeneratorRestMinutes, 1, 120), Width = 62 };
        row.Controls.AddRange([enabled, new Label { Text = L.T("Generator: nach"), AutoSize = true }, work,
            new Label { Text = L.T("min Arbeit pausieren für"), AutoSize = true }, rest, new Label { Text = "min", AutoSize = true }]);
        var tips = new ToolTip { AutoPopDelay = 18000 };
        foreach (Control control in row.Controls) L.SetToolTip(tips, control, "Optionale Abkühlpause für die Branchgenerierung. Laufende Engine-Suchen werden beendet, dann pausieren alle Aufträge. Andere Modi bleiben benutzbar. Dies ersetzt nicht die thermischen Schutzfunktionen des Computers.");
        void Save() { Preferences.Current.GeneratorRestEnabled = enabled.Checked; Preferences.Current.GeneratorWorkMinutes = (int)work.Value; Preferences.Current.GeneratorRestMinutes = (int)rest.Value; Preferences.Save(); }
        enabled.CheckedChanged += (_, _) => Save(); work.ValueChanged += (_, _) => Save(); rest.ValueChanged += (_, _) => Save();
        row.Disposed += (_, _) => tips.Dispose();
        return row;
    }
}
