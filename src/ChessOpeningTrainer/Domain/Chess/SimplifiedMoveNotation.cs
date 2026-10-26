namespace ChessOpeningTrainer;

/// <summary>Partial, language-independent SAN/UCI input. Only supplied moves can match.</summary>
public static class SimplifiedMoveNotation
{
    public static IReadOnlyList<Move> Match(Position position, string input, IEnumerable<Move> legal)
    {
        var moves = legal.ToArray();
        string text = new(input.Where(c => !char.IsWhiteSpace(c) && c is not 'x' and not 'X' and not '+' and not '#' and not '!' and not '?').ToArray());
        if (text.Length == 0) return moves;
        if (text.Replace('0', 'O').StartsWith("O", StringComparison.OrdinalIgnoreCase))
        {
            string castle = text.Replace('0', 'O').ToUpperInvariant();
            return moves.Where(m => m.IsCastle && (castle == "O" || (castle == "O-O" && m.To > m.From) || (castle == "O-O-O" && m.To < m.From))).ToArray();
        }
        char? piece = null, promotion = null;
        // Lower-case b means a file; use B for a bishop. Other piece letters are unambiguous.
        if ("PNBRQKpnrqk".Contains(text[0])) { piece = char.ToUpperInvariant(text[0]); text = text[1..]; }
        if (text.Length > 0 && "QRBNqrbn".Contains(text[^1]) && (text.Contains('=') || text.Any(char.IsDigit)))
        { promotion = char.ToLowerInvariant(text[^1]); text = text[..^1].TrimEnd('='); }
        text = text.ToLowerInvariant();
        if (text.Any(c => c is not (>= 'a' and <= 'h') and not (>= '1' and <= '8'))) return [];
        string files = new(text.Where(c => c is >= 'a' and <= 'h').ToArray());
        string ranks = new(text.Where(c => c is >= '1' and <= '8').ToArray());
        if (files.Length > 2 || ranks.Length > 2) return [];
        bool Fits(Move m, bool sourceOnly)
        {
            if (piece is char p && char.ToUpperInvariant(position.Squares[m.From]) != p) return false;
            if (promotion is char pr && char.ToLowerInvariant(m.Promotion) != pr) return false;
            int fromFile = m.From % 8, toFile = m.To % 8, fromRank = m.From / 8, toRank = m.To / 8;
            if (files.Length == 2 && (fromFile != files[0] - 'a' || toFile != files[1] - 'a')) return false;
            if (ranks.Length == 2 && (fromRank != ranks[0] - '1' || toRank != ranks[1] - '1')) return false;
            if (files.Length == 1 && (sourceOnly ? fromFile : toFile) != files[0] - 'a') return false;
            if (ranks.Length == 1 && (sourceOnly ? fromRank : toRank) != ranks[0] - '1') return false;
            return true;
        }
        var matching = moves.Where(m => Fits(m, false)).ToArray();
        // SAN omits P. Prefer pawn interpretations unless full source/destination coordinates were supplied.
        if (piece is null && !(files.Length == 2 && ranks.Length == 2))
        {
            var pawns = matching.Where(m => char.ToLowerInvariant(position.Squares[m.From]) == 'p').ToArray();
            if (pawns.Length > 0) return pawns;
        }
        // A bare occupied source square/file can start navigation when it is not a destination.
        return matching.Length > 0 || files.Length == 2 || ranks.Length == 2 ? matching : moves.Where(m => Fits(m, true)).ToArray();
    }
}
