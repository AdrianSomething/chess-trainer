namespace ChessOpeningTrainer;

public sealed partial class OpeningGeneratorForm
{
    private readonly List<PreferredStep> preferredMoves = [];
    private readonly CheckBox preferMode = new() { Text = "Wunschzüge planen", Appearance = Appearance.Button, TextAlign = ContentAlignment.MiddleCenter, Width = 190, Height = 32 };
    private readonly ScrollNumber preferenceDeviations = new() { Minimum = 1, Maximum = 100, Value = 3, Width = 100 };
    private readonly ScrollNumber preferenceMinimum = new() { Minimum = -20, Maximum = 20, DecimalPlaces = 2, Increment = .1m, Value = -.5m, Width = 100 };
    private readonly ScrollNumber preferenceLoss = new() { Minimum = 0, Maximum = 20, DecimalPlaces = 2, Increment = .01m, Value = .1m, Width = 100 };

    private void InitializePreferredMoves(Button undoButton, Button resetButton)
    {
        controls.Controls.Remove(undoButton); controls.Controls.Remove(resetButton);
        var buttons = new FlowLayoutPanel { Width = 575, Height = 38, WrapContents = false };
        undoButton.Height = resetButton.Height = 32;
        buttons.Controls.AddRange([undoButton, resetButton, preferMode]); controls.Controls.Add(buttons);
        var toggle = new Button { Text = "▸ " + L.T("Wunschzug-Einstellungen"), Width = 560, Height = 28, FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleLeft };
        toggle.FlatAppearance.BorderSize = 0;
        var settings = new FlowLayoutPanel { Width = 575, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Visible = false };
        controls.Controls.Add(toggle); controls.Controls.Add(settings);
        toggle.Click += (_, _) => { settings.Visible = !settings.Visible; toggle.Text = (settings.Visible ? "▾ " : "▸ ") + L.T("Wunschzug-Einstellungen"); };
        void Row(string title, ScrollNumber input, string description)
        {
            var row = new FlowLayoutPanel { Width = 560, Height = 36, WrapContents = false };
            var label = new Label { Text = title, Width = 415, Height = 32 };
            row.Controls.AddRange([label, input]); settings.Controls.Add(row);
            L.SetToolTip(tips, label, description); L.SetToolTip(tips, input, description);
            input.ValueChanged += (_, _) => Dirty();
        }
        Row("Abbruch nach Abweichungen", preferenceDeviations, "Nach so vielen aufeinanderfolgenden eigenen Zügen außerhalb des Plans gelten nur noch die Engine-Regeln. Ein ausgeführter Wunschzug setzt den Zähler zurück; jeder Ast zählt getrennt.");
        Row("Mindestbewertung (eigene Sicht)", preferenceMinimum, "Ein Wunschzug muss aus Sicht der lernenden Seite strikt besser als diese Bewertung sein. Sonst wird die nächste ausführbare Präferenz geprüft.");
        Row("Maximaler Verlust zum Bestzug", preferenceLoss, "Ein Wunschzug darf höchstens diesen Verlust in Bauerneinheiten gegenüber dem besten Enginezug haben. Lernkompression hat Vorrang; ohne passende Präferenz gilt der beste Zug.");
        L.SetToolTip(tips, preferMode, "Zuerst die echte Startsequenz spielen. Dann nur eigene Figuren in gewünschter Reihenfolge planen. Gegner blockieren den Plan nicht; eigene Figuren müssen zuerst den Weg freigeben. Die Engine überprüft später jeden tatsächlich gespielten Zug.");
        preferMode.CheckedChanged += (_, _) =>
        {
            board.MoveProvider = preferMode.Checked ? p => Enumerable.Range(0, 64).SelectMany(from => Enumerable.Range(0, 64)
                .Where(to => PreferredMovePlan.CanAppend(p, color.SelectedIndex == 1, from, to)).Select(to => new Move { From = from, To = to })).ToArray() : null;
            RefreshPosition();
        };
        board.ForegroundPaint += (_, e) => PaintPreferredMoves(e.Graphics);
    }
    private bool AppendPreferredMove(int from, int to)
    {
        var preview = PreferredMovePlan.Preview(position, preferredMoves);
        if (!PreferredMovePlan.CanAppend(preview, color.SelectedIndex == 1, from, to)) return false;
        preferredMoves.Add(new(from, to, preview.Squares[from])); RefreshPosition(); return true;
    }
    private void PaintPreferredMoves(Graphics g)
    {
        if (!preferMode.Checked) return;
        var bounds = board.PlayingBounds; float cell = bounds.Width / 8;
        foreach (var group in preferredMoves.Select((step, index) => (step, index)).GroupBy(x => x.step.To))
        {
            int square = group.Key, col = board.Flip ? 7 - square % 8 : square % 8, row = board.Flip ? square / 8 : 7 - square / 8;
            var rect = new RectangleF(bounds.Left + col * cell, bounds.Top + row * cell, cell, cell);
            using var fade = new SolidBrush(Color.FromArgb(100, Skins.Color(Square.IsDark(square) ? Skins.Current.Dark : Skins.Current.Light, Color.Gray)));
            g.FillRectangle(fade, rect);
            using var font = new Font(Font.FontFamily, Math.Max(10, cell * .22f), FontStyle.Bold, GraphicsUnit.Pixel);
            string label = string.Join(",", group.Select(x => x.index + 1));
            var labelBounds = new Rectangle((int)rect.X + 2, (int)rect.Y + 2, (int)cell - 4, (int)(cell * .3f));
            TextRenderer.DrawText(g, label, font, labelBounds, UiPalette.Gainsboro, UiPalette.FromArgb(43, 49, 55), TextFormatFlags.Left | TextFormatFlags.NoPadding);
        }
    }
    private void RestorePreferredMoves(GeneratorOptions options)
    {
        preferMode.Checked = false;
        preferredMoves.Clear(); preferredMoves.AddRange(options.PreferredMoves);
        preferenceDeviations.Value = options.PreferenceDeviationLimit;
        preferenceMinimum.Value = (decimal)options.PreferenceMinimumEvaluation;
        preferenceLoss.Value = (decimal)options.PreferenceMaximumLoss;
    }
}
