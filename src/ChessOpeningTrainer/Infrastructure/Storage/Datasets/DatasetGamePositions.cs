namespace ChessOpeningTrainer;

/// <summary>Legal, exact positions before each move, plus the final position.</summary>
internal sealed record DatasetGamePositions(string Result, List<(byte[] Key, ushort? Move)> Items)
{
    internal static DatasetGamePositions Project(DatasetGame game)
    {
        var p = Position.FromFen(game.StartFen);
        var items = new List<(byte[], ushort?)>();
        foreach (string uci in game.Moves)
        {
            items.Add((DatasetPositionKey.Encode(p), PackedMove.Encode(uci)));
            var move = MoveGenerator.GenerateLegal(p).FirstOrDefault(m => m.Uci == uci) ?? throw new InvalidDataException("Invalid stored move.");
            p.Apply(move);
        }
        items.Add((DatasetPositionKey.Encode(p), null));
        return new(game.Result, items);
    }
}
