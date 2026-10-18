namespace ChessOpeningTrainer;

public static class VisionMemory
{
    public static Position Generate(int difficulty, bool white, CancellationToken token)
    {
        var (min, max) = difficulty switch { 0 => (4, 8), 2 => (17, 32), _ => (9, 16) };
        while (true)
        {
            token.ThrowIfCancellationRequested();
            var p = VisionRules.Generate(0, token, white).Position.Clone();
            int target = Random.Shared.Next(min, max + 1);
            if (p.Squares.Count(c => c != '\0') < target) continue;
            var removable = Enumerable.Range(0, 64).Where(s => p.Squares[s] != '\0' && char.ToLowerInvariant(p.Squares[s]) != 'k').OrderBy(_ => Random.Shared.Next()).ToArray();
            int remove = p.Squares.Count(c => c != '\0') - target;
            foreach (int square in removable.Take(remove)) p.Squares[square] = '\0';
            p.Castling = ""; p.EnPassant = null;
            if (MoveGenerator.IsKingInCheck(p, !white)) continue;
            return p;
        }
    }
}
