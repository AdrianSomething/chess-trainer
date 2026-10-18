namespace ChessOpeningTrainer;

public sealed class SafeSquareForm : VisionChallengeForm
{
    private readonly BoardControl board = new() { Dock = DockStyle.Fill };
    private Position position = new();
    private int origin;
    private HashSet<int> safe = [];
    private readonly VisionBuffer<VisionSafetyExercise> buffer;
    private readonly bool ownsBuffer;
    private readonly CheckBox hideDestinations = new() { Text = "Mögliche Zielfelder ausblenden", Dock = DockStyle.Top, Height = 30 };
    public SafeSquareForm(string directory, VisionBuffer<VisionSafetyExercise>? preparedBuffer = null) : base(directory, 3, "Figur retten",
        "Rette die markierte Figur auf ein sicheres Feld, das keine gegnerische Figur angreift.")
    {
        ownsBuffer = preparedBuffer is null;
        buffer = preparedBuffer ?? new(VisionSafety.Generate, e => e.Position.ToFen() + "|" + e.Origin, Lifetime.Token);
        AddView(board);
        Controls.Add(hideDestinations);
        hideDestinations.CheckedChanged += (_, _) => UpdateDestinations();
        board.SquareClicked += Answer;
        board.SquareDragged += (from, to) => { if (from == origin) Answer(to); };
    }
    protected override async Task Prepare()
    {
        var result = await buffer.Next(Lifetime.Token, Round % 2 == 0);
        await this.InvokeAsync(() =>
        {
            position = result.Position; origin = result.Origin; safe = result.Destinations;
            board.MarkedSquares.Clear(); board.Arrows.Clear(); board.ErrorArrows.Clear(); board.ErrorSquares.Clear();
            BoardPresentation.Preview(board, position, !position.WhiteToMove);
            board.InputEnabled = true; board.HintMove = new() { From = origin, To = origin };
            board.SelectedSquare = origin;
            UpdateDestinations();
        }, Lifetime.Token);
    }
    private void Answer(int square)
    {
        if (!CanAnswer || square == origin) return;
        bool good = safe.Contains(square);
        if (!good)
        {
            bool legal = MoveGenerator.GenerateLegal(position).Any(m => m.From == origin && m.To == square);
            Statistics.Miss(3, position.Squares[origin], legal ? "Zielfeld bleibt angegriffen" : "Illegales Zielfeld");
            board.ErrorSquares.Add(square);
        }
        else
        {
            var move = MoveGenerator.GenerateLegal(position).First(m => m.From == origin && m.To == square);
            var next = position.Clone(); next.Apply(move);
            board.Position = next; board.HintMove = move;
        }
        board.MarkedSquares.Clear(); board.MarkedSquares.UnionWith(safe); board.Invalidate();
        Finish(good ? 0 : 1, good ? 1 : 0, good ? "Richtig. Sichere Zielfelder sind orange markiert." : "Dieses Feld ist nicht sicher oder der Zug ist illegal. Sichere Felder sind orange markiert.");
    }
    private void UpdateDestinations()
    {
        board.ShowDestinationHints = !hideDestinations.Checked;
        board.CandidateMoves = hideDestinations.Checked ? [] : MoveGenerator.GenerateLegal(position).Where(m => m.From == origin).ToList();
        board.Invalidate();
    }
    protected override void DisposeTrainingResources() { if (ownsBuffer) buffer.Dispose(); }
}
