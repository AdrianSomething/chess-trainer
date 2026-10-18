using System.Drawing.Drawing2D;
using ChessMove = ChessOpeningTrainer.Move;

namespace ChessOpeningTrainer;
public sealed class ProgressStrip : Control
{
    public Dictionary<string, int> Counts { get; set; } = new();

    private static readonly (string Name, Color Color)[] Parts = [("Gelernt", UiPalette.FromArgb(60, 174, 95)), ("Zu Wiederholen", UiPalette.FromArgb(240, 170, 55)), ("Angeschaut", UiPalette.FromArgb(79, 138, 218)), ("Unbekannt", UiPalette.FromArgb(124, 132, 142))];
    public ProgressStrip()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.ResizeRedraw, true);
        Height = 42;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        var total = Math.Max(1, Parts.Sum(p => Math.Max(0, Counts.GetValueOrDefault(p.Name))));
        var x = 0f;
        foreach (var(name, color)in Parts)
        {
            var count = Counts.GetValueOrDefault(name);
            var width = Width * count / (float)total;
            using var brush = new SolidBrush(color);
            e.Graphics.FillRectangle(brush, x, 0, width, 15);
            x += width;
        }

        using var border = new Pen(UiPalette.FromArgb(70, Color.White));
        e.Graphics.DrawRectangle(border, 0, 0, Width - 1, 15);
        using var font = new Font(Font.FontFamily, 8, FontStyle.Regular);
        using var text = new SolidBrush(UiPalette.Gainsboro);
        var labels = string.Join("   ", Parts.Where(x => Counts.GetValueOrDefault(x.Name) > 0).Select(x => $"{L.T(x.Name)}: {Counts.GetValueOrDefault(x.Name)}"));
        e.Graphics.DrawString(labels, font, text, new PointF(0, 20));
    }
}
