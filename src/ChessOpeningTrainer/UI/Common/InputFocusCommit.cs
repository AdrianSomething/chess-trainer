using System.Runtime.InteropServices;

namespace ChessOpeningTrainer;

/// <summary>One form's outside-click numeric commit, optionally including text inputs. Never consumes the original click.</summary>
public sealed class InputFocusCommit : IMessageFilter, IDisposable
{
    private readonly Form form;
    private readonly Action<ScrollNumber> commit;
    private readonly bool includeTextBoxes;
    private bool disposed;
    public InputFocusCommit(Form form, Action<ScrollNumber>? commit = null, bool includeTextBoxes = false)
    {
        this.form = form; this.commit = commit ?? (number => number.CommitEditedValue());
        this.includeTextBoxes = includeTextBoxes;
        Application.AddMessageFilter(this); form.Disposed += FormDisposed;
    }
    public bool PreFilterMessage(ref Message message)
    {
        if (disposed || form.IsDisposed || message.Msg != 0x0201) return false;
        var target = Control.FromChildHandle(message.HWnd);
        if (target?.FindForm() != form) return false;
        Control? focusedControl = Control.FromChildHandle(GetFocus());
        Control? focus = focusedControl;
        while (focus is not null && focus is not ScrollNumber) focus = focus.Parent;
        if (focus is ScrollNumber number)
        {
            if (number.FindForm() != form || target == number || number.Contains(target)) return false;
            commit(number);
        }
        else
        {
            if (!includeTextBoxes) return false;
            focus = focusedControl;
            while (focus is not null && focus is not TextBox) focus = focus.Parent;
            if (focus is not TextBox text || text.FindForm() != form || target == text || text.Contains(target)) return false;
            // TextBox edits already update Text immediately. Dropping focus
            // commits ordinary WinForms validation without rewriting the text.
            text.DeselectAll();
        }
        form.ActiveControl = null;
        return false;
    }
    private void FormDisposed(object? sender, EventArgs e) => Dispose();
    public void Dispose()
    {
        if (disposed) return;
        disposed = true; Application.RemoveMessageFilter(this); form.Disposed -= FormDisposed;
    }
    [DllImport("user32.dll")] private static extern IntPtr GetFocus();
}
