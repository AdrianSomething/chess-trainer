using System.Drawing.Drawing2D;
using ChessMove = ChessOpeningTrainer.Move;

namespace ChessOpeningTrainer;
public partial class BoardControl
{
    protected virtual bool UnrestrictedDrag => false;
    protected override void OnMouseDown(MouseEventArgs e)
    {
        var square = PointToSquare(e.Location);
        if (e.Button == MouseButtons.Left)
        {
            MarkedSquares.Clear();
            Arrows.Clear();
            PreviewArrow = null;
            _leftStart = square;
            _pointerStart = _pointer = e.Location;
            _dragging = false;
            Capture = true;
            Invalidate();
        }

        if (e.Button == MouseButtons.Right)
        {
            _rightStart = square;
            _pointerStart = e.Location;
            _dragging = false;
            Capture = true;
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (Math.Abs(e.X - _pointerStart.X) + Math.Abs(e.Y - _pointerStart.Y) > 7)
            _dragging = true;
        _pointer = e.Location;
        if (e.Button == MouseButtons.Left)
        {
            if (_dragging && InputEnabled && _leftStart is int fromSquare && Position is not null)
                CandidateMoves = UnrestrictedDrag ? [] : PremoveSide is bool side ? Premoves.Candidates(Position, fromSquare, side) : (MoveProvider?.Invoke(Position) ?? MoveGenerator.GenerateLegal(Position)).Where(m => m.From == fromSquare).ToList();
            Invalidate();
        }

        var current = PointToSquare(e.Location);
        if (e.Button == MouseButtons.Right && _rightStart is int from && current is int to && from != to)
        {
            PreviewArrow = new BoardArrow(from, to);
            Invalidate();
        }

        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        var square = PointToSquare(e.Location);
        if (e.Button == MouseButtons.Left)
        {
            if (_leftStart is int from && square is int to)
            {
                if (_dragging && from != to)
                    SquareDragged?.Invoke(from, to);
                else
                    SquareClicked?.Invoke(to);
            }
            else
                BlankClicked?.Invoke();
            _leftStart = null;
            _dragging = false;
            Capture = false;
            Invalidate();
        }

        if (e.Button == MouseButtons.Right)
        {
            if (_rightStart is int from && square is int to)
            {
                if (_dragging && from != to)
                {
                    var arrow = new BoardArrow(from, to);
                    var existing = Arrows.FindIndex(x => x == arrow);
                    if (existing >= 0)
                        Arrows.RemoveAt(existing);
                    else
                        Arrows.Add(arrow);
                }
                else if (!MarkedSquares.Add(to))
                    MarkedSquares.Remove(to);
            }

            _rightStart = null;
            PreviewArrow = null;
            Capture = false;
            Invalidate();
        }

        base.OnMouseUp(e);
    }

    private int VisualToSquare(int col, int row)
    {
        var file = Flip ? 7 - col : col;
        var rank = Flip ? row : 7 - row;
        return Square.At(file, rank);
    }

    protected int? PointToSquare(Point point)
    {
        var size = BoardSize;
        var cell = size / 8f;
        point.Offset(-(int)BoardLeft, -22);
        if (point.X < 0 || point.Y < 0 || point.X >= size || point.Y >= size)
            return null;
        return VisualToSquare((int)(point.X / cell), (int)(point.Y / cell));
    }

    private PointF SquareCenter(int square, float cell)
    {
        var file = square % 8;
        var rank = square / 8;
        var col = Flip ? 7 - file : file;
        var row = Flip ? rank : 7 - rank;
        return new PointF((col + .5f) * cell, (row + .5f) * cell);
    }
}
