using System.Text.Json;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;

namespace ChessOpeningTrainer;
public static class PgnImport
{
    public static List<PersonalGame> Parse(string text)
    {
        var result = new List<PersonalGame>();
        var tags = new Dictionary<string, string>();
        var body = new StringBuilder();
        void Flush()
        {
            if (string.IsNullOrWhiteSpace(body.ToString()))
                return;
            var game = new PersonalGame
            {
                Tags = new(tags),
                StartFen = tags.GetValueOrDefault("FEN") ?? Position.InitialFen
            };
            var p = Position.FromFen(game.StartFen);
            var clean = new StringBuilder();
            int comment = 0, variation = 0;
            bool lineComment = false;
            foreach (char c in body.ToString())
            {
                if (c == '\n')
                    lineComment = false;
                if (lineComment)
                    continue;
                if (c == ';' && comment == 0)
                {
                    lineComment = true;
                    continue;
                }

                if (c == '{')
                {
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
                if (c == '(')
                {
                    variation++;
                    continue;
                }

                if (c == ')')
                {
                    variation = Math.Max(0, variation - 1);
                    continue;
                }

                if (variation == 0)
                    clean.Append(c);
            }

            foreach (string raw in Regex.Split(clean.ToString(), @"\s+"))
            {
                string token = Regex.Replace(raw, @"^\d+\.(\.\.)?", "");
                token = Regex.Replace(token, @"\$\d+", "");
                if (token.Length == 0 || token is "1-0" or "0-1" or "1/2-1/2" or "*" or "e.p.")
                    continue;
                string Norm(string a) => a.Replace("0", "O").Replace("=", "").TrimEnd('+', '#', '!', '?');
                var legal = MoveGenerator.GenerateLegal(p);
                var matches = legal.Where(m => m.Uci == token || Norm(SanNotation.Format(p, m)) == Norm(token)).ToList();
                if (matches.Count != 1)
                    throw new InvalidDataException($"Partie {result.Count + 1}, Halbzug {game.Moves.Count + 1}: ungültiger oder mehrdeutiger Zug '{token}'.");
                var move = matches[0];
                game.Moves.Add(move.Uci);
                p.Apply(move);
            }

            if (game.Moves.Count > 0)
            {
                game.Id = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(game.StartFen + string.Join(" ", game.Moves) + game.White + game.Black)));
                result.Add(game);
            }

            body.Clear();
            tags.Clear();
        }

        foreach (string line in text.Replace("\r", "").Split('\n'))
        {
            var tag = Regex.Match(line, "^\\s*\\[(\\w+)\\s+\"((?:\\\\.|[^\"])*)\"\\s*\\]\\s*$");
            if (tag.Success)
            {
                if (body.Length > 0 && !string.IsNullOrWhiteSpace(body.ToString()))
                    Flush();
                tags[tag.Groups[1].Value] = tag.Groups[2].Value.Replace("\\\"", "\"");
            }
            else
                body.AppendLine(line);
        }

        Flush();
        return result;
    }
}
