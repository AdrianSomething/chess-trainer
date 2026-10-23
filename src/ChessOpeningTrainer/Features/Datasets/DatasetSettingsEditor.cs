namespace ChessOpeningTrainer;

public static class DatasetSettingsEditor
{
    public static Control Create()
    {
        var layout = DatasetControls.Column();
        var settings = Preferences.Current.Datasets;
        var tips = new ToolTip();
        layout.Disposed += (_, _) => tips.Dispose();
        void Add(string caption, string explanation, int value, int max, Action<int> set, int min = 1)
        {
            var row = DatasetControls.Number(caption, value, min, max, n => { set((int)n); Preferences.Save(); });
            L.SetToolTip(tips, row, explanation);
            foreach (Control child in row.Controls)
                L.SetToolTip(tips, child, explanation);
            layout.Controls.Add(row);
        }
        Add("Arbeiter: Datensatzbaum", "Parallele Rekonstruktion der Schachstellungen. Mehr Arbeiter nutzen mehr CPU; das Schreiben des gemeinsamen Index bleibt koordiniert.", settings.IndexWorkers, 32, n => settings.IndexWorkers = n);
        Add("Arbeiter: Import und Zuschneiden", "Anzahl gleichzeitig geprüfter Partien. Ein höherer Wert beschleunigt die Zugprüfung, benötigt aber mehr CPU und Arbeitsspeicher.", settings.FilterWorkers, 32, n => settings.FilterWorkers = n);
        Add("Arbeiter: Filterschätzung", "CPU-Arbeiter für die Vorschau von Filterergebnis und Dateigröße. Die eigentliche Verarbeitung verwendet die anderen Arbeitergrenzen.", settings.EstimateWorkers, 16, n => settings.EstimateWorkers = n);
        Add("Stichprobe: Partien", "Zufällig beim Import gespeicherte Beispielpartien für schnelle Vorhersagen. Größere Stichproben erhöhen Aufwand und Genauigkeit. Bestehende Datensätze bleiben unverändert.", settings.SampleSize, 10000, n => settings.SampleSize = n, 100);
        layout.Controls.Add(new Label { Text = "Änderungen gelten für neue Aufträge. Stichproben werden beim Import erzeugt; bestehende Datensätze behalten ihre Stichprobe. Alle Tiefen werden indexiert. Sehr große Archive benötigen viel freien Speicher.", Width = 470, Height = 110, Margin = new(4, 12, 4, 4) });
        return layout;
    }
}
