namespace ChessOpeningTrainer;

public sealed partial class FreeGameForm
{
    private void PreviewRoute(IReadOnlyList<Position> route)
    {
        if (route.Count == 0) return;
        if (Display.ToFen() != route[0].ToFen()) exploration.Push(Display.Clone());
        foreach (var state in route.Take(route.Count - 1)) exploration.Push(state.Clone());
        SetPreview(route[^1], false);
    }

    private async Task ContinueFromDisplayedPosition()
    {
        if (!review || busy) return;
        var desired = Display.Clone();
        var recorded = states.Append(position).ToList();
        int anchor = recorded.FindLastIndex(p => p.ToFen() == desired.ToFen());
        var extension = new List<Position>();
        if (anchor < 0)
        {
            var route = exploration.Reverse().Append(desired).ToArray();
            for (int i = route.Length - 2; i >= 0 && anchor < 0; i--)
            {
                anchor = recorded.FindLastIndex(p => p.ToFen() == route[i].ToFen());
                if (anchor >= 0) extension.AddRange(route.Skip(i + 1));
            }
        }
        if (anchor < 0) return;
        var nextStates = recorded.Take(anchor + 1).Select(p => p.Clone()).ToList();
        var nextMoves = moves.Take(anchor).ToList();
        foreach (var after in extension)
        {
            var before = nextStates[^1];
            if (before.ToFen() == after.ToFen()) continue;
            var move = HistoryPlayback.FindMove(before, after, LegalMoves(before));
            if (move is null) return; // Never silently discard an unconnected exploration.
            nextMoves.Add(move.Uci); nextStates.Add(after.Clone());
        }
        Cancel();
        position = nextStates[^1].Clone();
        states.Clear(); states.AddRange(nextStates.Take(nextStates.Count - 1));
        moves.Clear(); moves.AddRange(nextMoves);
        explored = null; review = false; exploration.Clear();
        history.Items.Clear(); history.Items.Add("Startstellung");
        for (int i = 0; i < moves.Count; i++)
        {
            var before = nextStates[i];
            var move = LegalMoves(before).First(m => m.Uci == moves[i]);
            history.Items.Add($"{before.Fullmove}{(before.WhiteToMove ? "." : "…")} {kingRules?.Notation(before, move) ?? SanNotation.Format(before, move)}");
        }
        Clear(); Repetitions(); RefreshPosition();
        await EngineTurn();
    }
}
