namespace ChessOpeningTrainer;

internal static class EngineOpponentControls
{
    public static Control Create(NumericUpDown depth, NumericUpDown intelligence)
    {
        var panel = new TableLayoutPanel { Width = 310, Height = 64, ColumnCount = 2, RowCount = 2, Margin = Padding.Empty };
        panel.ColumnStyles.Add(new(SizeType.Percent, 100)); panel.ColumnStyles.Add(new(SizeType.Absolute, 78));
        panel.RowStyles.Add(new(SizeType.Percent, 50)); panel.RowStyles.Add(new(SizeType.Percent, 50));
        panel.Controls.Add(new Label { Text = "Engine-Suchtiefe", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        panel.Controls.Add(new Label { Text = "Engine-Intelligenz (%)", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
        panel.Controls.Add(depth, 1, 0); panel.Controls.Add(intelligence, 1, 1);
        depth.Dock = intelligence.Dock = DockStyle.Fill;
        var tips = new ToolTip { AutoPopDelay = 20000 };
        L.SetToolTip(tips, depth, "Suchtiefe in Halbzügen ohne festes Zeitlimit. Hohe Werte wie 40 können sehr lange dauern. Neue Partie, Zurück oder Schließen bricht die Suche ab.");
        L.SetToolTip(tips, intelligence, "0 %: schlechtester legaler Zug laut Suche. 50 %: Zufall aus allen legalen Zügen. 100 %: bester Enginezug. Zwischenwerte gewichten bessere oder schlechtere Züge; die Bewertung hängt von der Suchtiefe ab.");
        panel.Disposed += (_, _) => tips.Dispose(); return panel;
    }
}
