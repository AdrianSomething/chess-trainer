using System.Runtime.InteropServices;

namespace ChessOpeningTrainer;

/// <summary>Normal TabPage lifecycle with palette-painted, width-fitting headers.</summary>
public sealed class ThemedTabControl : TabControl, IMessageFilter
{
    private int hovered = -1;
    private bool fittingHeaders;
    private bool initialized;
    private bool hoverDisposed;
    private readonly ToolTip tip = new() { InitialDelay = 300, ReshowDelay = 100, AutoPopDelay = 10000, ShowAlways = true };
    private readonly System.Windows.Forms.Timer hoverTimer = new() { Interval = 350 };
    public ThemedTabControl()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        SizeMode = TabSizeMode.Fixed; ItemSize = new(100, 36); Padding = new(5, 3); ShowToolTips = false;
        BackColor = UiPalette.FromArgb(31, 35, 39); ForeColor = UiPalette.Gainsboro;
        SelectedIndexChanged += (_, _) => Invalidate();
        initialized = true;
        hoverTimer.Tick += (_, _) =>
        {
            hoverTimer.Stop();
            if (hovered >= 0 && hovered < TabCount && !IsDisposed && Visible)
                tip.Show(TabPages[hovered].Text, this, new Point(GetTabRect(hovered).Left + 4, GetTabRect(hovered).Bottom + 4), 10000);
        };
        // Native tab headers can consume mouse messages before managed
        // OnMouseMove/ToolTip relaying. Track their real message stream too.
        Application.AddMessageFilter(this);
        FitHeaders();
    }
    public override Rectangle DisplayRectangle => new(0, 42, Math.Max(0, ClientSize.Width), Math.Max(0, ClientSize.Height - 42));
    protected override void OnResize(EventArgs e) { base.OnResize(e); FitHeaders(); }
    protected override void OnControlAdded(ControlEventArgs e) { base.OnControlAdded(e); FitHeaders(); }
    protected override void OnControlRemoved(ControlEventArgs e) { base.OnControlRemoved(e); FitHeaders(); }
    private void FitHeaders()
    {
        // ItemSize updates the native tab geometry and can synchronously raise Resize.
        if (!initialized || fittingHeaders || IsDisposed) return;
        fittingHeaders = true;
        try
        {
            int width = Math.Clamp(Math.Max(1, ClientSize.Width - 8) / Math.Max(1, TabCount), 36, 190);
            Size desired = new(width, 36);
            if (ItemSize != desired) ItemSize = desired;
            Invalidate();
        }
        finally { fittingHeaders = false; }
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        for (int i = 0; i < TabCount; i++)
        {
            var rectangle = GetTabRect(i);
            using var fill = new SolidBrush(i == SelectedIndex ? UiPalette.FromArgb(57, 91, 119)
                : i == hovered ? UiPalette.FromArgb(58, 66, 74) : UiPalette.FromArgb(43, 49, 55));
            e.Graphics.FillRectangle(fill, rectangle);
            TextRenderer.DrawText(e.Graphics, TabPages[i].Text, Font, Rectangle.Inflate(rectangle, -5, -2), UiPalette.Gainsboro,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        UpdateHover(e.Location);
    }
    private void UpdateHover(Point point)
    {
        if (hoverDisposed || !initialized || !IsHandleCreated) return;
        int next = Enumerable.Range(0, TabCount).FirstOrDefault(i => GetTabRect(i).Contains(point), -1);
        if (next != hovered) { hovered = next; tip.Hide(this); tip.SetToolTip(this, next >= 0 ? TabPages[next].Text : ""); hoverTimer.Stop(); if (next >= 0) hoverTimer.Start(); Invalidate(new Rectangle(0, 0, Width, 42)); }
    }
    private void HideHover()
    {
        if (hoverDisposed || !initialized) return;
        hovered = -1; hoverTimer.Stop(); tip.Hide(this); tip.SetToolTip(this, ""); Invalidate(new Rectangle(0, 0, Width, 42));
    }
    public bool PreFilterMessage(ref Message message)
    {
        if (hoverDisposed || !IsHandleCreated || !Visible || message.Msg is not (0x0200 or 0x00A0)) return false;
        if (message.HWnd == Handle && message.Msg == 0x0200)
            UpdateHover(new((short)(message.LParam.ToInt64() & 0xffff), (short)((message.LParam.ToInt64() >> 16) & 0xffff)));
        else if (IsChild(Handle, message.HWnd)) UpdateHover(PointToClient(Cursor.Position));
        else if (hovered >= 0) HideHover(); // A different window may cover this header at the same screen coordinates.
        return false;
    }
    protected override void WndProc(ref Message message)
    {
        if (message.Msg == 0x0200 && initialized && !hoverDisposed)
            UpdateHover(new((short)(message.LParam.ToInt64() & 0xffff), (short)((message.LParam.ToInt64() >> 16) & 0xffff)));
        base.WndProc(ref message);
    }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); HideHover(); }
    protected override void OnVisibleChanged(EventArgs e) { base.OnVisibleChanged(e); if (!Visible) HideHover(); }
    protected override void OnHandleDestroyed(EventArgs e) { HideHover(); base.OnHandleDestroyed(e); }
    protected override void Dispose(bool disposing)
    {
        if (disposing && !hoverDisposed)
        {
            Application.RemoveMessageFilter(this); HideHover(); hoverDisposed = true;
            hoverTimer.Dispose(); tip.Dispose();
        }
        base.Dispose(disposing);
    }
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsChild(IntPtr parent, IntPtr child);
}
