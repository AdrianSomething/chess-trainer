using System.Drawing.Drawing2D;

namespace ChessOpeningTrainer;

/// <summary>Score, seven-heart life strip and independent chess-clock style time displays.</summary>
internal sealed partial class TrainingScoreboard : Control
{
    public int Score { get; set; }
    public int Lives { get; set; }
    public int MaximumLives { get; set; }
    public double? MoveTime { get; set; }
    public double? TotalTime { get; set; }
    public TrainingScoreboard() { DoubleBuffered = true; SetStyle(ControlStyles.ResizeRedraw, true); }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        using var ink = new SolidBrush(ForeColor);
        g.DrawString(L.T("Punkte") + ": " + Score, Font, ink, 0, 0);
        if (MaximumLives > 0)
        {
            float step = Math.Min(27, Math.Max(17, (Width - 100) / 7f));
            for (int i = 0; i < Math.Min(7, MaximumLives); i++)
            {
                float x = 95 + i * step, y = 2, w = step - 3;
                using var path = new GraphicsPath();
                path.AddBezier(x + w / 2, y + 5, x - 2, y - 5, x - 5, y + 8, x + w / 2, y + 20);
                path.AddBezier(x + w / 2, y + 20, x + w + 5, y + 8, x + w + 2, y - 5, x + w / 2, y + 5);
                using var heart = new SolidBrush(i < Lives ? Color.FromArgb(224, 70, 86) : UiPalette.FromArgb(70, 75, 80));
                g.FillPath(heart, path);
            }
        }
        using var clockFont = new Font("Segoe UI", 19, FontStyle.Bold);
        int top = 28;
        foreach (var (label, remaining) in new[] { ("Zugzeit", MoveTime), ("Gesamtzeit", TotalTime) })
        {
            if (remaining is not double value) continue;
            double t = Math.Max(0, value);
            string text = $"{(int)t / 60:00}:{(int)t % 60:00}.{(int)(t * 10) % 10}";
            using var background = new SolidBrush(UiPalette.FromArgb(43, 49, 55));
            g.FillRectangle(background, 0, top, Width, 42);
            g.DrawString(L.T(label), Font, ink, 8, top + 12);
            TextRenderer.DrawText(g, text, clockFont, new Rectangle(90, top + 1, Math.Max(90, Width - 98), 40), ForeColor,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            top += 46;
        }
        PaintLifeLoss(g);
    }
}
