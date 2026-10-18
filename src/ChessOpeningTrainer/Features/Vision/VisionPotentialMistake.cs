namespace ChessOpeningTrainer;

/// <summary>Distinguishes an overlooked move by the piece itself from uncovering its line.</summary>
internal static class VisionPotentialMistake
{
    public static string Classify(Position position, int origin, int target)
    {
        var turn = position.Clone(); turn.WhiteToMove = char.IsUpper(turn.Squares[origin]); turn.EnPassant = null;
        if (MoveGenerator.IsKingInCheck(turn, !turn.WhiteToMove)) return "Umpositionierung / Freiziehen";
        bool uncovering = false;
        foreach (var move in MoveGenerator.GenerateLegal(turn).Where(move => move.To != target))
        {
            var after = turn.Clone(); after.Apply(move);
            int location = origin == move.From ? move.To : origin;
            if (move.IsCastle && char.ToLowerInvariant(turn.Squares[origin]) == 'r')
            {
                int rook = move.To > move.From ? move.From + 3 : move.From - 4;
                if (origin == rook) location = move.To > move.From ? move.From + 1 : move.From - 1;
            }
            if (!VisionRules.Attacks(after, location, target)) continue;
            // Prefer the simpler self-move explanation when both mechanisms are possible.
            if (location != origin) return "Umpositionierung übersehen";
            uncovering = true;
        }
        return uncovering ? "Freiziehen übersehen" : "Umpositionierung / Freiziehen";
    }
}
