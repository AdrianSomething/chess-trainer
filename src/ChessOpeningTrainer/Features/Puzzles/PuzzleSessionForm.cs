namespace ChessOpeningTrainer;
public sealed partial class PuzzleSessionForm : LocalizedForm
{
    private readonly PuzzleSupply supply;
    private OpeningMistakeTrainer? openingMistakes;
    private bool openingMistakesMode;
    private readonly EndgameDecisions decisions;
    private readonly PositionalTrainer positionalTrainer;
    private readonly CheckBox positional = Check("Mittelspiel: klare Stellungszüge", false), positionalSubtle = Check("Mittelspiel: feine Unterschiede (tiefere Suche)", false);
    private readonly NumericUpDown minGood = Number(2, 8, 2), maxGood = Number(2, 8, 4);
    private List<(PersonalGame Game, GameMoment Moment)> mistakes = [];
    private int mistakeIndex;
    private bool mistakesMode;
    private readonly StockfishEngine engine;
    private readonly PuzzleBank bank;
    private readonly PuzzleProfiles profiles;
    private readonly EndgameFactory endgames;
    private readonly CancellationTokenSource lifetime = new();
    private CancellationTokenSource work = new(), analysis = new();
    private readonly TabPage endgameSetup = new("Endspieltraining");
    private readonly ComboBox sideChoice = new()
    {
        Width = 220,
        DropDownStyle = ComboBoxStyle.DropDownList
    };
    private readonly CheckBox optimal = Check("Kürzesten Lösungsweg verlangen (Engine-Näherung)", false);
    private readonly Button whole = new()
    {
        Text = "Gesamte Lösung anzeigen",
        AutoSize = true
    };
    private bool waitingReply;
    private Move? premove;
    private bool onSolutionLine;
    private readonly HashSet<int> trainedPieces = [];
    private int[] pieceIds = Enumerable.Range(0, 64).ToArray();
    private int supportingMoves;
    private bool IsPawnMode => puzzle?.GoalKind == "" && kind is "Bauernverteidigung" or "Bauernumwandlung";
    private bool IsAdvanced => puzzle?.GoalKind.StartsWith("Advanced") == true;

    private bool showingSolution;
    private long autoNextAt;
    private readonly AnalysisPresenter presenter;
    private readonly TabControl tabs = new PageDeck()
    {
        Dock = DockStyle.Fill
    };
    private readonly TabPage setup = new("Rätsel-Einstellungen"), play = new("Training");
    private readonly BoardControl board = new();
    private readonly EvalScale evaluation = new();
    private readonly VariationRows variants = new();
    private readonly Label objective = new()
    {
        ForeColor = Color.White,
        Font = new Font("Segoe UI", 19, FontStyle.Bold)
    };
    private readonly Label info = new()
    {
        ForeColor = UiPalette.Gainsboro
    }, stats = new()
    {
        AutoSize = true,
        ForeColor = UiPalette.LightSkyBlue
    };
    private readonly TrainingScoreboard clock = new() { ForeColor = UiPalette.Gainsboro };
    private readonly CheckBox continueTimeout = Check("Zugzeit abgelaufen: −1 Punkt statt Abbruch", false);
    private readonly Button next = new()
    {
        Text = "Nächstes Rätsel",
        AutoSize = true
    }, solution = new()
    {
        Text = "Lösung zeigen",
        AutoSize = true
    }, resume = new()
    {
        Text = "Zur aktuellen Stellung",
        AutoSize = true
    };
    private readonly ComboBox presets = new()
    {
        Width = 500,
        DropDownStyle = ComboBoxStyle.DropDownList
    }, difficulty = new()
    {
        Width = 200,
        DropDownStyle = ComboBoxStyle.DropDownList
    };
    private readonly TextBox name = new()
    {
        Width = 500
    };
    private readonly NumericUpDown perMove = Number(0, 3600, 0), total = Number(0, 86400, 0), lives = Number(0, 7, 0), defense = Number(3, 50, 10);
    private readonly CheckBox revealError = Check("Bei Fehler Lösungszug anzeigen", false), tactics = Check("Taktikrätsel einbeziehen", true), mate = Check("Matt in N", true), material = Check("Materialgewinn", true), draw = Check("Remis sichern (auch komplexere Kompositionen)", true);
    private readonly CheckedListBox modes = new()
    {
        Width = 520,
        Height = 145,
        CheckOnClick = true,
        BackColor = UiPalette.FromArgb(43, 49, 55),
        ForeColor = UiPalette.Gainsboro
    };
    private readonly CheckBox fullEndgames = Check("Figurenendspiele ganz bis zum Matt ausspielen (sonst kurze Mattaufgaben)", true);
    private readonly MoveHistory history = new()
    {
        BackColor = UiPalette.FromArgb(43, 49, 55),
        ForeColor = UiPalette.Gainsboro
    };
    private readonly List<Position> snapshots = [];
    private readonly Dictionary<string, int> repetitions = [];
    private readonly System.Windows.Forms.Timer timer = new()
    {
        Interval = 100
    };
    private readonly System.Diagnostics.Stopwatch tick = System.Diagnostics.Stopwatch.StartNew();
    private PuzzleProfile? profile;
    private Puzzle? puzzle;
    private string kind = "Taktik";
    private Position position = Position.FromFen(Position.InitialFen);
    private Position? review;
    private bool busy, question, active, failed, solverWhite, loading;
    private int? selected;
    private int ply, score, remainingLives, quietDefense;
    private int roundStartHighScore;
    private double moveLeft, totalLeft;
    private long previousTick;
    private int revision;
    private Position Display => review ?? position;

