namespace ChessOpeningTrainer;

/// <summary>Generator-only focus order; section parameters are traversed column by column.</summary>
internal sealed class GeneratorInputNavigation : IDisposable
{
    private readonly Control name, perspective, afterLast;
    private readonly ScrollNumber count;
    private readonly SectionEditor sections;
    private readonly ScrollNumber[] trailing;

    public GeneratorInputNavigation(Control name, Control perspective, ScrollNumber count, SectionEditor sections, Control afterLast, params ScrollNumber[] trailing)
    {
        this.name = name; this.perspective = perspective; this.count = count;
        this.sections = sections; this.afterLast = afterLast; this.trailing = trailing;
        foreach (var number in sections.AllInputs.Concat(trailing).Prepend(count)) number.NavigateInput = MoveFocus;
        name.KeyDown += CommitText; perspective.KeyDown += CommitText;
    }
    private void CommitText(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter || sender is not Control input) return;
        e.Handled = true; e.SuppressKeyPress = true;
        MoveFocus(input, 1);
    }
    private void MoveFocus(ScrollNumber number, int step) => MoveFocus((Control)number, step);
    private void MoveFocus(Control input, int step)
    {
        var order = new Control[] { name, perspective, count }.Concat(sections.FocusInputs).Concat(trailing)
            .Where(c => c.Visible && c.Enabled).ToList();
        int at = order.IndexOf(input);
        if (at < 0) return;
        int next = at + step;
        Control target = next >= order.Count ? afterLast : order[Math.Max(0, next)];
        if (!target.CanFocus) return;
        target.Focus();
        if (target is NumericUpDown number) number.Select(0, number.Text.Length);
        for (Control? parent = target.Parent; parent is not null; parent = parent.Parent)
            if (parent is ScrollableControl { AutoScroll: true } scroll) scroll.ScrollControlIntoView(target);
    }
    public void Dispose()
    {
        foreach (var number in sections.AllInputs.Concat(trailing).Prepend(count)) number.NavigateInput = null;
        name.KeyDown -= CommitText; perspective.KeyDown -= CommitText;
    }
}
