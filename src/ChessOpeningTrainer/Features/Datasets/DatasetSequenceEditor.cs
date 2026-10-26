namespace ChessOpeningTrainer;

/// <summary>Filter-prefix editor using the shared board and analysis presentation.</summary>
public sealed class DatasetSequenceEditor : UserControl
{
    private readonly BoardControl board = new();
    private readonly EvalScale scale = new();
    private readonly AnalysisPresenter analysis;
    private readonly VariationRows hiddenLines = new();
    private readonly List<Position> positions = [Position.FromFen(Position.InitialFen)];
    private readonly List<string> moves = [];
    private int? selected;
    public string[] Moves => moves.ToArray();
    public event Action? Changed;
    public DatasetSequenceEditor(StockfishEngine engine)
    {
        Height = 420;
        Width = 490;
        analysis = new(engine, scale, hiddenLines);
        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 45 };
        bar.Controls.Add(DatasetControls.Button("Zug zurück", () => { if (moves.Count > 0) { moves.RemoveAt(moves.Count - 1); positions.RemoveAt(positions.Count - 1); RefreshBoard(); } }, 170));
        bar.Controls.Add(DatasetControls.Button("Grundstellung", () => { moves.Clear(); positions.RemoveRange(1, positions.Count - 1); RefreshBoard(); }, 170));
        Controls.AddRange([board, scale, bar]);
        Resize += (_, _) => { int size = Math.Min(Width - 48, Height - 50); board.SetBounds(45, 48, size, size); scale.SetBounds(2, 68, 38, Math.Max(50, size - 40)); };
        board.SquareClicked += square => { if (selected is int from && Play(from, square)) return; selected = square; board.SelectedSquare = square; board.CandidateMoves = MoveGenerator.GenerateLegal(positions[^1]).Where(m => m.From == square).ToList(); board.Invalidate(); };
        board.SquareDragged += (from, to) => Play(from, to);
        board.BlankClicked += () => { selected = null; BoardPresentation.ClearSelection(board); };
        VisibleChanged += (_, _) => { if (Visible) RefreshBoard(); };
        BoardPresentation.Preview(board, positions[0], false);
    }
    private bool Play(int from, int to)
    {
        var options = MoveGenerator.GenerateLegal(positions[^1]).Where(m => m.From == from && m.To == to).ToList();
        if (options.Count == 0)
            return false;
        var move = options.Count == 1 ? options[0] : board.ChoosePromotion(FindForm()!, options, positions[^1].WhiteToMove);
        if (move is null)
            return false;
        var next = positions[^1].Clone();
        next.Apply(move);
        moves.Add(move.Uci);
        positions.Add(next);
        RefreshBoard();
        return true;
    }
    private void RefreshBoard()
    {
        selected = null;
        BoardPresentation.ClearSelection(board);
        BoardPresentation.Preview(board, positions[^1], false);
        analysis.Show(positions[^1], true, false, true, "");
        Changed?.Invoke();
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            analysis.Dispose();
            hiddenLines.Dispose();
        }
        base.Dispose(disposing);
    }
}
