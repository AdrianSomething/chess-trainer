using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChessOpeningTrainer;
public static class MoveGenerator
{
    private static readonly (int File, int Rank)[] Knight = [(1, 2), (2, 1), (2, -1), (1, -2), (-1, -2), (-2, -1), (-2, 1), (-1, 2)];
    private static readonly (int File, int Rank)[] King = [(1, 1), (1, 0), (1, -1), (0, -1), (-1, -1), (-1, 0), (-1, 1), (0, 1)];
    private static readonly (int File, int Rank)[] Bishop = [(1, 1), (1, -1), (-1, -1), (-1, 1)];
    private static readonly (int File, int Rank)[] Rook = [(1, 0), (-1, 0), (0, 1), (0, -1)];
    public static List<Move> GenerateLegal(Position pos)
    {
        var moves = GeneratePseudo(pos);
        return moves.Where(move =>
        {
            var next = pos.Clone();
            next.Apply(move);
            return !IsKingInCheck(next, !next.WhiteToMove);
        }).ToList();
    }

    internal static List<Move> GeneratePseudo(Position pos, bool allowCastling = true)
    {
        var result = new List<Move>();
        for (var from = 0; from < 64; from++)
        {
            var piece = pos.Squares[from];
            if (piece == '\0' || char.IsUpper(piece) != pos.WhiteToMove)
                continue;
            var type = char.ToLowerInvariant(piece);
            var file = from % 8;
            var rank = from / 8;
            switch (type)
            {
                case 'p':
                    Pawn(pos, result, from, file, rank);
                    break;
                case 'n':
                    Jump(pos, result, from, file, rank, Knight);
                    break;
                case 'b':
                    Slide(pos, result, from, file, rank, Bishop);
                    break;
                case 'r':
                    Slide(pos, result, from, file, rank, Rook);
                    break;
                case 'q':
                    Slide(pos, result, from, file, rank, Bishop);
                    Slide(pos, result, from, file, rank, Rook);
                    break;
                case 'k':
                    Jump(pos, result, from, file, rank, King);
                    if (allowCastling) Castle(pos, result, from);
                    break;
            }
        }

        return result;
    }

    private static void Pawn(Position p, List<Move> result, int from, int file, int rank)
    {
        var white = p.WhiteToMove;
        var dir = white ? 1 : -1;
        var start = white ? 1 : 6;
        var promotionRank = white ? 7 : 0;
        var oneRank = rank + dir;
        if (Square.OnBoard(file, oneRank) && p.Squares[Square.At(file, oneRank)] == '\0')
        {
            AddPawn(result, from, Square.At(file, oneRank), promotionRank);
            if (rank == start && p.Squares[Square.At(file, rank + 2 * dir)] == '\0')
                result.Add(new Move { From = from, To = Square.At(file, rank + 2 * dir) });
        }

        foreach (var df in new[]
        {
            -1,
            1
        }

        )
        {
            var targetFile = file + df;
            var targetRank = rank + dir;
            if (!Square.OnBoard(targetFile, targetRank))
                continue;
            var target = Square.At(targetFile, targetRank);
            var targetPiece = p.Squares[target];
            if (targetPiece != '\0' && char.IsUpper(targetPiece) != white)
                AddPawn(result, from, target, promotionRank);
            else if (p.EnPassant == target)
                result.Add(new Move { From = from, To = target, IsEnPassant = true });
        }
    }

    private static void AddPawn(List<Move> result, int from, int to, int promotionRank)
    {
        if (to / 8 == promotionRank)
            foreach (var p in new[]
            {
                'q',
                'r',
                'b',
                'n'
            }

            )
                result.Add(new Move { From = from, To = to, Promotion = p });
        else
            result.Add(new Move { From = from, To = to });
    }

    private static void Jump(Position p, List<Move> result, int from, int file, int rank, IEnumerable<(int File, int Rank)> directions)
    {
        foreach (var(df, dr)in directions)
        {
            var f = file + df;
            var r = rank + dr;
            if (!Square.OnBoard(f, r))
                continue;
            var to = Square.At(f, r);
            var occupant = p.Squares[to];
            if (occupant == '\0' || char.IsUpper(occupant) != p.WhiteToMove)
                result.Add(new Move { From = from, To = to });
        }
    }

    private static void Slide(Position p, List<Move> result, int from, int file, int rank, IEnumerable<(int File, int Rank)> directions)
    {
        foreach (var(df, dr)in directions)
        {
            for (int f = file + df, r = rank + dr; Square.OnBoard(f, r); f += df, r += dr)
            {
                var to = Square.At(f, r);
                var occupant = p.Squares[to];
                if (occupant == '\0')
                {
                    result.Add(new Move { From = from, To = to });
                    continue;
                }

                if (char.IsUpper(occupant) != p.WhiteToMove)
                    result.Add(new Move { From = from, To = to });
                break;
            }
        }
    }

