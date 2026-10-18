using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace ChessOpeningTrainer;

/// <summary>Skins Win32 chrome without replacing native input, keyboard or scrolling behavior.</summary>
internal sealed class NativeControlTheme : NativeWindow, IDisposable
{
    private readonly Control owner;
    private readonly bool stepper;
    private readonly List<NativeControlTheme> children = [];
    private bool painting, disposed, redrawSuppressed, repaintQueued;
    private Control? previousParent;
    private Rectangle previousBounds;
    private ComboArrowSurface? comboArrow;
    public NativeControlTheme(Control owner, bool stepper = false)
    {
        this.owner = owner; this.stepper = stepper;
        owner.HandleCreated += HandleCreated;
        owner.HandleDestroyed += HandleDestroyed;
        owner.Disposed += OwnerDisposed;
        owner.LocationChanged += OwnerLayoutChanged; owner.SizeChanged += OwnerLayoutChanged;
        owner.VisibleChanged += OwnerLayoutChanged; owner.ParentChanged += OwnerLayoutChanged;
        previousParent = owner.Parent; previousBounds = owner.Bounds;
        if (owner.IsHandleCreated) AttachHandle();
    }
    private NativeControlTheme(Control owner, IntPtr handle)
    {
        this.owner = owner;
        AssignHandle(handle);
    }
    private void HandleCreated(object? sender, EventArgs e) => AttachHandle();
    private void HandleDestroyed(object? sender, EventArgs e)
    {
        foreach (var child in children) child.Dispose(); children.Clear();
        ReleaseHandle();
    }
    private void OwnerDisposed(object? sender, EventArgs e) => Dispose();
    private void AttachHandle()
    {
        if (disposed) return;
        redrawSuppressed = false;
        AssignHandle(owner.Handle);
        if (owner is ComboBox combo)
        {
            const int clipChildren = 0x02000000;
            SetWindowLong(Handle, -16, GetWindowLong(Handle, -16) | clipChildren);
            if (comboArrow is null || comboArrow.IsDisposed)
            {
                comboArrow = new(combo);
                combo.Controls.Add(comboArrow);
            }
            UpdateComboArrow();
        }
        if (owner is UpDownBase)
            foreach (Control child in owner.Controls)
                if (child is not TextBoxBase) children.Add(new NativeControlTheme(child, true));
        RefreshTheme();
    }
    public void RefreshTheme()
    {
        if (disposed || Handle == IntPtr.Zero) return;
        bool light = UiPalette.FromArgb(31, 35, 39).GetBrightness() > .55f;
        SetWindowTheme(Handle, light ? "Explorer" : "DarkMode_Explorer", null);
        foreach (var child in children) child.RefreshTheme();
        owner.Invalidate();
        QueueSettledRepaint();
        if (owner is ComboBox && GetComboBoxInfo(Handle, out var info))
        {
            if (info.List != IntPtr.Zero && children.All(c => c.Handle != info.List)) children.Add(new NativeControlTheme(owner, info.List));
            if (info.List != IntPtr.Zero) SetWindowTheme(info.List, light ? "Explorer" : "DarkMode_Explorer", null);
        }
    }
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x000B /* WM_SETREDRAW */) redrawSuppressed = m.WParam == IntPtr.Zero;
        if (stepper && !disposed && !owner.IsDisposed && m.Msg is 0x000F or 0x0014 or 0x0317 or 0x0318)
        {
            // The numeric arrow window is completely owner-painted. Its native
            // erase/paint procedure never draws a white intermediate frame.
            if (m.Msg == 0x0014) { m.Result = new IntPtr(1); return; }
            bool print = m.Msg is 0x0317 or 0x0318;
            if (!print && (redrawSuppressed || !IsWindowVisible(Handle))) { ValidateRect(Handle, IntPtr.Zero); return; }
            IntPtr dc = print ? m.WParam : BeginPaint(Handle, out paintBuffer);
            try { if (dc != IntPtr.Zero) PaintStepper(dc); }
            finally { if (!print) EndPaint(Handle, ref paintBuffer); }
            m.Result = IntPtr.Zero; return;
        }
        base.WndProc(ref m);
        if (painting || disposed || owner.IsDisposed || Handle == IntPtr.Zero) return;
        // State changes request a later paint; never draw directly during
        // erasure, mouse processing, or a half-complete child-window layout.
        if (!stepper && owner is not ComboBox && m.Msg is 0x115 or 0x20A or 0xA0 or 0xA1 or 0xA2)
            QueueSettledRepaint();
        if (m.Msg is 0xF or 0x85 or 0x317 or 0x318)
        {
            bool print = m.Msg is 0x317 or 0x318;
            // Native chrome bypasses WinForms' double buffer. Respect redraw
            // batches even for WM_ERASEBKGND; otherwise arrows appear at interim
            // positions. WM_PRINT stays available for DrawToBitmap/previews.
            if (!print && (redrawSuppressed || !IsWindowVisible(Handle))) return;
            painting = true;
            try
            {
                IntPtr printDc = m.Msg is 0x317 or 0x318 ? m.WParam : IntPtr.Zero;
                if (owner is ComboBox && Handle == owner.Handle) comboArrow?.Invalidate();
                if (printDc == IntPtr.Zero) PaintScrollBar();
            }
            finally { painting = false; }
        }
    }

    private void OwnerLayoutChanged(object? sender, EventArgs e)
    {
        if (disposed) return;
        // Clear old native chrome when inputs shrink, hide, or change parents.
        // Invalidate the old location as well as the final one; a child-window
        // redraw alone cannot remove arrows left in its parent's former pixels.
        if (previousParent is { IsDisposed: false, Visible: true }) previousParent.Invalidate(previousBounds, true);
        previousParent = owner.Parent; previousBounds = owner.Bounds;
        UpdateComboArrow();
        QueueSettledRepaint();
    }

    private void UpdateComboArrow()
    {
        if (comboArrow is null || comboArrow.IsDisposed || Handle == IntPtr.Zero || !GetComboBoxInfo(Handle, out var info) || !GetClientRect(Handle, out var client)) return;
        Rectangle bounds = Rectangle.Intersect(info.Button.ToRectangle(), client.ToRectangle());
        comboArrow.Bounds = bounds;
        comboArrow.Visible = bounds.Width > 0 && bounds.Height > 0 && (info.ButtonState & 0x8000) == 0;
        comboArrow.BringToFront(); comboArrow.Invalidate();
    }

    private void QueueSettledRepaint()
    {
        if (disposed || repaintQueued || redrawSuppressed || Handle == IntPtr.Zero || !owner.IsHandleCreated || !owner.Visible) return;
        repaintQueued = true;
        try
        {
            owner.BeginInvoke((Action)(() =>
            {
                repaintQueued = false;
                if (disposed || owner.IsDisposed || !owner.Visible || !owner.IsHandleCreated || redrawSuppressed) return;
                // Only the non-client scrollbar needs a settled touch-up. A
                // parent/child invalidation here repainted every native input
                // for each mouse move over a settings or generator panel.
                if (painting) return;
                painting = true;
                try { PaintScrollBar(); }
                finally { painting = false; }
            }));
        }
        catch (InvalidOperationException) { repaintQueued = false; }
    }
    private void PaintStepper(IntPtr printDc)
    {
        using var graphics = printDc == IntPtr.Zero ? Graphics.FromHwnd(Handle) : Graphics.FromHdc(printDc);
        GetClientRect(Handle, out var bounds);
        var rect = bounds.ToRectangle();
        if (rect.Width <= 0 || rect.Height <= 0) return;
        graphics.SetClip(rect, CombineMode.Intersect);
        using var fill = new SolidBrush(UiPalette.FromArgb(43, 49, 55));
        graphics.FillRectangle(fill, rect);
        var upper = new Rectangle(rect.X, rect.Y, rect.Width, rect.Height / 2);
        var lower = new Rectangle(rect.X, upper.Bottom, rect.Width, rect.Height - upper.Height);
        DrawArrow(graphics, upper, down: false, owner.Enabled);
        DrawArrow(graphics, lower, down: true, owner.Enabled);
    }
    internal static void DrawArrow(Graphics graphics, Rectangle rect, bool down, bool enabled)
    {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        float x = rect.X + rect.Width / 2f, y = rect.Y + rect.Height / 2f;
        float size = Math.Min(3.5f, rect.Width / 4f), sign = down ? 1 : -1;
        using var pen = new Pen(enabled ? UiPalette.FromArgb(155, 163, 171) : UiPalette.FromArgb(94, 111, 127), 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        graphics.DrawLines(pen, new PointF[] { new(x - size, y - sign * size / 2), new(x, y + sign * size / 2), new(x + size, y - sign * size / 2) });
    }
    private void PaintScrollBar()
    {
        var scroll = new ScrollBarInfo { Size = (uint)Marshal.SizeOf<ScrollBarInfo>(), States = new uint[6] };
        if (!GetScrollBarInfo(Handle, unchecked((int)0xffff_fffb), ref scroll) || (scroll.States[0] & 0x8000) != 0) return;
        if (!GetWindowRect(Handle, out var window)) return;
        var rect = scroll.Bounds.ToRectangle(); rect.Offset(-window.Left, -window.Top);
        rect.Intersect(new Rectangle(0, 0, window.Right - window.Left, window.Bottom - window.Top));
        if (rect.Width <= 0 || rect.Height <= 0) return;
        var dc = GetWindowDC(Handle);
        if (dc == IntPtr.Zero) return;
        try
        {
            using var graphics = Graphics.FromHdc(dc);
            using var track = new SolidBrush(UiPalette.FromArgb(32, 38, 44));
            using var thumb = new SolidBrush(UiPalette.FromArgb(94, 111, 127));
            graphics.FillRectangle(track, rect);
            int top = rect.Top + scroll.ThumbTop, height = Math.Max(6, scroll.ThumbBottom - scroll.ThumbTop);
            if (rect.Width > 2 && top < rect.Bottom)
                graphics.FillRectangle(thumb, rect.X + Math.Max(1, (rect.Width - 6) / 2), top, Math.Min(6, rect.Width - 2), Math.Min(height, rect.Bottom - top));
        }
        finally { ReleaseDC(Handle, dc); }
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        owner.HandleCreated -= HandleCreated; owner.HandleDestroyed -= HandleDestroyed; owner.Disposed -= OwnerDisposed;
        owner.LocationChanged -= OwnerLayoutChanged; owner.SizeChanged -= OwnerLayoutChanged;
        owner.VisibleChanged -= OwnerLayoutChanged; owner.ParentChanged -= OwnerLayoutChanged;
        foreach (var child in children) child.Dispose(); children.Clear(); ReleaseHandle();
        comboArrow?.Dispose(); comboArrow = null;
    }
    private static bool GetComboBoxInfo(IntPtr handle, out ComboInfo info)
    {
        info = new ComboInfo { Size = Marshal.SizeOf<ComboInfo>() };
        return NativeGetComboBoxInfo(handle, ref info);
    }
    [StructLayout(LayoutKind.Sequential)] private struct Rect
    {
        public int Left, Top, Right, Bottom;
        public readonly Rectangle ToRectangle() => Rectangle.FromLTRB(Left, Top, Right, Bottom);
    }
    [StructLayout(LayoutKind.Sequential)] private struct ComboInfo { public int Size; public Rect Item, Button; public uint ButtonState; public IntPtr Combo, Edit, List; }
    [StructLayout(LayoutKind.Sequential)] private struct ScrollBarInfo
    {
        public uint Size; public Rect Bounds; public int LineButton, ThumbTop, ThumbBottom, Reserved;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)] public uint[] States;
    }
    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)] private static extern int SetWindowTheme(IntPtr handle, string theme, string? idList);
    [DllImport("user32.dll", EntryPoint = "GetComboBoxInfo")] private static extern bool NativeGetComboBoxInfo(IntPtr handle, ref ComboInfo info);
    [DllImport("user32.dll")] private static extern bool GetScrollBarInfo(IntPtr handle, int id, ref ScrollBarInfo info);
    [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr handle, out Rect rect);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr handle, out Rect rect);
    [DllImport("user32.dll")] private static extern IntPtr GetWindowDC(IntPtr handle);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr handle, IntPtr dc);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(IntPtr handle);
    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hwnd, int index);
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hwnd, int index, int value);
    private PaintInfo paintBuffer;
    [StructLayout(LayoutKind.Sequential)] private struct PaintInfo
    {
        public IntPtr Dc; [MarshalAs(UnmanagedType.Bool)] public bool Erase;
        public Rect Bounds; [MarshalAs(UnmanagedType.Bool)] public bool Restore;
        [MarshalAs(UnmanagedType.Bool)] public bool IncUpdate;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] Reserved;
    }
    [DllImport("user32.dll")] private static extern IntPtr BeginPaint(IntPtr hwnd, out PaintInfo paint);
    [DllImport("user32.dll")] private static extern bool EndPaint(IntPtr hwnd, ref PaintInfo paint);
    [DllImport("user32.dll")] private static extern bool ValidateRect(IntPtr hwnd, IntPtr rectangle);
}
