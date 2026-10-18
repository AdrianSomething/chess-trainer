namespace ChessOpeningTrainer;

/// <summary>One shared card style for the puzzle and chess-vision mode selectors.</summary>
public class ModeMenuButton : MenuIconButton
{
    public const int PreferredWidth = 460, PreferredHeight = 78;
    private string detail = "";
    private bool hovered;
    public string Detail { get => detail; set { detail = value; Invalidate(); } }
    public ModeMenuButton(string icon, string title, string description = "") : base(icon, title)
    {
        Detail = description; Width = PreferredWidth; Height = PreferredHeight;
        UseMnemonic = false; Padding = System.Windows.Forms.Padding.Empty;
        ForeColor = UiPalette.Gainsboro;
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(hovered ? UiPalette.FromArgb(58, 66, 74) : BackColor);
        using var frame = new Pen(Focused ? UiPalette.FromArgb(94, 111, 127) : UiPalette.FromArgb(58, 66, 74));
        e.Graphics.DrawRectangle(frame, 0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
        using var title = new Font(Font.FontFamily, 15, FontStyle.Bold);
        using var secondary = new Font(Font.FontFamily, 10);
        const TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix;
        TextRenderer.DrawText(e.Graphics, Text, title, new Rectangle(75, 12, Width - 85, 30), ForeColor, flags);
        TextRenderer.DrawText(e.Graphics, L.T(detail), secondary, new Rectangle(75, 44, Width - 85, 23), UiPalette.FromArgb(155, 163, 171), flags);
        DrawCardIcon(e.Graphics, new(15, 16, 50, 46));
    }
    protected virtual void DrawCardIcon(Graphics graphics, Rectangle bounds) =>
        PaintNavigationIcon(graphics, new(bounds.Left + (bounds.Width - 36) / 2, bounds.Top + (bounds.Height - 36) / 2, 36, 36));
    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hovered = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hovered = false; Invalidate(); }
}
