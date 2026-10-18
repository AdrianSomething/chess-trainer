namespace ChessOpeningTrainer;

public sealed record VisionSafetyExercise(Position Position, int Origin, HashSet<int> Destinations);
public static class VisionSafety
{
    public static VisionSafetyExercise Generate(bool white, CancellationToken token)
    {
        while (true)
        {
            var p = VisionRules.Generate(2, token, white).Position;
            foreach (int square in VisionRules.EnemyAttacks(p, p.WhiteToMove).Select(a => a.To).Distinct().OrderBy(_ => Random.Shared.Next()))
            {
                if (char.ToLowerInvariant(p.Squares[square]) == 'k') continue;
                var destinations = new HashSet<int>();
                var legal = MoveGenerator.GenerateLegal(p).Where(m => m.From == square).ToList();
                foreach (var move in legal)
                {
                    var next = p.Clone(); next.Apply(move);
                    if (!Enumerable.Range(0, 64).Any(i => next.Squares[i] != '\0' && char.IsUpper(next.Squares[i]) != p.WhiteToMove && VisionRules.Attacks(next, i, move.To))) destinations.Add(move.To);
                }
                if (destinations.Count > 0 && legal.Select(m => m.To).Distinct().Count() > destinations.Count)
                    return new(p, square, destinations);
            }
        }
    }
}
