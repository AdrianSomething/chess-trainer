namespace ChessOpeningTrainer;
public static class MaterialDisplay
{
    public static int Value(char piece) => char.ToLowerInvariant(piece) switch
    {
        'p' => 1,
        'n' or 'b' => 3,
        'r' => 5,
        'q' => 9,
        _ => 0
    };
    public static int Balance(Position position, bool white) => position.Squares.Sum(c => char.IsUpper(c) == white ? Value(c) : -Value(c));
    // FEN has no capture provenance. Surplus promoted pieces offset missing pawns.
    public static Dictionary<char, int> Missing(Position position, bool white)
    {
        var expected = new Dictionary<char, int>
        {
            {
                'q',
                1
            },
            {
                'r',
                2
            },
            {
                'b',
                2
            },
            {
                'n',
                2
            },
            {
                'p',
                8
            }
        };
        var counts = position.Squares.Where(c => c != '\0' && char.IsUpper(c) == white).GroupBy(char.ToLowerInvariant).ToDictionary(g => g.Key, g => g.Count());
        int promoted = expected.Where(k => k.Key != 'p').Sum(k => Math.Max(0, counts.GetValueOrDefault(k.Key) - k.Value));
        return expected.ToDictionary(k => white ? char.ToUpperInvariant(k.Key) : k.Key, k => Math.Max(0, k.Value - counts.GetValueOrDefault(k.Key) - (k.Key == 'p' ? promoted : 0)));
    }
}
