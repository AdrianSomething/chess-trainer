using System.Text;

namespace ChessOpeningTrainer;

/// <summary>Compact internal record; no names, URLs, comments or engine annotations.</summary>
public static class DatasetGameCodec
{
    public static byte[] Encode(DatasetGame game)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
        writer.Write((byte)1);
        writer.Write(game.StartFen == Position.InitialFen);
        if (game.StartFen != Position.InitialFen)
            writer.Write(game.StartFen);
        writer.Write(game.Result);
        writer.Write(game.WhiteElo);
        writer.Write(game.BlackElo);
        writer.Write(game.Seconds);
        writer.Write(game.Increment);
        writer.Write(game.Termination);
        writer.Write(game.Moves.Length);
        foreach (string uci in game.Moves)
        {
            writer.Write(PackedMove.Encode(uci));
        }
        writer.Flush();
        return stream.ToArray();
    }
    public static DatasetGame Decode(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var reader = new BinaryReader(stream, Encoding.UTF8);
        if (reader.ReadByte() != 1)
            throw new InvalidDataException("Unsupported dataset record.");
        string fen = reader.ReadBoolean() ? Position.InitialFen : reader.ReadString();
        string result = reader.ReadString();
        int white = reader.ReadInt32(), black = reader.ReadInt32(), seconds = reader.ReadInt32(), increment = reader.ReadInt32();
        string ending = reader.ReadString();
        int count = reader.ReadInt32();
        if (count < 0 || count > (stream.Length - stream.Position) / 2)
            throw new InvalidDataException("Invalid move count.");
        var moves = new string[count];
        for (int i = 0; i < count; i++)
            moves[i] = PackedMove.Decode(reader.ReadUInt16());
        return new(fen, moves, result, white, black, seconds, increment, ending);
    }
}
