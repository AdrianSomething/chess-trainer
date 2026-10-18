namespace ChessOpeningTrainer;
public static class GraphOperations
{
    public static List<(OpeningNode Node, Position Position, List<string> Path)> Positions(OpeningDefinition opening)
    {
        var list = new List<(OpeningNode, Position, List<string>)>();
        var queue = new Queue<(OpeningNode N, Position P, List<string> Path)>();
        var seen = new HashSet<OpeningNode>();
        queue.Enqueue((opening.Tree, Position.FromFen(opening.StartFen ?? Position.InitialFen), []));
        while (queue.TryDequeue(out var w))
        {
            if (!seen.Add(w.N))
                continue;
            list.Add((w.N, w.P, w.Path));
            foreach (var d in w.N.Moves)
            {
                var p = w.P.Clone();
                p.Apply(MoveGenerator.GenerateLegal(p).First(m => m.Uci == d.Uci));
                queue.Enqueue((d.Next, p, [..w.Path, d.Uci]));
            }
        }

        return list;
    }

    public static OpeningDefinition Combine(OpeningDefinition first, OpeningDefinition second)
    {
        if (first.LearnsWhite != second.LearnsWhite)
            throw new InvalidOperationException("Nur gleiche Lernperspektiven können kombiniert werden.");
        if (OpeningStore.PositionKey(Position.FromFen(first.StartFen ?? Position.InitialFen)) != OpeningStore.PositionKey(Position.FromFen(second.StartFen ?? Position.InitialFen)))
            throw new InvalidOperationException("Die Anfangsstellungen sind verschieden.");
        var merged = new OpeningDefinition
        {
            SchemaVersion = 2,
            Id = "combined_" + Guid.NewGuid().ToString("N"),
            Name = first.Name + " + " + second.Name,
            Perspective = first.Perspective,
            StartFen = first.StartFen,
            Tree = new()
            {
                Id = "n0",
                Fen = first.StartFen ?? Position.InitialFen
            },
            Source = "Zusammengeführte Repertoires"
        };
        Extend(merged, first);
        int conflicts = Extend(merged, second);
        merged.Description = $"Kombination; {conflicts} abweichende eigene Antworten behalten den Zug des zuerst gewählten Repertoires.";
        return merged;
    }

    public static int Extend(OpeningDefinition target, OpeningDefinition addition)
    {
        var map = Positions(target).GroupBy(w => OpeningStore.PositionKey(w.Position)).ToDictionary(g => g.Key, g => g.First().Node);
        int serial = map.Count, conflicts = 0;
        bool Reaches(OpeningNode from, OpeningNode to)
        {
            var seen = new HashSet<OpeningNode>();
            var stack = new Stack<OpeningNode>();
            stack.Push(from);
            while (stack.TryPop(out var n))
            {
                if (n == to)
                    return true;
                if (seen.Add(n))
                    foreach (var m in n.Moves)
                        stack.Push(m.Next);
            }

            return false;
        }

        foreach (var source in Positions(addition))
        {
            string key = OpeningStore.PositionKey(source.Position);
            if (!map.TryGetValue(key, out var node))
                continue;
            node.IsSubbranch |= source.Node.IsSubbranch;
            foreach (var move in source.Node.Moves)
            {
                if (source.Position.WhiteToMove == target.LearnsWhite && node.Moves.Count > 0 && node.Moves.All(m => m.Uci != move.Uci))
                {
                    conflicts++;
                    continue;
                }

                if (node.Moves.Any(m => m.Uci == move.Uci))
                    continue;
                var after = source.Position.Clone();
                after.Apply(MoveGenerator.GenerateLegal(after).First(m => m.Uci == move.Uci));
                string nextKey = OpeningStore.PositionKey(after);
                if (!map.TryGetValue(nextKey, out var next))
                {
                    next = new()
                    {
                        Id = "ext_" + Guid.NewGuid().ToString("N"),
                        Fen = after.ToFen(),
                        Comment = move.Next.Comment
                    };
                    map[nextKey] = next;
                    serial++;
                }

                if (Reaches(next, node))
                    continue;
                node.Moves.Add(new() { Uci = move.Uci, Next = next, NextId = next.Id, HumanUnfriendly = move.HumanUnfriendly, Explanation = move.Explanation });
            }
        }

        if (target.SchemaVersion == 2)
        {
            target.RootNodeId = target.Tree.Id;
            target.Nodes = Positions(target).Select(w => w.Node).ToDictionary(n => n.Id!);
        }

        OpeningStore.Validate(target);
        return conflicts;
    }

    public static async Task LabelHuman(OpeningDefinition opening, StockfishEngine engine, int depth, IProgress<string> report, CancellationToken token)
    {
        var positions = Positions(opening).Where(w => w.Position.WhiteToMove != opening.LearnsWhite && w.Node.Moves.Count > 0).ToList();
        var staged = new List<(OpeningMove Move, bool Label)>();
        int i = 0;
        await using var pool = new EnginePool(engine, 2, 1, token);
        foreach (var w in positions)
        {
            var r = await pool.Search(w.Position, depth).WaitAsync(token);
            double best = OpeningGenerator.Score(r.Lines[0]);
            foreach (var move in w.Node.Moves)
            {
                var line = r.Lines.FirstOrDefault(l => l.BestMove == move.Uci);
                staged.Add((move, line is null || OpeningGenerator.Score(line) < -2 || best - OpeningGenerator.Score(line) > 2));
            }

            report.Report($"Human-Moves: {++i}/{positions.Count}");
        }

        foreach (var(move, label)in staged)
            move.HumanUnfriendly = label;
        OpeningStore.Save(opening, opening.FilePath);
    }
}
