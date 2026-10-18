namespace ChessOpeningTrainer;

/// <summary>Wheel input scrolls settings; it never edits the value under the pointer.</summary>
internal sealed class SettingsWheelGuard(Form form) : IMessageFilter, IDisposable
{
    private bool disposed;
    public bool PreFilterMessage(ref Message message)
    {
        if (message.Msg != 0x20A || disposed || form.IsDisposed) return false;
        var control = Control.FromChildHandle(message.HWnd);
        var scope = control?.FindForm();
        while (scope is not null && scope != form) scope = scope.Owner;
        if (scope != form) return false;
        int delta = unchecked((short)((long)message.WParam >> 16));
        for (Control? ancestor = control; ancestor is not null; ancestor = ancestor.Parent)
            if (ancestor is ScrollableControl { AutoScroll: true } panel && panel.VerticalScroll.Visible)
            {
                panel.AutoScrollPosition = new(0, Math.Max(0, -panel.AutoScrollPosition.Y - delta));
                break;
            }
        return true;
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true; Application.RemoveMessageFilter(this);
    }
}
