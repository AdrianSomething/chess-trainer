namespace ChessOpeningTrainer;

/// <summary>A compact palette-based list; repaint only the changed hover rows.</summary>
internal sealed class BranchGameList : ListBox
{
    private int hovered = -1;
    public BranchGameList()
    {
        DrawMode = DrawMode.OwnerDrawFixed; ItemHeight = 30;
        BackColor = UiPalette.FromArgb(43, 49, 55); ForeColor = UiPalette.Gainsboro;
        BorderStyle = BorderStyle.FixedSingle;
    }
    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= Items.Count) return;
        bool selected = (e.State & DrawItemState.Selected) != 0;
        using var background = new SolidBrush(selected ? UiPalette.FromArgb(69, 77, 83)
            : e.Index == hovered ? UiPalette.FromArgb(54, 61, 67) : BackColor);
        e.Graphics.FillRectangle(background, e.Bounds);
        TextRenderer.DrawText(e.Graphics, Items[e.Index].ToString(), Font,
            new Rectangle(e.Bounds.X + 6, e.Bounds.Y, Math.Max(0, e.Bounds.Width - 12), e.Bounds.Height), ForeColor,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
    }
    protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); SetHover(IndexFromPoint(e.Location)); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); SetHover(-1); }
    protected override void WndProc(ref Message m)
    {
        int message = m.Msg; base.WndProc(ref m);
        if (message is 0x115 or 0x20a)
        {
            Point p = PointToClient(Cursor.Position);
            SetHover(ClientRectangle.Contains(p) ? IndexFromPoint(p) : -1);
        }
    }
    private void SetHover(int row)
    {
        if (row == hovered) return;
        int old = hovered; hovered = row;
        if (old >= 0 && old < Items.Count) Invalidate(GetItemRectangle(old));
        if (row >= 0 && row < Items.Count) Invalidate(GetItemRectangle(row));
    }
}
