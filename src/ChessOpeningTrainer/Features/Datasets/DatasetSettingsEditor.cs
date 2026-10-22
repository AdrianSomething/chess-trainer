namespace ChessOpeningTrainer;

public static class DatasetSettingsEditor
{
    public static Control Create()
    {
        var layout = DatasetControls.Column();
        var settings = Preferences.Current.Datasets;
        void Add(string caption, int value, int max, Action<int> set) => layout.Controls.Add(DatasetControls.Number(caption, value, 1, max, n => { set((int)n); Preferences.Save(); }));
        Add("Arbeiter: Datensatzbaum", settings.IndexWorkers, 32, n => settings.IndexWorkers = n);
        Add("Arbeiter: Import und Zuschneiden", settings.FilterWorkers, 32, n => settings.FilterWorkers = n);
        Add("Arbeiter: Filterschätzung", settings.EstimateWorkers, 16, n => settings.EstimateWorkers = n);
        Add("Stichprobe: Partien", settings.SampleSize, 10000, n => settings.SampleSize = n);
        layout.Controls.Add(new Label { Text = "Änderungen gelten für neue Aufträge. Stichproben werden beim Import erzeugt; bestehende Datensätze behalten ihre Stichprobe. Alle Tiefen werden indexiert. Sehr große Archive benötigen viel freien Speicher.", Width = 470, Height = 110, Margin = new(4, 12, 4, 4) });
        return layout;
    }
}
