namespace ChessOpeningTrainer;
public static class EndgameJudge
{
    public static async Task<bool> Accept(StockfishEngine engine, Position before, Move move, bool drawGoal, bool shortest, CancellationToken token)
    {
        var after = before.Clone();
        after.Apply(move);
        string state = MoveGenerator.GameState(after, new Dictionary<string, int>());
        if (state == "Schachmatt")
            return !drawGoal;
        if (state.StartsWith("Remis"))
            return drawGoal;
        var own = engine.AnalyzeAsync(before.ToFen(), 22, 3, 2, 128, token, 1000);
        var reply = engine.AnalyzeAsync(after.ToFen(), 22, 1, 2, 128, token, 1000);
        await Task.WhenAll(own, reply);
        token.ThrowIfCancellationRequested();
        var a = own.Result;
        var b = reply.Result;
        if (a.Error is not null || b.Error is not null)
            throw new IOException(a.Error ?? b.Error);
        double best = PersonalGameStore.Value(a), played = -PersonalGameStore.Value(b);
        if (drawGoal && played < -.35 || !drawGoal && played < 1.0 && b.Mate is not < 0)
            return false;
        if (shortest)
        {
            if (a.Mate is> 0)
                return b.Mate is < 0 && -b.Mate <= a.Mate - 1;
            return best - played <= .10;
        }

        return best - played <= .8;
    }
}
