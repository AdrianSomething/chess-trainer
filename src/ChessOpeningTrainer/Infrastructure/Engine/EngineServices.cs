namespace ChessOpeningTrainer;
public static class EngineServices
{
    public static async Task<EngineResult> CompleteSearch(StockfishEngine engine, Position p, int depth, bool all, CancellationToken token, int threads = 2)
    {
        var legal = MoveGenerator.GenerateLegal(p);
        if (legal.Count == 0)
            return new EngineResult();
        var r = await engine.AnalyzeAsync(p.ToFen(), depth, all ? legal.Count : 1, threads, 256, token, unlimited: true);
        token.ThrowIfCancellationRequested();
        if (r.Error is not null)
            throw new IOException(r.Error);
        var lines = r.Lines.Where(l => l.BestMove is not null).GroupBy(l => l.BestMove!).ToDictionary(g => g.Key, g => g.Last());
        // Stockfish may end a mate search before the nominal depth. Missing or
        // incomplete non-mate lines are searched individually, never dropped.
        foreach (var m in (all ? legal : legal.Where(m => m.Uci == r.BestMove).ToList()))
        {
            if (lines.TryGetValue(m.Uci, out var l) && (l.Depth >= depth || l.Mate is not null))
                continue;
            var one = await engine.AnalyzeAsync(p.ToFen(), depth, 1, threads, 256, token, unlimited: true, searchMoves: m.Uci);
            token.ThrowIfCancellationRequested();
            if (one.Error is not null)
                throw new IOException(one.Error);
            var line = one.Lines.FirstOrDefault();
            if (line is null || line.Depth < depth && line.Mate is null)
                throw new IOException("Engine beendet die Tiefensuche ohne vollständiges Ergebnis; kein Zeitlimit aktiv.");
            lines[m.Uci] = line;
        }

        var sorted = lines.Values.OrderByDescending(OpeningGenerator.Score).ToList();
        if (sorted.Count == 0)
            throw new IOException("Keine auswertbare Engineantwort.");
        return new EngineResult
        {
            BestMove = sorted[0].BestMove,
            ScoreCentipawns = sorted[0].ScoreCentipawns,
            Mate = sorted[0].Mate,
            Lines = sorted
        };
    }

    public static async Task ExtendVariations(StockfishEngine engine, Position root, EngineResult result, CancellationToken token, int minimum = 10)
    {
        await Task.WhenAll(result.Lines.Take(3).Where(l => !l.ContinuationChecked).Select(async line =>
        {
            var p = root.Clone();
            var valid = new List<string>();
            var seen = new Dictionary<string, int>
            {
                [p.RepetitionKey()] = 1
            };
            void Seen()
            {
                string key = p.RepetitionKey();
                seen[key] = seen.GetValueOrDefault(key) + 1;
            }

            foreach (var u in line.Pv.ToArray())
            {
                var m = MoveGenerator.GenerateLegal(p).FirstOrDefault(m => m.Uci == u);
                if (m is null)
                    break;
                valid.Add(u);
                p.Apply(m);
                Seen();
            }

            line.Pv.Clear();
            line.Pv.AddRange(valid);
            for (int tries = 0; line.Pv.Count < minimum && tries < minimum; tries++)
            {
                token.ThrowIfCancellationRequested();
                var state = MoveGenerator.GameState(p, seen);
                if (state != "" && state != "Schach")
                {
                    line.ContinuationNote = state;
                    break;
                }

                var r = await engine.AnalyzeAsync(p.ToFen(), 14, 1, 1, 64, token, 450);
                token.ThrowIfCancellationRequested();
                if (r.Error is not null)
                {
                    line.ContinuationNote = r.Error;
                    break;
                }

                var continuation = r.Lines.FirstOrDefault()?.Pv ?? (r.BestMove is string b ? new List<string>
                {
                    b
                }

                : []);
                bool moved = false;
                foreach (var u in continuation.Take(minimum - line.Pv.Count))
                {
                    var m = MoveGenerator.GenerateLegal(p).FirstOrDefault(m => m.Uci == u);
                    if (m is null)
                        break;
                    p.Apply(m);
                    line.Pv.Add(u);
                    moved = true;
                    Seen();
                    if (seen[p.RepetitionKey()] >= 3)
                    {
                        line.ContinuationNote = "Dreifache Wiederholung in der Variante";
                        line.ContinuationChecked = true;
                        return;
                    }
                }

                if (!moved)
                {
                    line.ContinuationNote = "Keine weitere PV geliefert";
                    break;
                }
            }

            line.ContinuationChecked = true;
        }));
    }

    public static async Task<(bool Accepted, EngineResult? Defense, string Reason)> CheckMateMove(StockfishEngine engine, Position p, Move move, int remaining, CancellationToken token)
    {
        var after = p.Clone();
        after.Apply(move);
        var legal = MoveGenerator.GenerateLegal(after);
        if (legal.Count == 0)
            return (MoveGenerator.IsKingInCheck(after, after.WhiteToMove), null, "");
        if (remaining <= 1)
            return (false, null, "Das Mattziel wird mit diesem Zug nicht erreicht.");
        var r = await engine.AnalyzeAsync(after.ToFen(), Math.Max(22, remaining * 3), 1, 4, 256, token, 6000);
        token.ThrowIfCancellationRequested();
        return (r.Mate is < 0 && -r.Mate <= remaining - 1, r, r.Error ?? "Ein erzwungenes Matt innerhalb des Zuglimits wurde nicht bestätigt.");
    }
}
