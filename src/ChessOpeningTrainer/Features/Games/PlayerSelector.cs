namespace ChessOpeningTrainer;

/// <summary>Typing is a draft; only Enter or an explicit list choice commits it.</summary>
public sealed class PlayerSelector : ComboBox
{
    private string previous = "";
    private bool committed, accepting;
    private PlayerSuggestions? suggestions;
    public PlayerSelector()
    {
        DropDownStyle = ComboBoxStyle.DropDown;
        // Windows' autocomplete popup ignores the app palette. Our nonactivating
        // suggestions keep typing in the combo and use the same themed list chrome.
        AutoCompleteMode = AutoCompleteMode.None;
        AutoCompleteSource = AutoCompleteSource.None;
    }
    protected override void OnEnter(EventArgs e) { previous = Text; committed = false; base.OnEnter(e); }
    protected override void OnSelectionChangeCommitted(EventArgs e)
    {
        committed = true; previous = SelectedItem?.ToString() ?? previous;
        suggestions?.Hide();
        base.OnSelectionChangeCommitted(e);
    }
    protected override void OnTextUpdate(EventArgs e)
    {
        base.OnTextUpdate(e);
        if (accepting) return;
        committed = false;
        var matches = Items.Cast<object>().Select(item => item.ToString() ?? "")
            .Where(name => name.Contains(Text, StringComparison.CurrentCultureIgnoreCase)).Take(30).ToArray();
        if (matches.Length == 0) { suggestions?.Hide(); return; }
        suggestions ??= new PlayerSuggestions(Commit);
        suggestions.SetMatches(matches);
        suggestions.Location = PointToScreen(new(0, Height));
        suggestions.Width = Math.Max(Width, 210);
        suggestions.Height = Math.Min(160, matches.Length * suggestions.RowHeight + 2);
        var available = Screen.FromControl(this).WorkingArea;
        suggestions.Left = Math.Clamp(suggestions.Left, available.Left, Math.Max(available.Left, available.Right - suggestions.Width));
        if (suggestions.Bottom > available.Bottom) suggestions.Top = Math.Max(available.Top, PointToScreen(Point.Empty).Y - suggestions.Height);
        if (!suggestions.Visible && FindForm() is Form owner) suggestions.Show(owner);
    }
    private void Commit(string name)
    {
        var match = Items.Cast<object>().FirstOrDefault(item => item.ToString() == name);
        if (match is null) return;
        accepting = true;
        SelectedItem = match;
        Text = name;
        SelectionStart = Text.Length;
        SelectionLength = 0;
        accepting = false;
        OnSelectionChangeCommitted(EventArgs.Empty);
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (suggestions is { Visible: true } && e.KeyCode is Keys.Up or Keys.Down)
        {
            suggestions.MoveSelection(e.KeyCode == Keys.Down ? 1 : -1);
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            suggestions?.Hide();
            accepting = true;
            SelectedItem = Items.Cast<object>().FirstOrDefault(item => item.ToString() == previous);
            Text = previous;
            accepting = false;
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Enter)
        {
            string? name = suggestions is { Visible: true } ? suggestions.SelectedName
                : Items.Cast<object>().FirstOrDefault(x => string.Equals(x.ToString(), Text, StringComparison.CurrentCultureIgnoreCase))?.ToString();
            if (name is not null) Commit(name);
            e.SuppressKeyPress = true;
        }
        base.OnKeyDown(e);
    }
    protected override void OnDropDown(EventArgs e) { suggestions?.Hide(); base.OnDropDown(e); }
    protected override void OnLeave(EventArgs e)
    {
        suggestions?.Hide();
        if (!committed) { SelectedItem = Items.Cast<object>().FirstOrDefault(x => x.ToString() == previous); Text = previous; }
        base.OnLeave(e);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) suggestions?.Dispose();
        base.Dispose(disposing);
    }
}
