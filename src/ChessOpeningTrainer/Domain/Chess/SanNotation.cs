using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChessOpeningTrainer;
public static class SanNotation
{
    public static string Format(Position before, Move move)
    {
        var piece = before.Squares[move.From];
        var type = char.ToLowerInvariant(piece);
        if (type == 'k' && Math.Abs(move.To - move.From) == 2)
            return AddSuffix(before, move, move.To > move.From ? "O-O" : "O-O-O");
        var capture = before.Squares[move.To] != '\0' || (type == 'p' && move.To == before.EnPassant && move.From % 8 != move.To % 8);
        var label = type == 'p' ? "" : char.ToUpperInvariant(type).ToString();
        if (type != 'p')
        {
            var alternatives = MoveGenerator.GenerateLegal(before).Where(x => x.To == move.To && x.From != move.From && char.ToLowerInvariant(before.Squares[x.From]) == type).ToList();
            if (alternatives.Count > 0)
            {
                var sameFile = alternatives.Any(x => x.From % 8 == move.From % 8);
                var sameRank = alternatives.Any(x => x.From / 8 == move.From / 8);
                label += !sameFile ? ((char)('a' + move.From % 8)).ToString() : !sameRank ? (move.From / 8 + 1).ToString() : Square.Name(move.From);
            }
        }
        else if (capture)
            label = ((char)('a' + move.From % 8)).ToString();
        label += capture ? "x" : "";
        label += Square.Name(move.To);
        if (move.Promotion != '\0')
            label += "=" + char.ToUpperInvariant(move.Promotion);
        return AddSuffix(before, move, label);
    }

    private static string AddSuffix(Position before, Move move, string text)
    {
        var after = before.Clone();
        after.Apply(move);
        if (!MoveGenerator.IsKingInCheck(after, after.WhiteToMove))
            return text;
        return MoveGenerator.GenerateLegal(after).Count == 0 ? text + "#" : text + "+";
    }
}
