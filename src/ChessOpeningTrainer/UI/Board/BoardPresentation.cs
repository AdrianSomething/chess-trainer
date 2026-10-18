namespace ChessOpeningTrainer;
/// <summary>
/// Shared adapter for read-only board previews. Game/session controllers keep ownership
/// of legal moves and history; a future frontend only needs to replace this UI adapter.
/// </summary>
public static class BoardPresentation
{
    public static void Preview(BoardControl board, Position position, bool flipped)
    {
        board.Position = position;
        board.Flip = flipped;
        board.Invalidate();
    }

    public static void ClearSelection(BoardControl board)
    {
        board.SelectedSquare = null;
        board.CandidateMoves = [];
        board.Invalidate();
    }
}
