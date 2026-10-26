namespace ChessOpeningTrainer;

/// <summary>One double-buffered surface: only visible rows are painted while scrolling.</summary>
internal sealed class DatasetDownloadList : Control
{
    private const int Header = 35, RowHeight = 32;
    private readonly HashSet<int> selected = [];
    private IReadOnlyList<DatasetDownloadOption> options = [];
    private int anchor, offset;
    private bool dragging;
    private float grab;
    public event Action? SelectionChanged;
    public IReadOnlyList<DatasetDownloadOption> Selection => selected.Order().Select(i => options[i]).ToArray();
    private int ViewHeight => Math.Max(0, Height - Header);
    private int Extent => Math.Max(0, options.Count * RowHeight - ViewHeight);
    private RectangleF Thumb
    {
        get
        {
            float length = Math.Min(ViewHeight, Math.Max(28, ViewHeight * (float)ViewHeight / Math.Max(1, options.Count * RowHeight)));
            return new(Width - 8, Header + (Extent == 0 ? 0 : offset * (ViewHeight - length) / Extent), 6, length);
        }
    }
    public DatasetDownloadList()
    {
        Dock = DockStyle.Fill; DoubleBuffered = true;
        SetStyle(ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
    }
    public void SetOptions(IReadOnlyList<DatasetDownloadOption> values)
    {
        options = values; selected.Clear(); anchor = offset = 0;
        Invalidate(); SelectionChanged?.Invoke();
    }
    private void ScrollTo(int value)
    {
        offset = Math.Clamp(value, 0, Extent);
        Invalidate(); Update(); // Paint rows and thumb together, even during a wheel burst.
    }
    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        if (e is HandledMouseEventArgs handled) handled.Handled = true;
        int lines = SystemInformation.MouseWheelScrollLines;
        int pixels = lines < 0 ? ViewHeight : RowHeight * lines;
        ScrollTo(offset - (int)Math.Round(e.Delta / 120.0 * pixels));
    }
    protected override void OnResize(EventArgs e) { base.OnResize(e); offset = Math.Clamp(offset, 0, Extent); Invalidate(); }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left || e.Y < Header) return;
        Focus();
        if (e.X >= Width - 13 && Extent > 0)
        {
            var thumb = Thumb; grab = thumb.Contains(e.Location) ? e.Y - thumb.Y : thumb.Height / 2;
            dragging = Capture = true; OnMouseMove(e); return;
        }
        if (e.Clicks > 1) return;
        int index = (e.Y - Header + offset) / RowHeight;
        if (index < 0 || index >= options.Count) return;
        SelectRow(index, ModifierKeys);
    }
    private void SelectRow(int index, Keys modifiers)
    {
        if ((modifiers & Keys.Shift) != 0)
        {
            if ((modifiers & Keys.Control) == 0) selected.Clear();
            for (int n = Math.Min(anchor, index); n <= Math.Max(anchor, index); n++) selected.Add(n);
        }
        else
        {
            if ((modifiers & Keys.Control) == 0) selected.Clear();
            if (!selected.Add(index)) selected.Remove(index);
            anchor = index;
        }
        Invalidate(); SelectionChanged?.Invoke();
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (dragging) ScrollTo((int)((e.Y - Header - grab) * Extent / Math.Max(1, ViewHeight - Thumb.Height)));
    }
    protected override void OnMouseUp(MouseEventArgs e) { dragging = Capture = false; base.OnMouseUp(e); }
    protected override void OnMouseCaptureChanged(EventArgs e) { if (!Capture) dragging = false; base.OnMouseCaptureChanged(e); }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        int width = Math.Max(0, Width - 13);
        void Cells(int y, int height, string[] values)
        {
            for (int i = 0; i < values.Length; i++)
                TextRenderer.DrawText(g, values[i], Font, new Rectangle(i * width / 3 + 10, y, Math.Max(0, width / 3 - 10), height), ForeColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
        using var background = new SolidBrush(UiPalette.FromArgb(31, 35, 39));
        using var selection = new SolidBrush(UiPalette.FromArgb(75, 85, 94));
        using var header = new SolidBrush(UiPalette.FromArgb(43, 49, 55));
        g.FillRectangle(background, ClientRectangle);
        var state = g.Save(); g.SetClip(new Rectangle(0, Header, width, ViewHeight));
        for (int i = offset / RowHeight; i < options.Count && Header + i * RowHeight - offset < Height; i++)
        {
            int y = Header + i * RowHeight - offset;
            if (selected.Contains(i)) g.FillRectangle(selection, 0, y, width, RowHeight);
            var item = options[i]; Cells(y, RowHeight, [item.Name, $"{item.Bytes / 1e9:0.000} GB", item.Games.ToString("N0")]);
        }
        g.Restore(state);
        g.FillRectangle(header, 0, 0, Width, Header);
        Cells(0, Header, [L.T("Zeitraum"), L.T("Downloadgröße (ca.)"), L.T("Partien")]);
        if (Extent > 0)
        {
            using var track = new SolidBrush(UiPalette.FromArgb(32, 38, 44));
            using var thumb = new SolidBrush(UiPalette.FromArgb(94, 111, 127));
            g.FillRectangle(track, Width - 9, Header, 8, ViewHeight); g.FillRectangle(thumb, Thumb);
        }
    }
}
