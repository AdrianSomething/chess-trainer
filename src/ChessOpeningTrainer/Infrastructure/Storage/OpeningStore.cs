using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChessOpeningTrainer;
public static class OpeningStore
{
    public static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };
    public static List<OpeningDefinition> Load(string folder, out List<string> errors)
    {
        Directory.CreateDirectory(folder);
        errors = [];
        var result = new List<OpeningDefinition>();
        foreach (var file in Directory.EnumerateFiles(folder, "*.json", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var opening = JsonSerializer.Deserialize<OpeningDefinition>(File.ReadAllText(file), Json) ?? throw new InvalidDataException("Die Datei ist leer.");
                if (opening.Lines is { Count: > 0 })
                    opening.Tree = BuildTreeFromLines(opening.Lines);
                ResolveGraph(opening);
                Validate(opening);
                opening.FilePath = file;
                result.Add(opening);
            }
            catch (Exception ex)
            {
                errors.Add($"{Path.GetFileName(file)}: {ex.Message}");
            }
        }

        return result.OrderBy(x => x.Name).ToList();
    }

    public static void ResolveGraph(OpeningDefinition opening)
    {
        if (opening.SchemaVersion != 2)
            return;
        if (opening.Nodes is null || opening.RootNodeId is null || !opening.Nodes.TryGetValue(opening.RootNodeId, out var root))
            throw new InvalidDataException("Graphwurzel fehlt.");
        foreach (var(id, node)in opening.Nodes)
        {
            node.Id = id;
            foreach (var move in node.Moves)
            {
                if (move.NextId is null || !opening.Nodes.TryGetValue(move.NextId, out var next))
                    throw new InvalidDataException("Ungültiger Knotenverweis.");
                move.Next = next;
            }
        }

        opening.Tree = root;
    }

    public static string PositionKey(Position p) => p.RepetitionKey();
    public static string NodeProgressKey(OpeningDefinition o, OpeningNode n, string uci) => o.Id + "/node/" + n.Id + "/" + uci;
    private static void ValidateGraph(OpeningDefinition o)
    {
        if (string.IsNullOrWhiteSpace(o.Id) || string.IsNullOrWhiteSpace(o.Name) || o.Perspective is not ("white" or "black"))
            throw new InvalidDataException("Ungültige Graph-Metadaten.");
        var seen = new Dictionary<OpeningNode, string>();
        var active = new HashSet<OpeningNode>();
        void Visit(OpeningNode n, Position p)
        {
            if (active.Contains(n))
                throw new InvalidDataException("Zyklen im Openinggraph sind nicht erlaubt.");
            if (seen.TryGetValue(n, out var key))
            {
                if (key != PositionKey(p))
                    throw new InvalidDataException("Verweis führt zu anderer Stellung.");
                return;
            }

            if (n.Id is null)
                throw new InvalidDataException("Graphknoten braucht ID.");
            if (n.Fen is not null && PositionKey(Position.FromFen(n.Fen)) != PositionKey(p))
                throw new InvalidDataException("Knoten-FEN passt nicht zur Zugfolge.");
            seen[n] = PositionKey(p);
            active.Add(n);
            if (n.Moves.Count > 0 && p.WhiteToMove == o.LearnsWhite && n.Moves.Count != 1)
                throw new InvalidDataException("Genau ein eigener Lernzug erforderlich.");
            if (n.Moves.Select(m => m.Uci).Distinct().Count() != n.Moves.Count)
                throw new InvalidDataException("Doppelte Kante.");
            foreach (var d in n.Moves)
            {
                var m = MoveGenerator.GenerateLegal(p).FirstOrDefault(m => m.Uci == d.Uci) ?? throw new InvalidDataException("Illegaler Graphzug: " + d.Uci);
                var after = p.Clone();
                after.Apply(m);
                Visit(d.Next, after);
            }

            active.Remove(n);
        }

        Visit(o.Tree, Position.FromFen(o.StartFen ?? Position.InitialFen));
    }

    private static List<LearningTarget> GraphTargets(OpeningDefinition o)
    {
        var result = new List<LearningTarget>();
        var seen = new HashSet<OpeningNode>();
        var queue = new Stack<(OpeningNode Node, Position P, List<OpeningMove> Prefix)>();
        queue.Push((o.Tree, Position.FromFen(o.StartFen ?? Position.InitialFen), []));
        while (queue.TryPop(out var w))
        {
            if (!seen.Add(w.Node))
                continue;
            foreach (var d in w.Node.Moves.AsEnumerable().Reverse())
            {
                if (w.P.WhiteToMove == o.LearnsWhite)
                    result.Add(new(NodeProgressKey(o, w.Node, d.Uci), w.Prefix, d));
                var p = w.P.Clone();
                p.Apply(MoveGenerator.GenerateLegal(p).First(m => m.Uci == d.Uci));
                queue.Push((d.Next, p, [..w.Prefix, d]));
            }
        }

        return result;
    }

    public static void Save(OpeningDefinition o, string file)
    {
        Validate(o);
        if (o.SchemaVersion == 1)
        {
            o.Lines = null;
            File.WriteAllText(file, JsonSerializer.Serialize(o, Json));
            return;
        }

        var nodes = new Dictionary<string, OpeningNode>();
        var visited = new HashSet<OpeningNode>();
        void Visit(OpeningNode n)
        {
            if (!visited.Add(n))
                return;
            nodes[n.Id!] = new OpeningNode
            {
                Id = n.Id,
                Fen = n.Fen,
                Comment = n.Comment,
                IsSubbranch = n.IsSubbranch,
                SubbranchColor = n.SubbranchColor,
                Moves = n.Moves.Select(d => new OpeningMove { Uci = d.Uci, Name = d.Name, Explanation = d.Explanation, NextId = d.Next.Id, HumanUnfriendly = d.HumanUnfriendly }).ToList()
            };
            foreach (var d in n.Moves)
                Visit(d.Next);
        }

        Visit(o.Tree);
        var copy = new OpeningDefinition
        {
            SchemaVersion = 2,
            Id = o.Id,
            Name = o.Name,
            Perspective = o.Perspective,
            Description = o.Description,
            Source = o.Source,
            StartFen = o.StartFen,
            RootNodeId = o.Tree.Id,
            Nodes = nodes
        };
        File.WriteAllText(file, JsonSerializer.Serialize(copy, Json));
    }

    private static OpeningNode BuildTreeFromLines(IEnumerable<OpeningLine> lines)
    {
        var root = new OpeningNode();
        foreach (var line in lines)
        {
            if (line.Moves.Count == 0)
                throw new InvalidDataException("Eine line braucht mindestens einen Zug.");
            var node = root;
            for (var ply = 0; ply < line.Moves.Count; ply++)
            {
                var uci = line.Moves[ply].Trim().ToLowerInvariant();
                if (!Move.TryParseUci(uci, out _))
                    throw new InvalidDataException($"Ungültiger UCI-Zug in line: {uci}");
                var move = node.Moves.FirstOrDefault(x => x.Uci.Equals(uci, StringComparison.OrdinalIgnoreCase));
                if (move is null)
                {
                    move = new OpeningMove
                    {
                        Uci = uci,
                        Name = ply == line.Moves.Count - 1 ? line.Name : null,
                        Explanation = ply == line.Moves.Count - 1 ? line.Description : null
                    };
                    node.Moves.Add(move);
                }

                node = move.Next;
            }
        }

        return root;
    }

    public static void Validate(OpeningDefinition opening)
    {
        if (opening.SchemaVersion is not (1 or 2))
            throw new InvalidDataException("Unterstützt werden schemaVersion 1 und 2.");
        if (opening.SchemaVersion == 2)
        {
            ValidateGraph(opening);
            return;
        }

        if (string.IsNullOrWhiteSpace(opening.Id))
            throw new InvalidDataException("id fehlt.");
        if (string.IsNullOrWhiteSpace(opening.Name))
            throw new InvalidDataException("name fehlt.");
        if (!opening.Perspective.Equals("white", StringComparison.OrdinalIgnoreCase) && !opening.Perspective.Equals("black", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("perspective muss 'white' oder 'black' sein.");
        _ = Position.FromFen(opening.StartFen ?? Position.InitialFen);
        ValidateNode(opening.Tree, "tree");
        ValidateLegality(opening.Tree, Position.FromFen(opening.StartFen ?? Position.InitialFen), "tree", opening.LearnsWhite);
    }

    private static void ValidateNode(OpeningNode node, string path)
    {
        node.Moves ??= [];
        foreach (var(move, index)in node.Moves.Select((x, i) => (x, i)))
        {
            if (move is null || !Move.TryParseUci(move.Uci, out _))
                throw new InvalidDataException($"{path}.moves[{index}].uci muss z. B. e2e4 sein.");
            move.Next ??= new OpeningNode();
            ValidateNode(move.Next, $"{path}.moves[{index}].next");
        }
    }

    private static void ValidateLegality(OpeningNode node, Position position, string path, bool learnerWhite)
    {
        if (node.Moves.Count > 0 && position.WhiteToMove == learnerWhite && node.Moves.Count != 1)
            throw new InvalidDataException($"{path} ist ein Lernknoten und braucht genau einen Zug fuer die gewaehlte Perspektive.");
        foreach (var(definition, index)in node.Moves.Select((x, i) => (x, i)))
        {
            Move.TryParseUci(definition.Uci, out var requested);
            var legal = MoveGenerator.GenerateLegal(position).FirstOrDefault(x => x.Equals(requested));
            if (legal is null)
                throw new InvalidDataException($"{path}.moves[{index}] ({definition.Uci}) ist aus dieser Stellung nicht legal.");
            var next = position.Clone();
            next.Apply(legal);
            ValidateLegality(definition.Next, next, $"{path}.moves[{index}].next", learnerWhite);
        }
    }

    public static IEnumerable<(string Key, OpeningMove Move)> LearnerMoves(OpeningDefinition opening)
    {
        if (opening.SchemaVersion == 2)
            return LearningTargets(opening).Select(t => (t.Key, t.Expected));
        var root = Position.FromFen(opening.StartFen ?? Position.InitialFen);
        return Walk(opening.Tree, root, opening.Id, opening.LearnsWhite);
    }

    private static IEnumerable<(string Key, OpeningMove Move)> Walk(OpeningNode node, Position position, string path, bool learnerWhite)
    {
        foreach (var moveDef in node.Moves)
        {
            if (!Move.TryParseUci(moveDef.Uci, out var move))
                continue;
            var key = path + "/" + moveDef.Uci.ToLowerInvariant();
            if (position.WhiteToMove == learnerWhite)
                yield return (key, moveDef);
            var nextPos = position.Clone();
            var legal = MoveGenerator.GenerateLegal(nextPos).FirstOrDefault(x => x.Equals(move));
            if (legal is null)
                continue;
            nextPos.Apply(legal);
            foreach (var child in Walk(moveDef.Next, nextPos, key, learnerWhite))
                yield return child;
        }
    }

    public sealed record LearningTarget(string Key, List<OpeningMove> Prefix, OpeningMove Expected);
    public static List<LearningTarget> LearningTargets(OpeningDefinition opening)
    {
        if (opening.SchemaVersion == 2)
            return GraphTargets(opening);
        var result = new List<LearningTarget>();
        WalkTargets(opening.Tree, Position.FromFen(opening.StartFen ?? Position.InitialFen), opening.Id, opening.LearnsWhite, [], result);
        return result;
    }

    private static void WalkTargets(OpeningNode node, Position position, string path, bool learnerWhite, List<OpeningMove> prefix, List<LearningTarget> result)
    {
        foreach (var moveDef in node.Moves)
        {
            if (!Move.TryParseUci(moveDef.Uci, out var requested))
                continue;
            var legal = MoveGenerator.GenerateLegal(position).FirstOrDefault(x => x.Equals(requested));
            if (legal is null)
                continue;
            var key = path + "/" + moveDef.Uci.ToLowerInvariant();
            if (position.WhiteToMove == learnerWhite)
                result.Add(new LearningTarget(key, [..prefix], moveDef));
            var next = position.Clone();
            next.Apply(legal);
            prefix.Add(moveDef);
            WalkTargets(moveDef.Next, next, key, learnerWhite, prefix, result);
            prefix.RemoveAt(prefix.Count - 1);
        }
    }
}
