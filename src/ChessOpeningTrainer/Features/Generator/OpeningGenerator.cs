namespace ChessOpeningTrainer;
public sealed class OpeningGenerator
{
    private readonly StockfishEngine engine;
    public OpeningGenerator(StockfishEngine engine)
    {
        this.engine = engine;
    }

    public static double Score(EngineLine l) => l.Mate is int m ? (m > 0 ? 1000 - m * .01 : -1000 - m * .01) : (l.ScoreCentipawns ?? 0) / 100d;
    public static List<EngineLine> Plausible(EngineResult r, GeneratorOptions o, double? replyLoss = null)
    {
        if (r.Lines.Count == 0)
            return[];
        double best = Score(r.Lines[0]);
        return r.Lines.Where(l => Score(l) > o.EasyThreshold && best - Score(l) <= (replyLoss ?? o.MaxReplyLoss)).ToList();
    }

    public static EngineLine ChooseLearner(Position p, IReadOnlyList<EngineLine> lines, GeneratorOptions o, Func<Position, bool> known)
    {
        var best = lines[0];
        var legal = MoveGenerator.GenerateLegal(p).ToDictionary(m => m.Uci);
        foreach (var line in lines.OrderByDescending(Score))
        {
            if (Score(best) - Score(line) > o.CompressionLoss + 1e-8)
                continue;
            if (best.Mate != line.Mate && (best.Mate is not null || line.Mate is not null))
                continue;
            if (line.BestMove is not { } uci || !legal.TryGetValue(uci, out var m))
                continue;
            var after = p.Clone();
            after.Apply(m);
            if (known(after))
                return line;
        }

        // Compression has priority, including when its allowed loss is larger
        // than the variation budget. Zero retains the original deterministic
        // best-move fallback and does not consume random choices.
        if (o.LearnerVariationLoss <= 0) return best;
        var choices = lines.Where(line => line.BestMove is { } u && legal.ContainsKey(u)
            && Score(line) > Score(best) - o.LearnerVariationLoss + 1e-8).ToArray();
        return choices.Length == 0 ? best : choices[Random.Shared.Next(choices.Length)];
    }

    public async Task<(int Lower, int Upper)> EstimateAsync(Position start, GeneratorOptions options, CancellationToken token, IReadOnlyList<string>? setup = null)
    {
        options.Validate();
        // A bounded real pilot includes merging and both score filters. Extrapolate
        // measured per-layer unique learner nodes, not raw legal-move counts.
        int prefix = setup?.Select((_, i) => (i % 2 == 0) == options.LearnWhite).Count(x => x) ?? 0;
        var sampleOptions = new GeneratorOptions
        {
            LearnWhite = options.LearnWhite,
            MinDepth = Math.Min(options.MinDepth, 4),
            MaxDepth = Math.Min(options.MaxDepth, 4),
            EasyThreshold = options.EasyThreshold,
            ComplexThreshold = options.ComplexThreshold,
            MaxPositions = Math.Min(options.MaxPositions, prefix + 16),
            SearchDepth = Math.Min(options.SearchDepth, 7),
            MaxReplyLoss = options.MaxReplyLoss,
            CompressionLoss = options.CompressionLoss,
            LearnerVariationLoss = options.AtDepth(0).LearnerVariationLoss,
            Threads = options.Threads,
            Workers = options.Workers,
            MergeTranspositions = options.MergeTranspositions,
            HumanFilter = options.HumanFilter,
            HumanDepth = options.HumanDepth,
            TwoStages = options.TwoStages,
            MiddleDepth = Math.Min(options.MiddleDepth, 4),
            LateComplexThreshold = options.LateComplexThreshold,
            LateReplyLoss = options.LateReplyLoss
        };
        sampleOptions.Sections = options.Sections.Where((_, i) => i == 0 || options.Sections[i - 1].EndDepth < sampleOptions.MaxDepth)
            .Select(s => new DepthSection { EndDepth = Math.Min(s.EndDepth, sampleOptions.MaxDepth),
                ComplexThreshold = s.ComplexThreshold, EasyThreshold = s.EasyThreshold, SearchDepth = Math.Min(s.SearchDepth, 7),
                MaxReplyLoss = s.MaxReplyLoss, CompressionLoss = s.CompressionLoss, RetainBest = s.RetainBest,
                RetainMinEval = s.RetainMinEval, LearnerVariationLoss = s.LearnerVariationLoss }).ToList();
        var sample = await GenerateAsync(start, "Schätzprobe", sampleOptions, _ => Task.FromResult(false), null, token, setup);
        token.ThrowIfCancellationRequested();
        int measured = OpeningStore.LearnerMoves(sample).Count();
        // A shallow sample cannot predict exponentially branching, filtered graphs.
        // Report evidence plus the actual consent cap, never a fabricated extrapolation.
        return (measured, options.MaxPositions);
    }

