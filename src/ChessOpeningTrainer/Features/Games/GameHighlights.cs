namespace ChessOpeningTrainer;

public sealed record GameHighlight(int PositionIndex, string Description);
public static class GameHighlights
{
    public static string Describe(GameMoment m) => m.Category switch
    {
        "Patzer" => "Dieser Zug verschenkte einen entscheidenden Vorteil. Suche eine bessere Fortsetzung.",
        "Verpasste Chance" => "Nach dem Fehler des Gegners konntest du den gewonnenen Vorteil nicht festhalten.",
        "Schlecht" => "Dieser Zug verschlechterte deine Stellung. Vergleiche ihn mit der besten Fortsetzung.",
        "Bester Zug" when m.BestMoves.Count == 1 => "Dieser Zug war die einzige Möglichkeit, deine Stellungsbewertung zu behalten.",
        "Bester Zug" => "Du hast den besten Zug der gespeicherten Analyse gefunden.",
        _ => "Ein guter Zug, der die Stellung weitgehend erhält."
    };
    public static List<GameHighlight> Build(PersonalGame game, string player)
    {
        var positions = game.Positions();
        var result = game.Analysis.Where(m => m.Ply < game.Moves.Count &&
            (positions[m.Ply].WhiteToMove ? game.White : game.Black) == player &&
            (m.Category is "Patzer" or "Verpasste Chance" or "Schlecht" || m.Category == "Bester Zug" && m.BestMoves.Count == 1))
            .Select(m => new GameHighlight(m.Ply + 1, Describe(m))).ToList();
        result.Add(new(game.Moves.Count, "Endstellung der Partie · " + game.Tags.GetValueOrDefault("Result", "*")));
        return result.GroupBy(h => h.PositionIndex).OrderBy(g => g.Key)
            .Select(g => new GameHighlight(g.Key, string.Join("\n", g.Select(h => h.Description).Distinct()))).ToList();
    }

    // Keep the previous public signature for callers of older versions. Opening
    // files no longer participate in classification or highlight selection.
    public static List<GameHighlight> Build(PersonalGame game, string player, IEnumerable<OpeningDefinition> openings) => Build(game, player);
}
