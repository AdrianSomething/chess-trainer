using ChessMove = ChessOpeningTrainer.Move;

namespace ChessOpeningTrainer;
public sealed partial class MainForm : LocalizedForm
{
    private readonly CheckBox showDatasetStats = new() { Text = "Partiestatistik", Width = 125, Height = 24 };
    private readonly DatasetResultsStrip datasetStats = new();
    private DatasetFrequencyBinding? datasetBinding;
    private readonly string dataDir = Environment.GetEnvironmentVariable("CHESS_TRAINER_DATA") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChessOpeningTrainer");
    private readonly ProgressStore progress;
    private readonly Button branch = Button("In Branch anzeigen");
    private readonly NumericUpDown learningDepth = new ScrollNumber { Minimum = 1, Maximum = 40, Value = 22, Width = 70 };
    private readonly NumericUpDown learningIntelligence = new ScrollNumber { Maximum = 100, Value = 100, Width = 70 };
    private Control? opponentSettings;
    private readonly CheckBox automaticEngine = new() { Text = "Automatisches Engine-Spiel", AutoSize = true };
    private readonly ComboBox difficulty = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 140
    };
    private readonly StockfishEngine engine = new(AppContext.BaseDirectory);
    private readonly PuzzleBank puzzles;
    private readonly CancellationTokenSource lifetime = new();
    private CancellationTokenSource analysis = new();
    private CancellationTokenSource opponent = new();
    private readonly Panel home = new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true
    };
    private readonly Panel game = new()
    {
        Dock = DockStyle.Fill,
        Visible = false
    };
    private readonly BoardControl board = new();
    private readonly EvalScale evaluation = new();
    private readonly Label title = Label(17), book = Label(11), feedback = Label(14);
    private readonly Button next = Button("Nächster Lernzug  ↵"), reveal = Button("Lösung zeigen"), retry = Button("Erneut versuchen"), reset = Button("Anfangsstellung"), regenerate = Button("Andere Antwort"), resume = Button("Zur aktuellen Stellung");
    private readonly CheckBox suggestions = new()
    {
        Text = "Top 3 + Varianten anzeigen",
        AutoSize = true,
        ForeColor = UiPalette.Gainsboro
    };
    private readonly VariationRows variations = new();
    private readonly MoveHistory history = new()
    {
        IntegralHeight = false
    };
    private Position? explored;
    private Position Display => review && explored is not null ? explored : position;

    private readonly Dictionary<string, EngineResult> analysisCache = new();
    private AnalysisPresenter? presenter;
    private bool draggingMove;
    private bool settingSuggestions;
    private bool? suggestionPreference;
    private readonly Button exploreBack = Button("Aktuelle Stellung neu setzen");
    private readonly Button freeFromHere = Button("Ab hier weiterspielen");
    private readonly Stack<Position> explorationHistory = new();
    private readonly Label variantCaption = Label(10), historyCaption = Label(10);
    private readonly FlowLayoutPanel actions = new()
    {
        WrapContents = true
    };
    private OpeningDefinition? opening;
    private Position position = Position.FromFen(Position.InitialFen);
    private OpeningNode? node;
    private string path = "";
    private string mode = "";
    private bool humanWhite, inTree, busy, question, failed, review;
    private bool waitingReply;
    private ChessMove? premove;
    private string? targetKey;
    private int revision;
    private int? selected;
    private int regenIndex = -1;
    private string? lastOpponent;
    private readonly List<Snapshot> snapshots = [];
    private readonly Dictionary<string, int> repetitions = new();
    private bool listUpdating;
    private Puzzle? puzzle;
    private int puzzlePly;
    private sealed record Snapshot(Position Position, OpeningNode? Node, string Path, bool InTree, string Text);
    private sealed record Preview(Position Position, string Text)
    {
        public override string ToString() => Text;
    }

    private readonly List<OpeningStore.LearningTarget> targets = [];
    private bool awaitingContinuation;
    private OpeningGeneratorForm? generatorWindow;
    private PuzzleSessionForm? puzzleWindow;
    private GameLibraryForm? gamesWindow;
    private readonly ComboBox cardVariations = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 330
    };
    private List<OpeningStore.LearningTarget> currentGroup = [];
    private OpeningStore.LearningTarget? forcedTarget;
    private bool updatingCardVariations;
    private readonly Queue<OpeningStore.LearningTarget> branchLessons = [];
    private readonly bool learningOnly;
    private readonly ProgressStrip lessonProgress = new();
    public MainForm(bool learningOnly = false, ProgressStore? sharedProgress = null)
    {
        historyPlayback = new(board);
        this.learningOnly = learningOnly;
        Text = learningOnly ? "Lernmodus" : "Chess Opening Trainer";
        FormClosed += (_, _) => presenter?.Dispose();
        AnalysisSearch.Warm(engine);
        MinimumSize = new Size(1000, 740);
        Size = new Size(1250, 860);
        BackColor = UiPalette.FromArgb(31, 35, 39);
        ForeColor = UiPalette.Gainsboro;
        Font = new Font("Segoe UI", 10);
        progress = sharedProgress ?? new ProgressStore(dataDir);
        if (!learningOnly)
        {
            AppSounds.Initialize(dataDir);
#if PUBLIC_RELEASE
            BundledSampleGame.Initialize(dataDir);
#endif
        }
        difficulty.Items.AddRange(["Alle", "Leicht", "Normal", "Schwer"]);
        difficulty.SelectedIndex = 0;
        supply = new PuzzleSupply(engine, dataDir, AppContext.BaseDirectory);
        puzzles = supply.Bank;
        Controls.Add(home);
        Controls.Add(game);
        var back = new MenuIconButton("back") { AccessibleName = "Zurück zu Openings" };
        back.SetBounds(10, 10, 38, 34);
        back.Click += (_, _) =>
        {
            if (learningOnly)
            {
                Close();
                return;
            }

            InvalidateWork();
            game.Visible = false;
            home.Visible = true;
            LoadHome();
        };
        title.SetBounds(60, 14, 900, 32);
        feedback.Padding = new Padding(10);
        feedback.BackColor = UiPalette.FromArgb(44, 51, 57);
        foreach (var list in new[]
        {
            history
        }

        )
        {
            list.BackColor = UiPalette.FromArgb(43, 49, 55);
            list.ForeColor = UiPalette.Gainsboro;
            list.BorderStyle = BorderStyle.None;
            list.HorizontalScrollbar = true;
        }

        variantCaption.Text = "Enginevarianten · Schritte zum Anschauen anklicken";
        historyCaption.Text = "Zugfolge · Stellung zum Anschauen anklicken";
        game.Controls.AddRange([back, title, board, evaluation, book, feedback, suggestions, variations, history, actions, variantCaption, historyCaption]);
        game.Controls.AddRange([showDatasetStats, datasetStats]);
        datasetBinding = new(board, datasetStats, () => showDatasetStats.Checked && (mode == "Freeplay" || mode == "Lernen" && !question), dataDir);
        showDatasetStats.CheckedChanged += (_, _) => { datasetBinding.Refresh(); LayoutGame(); };
        actions.Controls.AddRange([retry, regenerate]);
        game.Controls.AddRange([branch, exploreBack, automaticEngine, reveal, reset, freeFromHere]);
        opponentSettings = EngineOpponentControls.Create(learningDepth, learningIntelligence);
        game.Controls.Add(opponentSettings);
        game.Controls.Add(resume);
        game.Controls.Add(next);
        game.Controls.Add(lessonReturn);
        resume.AutoSize = false;
        resume.VisibleChanged += (_, _) => LayoutGame();
        freeFromHere.Click += (_, _) =>
        {
            ContinueFreeplayHere();
        };
        automaticEngine.CheckedChanged += (_, _) =>
        {
            if (mode != "Lernen") return;
            InvalidateWork();
            ClearSelection();
            if (automaticEngine.Checked) { awaitingContinuation = false; board.Arrows.Clear(); }
            if (!review) Advance();
            else RefreshBoard();
        };
        branch.Click += (_, _) =>
        {
            if (opening is not null)
                OpenBranch(opening, NearestOpeningPosition());
        };
        game.Controls.Add(lessonProgress);
        game.Controls.Add(cardVariations);
        cardVariations.SelectionChangeCommitted += (_, _) =>
        {
            if (!updatingCardVariations && question && cardVariations.SelectedIndex >= 0)
            {
                forcedTarget = currentGroup[cardVariations.SelectedIndex];
                NextQuestion();
            }
        };
        exploreBack.Click += (_, _) => ResetCurrentLessonPosition();
        foreach (Button b in actions.Controls)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.BackColor = UiPalette.FromArgb(31, 35, 39);
            b.ForeColor = UiPalette.Gainsboro;
            b.Width = 164;
            b.Height = 36;
            b.Margin = new Padding(0, 0, 8, 8);
        }
        foreach (var b in new[] { next, resume, lessonReturn, branch, exploreBack, reveal, reset, freeFromHere })
        { b.AutoSize = false; b.FlatStyle = FlatStyle.Flat; b.BackColor = UiPalette.FromArgb(31,35,39); b.ForeColor = UiPalette.Gainsboro; }
        lessonReturn.Click += async (_, _) => { if (LearningReturnIndex >= 0) await PreviewHistoryAsync(LearningReturnIndex, HistoryNavigationKind.Immediate); };

        next.Click += (_, _) =>
        {
            if (mode == "Rätsel")
                StartPuzzle();
            else if (mode == "Lernen" && question)
            {
                if (review) PreviewBoard(position);
                Reveal();
            }
            else
                NextQuestion();
        };
        reveal.Click += (_, _) => Reveal();
        retry.Click += (_, _) =>
        {
            failed = true;
            Feedback("Noch einmal – wähle deinen Zug.", null);
        };
        reset.Click += (_, _) =>
        {
            if (opening is not null)
                Start(opening, mode);
        };
        resume.Click += (_, _) => { PreviewBoard(position); ResumeAutomaticPlayAfterReview(); };
        variations.NavigateRoute += PreviewVariation;
        suggestions.CheckedChanged += (_, _) =>
        {
            if (!settingSuggestions)
                suggestionPreference = suggestions.Checked;
            variations.Visible = suggestions.Checked && (!question || review) && !HideUnansweredLines;
            LayoutGame();
            Analyze();
        };
        regenerate.Click += (_, _) => Regenerate();
        history.NavigationRequested += async (target, kind) => await PreviewHistoryAsync(target, kind);
        history.SelectedIndexChanged += async (_, _) =>
        {
            if (!listUpdating && !history.IsNavigationInput && history.SelectedIndex >= 0)
                await PreviewHistoryAsync(history.SelectedIndex, HistoryNavigationKind.Animated);
        };
        board.SquareClicked += ClickSquare;
        board.SquareDragged += DragSquare;
        board.BlankClicked += () =>
        {
            premove = null;
            board.QueuedMove = null;
            ClearSelection();
        };
        Resize += (_, _) => LayoutGame();
        FormClosed += (_, _) =>
        {
            lifetime.Cancel();
            InvalidateWork();
        };
        FormClosing += (_, e) =>
        {
            if (generatorWindow?.IsWorking == true)
            {
                e.Cancel = true;
                generatorWindow.Activate();
                L.Message(this, "Die Generierung läuft noch. Bitte im Generator zuerst abschließen oder abbrechen und den Teilbaum speichern.", "Generierung aktiv");
            }
        };
        FormClosed += (_, _) =>
        {
            if (learningOnly)
                return;
            foreach (var window in Application.OpenForms.Cast<Form>().Where(f => f != this).ToArray())
                window.Close();
        };
        Shown += (_, _) =>
        {
            if (!learningOnly)
            {
                StartSupply();
                if (!Preferences.Current.TutorialOffered) OfferTutorial();
            }
        };
        LoadHome();
        FormClosed += (_, _) => tutorial?.Dispose();
    }

    private static Label Label(float size) => new()
    {
        ForeColor = UiPalette.Gainsboro,
        Font = new Font("Segoe UI", size),
        AutoSize = false
    };
    private static Button Button(string text) => new()
    {
        Text = text,
        FlatStyle = FlatStyle.Flat,
        BackColor = UiPalette.FromArgb(57, 91, 119),
        ForeColor = Color.White,
        Cursor = Cursors.Hand
    };
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (game.Visible && mode == "Lernen" && question && keyData == Keys.Space)
        {
            if (!busy)
            {
                if (review)
                    PreviewBoard(position);
                Reveal();
            }

            return true;
        }

        if (game.Visible && next.Visible && next.Enabled && (keyData == Keys.Space || keyData == Keys.Enter && mode != "Lernen"))
        {
            next.PerformClick();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void InvalidateWork()
    {
        historyPlayback.Cancel();
        revision++;
        waitingReply = false;
        premove = null;
        board.QueuedMove = null;
        analysis.Cancel();
        opponent.Cancel();
        busy = false;
        board.StopAnimation();
        analysis.Dispose();
        opponent.Dispose();
        analysis = new();
        opponent = new();
    }
}
