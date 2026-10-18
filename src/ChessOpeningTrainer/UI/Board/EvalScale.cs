using System.Drawing.Drawing2D;
using ChessMove = ChessOpeningTrainer.Move;

namespace ChessOpeningTrainer;
public sealed class EvalScale : Control
{
    public bool WhiteAtBottom { get; set; } = true;

    private int? score;
    private double shown = .5, from = .5, target = .5;
    private readonly System.Diagnostics.Stopwatch motion = new();
    private readonly System.Windows.Forms.Timer smooth = new()
    {
        Interval = 10
    };
    public int? Score
    {
        get => score;
        set
        {
            if (score == value)
                return;
            score = value;
            from = shown;
            target = value >= 10000 ? 1 : value <= -10000 ? 0 : .5 + .48 * Math.Tanh(Math.Clamp(value ?? 0, -700, 700) / 400d);
            motion.Restart();
            smooth.Start();
            Invalidate();
        }
    }

    public string TextValue { get; set; } = "—";

    public EvalScale()
    {
        SetStyle(ControlStyles.ResizeRedraw, true);
        smooth.Tick += (_, _) =>
        {
            double t = Math.Min(1, motion.Elapsed.TotalMilliseconds / 100);
            shown = from + (target - from) * (t * t * (3 - 2 * t));
            Invalidate();
            if (t >= 1)
                smooth.Stop();
        };
        DoubleBuffered = true;
        Size = new Size(56, 250);
        // Alpha layers are painted in OnPaint; the base itself must be opaque in WinForms.
        BackColor = UiPalette.FromArgb(31, 35, 39);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var track = new Rectangle(8, 0, 24, Height - 1);
        using var outline = new Pen(UiPalette.FromArgb(110, 255, 255, 255));
        using var dark = new SolidBrush(UiPalette.FromArgb(155, 24, 28, 31));
        using var light = new SolidBrush(UiPalette.FromArgb(170, 244, 244, 240));
        var score = Math.Clamp(Score ?? 0, -700, 700);
        var whiteFraction = motion.ElapsedMilliseconds >= 100 ? target : shown;
        var bottomFraction = WhiteAtBottom ? whiteFraction : 1 - whiteFraction;
        var boundary = track.Bottom - (int)Math.Round(bottomFraction * track.Height);
        g.FillRectangle(WhiteAtBottom ? dark : light, new Rectangle(track.X, track.Y, track.Width, Math.Max(0, boundary - track.Top)));
        g.FillRectangle(WhiteAtBottom ? light : dark, new Rectangle(track.X, boundary, track.Width, Math.Max(0, track.Bottom - boundary)));
        g.DrawRectangle(outline, track);
        using var separator = new Pen(UiPalette.FromArgb(150, 255, 255, 255), 1);
        g.DrawLine(separator, track.X, boundary, track.Right, boundary);
        using var font = new Font(Font.FontFamily, 9, FontStyle.Bold);
        using var text = new SolidBrush(UiPalette.Gainsboro);
        using var sf = new StringFormat
        {
            Alignment = StringAlignment.Center
        };
        using var badge = new SolidBrush(BackColor);
        g.FillRectangle(badge, 0, Height / 2 - 11, Width, 22);
        g.DrawString(L.T(TextValue), font, text, new RectangleF(0, Height / 2 - 9, Width, 18), sf);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            smooth.Dispose();
        base.Dispose(disposing);
    }
}
