using System.Runtime.InteropServices;

namespace ChessOpeningTrainer;

/// <summary>Suppresses native input painting until a section layout is final.</summary>
internal sealed class GeneratorLayoutBatch : IDisposable
{
    private readonly Control root;
    private readonly List<(Control Control, bool NativeVisible)> controls = [];

    public GeneratorLayoutBatch(Control root)
    {
        this.root = root;
        if (!root.IsHandleCreated || !root.Visible) return;
        void Freeze(Control control)
        {
            if (control.IsHandleCreated)
            {
                controls.Add((control, (GetWindowLong(control.Handle, -16 /* GWL_STYLE */) & 0x10000000 /* WS_VISIBLE */) != 0));
                SendMessage(control.Handle, 0x000B /* WM_SETREDRAW */, IntPtr.Zero, IntPtr.Zero);
            }
            foreach (Control child in control.Controls) Freeze(child);
        }
        Freeze(root);
    }

    public void Dispose()
    {
        // Re-enable every HWND, including hidden columns, so a later Show() or
        // hidden-window configuration cannot retain an old redraw lock. Preserve
        // native child visibility independently of inherited Control.Visible:
        // an arrow inside a hidden numeric field still needs WS_VISIBLE itself.
        foreach (var (control, nativeVisible) in controls.AsEnumerable().Reverse())
            if (!control.IsDisposed && control.IsHandleCreated)
            {
                bool visible = control.Parent == root ? control.Visible : nativeVisible;
                SendMessage(control.Handle, 0x000B, new IntPtr(1), IntPtr.Zero);
                if (!visible) ShowWindow(control.Handle, 0 /* SW_HIDE */);
            }
        if (root.IsHandleCreated && root.Visible)
            RedrawWindow(root.Handle, IntPtr.Zero, IntPtr.Zero, 0x0001 | 0x0004 | 0x0080 | 0x0400 /* invalidate + erase + children + frame */);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hwnd, int message, IntPtr wparam, IntPtr lparam);
    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hwnd, int index);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool ShowWindow(IntPtr hwnd, int command);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RedrawWindow(IntPtr hwnd, IntPtr update, IntPtr region, uint flags);
}
