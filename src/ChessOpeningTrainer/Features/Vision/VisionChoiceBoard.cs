using System.Drawing.Drawing2D;

namespace ChessOpeningTrainer;

public sealed class VisionChoiceBoard : BoardControl
{
    private readonly System.Windows.Forms.Timer tween = new() { Interval = 15 };
    private readonly System.Diagnostics.Stopwatch motion = new();
    private double from, shown, target;
    public bool ChoiceLocked { get; private set; }
    public bool WrongChoice { get; private set; }
    public bool ShowVerdict { get; set; }
    public double MarkerFraction => shown;
    public float MarkerThickness => ChoiceLocked && target > 0 ? 4f : 2.5f;
    public VisionChoiceBoard()
    {
        tween.Tick += (_, _) =>
        {
            double t = Math.Min(1, motion.Elapsed.TotalMilliseconds / 250); shown = from + (target - from) * t * t * (3 - 2 * t);
            Invalidate(); if (t >= 1) tween.Stop();
        };
    }
    public void ResetChoice() { ChoiceLocked = WrongChoice = ShowVerdict = false; Animate(0); HintColor = null; }
    public void LockChoice(bool chosen, bool wrong) { ChoiceLocked = true; WrongChoice = wrong; Animate(chosen ? 1d / 3 : 0); }
    private void Animate(double value) { from = shown; target = value; if (Math.Abs(from - target) < .0001) { shown = target; tween.Stop(); Invalidate(); return; } motion.Restart(); tween.Start(); Invalidate(); }
    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); if (!ChoiceLocked) Animate(.25); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); if (!ChoiceLocked) Animate(0); }
    protected override void OnPaint(PaintEventArgs e)
    {
        var saved = e.Graphics.Save(); base.OnPaint(e); e.Graphics.Restore(saved);
        var area = PlayingBounds; float length = (float)(area.Width * shown);
        using var outline = new Pen(UiPalette.Gainsboro, MarkerThickness) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        float cx = area.Left + area.Width / 2, cy = area.Top + area.Height / 2;
        if (length > .1f)
        {
            e.Graphics.DrawLine(outline, cx - length / 2, area.Top - 5, cx + length / 2, area.Top - 5);
            e.Graphics.DrawLine(outline, cx - length / 2, area.Bottom + 5, cx + length / 2, area.Bottom + 5);
            e.Graphics.DrawLine(outline, area.Left - 5, cy - length / 2, area.Left - 5, cy + length / 2);
            e.Graphics.DrawLine(outline, area.Right + 5, cy - length / 2, area.Right + 5, cy + length / 2);
        }
        if (WrongChoice && ShowVerdict)
        {
            using var cross = new Pen(Color.FromArgb(225, 225, 50, 60), Math.Max(2, area.Width / 130));
            float inset = cross.Width / 2 + 1;
            e.Graphics.DrawLine(cross, area.Left + inset, area.Top + inset, area.Right - inset, area.Bottom - inset);
            e.Graphics.DrawLine(cross, area.Right - inset, area.Top + inset, area.Left + inset, area.Bottom - inset);
        }
    }
    protected override void Dispose(bool disposing) { if (disposing) tween.Dispose(); base.Dispose(disposing); }
}
