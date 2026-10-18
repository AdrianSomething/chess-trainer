namespace ChessOpeningTrainer;

public sealed class MarkingSettingsEditor : UserControl
{
    public MarkingSettingsEditor()
    {
        var preview = new BoardControl { Dock = DockStyle.Fill, MinimumSize = Size.Empty, InputEnabled = false,
            Position = Position.FromFen(Position.InitialFen), SelectedSquare = 12 };
        preview.Arrows.Add(new BoardArrow(12, 28));
        preview.Arrows.Add(new BoardArrow(6, 21));
        preview.MarkedSquares.Add(35);
        var rows = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 160, Padding = new(10), WrapContents = true };
        var width = new ScrollNumber { Minimum = 30, Maximum = 250, Increment = 10, Width = 80,
            Value = Math.Clamp(Preferences.Current.ArrowThicknessPercent, 30, 250) };
        rows.Controls.Add(new Label { Text = "Pfeildicke (%)", AutoSize = true, Padding = new(0, 6, 0, 0) });
        rows.Controls.Add(SettingsReset.Wrap(width, new UserPreferences().ArrowThicknessPercent));
        var head = new ScrollNumber { Minimum = 30, Maximum = 250, Increment = 10, Width = 80,
            Value = Math.Clamp(Preferences.Current.ArrowHeadPercent, 30, 250) };
        rows.Controls.Add(new Label { Text = "Pfeilspitze (%)", AutoSize = true, Padding = new(0, 6, 0, 0) });
        rows.Controls.Add(SettingsReset.Wrap(head, new UserPreferences().ArrowHeadPercent));
        void ColorButton(string label, Func<string> read, Action<string> write, string defaultValue)
        {
            var button = new Button { Text = label, AutoSize = true };
            button.Click += (_, _) =>
            {
                string previous = read();
                using var dialog = new ColorWheelDialog(Skins.Color(previous, Color.Orange)) { Owner = FindForm() };
                dialog.ColorChanged += color => { write(ColorTranslator.ToHtml(color)); Preferences.PreviewAppearance(); };
                if (dialog.ShowDialog(this) != DialogResult.OK) { write(previous); Preferences.PreviewAppearance(); return; }
                write(ColorTranslator.ToHtml(dialog.SelectedColor)); Preferences.Save();
            };
            rows.Controls.Add(SettingsReset.Wrap(button, () => !read().Equals(defaultValue, StringComparison.OrdinalIgnoreCase), () => { write(defaultValue); Preferences.Save(); }));
        }
        ColorButton("Pfeilfarbe", () => Preferences.Current.ArrowColor, c => Preferences.Current.ArrowColor = c, new UserPreferences().ArrowColor);
        ColorButton("Auswahlfarbe", () => Preferences.Current.SelectionColor, c => Preferences.Current.SelectionColor = c, new UserPreferences().SelectionColor);
        ColorButton("Feldmarkierung", () => Preferences.Current.SquareMarkColor, c => Preferences.Current.SquareMarkColor = c, new UserPreferences().SquareMarkColor);
        width.ValueChanged += (_, _) => { Preferences.Current.ArrowThicknessPercent = (int)width.Value; Preferences.Save(); preview.Invalidate(); };
        head.ValueChanged += (_, _) => { Preferences.Current.ArrowHeadPercent = (int)head.Value; Preferences.Save(); };
        Controls.Add(preview); Controls.Add(rows);
    }
}
