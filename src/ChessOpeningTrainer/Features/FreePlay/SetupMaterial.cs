namespace ChessOpeningTrainer;

// Stockfish rejects material requiring more than eight original pawns per side.
// Custom two-sided play may still explore such artificial setups.
public static class SetupMaterial
{
    public static string? EngineWarning(Position position)
    {
        if (position.Squares.Count(c => c == 'K') != 1 || position.Squares.Count(c => c == 'k') != 1)
            return "Mehrkönig-Spiel: Stockfish unterstützt diese Stellung nicht. Engine und Bewertung sind ausgeschaltet. Könige werden geschlagen; es gibt kein Schach und keine Rochade.";
        foreach (bool white in new[] { true, false })
        {
            int Count(char piece) => position.Squares.Count(c => c == (white ? piece : char.ToLowerInvariant(piece)));
            int promoted = Math.Max(0, Count('Q') - 1) + Math.Max(0, Count('R') - 2) +
                Math.Max(0, Count('B') - 2) + Math.Max(0, Count('N') - 2);
            if (Count('P') + promoted > 8)
                return "Künstliche Materialverteilung: Stockfish unterstützt diese Stellung nicht. Spielen mit beiden Seiten ist möglich; Engine und Bewertung sind ausgeschaltet.";
        }
        return null;
    }
}
