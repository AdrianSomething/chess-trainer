namespace ChessOpeningTrainer;
public sealed class PieceToolButton : Button
{
    public char Piece { get; init; }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Piece != '\0')
            BoardControl.DrawPiece(e.Graphics, Piece, new(Width / 2f, Height / 2f), Math.Min(Width, Height) * .95f);
    }
}
