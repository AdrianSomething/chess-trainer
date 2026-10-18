using System.Runtime.InteropServices;

namespace ChessOpeningTrainer;

/// <summary>
/// Owns the arrow's child-window region. Native ComboBox hover drawing is clipped
/// behind this surface, rather than briefly showing white and being overpainted.
/// Selection, keyboard input and the dropdown remain the original ComboBox's.
/// </summary>
internal sealed class ComboArrowSurface : Control
{
    private readonly ComboBox combo;
    public ComboArrowSurface(ComboBox combo)
    {
        this.combo = combo; Name = "ThemedComboArrow"; TabStop = false; Visible = false;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        SetStyle(ControlStyles.Selectable, false);
        BackColor = UiPalette.FromArgb(43, 49, 55);
    }
    protected override void OnPaintBackground(PaintEventArgs e) => e.Graphics.Clear(UiPalette.FromArgb(43, 49, 55));
    protected override void OnPaint(PaintEventArgs e) => NativeControlTheme.DrawArrow(e.Graphics, ClientRectangle, true, combo.Enabled);
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left || !combo.Enabled) return;
        combo.Focus(); combo.DroppedDown = !combo.DroppedDown;
    }
    protected override void OnMouseWheel(MouseEventArgs e)
    {
        // SettingsWheelGuard sees the child through its ComboBox ancestry.
        // Other screens retain the ComboBox's ordinary wheel behavior.
        var point = PointToScreen(e.Location);
        SendMessage(combo.Handle, 0x020A, (IntPtr)(e.Delta << 16), (IntPtr)((point.Y << 16) | (point.X & 0xffff)));
    }
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hwnd, int message, IntPtr wparam, IntPtr lparam);
}
