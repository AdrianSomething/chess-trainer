namespace ChessOpeningTrainer;

/// <summary>Commit local-game names on Enter or a left click outside the active text field.</summary>
internal sealed class NameInputCommit : IMessageFilter, IDisposable
{
    private readonly Form form;
    private readonly TextBox[] fields;
    private bool disposed;
    public NameInputCommit(Form form, params TextBox[] fields)
    {
        this.form = form; this.fields = fields;
        foreach (var field in fields) { field.KeyDown += OnKeyDown; field.Leave += OnLeave; }
        Application.AddMessageFilter(this);
    }
    private void Commit(TextBox field) { field.Text = field.Text.Trim(); form.ActiveControl = null; }
    private void OnLeave(object? sender, EventArgs e) { if (sender is TextBox field && !field.IsDisposed && !field.Disposing) field.Text = field.Text.Trim(); }
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (disposed || form.IsDisposed || form.Disposing || e.KeyCode != Keys.Enter || sender is not TextBox field) return;
        e.SuppressKeyPress = true; Commit(field);
    }
    public bool PreFilterMessage(ref Message message)
    {
        if (disposed || message.Msg is not (0x201 or 0xA1) || form.IsDisposed) return false;
        var focused = fields.FirstOrDefault(f => !f.IsDisposed && f.Focused);
        if (focused is null) return false;
        var point = new Point(unchecked((short)(long)message.LParam), unchecked((short)((long)message.LParam >> 16)));
        if (message.Msg == 0x201)
            point = Control.FromChildHandle(message.HWnd) is { } target ? target.PointToScreen(point) : Cursor.Position;
        if (!focused.RectangleToScreen(focused.ClientRectangle).Contains(point)) Commit(focused);
        return false;
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true; Application.RemoveMessageFilter(this);
        foreach (var field in fields) { field.KeyDown -= OnKeyDown; field.Leave -= OnLeave; }
    }
}
