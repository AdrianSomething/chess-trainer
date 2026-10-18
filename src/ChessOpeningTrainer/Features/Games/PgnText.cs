using System.Text;
using System.Text.RegularExpressions;

namespace ChessOpeningTrainer;
public static class PgnText
{
    public static string Write(PersonalGame game, bool nextNumber = false)
    {
        var result = new StringBuilder();
        var tags = new Dictionary<string, string>(game.Tags);
        foreach (string key in new[]
        {
            "Event",
            "Site",
            "Date",
            "Round",
            "White",
            "Black",
            "Result"
        }

        )
            if (!tags.ContainsKey(key))
                tags[key] = key == "Result" ? "*" : "";
        if (game.StartFen != Position.InitialFen)
        {
            tags["SetUp"] = "1";
            tags["FEN"] = game.StartFen;
        }

        foreach (var(key, value)in tags)
            result.AppendLine($"[{key} \"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"]");
        result.AppendLine();
        var p = Position.FromFen(game.StartFen);
        for (int i = 0; i < game.Moves.Count; i++)
        {
            var move = MoveGenerator.GenerateLegal(p).First(m => m.Uci == game.Moves[i]);
            if (p.WhiteToMove)
                result.Append(p.Fullmove + ". ");
            else if (i == 0)
                result.Append(p.Fullmove + "... ");
            result.Append(SanNotation.Format(p, move) + " ");
            p.Apply(move);
        }

        if (nextNumber && p.WhiteToMove)
            result.Append(p.Fullmove + ". ");
        result.Append(string.IsNullOrWhiteSpace(tags["Result"]) ? "*" : tags["Result"]);
        return result.ToString();
    }
}
