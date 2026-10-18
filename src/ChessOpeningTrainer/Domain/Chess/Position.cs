using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChessOpeningTrainer;
public sealed class Position
{
    public const string InitialFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    public char[] Squares { get; } = new char[64];
    public List<char> Captures { get; } = [];
    public bool WhiteToMove { get; set; } = true;
    public string Castling { get; set; } = "KQkq";
    public int? EnPassant { get; set; }
    public int Halfmove { get; set; }
    public int Fullmove { get; set; } = 1;

    public Position Clone()
    {
        var p = new Position
        {
            WhiteToMove = WhiteToMove,
            Castling = Castling,
            EnPassant = EnPassant,
            Halfmove = Halfmove,
            Fullmove = Fullmove
        };
        Array.Copy(Squares, p.Squares, 64);
        p.Captures.AddRange(Captures);
        return p;
    }

    public static Position FromFen(string fen)
    {
        var sections = fen.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (sections.Length != 6)
            throw new InvalidDataException("FEN muss sechs Felder haben.");
        var p = new Position
        {
            WhiteToMove = sections[1] == "w",
            Castling = sections[2] == "-" ? "" : sections[2],
            Halfmove = int.Parse(sections[4]),
            Fullmove = int.Parse(sections[5])
        };
        var ranks = sections[0].Split('/');
        if (ranks.Length != 8)
            throw new InvalidDataException("Ungültige FEN-Brettdaten.");
        for (var displayRank = 0; displayRank < 8; displayRank++)
        {
            var file = 0;
            foreach (var c in ranks[displayRank])
            {
                if (char.IsDigit(c))
                {
                    file += c - '0';
                    continue;
                }

                if (!"prnbqkPRNBQK".Contains(c) || file > 7)
                    throw new InvalidDataException("Ungültige FEN-Figur.");
                p.Squares[Square.At(file++, 7 - displayRank)] = c;
            }

            if (file != 8)
                throw new InvalidDataException("Jede FEN-Reihe braucht acht Felder.");
        }

        if (sections[3] != "-")
        {
            if (!Square.TryParse(sections[3], out var ep))
                throw new InvalidDataException("Ungültiges en-passant-Feld in FEN.");
            p.EnPassant = ep;
        }

        if (p.Squares.Count(x => x == 'K') != 1 || p.Squares.Count(x => x == 'k') != 1)
            throw new InvalidDataException("FEN benötigt genau zwei Könige.");
        return p;
    }

    public string ToFen()
    {
        var rankStrings = new List<string>();
        for (var rank = 7; rank >= 0; rank--)
        {
            var empty = 0;
            var sb = new StringBuilder();
            for (var file = 0; file < 8; file++)
            {
                var piece = Squares[Square.At(file, rank)];
                if (piece == '\0')
                {
                    empty++;
                    continue;
                }

                if (empty > 0)
                {
                    sb.Append(empty);
                    empty = 0;
                }

                sb.Append(piece);
            }

            if (empty > 0)
                sb.Append(empty);
            rankStrings.Add(sb.ToString());
        }

        return string.Join('/', rankStrings) + $" {(WhiteToMove ? "w" : "b")} {(string.IsNullOrEmpty(Castling) ? "-" : Castling)} {(EnPassant is int ep ? Square.Name(ep) : "-")} {Halfmove} {Fullmove}";
    }

    public string RepetitionKey() => string.Join(' ', ToFen().Split(' ')[..4]);
    public void Apply(Move move)
    {
        var piece = Squares[move.From];
        var captured = Squares[move.To];
        var whitePiece = char.IsUpper(piece);
        var isPawn = char.ToLowerInvariant(piece) == 'p';
        var isKing = char.ToLowerInvariant(piece) == 'k';
        var isEnPassant = isPawn && move.To == EnPassant && captured == '\0' && move.From % 8 != move.To % 8;
        Squares[move.From] = '\0';
        if (isEnPassant)
        {
            var captureSquare = move.To + (whitePiece ? -8 : 8);
            captured = Squares[captureSquare];
            Squares[captureSquare] = '\0';
        }

        if (captured != '\0')
            Captures.Add(captured);
        Squares[move.To] = piece;
        if (isKing && Math.Abs(move.To - move.From) == 2)
        {
            var rookFrom = move.To > move.From ? move.From + 3 : move.From - 4;
            var rookTo = move.To > move.From ? move.From + 1 : move.From - 1;
            Squares[rookTo] = Squares[rookFrom];
            Squares[rookFrom] = '\0';
        }

        if (isPawn && (move.To / 8 == 7 || move.To / 8 == 0))
        {
            var promotion = move.Promotion == '\0' ? 'q' : char.ToLowerInvariant(move.Promotion);
            Squares[move.To] = whitePiece ? char.ToUpperInvariant(promotion) : promotion;
        }

        Castling = RemoveCastling(Castling, move.From, move.To, piece, captured);
        EnPassant = isPawn && Math.Abs(move.To - move.From) == 16 ? (move.To + move.From) / 2 : null;
        Halfmove = isPawn || captured != '\0' ? 0 : Halfmove + 1;
        if (!WhiteToMove)
            Fullmove++;
        WhiteToMove = !WhiteToMove;
    }

    private static string RemoveCastling(string castling, int from, int to, char piece, char captured)
    {
        var c = castling;
        if (piece == 'K')
            c = c.Replace("K", "").Replace("Q", "");
        if (piece == 'k')
            c = c.Replace("k", "").Replace("q", "");
        if (from == 0 || to == 0 && captured == 'R')
            c = c.Replace("Q", "");
        if (from == 7 || to == 7 && captured == 'R')
            c = c.Replace("K", "");
        if (from == 56 || to == 56 && captured == 'r')
            c = c.Replace("q", "");
        if (from == 63 || to == 63 && captured == 'r')
            c = c.Replace("k", "");
        return c;
    }
}
