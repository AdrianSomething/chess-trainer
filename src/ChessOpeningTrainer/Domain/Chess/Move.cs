using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChessOpeningTrainer;
public sealed class Move : IEquatable<Move>
{
    public int From { get; init; }
    public int To { get; init; }
    public char Promotion { get; init; }
    public bool IsEnPassant { get; init; }
    public bool IsCastle { get; init; }
    public string Uci => Square.Name(From) + Square.Name(To) + (Promotion == '\0' ? "" : char.ToLowerInvariant(Promotion));

    public bool Equals(Move? other) => other is not null && From == other.From && To == other.To && char.ToLowerInvariant(Promotion) == char.ToLowerInvariant(other.Promotion);
    public override bool Equals(object? obj) => Equals(obj as Move);
    public override int GetHashCode() => HashCode.Combine(From, To, char.ToLowerInvariant(Promotion));
    public static bool TryParseUci(string? uci, out Move move)
    {
        move = new Move();
        if (string.IsNullOrWhiteSpace(uci) || (uci.Length != 4 && uci.Length != 5))
            return false;
        if (!Square.TryParse(uci[..2], out var from) || !Square.TryParse(uci.Substring(2, 2), out var to))
            return false;
        var promotion = '\0';
        if (uci.Length == 5)
        {
            promotion = char.ToLowerInvariant(uci[4]);
            if (!"qrbn".Contains(promotion))
                return false;
        }

        move = new Move
        {
            From = from,
            To = to,
            Promotion = promotion
        };
        return true;
    }
}
