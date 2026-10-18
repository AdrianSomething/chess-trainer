namespace ChessOpeningTrainer;
public sealed class ScrollNumber : NumericUpDown
{
    /// <summary>Optional fallback used when the edited text is empty at commit.</summary>
    public decimal? EmptyDefaultValue { get; set; }
    /// <summary>Opt-in Enter callback, after committing. Enter is consumed without a beep.</summary>
    public Action<ScrollNumber>? EnterCommitted { get; set; }
    /// <summary>Opt-in previous/next focus navigation. Ordinary numerics keep arrow stepping.</summary>
    public Action<ScrollNumber, int>? NavigateInput { get; set; }

    public void CommitEditedValue()
    {
        if (string.IsNullOrWhiteSpace(Text))
        {
            Value = Math.Clamp(EmptyDefaultValue ?? Value, Minimum, Maximum);
            UpdateEditText();
            // NumericUpDown intentionally leaves an empty edit unchanged when
            // its value did not change. Restore that case explicitly, using the
            // same numeric format rather than artificially toggling Value/events.
            if (string.IsNullOrWhiteSpace(Text))
            {
                UserEdit = false;
                ChangingText = true;
                Text = Hexadecimal ? ((long)Value).ToString("X", System.Globalization.CultureInfo.InvariantCulture)
                    : Value.ToString((ThousandsSeparator ? "N" : "F") + DecimalPlaces, System.Globalization.CultureInfo.CurrentCulture);
            }
        }
        else ValidateEditText();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Enter && (EnterCommitted is not null || NavigateInput is not null || EmptyDefaultValue is not null))
        {
            CommitEditedValue();
            if (EnterCommitted is not null) EnterCommitted(this);
            else NavigateInput?.Invoke(this, 1);
            return true;
        }
        if (NavigateInput is not null && keyData is Keys.Up or Keys.Down)
        {
            CommitEditedValue();
            NavigateInput(this, keyData == Keys.Up ? -1 : 1);
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if (e is HandledMouseEventArgs handled)
            handled.Handled = true;
        for (Control? p = Parent; p is not null; p = p.Parent)
            if (p is ScrollableControl sc && sc.AutoScroll && sc.VerticalScroll.Visible)
            {
                sc.AutoScrollPosition = new(0, Math.Max(0, -sc.AutoScrollPosition.Y - e.Delta));
                break;
            }
    }
}
