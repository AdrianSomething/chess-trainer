namespace ChessOpeningTrainer;

/// <summary>Small horizontal white/black evaluation revealed only after choosing a comparison.</summary>
public sealed class VisionComparisonEvaluation : Control
{
    public double? ScoreWhite { get; set; }
    public VisionComparisonEvaluation() { DoubleBuffered = true; Height = 34; SetStyle(ControlStyles.ResizeRedraw, true); }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var track = new Rectangle(1, 1, Math.Max(1, Width - 3), 8);
        double fraction = .5 + .48 * Math.Tanh(Math.Clamp(ScoreWhite ?? 0, -7, 7) / 4);
        int whiteWidth = (int)Math.Round(track.Width * fraction);
        using var white = new SolidBrush(Color.FromArgb(244, 244, 240));
        using var black = new SolidBrush(Color.FromArgb(24, 28, 31));
        e.Graphics.FillRectangle(white, track.Left, track.Top, whiteWidth, track.Height);
        e.Graphics.FillRectangle(black, track.Left + whiteWidth, track.Top, track.Width - whiteWidth, track.Height);
        using var line = new Pen(UiPalette.FromArgb(94, 111, 127)); e.Graphics.DrawRectangle(line, track);
        string value = ScoreWhite is double score ? score.ToString("+0.00;-0.00;0.00") : L.T("Bewertung wird vorbereitet …");
        var text = new Rectangle(1, 12, Math.Max(1, Width - 2), Math.Max(18, Height - 12));
        TextRenderer.DrawText(e.Graphics, value, Font, text, UiPalette.Gainsboro, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
    }
}
