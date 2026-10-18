using System.Drawing.Drawing2D;

namespace ChessOpeningTrainer;

internal sealed partial class TrainingScoreboard
{
    private sealed record LifeLoss(int Index, int Style, long Started);
    private readonly List<LifeLoss> lifeLosses = [];
    private readonly System.Windows.Forms.Timer animationTimer = new() { Interval = 15 };
    private bool lossTimerInitialized;
    private double LossDuration => Math.Max(1, 500 * Math.Clamp(Preferences.Current.AnimationPercent, 0, 200) / 100d);
    public void ResetLossAnimation() { animationTimer.Stop(); lifeLosses.Clear(); Invalidate(); }
    public void AnimateLoss(int index, int style)
    {
        if (index < 0 || index >= 7 || Preferences.Current.AnimationPercent == 0 || IsDisposed) return;
        if (!lossTimerInitialized)
        {
            lossTimerInitialized = true;
            animationTimer.Tick += (_, _) =>
            {
                lifeLosses.RemoveAll(effect => Environment.TickCount64 - effect.Started >= LossDuration);
                if (lifeLosses.Count == 0) animationTimer.Stop();
                Invalidate();
            };
        }
        lifeLosses.Add(new(index, Math.Clamp(style, 0, 1), Environment.TickCount64));
        animationTimer.Start(); Invalidate();
    }
    private void PaintLifeLoss(Graphics graphics)
    {
        float step = Math.Min(27, Math.Max(17, (Width - 100) / 7f));
        foreach (var effect in lifeLosses)
        {
            float t = (float)Math.Clamp((Environment.TickCount64 - effect.Started) / LossDuration, 0, 1);
            using var ink = new SolidBrush(Color.FromArgb((int)(255 * (1 - t)), 224, 70, 86));
            float x = 95 + effect.Index * step + (step - 3) / 2, y = 12;
            using var heart = HeartPath();
            if (effect.Style == 0)
            {
                var state = graphics.Save();
                graphics.TranslateTransform(x, y + 44 * t * t);
                float size = (step - 3) * (1 + 3 * t * t);
                graphics.ScaleTransform(size, size);
                graphics.FillPath(ink, heart); graphics.Restore(state);
            }
            else
                foreach (int side in new[] { -1, 1 })
                {
                    var state = graphics.Save();
                    graphics.TranslateTransform(x + side * 24 * t, y + 73 * t * t);
                    graphics.RotateTransform(side * 28 * t);
                    float size = (step - 3) * (1 + .75f * t);
                    graphics.ScaleTransform(size, size);
                    using var half = new GraphicsPath();
                    half.AddPolygon(new PointF[] { new(side * .7f, -.6f), new(.02f, -.6f), new(-.08f, -.15f), new(.08f, .02f), new(-.06f, .22f), new(.02f, .6f), new(side * .7f, .6f) });
                    graphics.SetClip(half, CombineMode.Intersect);
                    graphics.FillPath(ink, heart); graphics.Restore(state);
                }
        }
    }
    private static GraphicsPath HeartPath()
    {
        var path = new GraphicsPath();
        path.AddBezier(0, -.27f, -.55f, -.75f, -.8f, -.03f, 0, .53f);
        path.AddBezier(0, .53f, .8f, -.03f, .55f, -.75f, 0, -.27f);
        path.CloseFigure(); return path;
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { animationTimer.Dispose(); lifeLosses.Clear(); }
        base.Dispose(disposing);
    }
}
