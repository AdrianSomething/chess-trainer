namespace ChessOpeningTrainer;

/// <summary>Lossless, order-preserving RLE of the exact 35-byte position key.
/// Zero runs use (0, 255 - length): longer runs sort before shorter runs,
/// just as in the expanded key. Thus SQLite can stream its primary-key order
/// into the final prefix codec without a second full-dataset sort.</summary>
internal static class DatasetPositionKey
{
    internal const string Pieces = "\0PNBRQKpnbrqk";
    public static byte[] Encode(Position position)
    {
        byte[] board = Legacy(position);
        using var output = new MemoryStream(40);
        for (int i = 0; i < board.Length; i++)
        {
            if (board[i] != 0)
            {
                output.WriteByte(board[i]);
                continue;
            }
            int length = 1;
            while (i + length < board.Length && board[i + length] == 0)
                length++;
            output.WriteByte(0);
            output.WriteByte((byte)(255 - length));
            i += length - 1;
        }
        return output.ToArray();
    }
    public static Position Decode(byte[] key)
    {
        byte[] board = Expand(key);
        var position = new Position { WhiteToMove = board[32] != 0, Castling = new string("KQkq".Where((_, i) => (board[33] & (1 << i)) != 0).ToArray()), EnPassant = board[34] == 0 ? null : board[34] - 1 };
        for (int i = 0; i < 32; i++)
        {
            if ((board[i] & 15) >= Pieces.Length || (board[i] >> 4) >= Pieces.Length)
                throw new InvalidDataException("Invalid piece code.");
            position.Squares[2 * i] = Pieces[board[i] & 15];
            position.Squares[2 * i + 1] = Pieces[board[i] >> 4];
        }
        return position;
    }
    public static byte[] Expand(byte[] key)
    {
        var board = new byte[35];
        int offset = 0;
        for (int i = 0; i < key.Length; i++)
        {
            if (offset >= board.Length)
                throw new InvalidDataException("Position key overrun.");
            if (key[i] != 0)
            {
                board[offset++] = key[i];
                continue;
            }
            if (++i >= key.Length)
                throw new InvalidDataException("Incomplete zero run.");
            int length = 255 - key[i];
            if (length == 0 || offset + length > board.Length)
                throw new InvalidDataException("Invalid zero run.");
            offset += length;
        }
        if (offset != board.Length)
            throw new InvalidDataException("Incomplete position key.");
        return board;
    }
    public static byte[] Legacy(Position position)
    {
        var key = new byte[35];
        for (int i = 0; i < 64; i++)
            key[i / 2] |= (byte)(Math.Max(0, Pieces.IndexOf(position.Squares[i])) << ((i % 2) * 4));
        key[32] = (byte)(position.WhiteToMove ? 1 : 0);
        foreach (char c in position.Castling)
        {
            int bit = "KQkq".IndexOf(c);
            if (bit >= 0)
                key[33] |= (byte)(1 << bit);
        }
        key[34] = (byte)(position.EnPassant is { } ep && MoveGenerator.GenerateLegal(position).Any(m => m.IsEnPassant) ? ep + 1 : 0);
        return key;
    }
}
