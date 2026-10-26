using System.Drawing.Drawing2D;

namespace ChessOpeningTrainer;

internal sealed class DatasetActivityLabel : Label
{
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 75 };
    public bool Busy { get => timer.Enabled; set { timer.Enabled = value; Invalidate(); } }
    public DatasetActivityLabel()
    {
        DoubleBuffered = true;
        timer.Tick += (_, _) => Invalidate(new Rectangle(2, 3, 28, 28));
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (!Busy) return;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(ForeColor, 2.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        e.Graphics.DrawArc(pen, 5, 8, 18, 18, Environment.TickCount64 % 1200 * .3f, 265);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) timer.Dispose();
        base.Dispose(disposing);
    }
}
