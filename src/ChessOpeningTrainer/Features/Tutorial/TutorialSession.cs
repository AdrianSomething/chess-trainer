namespace ChessOpeningTrainer;

/// <summary>Owned by the home window; no global tutorial state and no UI reflection.</summary>
internal sealed class TutorialSession : IDisposable, IMessageFilter
{
    private readonly IReadOnlyList<TutorialStep> steps;
    private readonly Dictionary<Form, TutorialOverlay> overlays = [];
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 100 };
    private readonly Action finished;
    private int index = -1;
    private int furthest = -1;
    private bool disposed, interacted, changing;
    private DateTime entered;
    private DateTime noticeUntil;
    public TutorialSession(IReadOnlyList<TutorialStep> steps, Action finished)
    {
        this.steps = steps; this.finished = finished;
        Application.AddMessageFilter(this);
        timer.Tick += (_, _) => Refresh(); timer.Start(); Next();
    }
    private void Next()
    {
        if (disposed || changing) return;
        if (index < furthest) { Review(index + 1); return; }
        changing = true;
        try
        {
            if (index >= 0) steps[index].Leave?.Invoke();
            if (++index >= steps.Count) { Dispose(); return; }
            furthest = index;
            interacted = false; entered = DateTime.UtcNow;
            steps[index].Enter?.Invoke(); Refresh();
        }
        finally { changing = false; }
    }
    private void Review(int destination)
    {
        if (disposed || destination < 0 || destination > furthest) return;
        var window = steps[destination].Window();
        if (window is null || window.IsDisposed) return;
        index = destination; entered = DateTime.UtcNow; noticeUntil = default;
        window.Show(); window.Activate(); Refresh();
    }
    private bool CanAdvance => index < furthest || !steps[index].Interaction;
    private void Refresh()
    {
        if (disposed || index < 0 || index >= steps.Count) return;
        var step = steps[index]; var active = step.Window();
        if (active is { IsDisposed: true }) { Dispose(); return; }
        var forms = Application.OpenForms.Cast<Form>().Where(f => f.Visible && f.TopLevel && f is not HoverBoard).ToArray();
        foreach (var stale in overlays.Keys.Where(f => f.IsDisposed || !forms.Contains(f)).ToArray()) { overlays[stale].Dispose(); overlays.Remove(stale); }
        foreach (var form in forms)
        {
            if (!overlays.TryGetValue(form, out var overlay))
            {
                overlay = new(); overlays.Add(form, overlay);
                overlay.Advance += () => { if (form == steps[index].Window() && CanAdvance) Next(); };
                overlay.Previous += () => { if (form == steps[index].Window()) Review(index - 1); };
                overlay.Abort += Dispose; form.Controls.Add(overlay); overlay.BringToFront();
            }
            bool here = form == active;
            string text = here ? $"{index + 1}/{steps.Count}\n" + L.T(step.Text) : L.T("Der Überblick geht im anderen Fenster weiter:") + "\n" + (active?.Text ?? L.T("Fenster wird vorbereitet …"));
            if (here && DateTime.UtcNow < noticeUntil) text += "\n" + L.T("Bitte zuerst den Überblick beenden, bevor du diese Funktion ausprobierst.");
            overlay.UpdateView(form, here ? step.Target(form) : null, text, here && CanAdvance, here && index > 0);
        }
        if (index == furthest && !changing && step.Interaction && DateTime.UtcNow - entered > TimeSpan.FromMilliseconds(350) &&
            (step.Completed?.Invoke() ?? interacted)) Next();
    }
    public bool PreFilterMessage(ref Message m)
    {
        if (disposed || index < 0 || index >= steps.Count) return false;
        var step = steps[index];
        var source = Control.FromHandle(m.HWnd);
        bool inOverlay = overlays.Values.Any(o => o == source || source is not null && o.Contains(source));
        if (m.Msg == 0x0201 && step.Window() is { } active && overlays.TryGetValue(active, out var activeOverlay)
            && CanAdvance && activeOverlay.IsAdvanceAt(Cursor.Position))
        {
            foreach (var combo in Descendants(active).OfType<ComboBox>().Where(c => c.DroppedDown)) combo.DroppedDown = false;
            Next(); return true;
        }
        if ((m.Msg is 0x0201 or 0x0202) && !step.Interaction && !step.AllowInput && !inOverlay)
        {
            if (m.Msg == 0x0201) { noticeUntil = DateTime.UtcNow.AddSeconds(3); Refresh(); }
            return true;
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
        Application.RemoveMessageFilter(this); timer.Dispose();
        foreach (var overlay in overlays.Values) overlay.Dispose(); overlays.Clear();
        finished();
    }
    internal static IEnumerable<Control> Descendants(Control root) => root.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));
    internal static Control? Find(Form form, string text) => Descendants(form).FirstOrDefault(c => c.Visible && (c.Text == L.T(text) || c.AccessibleName == text || c.Name == text));
    internal static Control? Of<T>(Form form) where T : Control => Descendants(form).OfType<T>().FirstOrDefault(c => c.Visible);
}