    public async Task<OpeningDefinition> GenerateAsync(Position start, string name, GeneratorOptions options, Func<int, Task<bool>> continueAtLimit, IProgress<GeneratorProgress>? progress, CancellationToken token, IReadOnlyList<string>? setup = null, IProgress<GeneratorGraphSnapshot>? graph = null, IProgress<GeneratorSearchDepth>? searchDepthProgress = null, IOpeningSearch? sharedSearch = null)
    {
        options.Validate();
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Bitte einen Namen eingeben.");
        var initial = setup is null ? start.Clone() : Position.FromFen(Position.InitialFen);
        if (setup is not null)
        {
            var check = initial.Clone();
            var prefixSeen = new HashSet<string>
            {
                OpeningStore.PositionKey(check)
            };
            foreach (var u in setup)
            {
                var m = MoveGenerator.GenerateLegal(check).FirstOrDefault(m => m.Uci == u) ?? throw new ArgumentException("Ungültige Startsequenz.");
                check.Apply(m);
                if (options.MergeTranspositions && !prefixSeen.Add(OpeningStore.PositionKey(check)))
                    throw new ArgumentException("Die Startsequenz enthält einen Stellungskreis. Entferne die Wiederholung oder deaktiviere Transpositionskompression.");
            }

            if (check.ToFen() != start.ToFen())
                throw new ArgumentException("Startsequenz und Brett stimmen nicht überein.");
        }

        var opening = new OpeningDefinition
        {
            SchemaVersion = 2,
            Id = "custom_" + Guid.NewGuid().ToString("N"),
            Name = name,
            Perspective = options.LearnWhite ? "white" : "black",
            StartFen = initial.ToFen(),
            Source = "Stockfish · Graphgenerator",
            Description = $"Startsequenz inklusive; Baumtiefe {options.MinDepth}–{options.MaxDepth} Halbzüge danach. Kompressionsverlust ≤ {options.CompressionLoss:0.00} Bauern. Eigener Variationsverlust {options.LearnerVariationLoss:0.00} Bauern (0 = aus). Engine-Tiefe {options.SearchDepth}, ohne Zeitlimit."};
        if (options.Sections.Count > 0)
            opening.Description = "Startsequenz inklusive; Abschnittswerte (Endtiefe / Komplexität / Easy / Engine-Tiefe / Gegnerverlust / Kompressionsverlust / Komplexitätsausnahme Top X / Mindestwert Weiß / eigener Variationsverlust): " + string.Join("; ", options.Sections.Select(s => $"{s.EndDepth} / {s.ComplexThreshold} / {s.EasyThreshold} / {s.SearchDepth} / {s.MaxReplyLoss} / {s.CompressionLoss} / {s.RetainBest} / {s.RetainMinEval} / {s.LearnerVariationLoss}"));
        var known = new Dictionary<string, OpeningNode>();
        var levels = new Dictionary<string, int>();
        var learnerMoveNodes = new HashSet<string>();
        var completedPreviewNodes = new HashSet<string>();
        var terminalPreviewCounters = new HashSet<string>();
        var snapshotClock = System.Diagnostics.Stopwatch.StartNew();
        int serial = 0, cards = 0, limit = options.MaxPositions;
        bool stoppedAtLimit = false;
        void ReportGraph(int pendingCount, bool finished = false, bool force = false, bool stopped = false)
        {
            if (graph is null || !force && !finished && snapshotClock.ElapsedMilliseconds < 300) return;
            snapshotClock.Restart();
            graph.Report(new(known.Values.Select(n => new GeneratorGraphNode(n.Id!, levels[n.Id!], learnerMoveNodes.Contains(n.Id!), completedPreviewNodes.Contains(n.Id!), terminalPreviewCounters.Contains(n.Id!) && n.Moves.Count == 0)).ToArray(),
                known.Values.SelectMany(n => n.Moves.Select(m => new GeneratorGraphEdge(n.Id!, m.Next.Id!))).ToArray(),
                cards, pendingCount, options.MaxDepth + (setup?.Count ?? 0), finished, stopped));
        }
        string Key(Position p, int absoluteDepth) => (options.MergeTranspositions ? "" : absoluteDepth + "|") + OpeningStore.PositionKey(p);
        bool Reaches(OpeningNode from, OpeningNode target)
        {
            var seen = new HashSet<OpeningNode>();
            var todo = new Stack<OpeningNode>();
            todo.Push(from);
            while (todo.TryPop(out var n))
            {
                if (n == target)
                    return true;
                if (seen.Add(n))
                    foreach (var d in n.Moves)
                        todo.Push(d.Next);
            }

            return false;
        }

        OpeningNode Register(Position p, int level, out bool created)
        {
            string k = Key(p, level);
            if (known.TryGetValue(k, out var n))
            {
                created = false;
                return n;
            }

            n = new OpeningNode
            {
                Id = "n" + serial++,
                Fen = p.ToFen()
            };
            known[k] = n;
            levels[n.Id!] = level;
            if (level > 0 && p.WhiteToMove != options.LearnWhite) learnerMoveNodes.Add(n.Id!);
            created = true;
            return n;
        }

        opening.Tree = Register(initial, 0, out _);
        opening.RootNodeId = opening.Tree.Id;
        async Task<bool> Permit()
        {
            if (cards < limit)
                return true;
            ReportGraph(0, force: true);
            progress?.Report(new(cards, 0, "Kartenlimit: Bestätigung nötig (inklusive Startsequenz)"));
            if (!await continueAtLimit(cards))
                return false;
            limit += options.MaxPositions;
            return true;
        }

        var current = opening.Tree;
        var prefixPosition = initial.Clone();
        int offset = 0;
        var queue = new Queue<(Position P, OpeningNode Node, int Depth, HashSet<string> Seen)>();
        using var searches = CancellationTokenSource.CreateLinkedTokenSource(token);
        int workers = Math.Min(options.Workers, options.Threads), threads = Math.Max(1, options.Threads / Math.Min(options.Workers, options.Threads));
        var pending = new Dictionary<string, Task<EngineResult>>();
        var reachedDepths = new System.Collections.Concurrent.ConcurrentDictionary<string, int>();
        string? currentSearchKey = null;
        int currentTreeDepth = 0;
        await using var pool = sharedSearch is null ? new EnginePool(engine, workers, threads, searches.Token) : null;
        Task<EngineResult> RunSearch(Position p, int depth)
        {
            if (searchDepthProgress is null) return sharedSearch?.Search(p, depth, null, searches.Token) ?? pool!.Search(p, depth);
            string key = p.ToFen() + "|" + depth;
            void ReportDepth(int reached)
            {
                int achieved = reachedDepths.AddOrUpdate(key, reached, (_, previous) => Math.Max(previous, reached));
                if (Volatile.Read(ref currentSearchKey) == key)
                    searchDepthProgress?.Report(new(achieved, depth, Volatile.Read(ref currentTreeDepth), options.MaxDepth));
            }
            return sharedSearch?.Search(p, depth, ReportDepth, searches.Token) ?? pool!.Search(p, depth, depthProgress: ReportDepth);
        }
        Task<EngineResult> Search(Position p, int depth)
        {
            string k = p.ToFen() + "|" + depth;
            if (!pending.TryGetValue(k, out var task))
            {
                task = RunSearch(p, depth);
                pending[k] = task;
            }

            return task;
        }

        try
        {
            ReportGraph(1, force: true);
            foreach (var u in setup ?? [])
            {
                token.ThrowIfCancellationRequested();
                bool learner = prefixPosition.WhiteToMove == options.LearnWhite;
                if (learner && !await Permit())
                {
                    ReportGraph(1, force: true, stopped: true);
                    OpeningStore.Validate(opening);
                    return opening;
                }

                var m = MoveGenerator.GenerateLegal(prefixPosition).First(m => m.Uci == u);
                prefixPosition.Apply(m);
                offset++;
                var next = Register(prefixPosition, offset, out _);
                current.Moves.Add(new() { Uci = u, Next = next, NextId = next.Id, Explanation = "Manuell vorgegebene Startsequenz" });
                if (learner)
                    cards++;
                current = next;
            }

            queue.Enqueue((start.Clone(), current, 0, []));
            ReportGraph(queue.Count, force: true);
            var expanded = new HashSet<OpeningNode>();
            while (queue.TryDequeue(out var work))
            {
                token.ThrowIfCancellationRequested();
                bool learner = work.P.WhiteToMove == options.LearnWhite;
                var effective = options.AtDepth(work.Depth);
                if (work.Depth >= options.MaxDepth && learner || !expanded.Add(work.Node) || !work.Seen.Add(OpeningStore.PositionKey(work.P)))
                {
                    completedPreviewNodes.Add(work.Node.Id!);
                    if (learner && work.Depth >= options.MaxDepth && levels[work.Node.Id!] > 0) terminalPreviewCounters.Add(work.Node.Id!);
                    continue;
                }
                var state = MoveGenerator.GameState(work.P, new Dictionary<string, int>());
                if (state != "" && state != "Schach")
                {
                    completedPreviewNodes.Add(work.Node.Id!);
                    if (learner && levels[work.Node.Id!] > 0) terminalPreviewCounters.Add(work.Node.Id!);
                    continue;
                }
                if (learner && !await Permit())
                {
                    stoppedAtLimit = true;
                    break;
                }
                progress?.Report(new(cards, queue.Count, $"Tiefe {work.Depth}/{options.MaxDepth} · {pool?.ActiveSearches ?? 0}/{workers} aktive Arbeiter × {threads} Threads · Engine {effective.SearchDepth}"));
                ReportGraph(queue.Count + 1);
                // All learner alternatives are needed to measure compression loss.
                Volatile.Write(ref currentTreeDepth, work.Depth);
                Volatile.Write(ref currentSearchKey, work.P.ToFen() + "|" + effective.SearchDepth);
                searchDepthProgress?.Report(new(reachedDepths.GetValueOrDefault(currentSearchKey), effective.SearchDepth, work.Depth, options.MaxDepth));
                var searchTask = Search(work.P, effective.SearchDepth);
                foreach (var nextWork in queue.Where(w => w.Depth <= options.MaxDepth && !expanded.Contains(w.Node) && !pending.ContainsKey(w.P.ToFen() + "|" + options.AtDepth(w.Depth).SearchDepth)).Take(Math.Max(0, workers * 3 - pending.Count)))
                    _ = Search(nextWork.P, options.AtDepth(nextWork.Depth).SearchDepth);
                var r = await searchTask;
                searchDepthProgress?.Report(new(r.Lines.Count == 0 ? 0 : r.Lines.Max(l => l.Depth), effective.SearchDepth, work.Depth, options.MaxDepth));
                Volatile.Write(ref currentSearchKey, null);
                pending.Remove(work.P.ToFen() + "|" + effective.SearchDepth);
                reachedDepths.TryRemove(work.P.ToFen() + "|" + effective.SearchDepth, out _);
                bool late = effective.TwoStages && work.Depth >= effective.MiddleDepth;
                var candidates = learner ? new List<EngineLine>
                {
                    ChooseLearner(work.P, r.Lines, effective, p => known.TryGetValue(Key(p, offset + work.Depth + 1), out var n) && !Reaches(n, work.Node))
                }

                : Plausible(r, effective, late ? effective.LateReplyLoss : null);
                HashSet<string?>? humanMoves = null;
                if (!learner && options.HumanFilter)
                {
                    var shallow = await RunSearch(work.P, Math.Min(options.HumanDepth, effective.SearchDepth));
                    humanMoves = Plausible(shallow, effective, late ? effective.LateReplyLoss : null).Select(l => l.BestMove).ToHashSet();
                }

                bool stop = !learner && (work.Depth >= options.MaxDepth || work.Depth >= options.MinDepth && candidates.Count >= (late ? effective.LateComplexThreshold : effective.ComplexThreshold));
                if (stop)
                    work.Node.Comment = work.Depth >= options.MaxDepth ? "Tiefengrenze: mögliche Gegenantworten" : "Komplexitätsgrenze: mögliche Gegenantworten";
                foreach (var line in candidates)
                {
                    var m = MoveGenerator.GenerateLegal(work.P).First(m => m.Uci == line.BestMove);
                    var after = work.P.Clone();
                    after.Apply(m);
                    if (work.Seen.Contains(OpeningStore.PositionKey(after)))
                        continue;
                    if (known.TryGetValue(Key(after, offset + work.Depth + 1), out var existing) && Reaches(existing, work.Node))
                        continue;
                    var next = Register(after, offset + work.Depth + 1, out bool created);
                    work.Node.Moves.Add(new OpeningMove { Uci = m.Uci, Next = next, NextId = next.Id, HumanUnfriendly = humanMoves is not null && !humanMoves.Contains(m.Uci), Explanation = $"Stockfish Tiefe {line.Depth}; Wert {Score(line):0.00}; Verlust zum besten Zug {Score(r.Lines[0]) - Score(line):0.00}" + (!created ? " · gemeinsame Folgestellung" : "") });
                    if (learner)
                        cards++;
                    bool rescued = stop && work.Depth < options.MaxDepth && effective.RetainBest > 0 && candidates.Take(effective.RetainBest).Contains(line) && (work.P.WhiteToMove ? Score(line) : -Score(line)) >= effective.RetainMinEval;
                    if (created && stop && !rescued) terminalPreviewCounters.Add(next.Id!);
                    if (created && (!stop || rescued))
                        queue.Enqueue((after, next, work.Depth + 1, new(work.Seen)));
                }
                completedPreviewNodes.Add(work.Node.Id!);
            }
        }
        catch (OperationCanceledException)
        {
            opening.Description += " · Teilgraph nach Abbruch";
        }
        finally
        {
            Volatile.Write(ref currentSearchKey, null);
            searches.Cancel();
            if (pool is not null) await pool.DisposeAsync();
            try
            {
                await Task.WhenAll(pending.Values);
            }
            catch
            {
            }
        }

        opening.Nodes = known.Values.ToDictionary(n => n.Id!);
        ReportGraph(queue.Count, !token.IsCancellationRequested && !stoppedAtLimit && queue.Count == 0, force: true, stopped: true);
        progress?.Report(new(cards, queue.Count, "Fertig · " + cards + " eindeutige Lernkarten"));
        OpeningStore.Validate(opening);
        return opening;
    }
}