    public PuzzleSessionForm(StockfishEngine engine, PuzzleBank bank, string dataDirectory, PuzzleSupply? supply = null)
    {
        historyPlayback = new(board);
        this.engine = engine;
        this.bank = bank;
        this.supply = supply ?? new(engine, dataDirectory, AppContext.BaseDirectory, bank);
        decisions = new(engine, lifetime.Token);
        positionalTrainer = this.supply.Positional;
        Action<string> positionalProgress = message =>
        {
            if (!IsDisposed && loading && (kind is "Stellungsgefühl" or "Feines Stellungsgefühl") && IsHandleCreated)
                BeginInvoke(() => info.Text = "Stellungsfilter prüft Selbstspiel …\n" + message);
        };
        positionalTrainer.Progress += positionalProgress;
        presenter = new(engine, evaluation, variants);
        profiles = PuzzleProfiles.Load(dataDirectory);
        endgames = this.supply.Endgames;
        Text = "Rätsel · Training und Presets";
        Size = new(1300, 900);
        MinimumSize = new(1050, 740);
        BackColor = UiPalette.FromArgb(31, 35, 39);
        ForeColor = UiPalette.Gainsboro;
        Controls.Add(tabs);
        SuspendLayout(); tabs.SuspendLayout();
        tabs.TabPages.AddRange([setup, endgameSetup, play]);
        PuzzleTabRegistry.Build(this, dataDirectory);
        tabs.ResumeLayout(true); ResumeLayout(true);
        _ = new InputFocusCommit(this,n =>
        {
            var fallback=n.EmptyDefaultValue;
            try { if(string.IsNullOrWhiteSpace(n.Text)) n.EmptyDefaultValue=Math.Clamp(0,n.Minimum,n.Maximum); n.CommitEditedValue(); }
            finally { n.EmptyDefaultValue=fallback; }
        });
        timer.Tick += (_, _) => ClockTick();
        timer.Start();
        FormClosed += (_, _) =>
        {
            historyPlayback.Cancel();
            revision++;
            lifetime.Cancel();
            positionalTrainer.Progress -= positionalProgress;
            work.Cancel();
            analysis.Cancel();
            timer.Dispose();
            presenter.Dispose();
            _ = decisions.DisposeAsync();
        };
        _ = PawnTablebase.Shared;
        _ = endgames.Fill(profiles.Last.Endgames, lifetime.Token);
        if (supply is null) _ = this.supply.Maintain(lifetime.Token);
        RefreshBoard();
    }

    private static NumericUpDown Number(int min, int max, int value) => new ScrollNumber
    {
        Minimum = min,
        Maximum = max,
        Value = value,
        Width = 150,
        EmptyDefaultValue = value,
        EnterCommitted = input => { if (input.FindForm() is { } form) form.ActiveControl = null; }
    };
    private static CheckBox Check(string text, bool value) => new()
    {
        Text = text,
        Checked = value,
        AutoSize = true,
        ForeColor = UiPalette.Gainsboro
    };
    private void Clear()
    {
        selected = null;
        BoardPresentation.ClearSelection(board);
    }

    private void AddSnapshot(string label, Position? snapshot = null)
    {
        var p = snapshot ?? position;
        snapshots.Add(p.Clone());
        history.Items.Add(label);
        history.SetPositions(snapshots);
        history.SelectedIndex = history.Items.Count - 1;
        string key = p.RepetitionKey();
        repetitions[key] = repetitions.GetValueOrDefault(key) + 1;
    }

