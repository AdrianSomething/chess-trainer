using ChessMove = ChessOpeningTrainer.Move;

namespace ChessOpeningTrainer;

public partial class BoardControl
{
    /// <summary>Undo a recorded move visually; legality comes from its original pre-move position.</summary>
    public Task AnimateReverseAsync(Position afterMove, ChessMove originalMove, Position beforeMove, int milliseconds = 125)
    {
        List<(char Piece, int From, int To)> pieces =
            [(afterMove.Squares[originalMove.To], originalMove.To, originalMove.From)];
        if (originalMove.IsCastle)
        {
            int rookFrom = originalMove.To > originalMove.From ? originalMove.From + 3 : originalMove.From - 4;
            int rookTo = originalMove.To > originalMove.From ? originalMove.From + 1 : originalMove.From - 1;
            pieces.Add((afterMove.Squares[rookTo], rookTo, rookFrom));
        }

        // Restoring the full pre-move board also restores a captured piece,
        // en-passant pawn, promotion pawn and castling/en-passant rights.
        return AnimatePiecesAsync(beforeMove, pieces, milliseconds);
    }
}
