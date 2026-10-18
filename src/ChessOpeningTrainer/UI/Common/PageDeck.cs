namespace ChessOpeningTrainer;

/// <summary>TabPage ownership/selection without native tab headers; navigation lives in feature UI.</summary>
public sealed class PageDeck : TabControl
{
    public PageDeck()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }
    public override Rectangle DisplayRectangle => ClientRectangle;
    protected override void OnPaint(PaintEventArgs e) => e.Graphics.Clear(BackColor);
}