    private void Preview(Position p)
    {
        if (busy && !historyPlayback.IsPlaying || loading)
            return;
        if (historyPlayback.IsPlaying) { historyPlayback.Cancel(); busy = false; }
        review = p.ToFen() == position.ToFen() ? null : p.Clone();
        Clear();
        RefreshBoard();
        if (!question)
            Analyze();
    }

    private void RefreshBoard()
    {
        UpdateScoreboard();
        UpdateGenerationDisplay();
        objective.Text = (puzzle?.MateGoal is int mateGoal ? "Matt in " + mateGoal : puzzle?.GoalKind switch
        {
            "Positional" => "Stellungsgefühl",
            "PersonalMistake" => "Chance nutzen",
            "AdvancedDraw" => "Remis retten",
            "AdvancedWin" => "Bis zum Matt",
            "Draw" => "Remis in " + puzzle.GoalMoves + " Zügen",
            _ => kind == "Bauernverteidigung" ? "Festung / Remis halten" : kind == "Bauernumwandlung" ? "Bauern umwandeln" : puzzle?.Theme ?? ""
        });
        board.Position = Display;
        board.PremoveSide = waitingReply ? solverWhite : null;
        board.Flip = !solverWhite;
        board.Outcome = MoveGenerator.GameState(Display, review is null ? repetitions : new Dictionary<string, int>());
        board.InputEnabled = !resultVisible && (!busy || waitingReply) && !loading && (review is not null || !question || active && (position.WhiteToMove == solverWhite || waitingReply)) && board.Outcome is "" or "Schach";
        board.Invalidate();
        evaluation.WhiteAtBottom = solverWhite;
        variants.Flip = !solverWhite;
        evaluation.Visible = variants.Visible = !question && !loading;
        bool lifeMode = profile?.Tactics == true && profile.Lives > 0;
        solution.Visible = question && (lifeMode || !profile?.Tactics == true || profile is { MoveSeconds: 0, TotalSeconds: 0 });
        whole.Visible = question && !lifeMode && (!profile?.Tactics == true || profile is { MoveSeconds: 0, TotalSeconds: 0 });
        next.Visible = !resultVisible && !resultDismissed && !lifeMode && !(profile?.Tactics == true && profile.MoveSeconds > 0);
        whole.Enabled = !busy || showingSolution;
        solution.Enabled = !busy && !loading;
        next.Enabled = !busy && !loading;
        resume.Visible = review is not null;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (tabs.SelectedTab == play && keyData is Keys.Space or Keys.Enter)
        {
            if (!question && !busy && !loading && active)
                _ = NextExercise();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private async void Analyze()
    {
        bool explain = !question && review is null && puzzle?.GoalKind == "Positional";
        variants.MaximumLines = explain ? puzzle!.AcceptedMoves.Count + 1 : 3;
        variants.AcceptedMoves = explain ? puzzle!.AcceptedMoves.ToHashSet() : [];
        presenter.Show(Display, true, !explain, true, MoveGenerator.GameState(Display, review is null ? repetitions : new Dictionary<string, int>()));
        if (!explain) return;
        int version = revision;
        var exercise = puzzle!;
        variants.Visible = true; variants.Placeholder();
        try
        {
            if (exercise.SolutionLines.Count == 0)
            {
                var root = Position.FromFen(exercise.Fen);
                var result = await engine.AnalyzeAsync(exercise.Fen, Math.Max(16, exercise.MinimumDepth), MoveGenerator.GenerateLegal(root).Count, 2, 128, work.Token, 10000);
                exercise.SolutionLines = result.Lines.Where(l => exercise.AcceptedMoves.Contains(l.BestMove ?? ""))
                    .Concat(result.Lines.OrderByDescending(OpeningGenerator.Score).Where(l => !exercise.AcceptedMoves.Contains(l.BestMove ?? "")).Take(1)).ToList();
            }
            if (version != revision || IsDisposed || review is not null) return;
            variants.ShowLines(Position.FromFen(exercise.Fen), new EngineResult { Lines = exercise.SolutionLines });
            const string explanation = "Varianten ab Rätselstellung: ✓ akzeptiert; danach der nächstbeste andere Zug.";
            if (!info.Text.Contains(explanation)) info.Text += "\n" + explanation;
            variants.Height = Math.Min(320, Math.Max(145, exercise.SolutionLines.Count * 36));
            history.Top = variants.Bottom + 17;
            history.Height = Math.Max(60, play.ClientSize.Height - history.Top - 18);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { if (!IsDisposed && version == revision) info.Text += "\nVarianten: " + ex.Message; }
    }
}
