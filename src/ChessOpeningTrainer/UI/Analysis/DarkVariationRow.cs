namespace ChessOpeningTrainer;
public sealed class DarkVariationRow : Panel
{
    public FlowLayoutPanel Content { get; } = new()
    {
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        WrapContents = false,
        Margin = Padding.Empty
    };

    private int offset;
    private bool sliding;
    private float grabOffset;
    private int TrackTop => Math.Max(0, Math.Min(Height - 9, Math.Max(Content.Bottom + 3, Height - 13)));
    public DarkVariationRow()
    {
        DoubleBuffered = true;
        Dock = DockStyle.Fill;
        Margin = Padding.Empty;
        BackColor = UiPalette.FromArgb(43, 49, 55);
        Controls.Add(Content);
        Content.SizeChanged += (_, _) => UpdateScroll();
        Resize += (_, _) => UpdateScroll();
    }

    private int Extent => Math.Max(0, Content.Width - Width);

    private void UpdateScroll()
    {
        offset = Math.Clamp(offset, 0, Extent);
        Content.Location = new Point(-offset, 0);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Extent == 0)
            return;
        using var track = new SolidBrush(UiPalette.FromArgb(32, 38, 44));
        using var thumb = new SolidBrush(UiPalette.FromArgb(94, 111, 127));
        e.Graphics.FillRectangle(track, 0, TrackTop, Width, 8);
        float length = Math.Max(28, Width * (float)Width / Content.Width);
        float left = offset * (Width - length) / Extent;
        e.Graphics.FillRectangle(thumb, left, TrackTop + 1, length, 6);
    }

    private void Slide(int x)
    {
        float length = Math.Max(28, Width * (float)Width / Math.Max(1, Content.Width));
        offset = (int)(Math.Clamp((x - grabOffset) / Math.Max(1, Width - length), 0, 1) * Extent);
        UpdateScroll();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left && e.Y >= TrackTop - 4 && Extent > 0)
        {
            float length = Math.Max(28, Width * (float)Width / Math.Max(1, Content.Width));
            float left = offset * (Width - length) / Extent;
            grabOffset = e.X >= left && e.X <= left + length ? e.X - left : length / 2;
            sliding = true;
            Capture = true;
            Slide(e.X);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (sliding)
            Slide(e.X);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        sliding = false;
        Capture = false;
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        offset = Math.Clamp(offset - Math.Sign(e.Delta) * 70, 0, Extent);
        UpdateScroll();
    }
}
