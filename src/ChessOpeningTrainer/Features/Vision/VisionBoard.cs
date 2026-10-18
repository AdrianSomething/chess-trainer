namespace ChessOpeningTrainer;

public sealed class VisionBoard : BoardControl
{
    public bool ArrowMode { get; set; }
    public bool Answering { get; set; }
    public event Action<int>? MarkPiece;
    public event Action<BoardArrow>? MarkAttack;
    private int? from;
    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (!Answering || e.Button is not (MouseButtons.Left or MouseButtons.Right)) return;
        from = PointToSquare(e.Location); Capture = true;
    }
    protected override void OnMouseMove(MouseEventArgs e) { }
    protected override void OnMouseUp(MouseEventArgs e)
    {
        Capture = false;
        if (!Answering) return;
        int? to = PointToSquare(e.Location);
        if (from is int a && to is int b && Position is not null)
        {
            if (!ArrowMode && a == b && Position.Squares[b] != '\0') MarkPiece?.Invoke(b);
            else if (ArrowMode && a != b && Position.Squares[a] != '\0' && Position.Squares[b] != '\0')
                MarkAttack?.Invoke(new(a, b));
        }
        from = null; Invalidate();
    }
}
