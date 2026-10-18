namespace ChessOpeningTrainer;

public sealed partial class PuzzleSessionForm
{
    private readonly Button openingBranch = new() { Text = "Im Branch anzeigen", AutoSize = true, Visible = false };

    private void AddPuzzleHistory()
    {
        var prefix = new List<(Position Position, string Label)>();
        if (puzzle?.SetupFen is string setupFen && puzzle.SetupMoves.Count > 0)
        {
            var current = Position.FromFen(setupFen);
            prefix.Add((current.Clone(), "Ausgangsstellung"));
            foreach (string uci in puzzle.SetupMoves)
            {
                var move = MoveGenerator.GenerateLegal(current).FirstOrDefault(m => m.Uci == uci);
                if (move is null) { prefix.Clear(); break; }
                string label = $"{current.Fullmove}{(current.WhiteToMove ? "." : "…")} {SanNotation.Format(current, move)}";
                current.Apply(move);
                prefix.Add((current.Clone(), label));
            }
            // Only include a legal prefix that reaches this exact exercise.
            if (current.ToFen() != position.ToFen()) prefix.Clear();
        }
        if (prefix.Count == 0) AddSnapshot("Rätselstellung");
        else
            for (int i = 0; i < prefix.Count; i++)
                AddSnapshot(prefix[i].Label + (i == prefix.Count - 1 ? " · Rätselstellung" : ""), prefix[i].Position);
    }
}
