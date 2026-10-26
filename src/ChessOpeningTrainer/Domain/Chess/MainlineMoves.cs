using System.Text;
using System.Text.RegularExpressions;

namespace ChessOpeningTrainer;

/// <summary>Legal SAN/UCI mainline decoding without formatting every candidate as SAN.</summary>
public static class MainlineMoves
{
    private static readonly Regex san = new(@"^(?<piece>[KQRBN])?(?<file>[a-h])?(?<rank>[1-8])?(?<capture>x)?(?<to>[a-h][1-8])(?:=?(?<promotion>[QRBN]))?$", RegexOptions.Compiled);
    private static readonly Regex numbers = new(@"^\d+\.+", RegexOptions.Compiled);
    public static string[] Parse(string body, Position position)
    {
        var moves = new List<string>();
        var word = new StringBuilder();
        int comment = 0, variation = 0;
        bool lineComment = false;
        void Consume()
        {
            string token = numbers.Replace(word.ToString(), "").TrimEnd('+', '#', '!', '?');
            word.Clear();
            if (token.Length == 0 || token[0] == '$' || token is "1-0" or "0-1" or "1/2-1/2" or "*" or "e.p." or "...")
                return;
            var legal = MoveGenerator.GenerateLegal(position);
            Move? move;
            if (Move.TryParseUci(token, out var uci))
                move = legal.SingleOrDefault(m => m.Equals(uci));
            else if (token.Replace('0', 'O') is "O-O" or "O-O-O")
                move = legal.SingleOrDefault(m => m.IsCastle && (token.Length == 3 ? m.To > m.From : m.To < m.From));
            else
            {
                var match = san.Match(token);
                if (!match.Success || !Square.TryParse(match.Groups["to"].Value, out int target))
                    throw new InvalidDataException("Invalid SAN: " + token);
                char piece = match.Groups["piece"].Success ? match.Groups["piece"].Value[0] : 'P';
                char promotion = match.Groups["promotion"].Success ? match.Groups["promotion"].Value[0] : '\0';
                bool capture = match.Groups["capture"].Success;
                move = legal.SingleOrDefault(m => m.To == target && char.ToUpperInvariant(position.Squares[m.From]) == piece &&
                    char.ToUpperInvariant(m.Promotion) == promotion &&
                    (m.IsEnPassant || position.Squares[m.To] != '\0') == capture &&
                    (!match.Groups["file"].Success || Square.Name(m.From)[0] == match.Groups["file"].Value[0]) &&
                    (!match.Groups["rank"].Success || Square.Name(m.From)[1] == match.Groups["rank"].Value[0]));
            }
            if (move is null)
                throw new InvalidDataException("Illegal SAN: " + token);
            moves.Add(move.Uci);
            position.Apply(move);
        }
        foreach (char c in body + " ")
        {
            if (c is '\n' or '\r')
                lineComment = false;
            if (lineComment)
                continue;
            if (c == '{')
            {
                if (comment == 0 && variation == 0)
                    Consume();
                comment++;
                continue;
            }
            if (c == '}')
            {
                comment = Math.Max(0, comment - 1);
                continue;
            }
            if (comment > 0)
                continue;
            if (c == ';')
            {
                if (variation == 0)
                    Consume();
                lineComment = true;
                continue;
            }
            if (c == '(')
            {
                if (variation == 0)
                    Consume();
                variation++;
                continue;
            }
            if (c == ')')
            {
                variation = Math.Max(0, variation - 1);
                continue;
            }
            if (variation > 0)
                continue;
            if (char.IsWhiteSpace(c))
                Consume();
            else
                word.Append(c);
        }
        return moves.ToArray();
    }
}
