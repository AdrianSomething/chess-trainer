namespace ChessOpeningTrainer;

/// <summary>Comparison only: an en-passant target matters when there really is a legal capture.</summary>
public static class BranchPositionIdentity
{
    public static string Key(Position position)
    {
        string[] fields = position.ToFen().Split(' ');
        if (position.EnPassant is not null && !MoveGenerator.GenerateLegal(position).Any(m => m.IsEnPassant)) fields[3] = "-";
        return string.Join(' ', fields.Take(4));
    }
}
