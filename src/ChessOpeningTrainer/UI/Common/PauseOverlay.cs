using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace ChessOpeningTrainer;

/// <summary>Small noninteractive pause badge; the shared board remains the only board renderer.</summary>
public sealed class PauseOverlay : Control
{
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 16 };
    private readonly Stopwatch watch = new();
    private float opacity, from, target;
    public bool Paused { get; private set; }
    /// <summary>Training-only privacy cover; false preserves the small local-game badge.</summary>
    public bool CoverContent { get; set; }
    public PauseOverlay()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        SetStyle(ControlStyles.ResizeRedraw, true);
        BackColor = Color.Transparent; Size = new(120, 120); Visible = false;
        timer.Tick += (_, _) =>
        {
            float fraction = Math.Min(1, (float)watch.Elapsed.TotalMilliseconds / 250);
            opacity = from + (target - from) * fraction;
            Invalidate();
            if (fraction >= 1) { timer.Stop(); if (target == 0) Visible = false; }
        };
    }
    public void SetPaused(bool value)
    {
        if (Paused == value) return;
        Paused = value; from = opacity; target = value ? 1 : 0;
        if (value) { Visible = true; BringToFront(); }
        watch.Restart(); timer.Start(); Invalidate();
    }
    public void HideImmediately()
    {
        timer.Stop(); watch.Stop(); Paused = false; opacity = from = target = 0; Visible = false;
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        if (CoverContent)
        {
            // A pause must hide the exercise immediately, before the badge fades in.
            using var cover = new SolidBrush(Color.FromArgb(Paused ? 255 : (int)(opacity * 255), UiPalette.FromArgb(43, 49, 55)));
            e.Graphics.FillRectangle(cover, ClientRectangle);
        }
        float badgeWidth = CoverContent ? Math.Min(120, Width) : Width;
        float badgeHeight = CoverContent ? Math.Min(120, Height) : Height;
        var state = e.Graphics.Save();
        e.Graphics.TranslateTransform((Width - badgeWidth) / 2, (Height - badgeHeight) / 2);
        using var plate = new SolidBrush(Color.FromArgb((int)(opacity * 225), UiPalette.FromArgb(43, 49, 55)));
        using var ink = new SolidBrush(Color.FromArgb((int)(opacity * 255), UiPalette.Gainsboro));
        e.Graphics.FillEllipse(plate, 2, 2, Math.Max(0, badgeWidth - 4), Math.Max(0, badgeHeight - 4));
        float left = badgeWidth * .32f, top = badgeHeight * .27f, width = badgeWidth * .12f, height = badgeHeight * .46f;
        e.Graphics.FillRectangle(ink, left, top, width, height);
        e.Graphics.FillRectangle(ink, badgeWidth * .56f, top, width, height);
        e.Graphics.Restore(state);
    }
    protected override void WndProc(ref Message message)
    {
        // In particular during fade-out, the badge must not intercept resumed play.
        if (message.Msg == 0x84) { message.Result = (IntPtr)(-1); return; }
        base.WndProc(ref message);
    }
    protected override void Dispose(bool disposing) { if (disposing) timer.Dispose(); base.Dispose(disposing); }
}
