namespace ChessOpeningTrainer;

/// <summary>Opt-in custom-game variant. Standard chess and engine FEN validation are unchanged.</summary>
public sealed record KingCaptureRules(int WhiteKings, int BlackKings, bool AllWhiteKings = true, bool AllBlackKings = true)
{
    public static bool IsMultiple(Position position) => position.Squares.Count(c => c == 'K') > 1 || position.Squares.Count(c => c == 'k') > 1;
    public IReadOnlyList<Move> Moves(Position position) => Winner(position) is null ? MoveGenerator.GeneratePseudo(position, allowCastling: false) : [];
    public bool? Winner(Position position)
    {
        bool Lost(char king, int initial, bool all)
        {
            int count = position.Squares.Count(c => c == king);
            return all ? count == 0 : count < initial;
        }
        if (Lost('K', WhiteKings, AllWhiteKings)) return false;
        if (Lost('k', BlackKings, AllBlackKings)) return true;
        return null;
    }
    public string State(Position position, IReadOnlyDictionary<string, int> repetitions)
    {
        if (Winner(position) is bool white) return white ? "Weiß gewinnt durch Königsschlag" : "Schwarz gewinnt durch Königsschlag";
        if (position.Halfmove >= 100) return "Remis: 50-Züge-Regel";
        if (repetitions.GetValueOrDefault(position.RepetitionKey()) >= 3) return "Remis: dreifache Stellungswiederholung";
        return Moves(position).Count == 0 ? "Remis: keine möglichen Züge" : "";
    }
    public string Notation(Position before, Move move)
    {
        char type = char.ToUpperInvariant(before.Squares[move.From]);
        bool capture = before.Squares[move.To] != '\0' || move.IsEnPassant;
        string name = type == 'P' ? capture ? Square.Name(move.From)[..1] : "" : type.ToString();
        if (type != 'P')
        {
            var alternatives = Moves(before).Where(m => m.To == move.To && m.From != move.From && char.ToUpperInvariant(before.Squares[m.From]) == type).ToArray();
            if (alternatives.Length > 0)
                name += alternatives.All(m => m.From % 8 != move.From % 8) ? Square.Name(move.From)[..1]
                    : alternatives.All(m => m.From / 8 != move.From / 8) ? Square.Name(move.From)[1..] : Square.Name(move.From);
        }
        return name + (capture ? "x" : "") + Square.Name(move.To) + (move.Promotion == '\0' ? "" : "=" + char.ToUpperInvariant(move.Promotion));
    }
}
