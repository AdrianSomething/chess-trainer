namespace ChessOpeningTrainer;

public sealed record BranchGameExit(PersonalGame Game, Position LastTheoryPosition, int PositionIndex, bool OpponentExit, bool TheoryEnd = false, string? IncomingSan = null);

/// <summary>Match game snapshots to opening positions without engine or file access.</summary>
public static class BranchGameAnalysis
{
    public static List<BranchGameExit> Find(OpeningDefinition opening, IEnumerable<PersonalGame> games, string player)
    {
        var known = GraphOperations.Positions(opening).GroupBy(w => BranchPositionIdentity.Key(w.Position))
            .ToDictionary(g => g.Key, g => g.SelectMany(w => w.Node.Moves).Select(m => m.Uci).ToHashSet());
        var forced = new HashSet<string>();
        var trunkNode = opening.Tree; var trunkPosition = Position.FromFen(opening.StartFen ?? Position.InitialFen);
        while (trunkNode.Moves.Count == 1 && forced.Add(BranchPositionIdentity.Key(trunkPosition)))
        {
            var edge = trunkNode.Moves[0]; trunkPosition.Apply(MoveGenerator.GenerateLegal(trunkPosition).First(m => m.Uci == edge.Uci));
            trunkNode = edge.Next;
        }
        if (trunkNode.Moves.Count == 0) forced.Add(BranchPositionIdentity.Key(trunkPosition));
        var result = new List<BranchGameExit>();
        foreach (var game in games)
        {
            bool? white = game.White.Trim().Equals(player.Trim(), StringComparison.OrdinalIgnoreCase) ? true
                : game.Black.Trim().Equals(player.Trim(), StringComparison.OrdinalIgnoreCase) ? false : null;
            if (white is null || white != opening.LearnsWhite) continue;
            List<Position> positions;
            try { positions = game.Positions(); } catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or FormatException) { continue; }
            string IncomingSan(int index)
            {
                if (index == 0) return "Start";
                var p = positions[index - 1];
                return SanNotation.Format(p, MoveGenerator.GenerateLegal(p).First(m => m.Uci == game.Moves[index - 1]));
            }
            BranchGameExit? exit = null;
            for (int i = positions.Count - 1; i >= 0; i--)
            {
                var before = positions[i];
                string key = BranchPositionIdentity.Key(before);
                if (!known.TryGetValue(key, out var moves)) continue;
                if (i == game.Moves.Count)
                {
                    if (i > 0 || opening.StartFen is not null) exit = new(game, before.Clone(), i, false, true, IncomingSan(i));
                    break;
                }
                bool own = before.WhiteToMove == white;
                if (!moves.Contains(game.Moves[i]) && (own || !forced.Contains(key)))
                    exit = new(game, before.Clone(), i, !own, IncomingSan: IncomingSan(i));
                // Latest common position wins, including transpositions. Do not
                // fall back to an earlier own exit after an ignored trunk exit.
                break;
            }
            if (exit is not null) result.Add(exit);
        }
        return result;
    }
}
