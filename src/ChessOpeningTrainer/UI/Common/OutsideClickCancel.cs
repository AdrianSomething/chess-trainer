namespace ChessOpeningTrainer;

/// <summary>Transient editor guard, removed as soon as its controls are closed.</summary>
internal sealed class OutsideClickCancel(Control input, Control accept, Action cancel) : IMessageFilter, IDisposable
{
    private bool disposed;
    public bool PreFilterMessage(ref Message message)
    {
        if (disposed || input.IsDisposed || message.Msg is not (0x201 or 0xA1)) return false;
        // Client/nonclient button messages have different coordinate spaces.
        int x = unchecked((short)(long)message.LParam), y = unchecked((short)((long)message.LParam >> 16));
        var point = new Point(x, y);
        if (message.Msg == 0x201 && Control.FromChildHandle(message.HWnd) is { } target)
            point = target.PointToScreen(point);
        Rectangle Bounds(Control control) => control.Parent?.RectangleToScreen(control.Bounds) ?? control.RectangleToScreen(control.ClientRectangle);
        if (!Bounds(input).Contains(point) && !Bounds(accept).Contains(point)) cancel();
        return false; // The click still performs its original action.
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true; Application.RemoveMessageFilter(this);
    }
}
