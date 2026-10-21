using System.Drawing.Drawing2D;

namespace ChessOpeningTrainer;

internal sealed class BranchLoadingOverlay : Control
{
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 40 };
    public BranchLoadingOverlay()
    {
        DoubleBuffered = true; BackColor = UiPalette.FromArgb(29, 33, 38);
        timer.Tick += (_, _) => Invalidate(); timer.Start();
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(UiPalette.Gainsboro, 3);
        e.Graphics.DrawArc(pen, Width / 2 - 18, Height / 2 - 40, 36, 36, Environment.TickCount64 % 1000 * .36f, 270);
        TextRenderer.DrawText(e.Graphics, L.T("Branch wird vorbereitet …"), Font, new Rectangle(0, Height / 2 + 10, Width, 32), UiPalette.Gainsboro, TextFormatFlags.HorizontalCenter);
    }
    protected override void Dispose(bool disposing) { if (disposing) timer.Dispose(); base.Dispose(disposing); }
}
