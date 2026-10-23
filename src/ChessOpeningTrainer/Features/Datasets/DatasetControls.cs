namespace ChessOpeningTrainer;

internal static class DatasetControls
{
    public static Button Button(string text, Action action, int width = 230)
    {
        var button = new Button
        {
            Text = L.T(text),
            Width = width,
            Height = 40,
            FlatStyle = FlatStyle.Flat,
            BackColor = UiPalette.FromArgb(43, 49, 55),
            ForeColor = UiPalette.Gainsboro,
            Margin = new(4, 5, 4, 5)
        };
        button.Click += (_, _) => action();
        return button;
    }
    public static FlowLayoutPanel Column() => new() { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new(12) };
    public static Panel Number(string text, decimal value, decimal min, decimal max, Action<decimal> changed, int step = 1)
    {
        var row = new Panel { Width = 490, Height = 40 };
        var input = new ScrollNumber { Minimum = min, Maximum = max, Value = Math.Clamp(value, min, max), Increment = step, Width = 105, Left = 365, Top = 5, EmptyDefaultValue = min };
        input.EnterCommitted = _ => row.Focus();
        input.ValueChanged += (_, _) => changed(input.Value);
        row.Controls.Add(new Label { Text = text, Left = 2, Top = 7, Width = 355, Height = 28 });
        row.Controls.Add(input);
        return row;
    }
}
