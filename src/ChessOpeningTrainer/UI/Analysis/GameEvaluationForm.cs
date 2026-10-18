namespace ChessOpeningTrainer;

public sealed class GameEvaluationForm : LocalizedForm
{
    private readonly GameEvaluationGraph graph = new() { Dock = DockStyle.Fill };
    private readonly Label selected = new() { Dock = DockStyle.Bottom, Height = 32, TextAlign = ContentAlignment.MiddleCenter };
    private PersonalGame? game;
    private List<EngineResult>? analysis;
    private bool complete;
    public event Action<int>? PositionSelected;
    public GameEvaluationGraph Graph => graph;

    public GameEvaluationForm()
    {
        Text = "Evaluationsgraph";
        Size = new(900, 470); MinimumSize = new(480, 300);
        BackColor = UiPalette.FromArgb(31, 35, 39); ForeColor = UiPalette.Gainsboro;
        Controls.Add(graph); Controls.Add(selected);
        graph.PositionSelected += i => PositionSelected?.Invoke(i);
    }

    public void Synchronize(PersonalGame selectedGame, int positionIndex, bool whiteAtBottom)
    {
        if (!ReferenceEquals(game, selectedGame) || !ReferenceEquals(analysis, selectedGame.PositionAnalyses) || complete != selectedGame.HasCompleteAnalysis)
        {
            game = selectedGame; analysis = selectedGame.PositionAnalyses;
            complete = selectedGame.HasCompleteAnalysis;
            graph.Samples = GameEvaluationSeries.Build(selectedGame);
            Text = "Evaluationsgraph · " + selectedGame.White + " – " + selectedGame.Black;
        }
        graph.CurrentIndex = positionIndex;
        graph.WhiteAtBottom = whiteAtBottom;
        selected.Text = graph.Samples.Count == 0 ? "Partie zuerst vollständig analysieren."
            : $"{L.T("Stellung")} {graph.CurrentIndex} · {graph.Samples[graph.CurrentIndex].Display} ({L.T("Weißsicht")})";
    }
}
