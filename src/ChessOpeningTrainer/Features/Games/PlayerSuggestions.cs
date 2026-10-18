namespace ChessOpeningTrainer;

/// <summary>A palette-aware completion list that never takes focus away from typing.</summary>
internal sealed class PlayerSuggestions : LocalizedForm
{
    private readonly CompletionList list = new()
    {
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.None,
        IntegralHeight = false,
        DrawMode = DrawMode.OwnerDrawFixed,
        ItemHeight = 24,
        BackColor = UiPalette.FromArgb(43, 49, 55),
        ForeColor = UiPalette.Gainsboro
    };
    public int RowHeight => list.ItemHeight;
    public string? SelectedName => list.SelectedItem?.ToString();
    protected override bool ShowWithoutActivation => true;
    protected override CreateParams CreateParams
    {
        get { var value = base.CreateParams; value.ExStyle |= 0x08000000; return value; }
    }
    protected override void WndProc(ref Message message)
    {
        if (message.Msg == 0x21) { message.Result = (IntPtr)3; return; }
        base.WndProc(ref message);
    }
    public PlayerSuggestions(Action<string> commit)
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        Padding = new(1);
        BackColor = UiPalette.FromArgb(83, 91, 99);
        Controls.Add(list);
        list.DrawItem += (_, e) =>
        {
            if (e.Index < 0) return;
            using var fill = new SolidBrush((e.State & DrawItemState.Selected) != 0
                ? UiPalette.FromArgb(57, 91, 119) : UiPalette.FromArgb(43, 49, 55));
            e.Graphics.FillRectangle(fill, e.Bounds);
            TextRenderer.DrawText(e.Graphics, list.Items[e.Index].ToString(), list.Font, e.Bounds,
                UiPalette.Gainsboro, TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        };
        list.MouseDown += (_, e) =>
        {
            int index = list.IndexFromPoint(e.Location);
            if (e.Button == MouseButtons.Left && index >= 0) commit(list.Items[index].ToString()!);
        };
    }
    public void SetMatches(string[] names)
    {
        list.BeginUpdate();
        list.Items.Clear();
        list.Items.AddRange(names);
        if (names.Length > 0) list.SelectedIndex = 0;
        list.EndUpdate();
    }
    public void MoveSelection(int direction)
    {
        if (list.Items.Count > 0)
            list.SelectedIndex = (list.SelectedIndex + direction + list.Items.Count) % list.Items.Count;
    }

    private sealed class CompletionList : ListBox
    {
        public CompletionList() => SetStyle(ControlStyles.Selectable, false);
        protected override void WndProc(ref Message message)
        {
            if (message.Msg == 0x201)
            {
                // Native ListBox clicks normally SetFocus before MouseDown. A
                // completion choice must not first discard the combo's draft.
                int x = unchecked((short)(long)message.LParam);
                int y = unchecked((short)((long)message.LParam >> 16));
                OnMouseDown(new(MouseButtons.Left, 1, x, y, 0));
                message.Result = IntPtr.Zero;
                return;
            }
            base.WndProc(ref message);
        }
    }
}
