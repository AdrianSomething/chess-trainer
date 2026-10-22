namespace ChessOpeningTrainer;

/// <summary>15 bits: source square (6), target square (6), promotion (3).
/// Castling and en passant are interpreted from the position, not stored as text.</summary>
public static class PackedMove
{
    public static ushort Encode(string uci)
    {
        if (!Move.TryParseUci(uci, out var move))
            throw new InvalidDataException("Invalid UCI move.");
        int promotion = "\0qrbn".IndexOf(char.ToLowerInvariant(move.Promotion));
        return (ushort)(move.From | move.To << 6 | promotion << 12);
    }

    public static string Decode(ushort value)
    {
        int promotion = value >> 12;
        if (promotion > 4)
            throw new InvalidDataException("Invalid packed promotion.");
        return Square.Name(value & 63) + Square.Name((value >> 6) & 63) +
            (promotion == 0 ? "" : "qrbn"[promotion - 1].ToString());
    }
}
