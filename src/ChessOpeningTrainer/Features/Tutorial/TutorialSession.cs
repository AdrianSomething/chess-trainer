namespace ChessOpeningTrainer;

/// <summary>Owned by the home window; no global tutorial state and no UI reflection.</summary>
internal sealed class TutorialSession : IDisposable, IMessageFilter
{
    private readonly IReadOnlyList<TutorialStep> steps;
    private readonly Dictionary<Form, TutorialOverlay> overlays = [];
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 100 };
    private readonly Action finished;
    private int index = -1;
    private bool disposed, interacted, changing, committed, satisfied;
    private readonly Action<int, int>? prepare;
    private ComboBox? watchedCombo;
    private DateTime entered;
    private DateTime noticeUntil;
    public TutorialSession(IReadOnlyList<TutorialStep> steps, Action finished) : this(steps, finished, null) { }
    public TutorialSession(IReadOnlyList<TutorialStep> steps, Action finished, Action<int, int>? prepare)
    {
        this.steps = steps; this.finished = finished; this.prepare = prepare;
        Application.AddMessageFilter(this);
        timer.Tick += (_, _) => Refresh(); timer.Start(); Next();
    }
    private void Next()
    {
        if (disposed || changing) return;
        if (index >= 0) steps[index].Leave?.Invoke();
        Review(index + 1);
    }
    private void Review(int destination)
    {
        if (disposed || changing || destination < 0) return;
        if (destination >= steps.Count) { Dispose(); return; }
        changing = true;
        try
        {
            int previous = index;
            if (index >= 0 && steps[index].Window() is { IsDisposed: false } oldWindow)
            {
                foreach (var combo in Descendants(oldWindow).OfType<ComboBox>().Where(c => c.DroppedDown)) combo.DroppedDown = false;
                foreach (var control in Descendants(oldWindow).Where(c => c.Capture)) control.Capture = false;
            }
            WatchCombo(null);
            index = destination; entered = DateTime.UtcNow; noticeUntil = default;
            interacted = committed = satisfied = false;
            prepare?.Invoke(index, previous);
            steps[index].Enter?.Invoke();
            var window = steps[index].Window();
            if (window is { IsDisposed: false }) { window.Show(); window.Activate(); }
            Refresh();
        }
        finally { changing = false; }
    }
    private bool CanAdvance => !steps[index].Interaction || satisfied;
    private void Forward()
    {
        if (disposed || changing) return;
        if (steps[index].Interaction && !satisfied) steps[index].Skip?.Invoke();
        Next();
    }
    private void WatchCombo(ComboBox? combo)
    {
        if (watchedCombo == combo) return;
        if (watchedCombo is not null) watchedCombo.SelectionChangeCommitted -= SelectionCommitted;
        watchedCombo = combo;
        if (combo is not null) combo.SelectionChangeCommitted += SelectionCommitted;
    }
    private void SelectionCommitted(object? sender, EventArgs e) => committed = true;
    private void Refresh()
    {
        if (disposed || index < 0 || index >= steps.Count) return;
        var step = steps[index]; var active = step.Window();
        if (active is { IsDisposed: true }) { Dispose(); return; }
        WatchCombo(active is null ? null : step.Target(active) as ComboBox);
        if (step.Interaction && !changing)
        {
            bool done = step.Completed?.Invoke() ?? interacted;
            // Native combos may change SelectedIndex while previewing a row.
            // Only CBN_SELENDOK (SelectionChangeCommitted) confirms the choice.
            satisfied |= done && (watchedCombo is null || committed && !watchedCombo.DroppedDown);
        }
        var forms = Application.OpenForms.Cast<Form>().Where(f => f.Visible && f.TopLevel && f is not HoverBoard).ToArray();
        foreach (var stale in overlays.Keys.Where(f => f.IsDisposed || !forms.Contains(f)).ToArray()) { overlays[stale].Dispose(); overlays.Remove(stale); }
        foreach (var form in forms)
        {
            if (!overlays.TryGetValue(form, out var overlay))
            {
                overlay = new(); overlays.Add(form, overlay);
                overlay.Advance += () => { if (form == steps[index].Window() && CanAdvance) Next(); };
                overlay.Previous += () => { if (form == steps[index].Window()) Review(index - 1); };
                overlay.Forward += () => { if (form == steps[index].Window()) Forward(); };
                overlay.Abort += Dispose; form.Controls.Add(overlay); overlay.BringToFront();
            }
            bool here = form == active;
            string text = here ? $"{index + 1}/{steps.Count}\n" + L.T(step.Text) : L.T("Der Überblick geht im anderen Fenster weiter:") + "\n" + (active?.Text ?? L.T("Fenster wird vorbereitet …"));
            if (here && DateTime.UtcNow < noticeUntil) text += "\n" + L.T("Bitte zuerst den Überblick beenden, bevor du diese Funktion ausprobierst.");
            overlay.UpdateViewArea(form, here ? step.Target(form) : null, text, here && CanAdvance, here && index > 0,
                here ? step.TargetArea : null);
        }
        if (!changing && step.Interaction && step.AutoAdvance && satisfied && DateTime.UtcNow - entered > TimeSpan.FromMilliseconds(350)) Next();
    }
    public bool PreFilterMessage(ref Message m)
    {
        if (disposed || index < 0 || index >= steps.Count) return false;
        var step = steps[index];
        var source = Control.FromHandle(m.HWnd);
        bool inOverlay = overlays.Values.Any(o => o == source || source is not null && o.Contains(source));
        bool clientMouse = m.Msg is >= 0x0201 and <= 0x0209;
        bool managedWindow = source?.FindForm() is { } sourceForm && overlays.ContainsKey(sourceForm);
        // Never swallow non-client input: title-bar close/maximize must work.
        // Treat down/up/double-click as a complete sequence, otherwise Button
        // can capture the mouse on DBLCLK and never receive the blocked up.
        if (clientMouse && managedWindow && !inOverlay &&
            (!step.Interaction && !step.AllowInput || step.Interaction && interacted && step.Target(step.Window()!) is Button))
        {
            if (source is not null) source.Capture = false;
            if (m.Msg == 0x0201) { noticeUntil = DateTime.UtcNow.AddSeconds(3); Refresh(); }
            return true;
        }
        if (m.Msg == 0x0201 && step.Window() is { } navigationHost && overlays.TryGetValue(navigationHost, out var navigationOverlay)
            && navigationOverlay.NavigationAt(Cursor.Position) is int direction && direction != 0)
        {
            foreach (var combo in Descendants(navigationHost).OfType<ComboBox>().Where(c => c.DroppedDown)) combo.DroppedDown = false;
            if (direction < 0) Review(index - 1); else Forward();
            return true;
        }
        if ((m.Msg is 0x0100 or 0x0101) && (Keys)(int)m.WParam is Keys.Left or Keys.Right)
        {
            if (m.Msg == 0x0100 && ((long)m.LParam & (1L << 30)) == 0)
            { if ((Keys)(int)m.WParam == Keys.Left) Review(index - 1); else Forward(); }
            return true;
        }
        if (m.Msg == 0x0201 && step.Window() is { } active && overlays.TryGetValue(active, out var activeOverlay)
            && CanAdvance && activeOverlay.IsAdvanceAt(Cursor.Position))
        {
            foreach (var combo in Descendants(active).OfType<ComboBox>().Where(c => c.DroppedDown)) combo.DroppedDown = false;
            Next(); return true;
        }
        // Consume the entire key sequence, including key-up/character messages:
        // Space may advance information, but never click Settings or the X.
        if ((m.Msg is 0x0100 or 0x0101 or 0x0102) && (Keys)(int)m.WParam == Keys.Space)
        {
            if (m.Msg == 0x0100 && ((long)m.LParam & (1L << 30)) == 0)
            {
                if (CanAdvance) Next();
                else step.SpaceAction?.Invoke();
            }
            return true;
        }
        if (m.Msg == 0x0100 && !step.Interaction && !step.AllowInput && !inOverlay) return true;
        if (m.Msg == 0x0202 && step.Interaction && step.Window() is { } window && step.Target(window) is { } target)
        {
            var control = source;
            if (control == target || control is not null && target.Contains(control)) interacted = true;
        }
        return false;
    }
    public void Dispose()
    {
        if (disposed) return; disposed = true;
        WatchCombo(null);
        Application.RemoveMessageFilter(this); timer.Dispose();
        foreach (var overlay in overlays.Values) overlay.Dispose(); overlays.Clear();
        finished();
    }
    internal static IEnumerable<Control> Descendants(Control root) => root.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));
    internal static Control? Find(Form form, string text) => Descendants(form).FirstOrDefault(c => c.Visible && (c.Text == L.T(text) || c.AccessibleName == text || c.Name == text));
    internal static Control? Of<T>(Form form) where T : Control => Descendants(form).OfType<T>().FirstOrDefault(c => c.Visible);
}