    private static void Castle(Position p, List<Move> result, int from)
    {
        var white = p.WhiteToMove;
        var rank = white ? 0 : 7;
        var kingStart = Square.At(4, rank);
        if (from != kingStart || IsSquareAttacked(p, kingStart, !white))
            return;
        var kingSide = white ? 'K' : 'k';
        var queenSide = white ? 'Q' : 'q';
        if (p.Castling.Contains(kingSide) && p.Squares[Square.At(5, rank)] == '\0' && p.Squares[Square.At(6, rank)] == '\0' && p.Squares[Square.At(7, rank)] == (white ? 'R' : 'r') && !IsSquareAttacked(p, Square.At(5, rank), !white) && !IsSquareAttacked(p, Square.At(6, rank), !white))
            result.Add(new Move { From = from, To = Square.At(6, rank), IsCastle = true });
        if (p.Castling.Contains(queenSide) && p.Squares[Square.At(1, rank)] == '\0' && p.Squares[Square.At(2, rank)] == '\0' && p.Squares[Square.At(3, rank)] == '\0' && p.Squares[Square.At(0, rank)] == (white ? 'R' : 'r') && !IsSquareAttacked(p, Square.At(3, rank), !white) && !IsSquareAttacked(p, Square.At(2, rank), !white))
            result.Add(new Move { From = from, To = Square.At(2, rank), IsCastle = true });
    }

    public static bool IsKingInCheck(Position p, bool whiteKing)
    {
        var king = whiteKing ? 'K' : 'k';
        var square = Array.IndexOf(p.Squares, king);
        return square < 0 || IsSquareAttacked(p, square, !whiteKing);
    }

    public static bool IsSquareAttacked(Position p, int target, bool byWhite)
    {
        var file = target % 8;
        var rank = target / 8;
        // Pawns attack one rank in their travel direction; examine their possible origin squares.
        var pawnRank = rank + (byWhite ? -1 : 1);
        if (pawnRank is >= 0 and < 8)
            foreach (var df in new[]
            {
                -1,
                1
            }

            )
            {
                var f = file + df;
                if (f is >= 0 and < 8 && p.Squares[Square.At(f, pawnRank)] == (byWhite ? 'P' : 'p'))
                    return true;
            }

        foreach (var(df, dr)in Knight)
        {
            var f = file + df;
            var r = rank + dr;
            if (Square.OnBoard(f, r) && p.Squares[Square.At(f, r)] == (byWhite ? 'N' : 'n'))
                return true;
        }

        foreach (var(df, dr)in King)
        {
            var f = file + df;
            var r = rank + dr;
            if (Square.OnBoard(f, r) && p.Squares[Square.At(f, r)] == (byWhite ? 'K' : 'k'))
                return true;
        }

        if (AttackedBySlider(p, file, rank, byWhite, Bishop, "bq"))
            return true;
        return AttackedBySlider(p, file, rank, byWhite, Rook, "rq");
    }

    private static bool AttackedBySlider(Position p, int file, int rank, bool byWhite, IEnumerable<(int File, int Rank)> directions, string types)
    {
        foreach (var(df, dr)in directions)
            for (int f = file + df, r = rank + dr; Square.OnBoard(f, r); f += df, r += dr)
            {
                var piece = p.Squares[Square.At(f, r)];
                if (piece == '\0')
                    continue;
                if (char.IsUpper(piece) == byWhite && types.Contains(char.ToLowerInvariant(piece)))
                    return true;
                break;
            }

        return false;
    }

    public static string GameState(Position p, IReadOnlyDictionary<string, int> repetitions)
    {
        var legal = GenerateLegal(p);
        if (legal.Count == 0)
            return IsKingInCheck(p, p.WhiteToMove) ? "Schachmatt" : "Remis durch Patt";
        if (p.Halfmove >= 100)
            return "Remis: 50-Züge-Regel";
        if (repetitions.TryGetValue(p.RepetitionKey(), out var count) && count >= 3)
            return "Remis: dreifache Stellungswiederholung";
        if (InsufficientMaterial(p))
            return "Remis: unzureichendes Material";
        return IsKingInCheck(p, p.WhiteToMove) ? "Schach" : "";
    }

    private static bool InsufficientMaterial(Position p)
    {
        var pieces = p.Squares.Where(x => x != '\0' && char.ToLowerInvariant(x) != 'k').ToList();
        if (pieces.Count == 0)
            return true;
        if (pieces.Count == 1 && "bn".Contains(char.ToLowerInvariant(pieces[0])))
            return true;
        if (pieces.All(x => char.ToLowerInvariant(x) == 'b'))
        {
            var colors = p.Squares.Select((piece, square) => (piece, square)).Where(x => char.ToLowerInvariant(x.piece) == 'b').Select(x => ((x.square % 8) + x.square / 8) % 2).Distinct().Count();
            return colors == 1;
        }

        return false;
    }
}
