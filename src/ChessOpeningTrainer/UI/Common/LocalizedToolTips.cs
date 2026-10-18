using System.Runtime.CompilerServices;

namespace ChessOpeningTrainer;

/// <summary>One language-change subscription per tooltip, released with either UI scope.</summary>
internal static class LocalizedToolTips
{
    private static readonly ConditionalWeakTable<ToolTip, Scope> scopes = new();
    internal static void Set(ToolTip tip, Control control, string source)
        => scopes.GetValue(tip, value => new Scope(value)).Set(control, source);

    private sealed class Scope
    {
        private readonly ToolTip tip;
        private readonly object sync = new();
        private readonly Dictionary<Control, string> sources = [];
        private readonly int ownerThread = Environment.CurrentManagedThreadId;
        private string language = Preferences.Current.Language;
        private bool listening, disposed;

        internal Scope(ToolTip tip)
        {
            this.tip = tip;
            tip.Disposed += OnTipDisposed;
        }

        internal void Set(Control control, string source)
        {
            lock (sync)
            {
                if (disposed || control.IsDisposed) return;
                if (!sources.ContainsKey(control)) control.Disposed += OnControlDisposed;
                sources[control] = source;
                if (!listening) { Preferences.Changed += Refresh; listening = true; }
            }
            RefreshControl(control);
        }

        private void Refresh()
        {
            Control[] controls;
            lock (sync)
            {
                if (disposed || language == Preferences.Current.Language) return;
                language = Preferences.Current.Language;
                controls = sources.Keys.ToArray();
            }
            foreach (var control in controls) RefreshControl(control);
        }

        private void RefreshControl(Control control)
        {
            if (control.IsDisposed || control.Disposing) return;
            if (control.IsHandleCreated && control.InvokeRequired)
            {
                try { control.BeginInvoke(() => RefreshControl(control)); }
                catch (InvalidOperationException) { /* Its window is closing. */ }
                return;
            }
            // Never create a native control handle on a background preference writer.
            if (!control.IsHandleCreated && Environment.CurrentManagedThreadId != ownerThread) return;
            lock (sync)
            {
                if (!disposed && sources.TryGetValue(control, out string? source))
                    tip.SetToolTip(control, L.T(source));
            }
        }

        private void OnControlDisposed(object? sender, EventArgs e)
        {
            if (sender is not Control control) return;
            lock (sync)
            {
                control.Disposed -= OnControlDisposed;
                sources.Remove(control);
                if (sources.Count == 0 && listening) { Preferences.Changed -= Refresh; listening = false; }
            }
        }

        private void OnTipDisposed(object? sender, EventArgs e)
        {
            lock (sync)
            {
                disposed = true;
                if (listening) { Preferences.Changed -= Refresh; listening = false; }
                foreach (var control in sources.Keys) control.Disposed -= OnControlDisposed;
                sources.Clear();
                tip.Disposed -= OnTipDisposed;
            }
        }
    }
}
