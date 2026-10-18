namespace ChessOpeningTrainer;

/// <summary>Palette-painted progress; retains ProgressBar's Value/Maximum contract.</summary>
internal sealed class GeneratorProgressBar : ProgressBar
{
    public GeneratorProgressBar() => SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
        ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(UiPalette.FromArgb(31, 35, 39));
        Rectangle track = Rectangle.Inflate(ClientRectangle, -10, -3);
        if (track.Width <= 0 || track.Height <= 0) return;
        using var background = new SolidBrush(UiPalette.FromArgb(43, 49, 55));
        using var fill = new SolidBrush(UiPalette.FromArgb(57, 91, 119));
        e.Graphics.FillRectangle(background, track);
        double fraction = Math.Clamp((Value - Minimum) / (double)Math.Max(1, Maximum - Minimum), 0, 1);
        e.Graphics.FillRectangle(fill, track.X, track.Y, (int)Math.Round(track.Width * fraction), track.Height);
    }
}
