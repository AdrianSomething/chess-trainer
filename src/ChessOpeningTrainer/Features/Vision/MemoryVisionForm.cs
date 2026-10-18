namespace ChessOpeningTrainer;

public sealed class MemoryVisionForm : VisionChallengeForm
{
    private readonly BoardControl board = new() { Dock = DockStyle.Fill, InputEnabled = false };
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 100 };
    private Position target = new();
    private PositionSetupControl? editor;
    private readonly VisionBuffer<Position> buffer;
    private readonly bool ownsBuffer;
    private readonly int level;
    public MemoryVisionForm(string directory, int difficulty = 1, VisionBuffer<Position>? preparedBuffer = null) : base(directory, 5 + Math.Clamp(difficulty, 0, 2), "Figuren merken · " + (difficulty == 0 ? "Leicht" : difficulty == 2 ? "Schwer" : "Mittel"),
        "Merke dir die Figuren und ihre Felder. Baue die Stellung anschließend möglichst genau wieder auf.")
    {
        level = Math.Clamp(difficulty, 0, 2);
        ownsBuffer = preparedBuffer is null;
        buffer = preparedBuffer ?? new((white, ct) => VisionMemory.Generate(level, white, ct), p => p.ToFen(), Lifetime.Token);
        AddView(board);
        timer.Tick += (_, _) =>
        {
            if (!CanAnswer || editor is not null) return;
            double left = Math.Max(0, 10 - Clock.Elapsed.TotalSeconds);
            Status.Text = $"Aufgabe {Round + 1}/30 · Merken: {left:0.0} s";
            if (left > 0) return;
            BeginReconstruction();
        };
        timer.Start();
    }
    private void BeginReconstruction()
    {
        if (editor is not null || !CanAnswer) return;
        board.Visible = false;
        editor = new(new Position { Castling = "", WhiteToMove = target.WhiteToMove }, reconstruction: true, flipped: !target.WhiteToMove) { Dock = DockStyle.Fill };
        AddView(editor); editor.Accepted += Answer; EnableAnswerConfirmation();
        Clock.Restart();
        Status.Text = "Baue die gemerkte Stellung wieder auf und bestätige den Aufbau.";
    }
    protected override async Task Prepare()
    {
        editor?.Dispose(); editor = null;
        var generated = await buffer.Next(Lifetime.Token, Round % 2 == 0);
        await this.InvokeAsync(() =>
        {
            target = generated;
            BoardPresentation.Preview(board, target, !target.WhiteToMove); board.Visible = true;
        }, Lifetime.Token);
    }
    protected override void ConfirmAnswer()
    {
        if (editor is not null && CanAnswer) Answer(editor.Result.Clone());
    }
    protected override double ActiveAnswerSeconds => editor is null ? 0 : base.ActiveAnswerSeconds;
    protected override string TimerText => Playing && editor is null ? $"{Math.Max(0, 10 - Clock.Elapsed.TotalSeconds):0.0} s" : base.TimerText;
    private void Answer(Position answer)
    {
        if (!CanAnswer) return;
        var wrong = Enumerable.Range(0, 64).Where(i => target.Squares[i] != answer.Squares[i]).ToList();
        foreach (int square in wrong)
        {
            char expected = target.Squares[square], actual = answer.Squares[square];
            string cause = expected == '\0' ? "Zusätzliche Figur auf leerem Feld"
                : actual == '\0' ? "Figur nicht wiederaufgebaut"
                : char.ToLowerInvariant(expected) != char.ToLowerInvariant(actual) ? "Falscher Figurentyp" : "Falsche Figurenfarbe";
            Statistics.Miss(5 + level, expected == '\0' ? actual : expected, cause);
        }
        editor!.Visible = false; board.Visible = true; board.MarkedSquares.Clear(); board.ErrorSquares.UnionWith(wrong); board.Invalidate();
        Finish(wrong.Count, Enumerable.Range(0, 64).Count(i => target.Squares[i] != '\0' && target.Squares[i] == answer.Squares[i]),
            $"{wrong.Count} Felder falsch. Die Originalstellung ist sichtbar; abweichende Felder sind rot.");
    }
    protected override void DisposeTrainingResources()
    {
        timer.Dispose(); if (ownsBuffer) buffer.Dispose();
    }
}
