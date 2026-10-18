namespace ChessOpeningTrainer;

public sealed class PlacementBoard : BoardControl
{
    protected override bool UnrestrictedDrag => true;
    public event Action<int>? Place;
    public event Action<int, int>? Relocate;

    public PlacementBoard()
    {
        SquareClicked += square => Place?.Invoke(square);
        SquareDragged += (from, to) => Relocate?.Invoke(from, to);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left) base.OnMouseDown(e);
        else if (e.Button == MouseButtons.Right && PointToSquare(e.Location) is int square && Position is not null)
        {
            Position.Squares[square] = '\0';
            Invalidate();
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right) base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left) base.OnMouseUp(e);
    }
}
