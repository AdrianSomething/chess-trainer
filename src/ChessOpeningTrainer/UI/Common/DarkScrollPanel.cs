namespace ChessOpeningTrainer;

/// <summary>Vertical counterpart of the variant scrollbar, for manually laid-out cards.</summary>
public sealed class DarkScrollPanel : Panel
{
    private int offset;
    private bool dragging;
    private float grab;
    public DarkScrollPanel() { DoubleBuffered = true; AutoScroll = false; SetStyle(ControlStyles.ResizeRedraw, true); }
    private int Total => Controls.Cast<Control>().Where(c => c.Visible).Select(c => c.Bottom + offset + 12).DefaultIfEmpty(0).Max();
    private int Extent => Math.Max(0, Total - ClientSize.Height);
    private RectangleF Thumb
    {
        get
        {
            float length = Math.Min(Height, Math.Max(28, Height * (float)Height / Math.Max(1, Total)));
            return new(Width - 8, Extent == 0 ? 0 : offset * (Height - length) / Extent, 6, length);
        }
    }
    private void ScrollTo(int next)
    {
        next = Math.Clamp(next, 0, Extent); int delta = offset - next;
        if (delta != 0)
        {
            SuspendLayout(); offset = next;
            foreach (Control child in Controls) child.Top += delta;
            ResumeLayout(false);
        }
        Invalidate();
    }
    protected override void OnControlAdded(ControlEventArgs e)
    {
        base.OnControlAdded(e);
        if (e.Control is null) return;
        void Wire(Control c)
        {
            c.MouseWheel += ForwardWheel;
            foreach (Control child in c.Controls) Wire(child);
        }
        Wire(e.Control);
    }
    private void ForwardWheel(object? sender, MouseEventArgs e)
    {
        if (e is HandledMouseEventArgs h) { if (h.Handled) return; h.Handled = true; }
        ScrollTo(offset - Math.Sign(e.Delta) * 75);
    }
    protected override void OnMouseWheel(MouseEventArgs e) => ForwardWheel(this, e);
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Extent == 0) return;
        using var track = new SolidBrush(UiPalette.FromArgb(32, 38, 44));
        using var thumb = new SolidBrush(UiPalette.FromArgb(94, 111, 127));
        e.Graphics.FillRectangle(track, Width - 9, 0, 8, Height); e.Graphics.FillRectangle(thumb, Thumb);
    }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left || e.X < Width - 13 || Extent == 0) return;
        var thumb = Thumb; grab = thumb.Contains(e.Location) ? e.Y - thumb.Y : thumb.Height / 2;
        Capture = dragging = true; OnMouseMove(e);
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (dragging) ScrollTo((int)((e.Y - grab) * Extent / Math.Max(1, Height - Thumb.Height)));
    }
    protected override void OnMouseUp(MouseEventArgs e) { Capture = dragging = false; base.OnMouseUp(e); }
    protected override void OnResize(EventArgs e) { base.OnResize(e); ScrollTo(offset); }
}
