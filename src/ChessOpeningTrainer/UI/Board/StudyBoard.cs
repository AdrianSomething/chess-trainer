namespace ChessOpeningTrainer;
public sealed class StudyBoard : UserControl
{
    private readonly BoardControl board = new();
    private readonly DatasetResultsStrip datasetStats = new();
    private readonly DatasetFrequencyBinding datasetBinding;
    private bool showDatasetStats;
    public bool ShowDatasetStatistics { get => showDatasetStats; set { showDatasetStats = value; datasetBinding.Refresh(); LayoutBoard(); } }
    private readonly EvalScale evaluation = new();
    private readonly VariationRows lines = new();
    private readonly MoveHistory history = new();
    private readonly AnalysisPresenter presenter;
    private readonly HistoryPlayback playback;
    private readonly Button current = new()
    {
        Text = "Zur aktuellen Stellung",
        AutoSize = false
    }, undo = new()
    {
        Text = "Zug zurück",
        AutoSize = true
    };
    private readonly MenuIconButton flip = new("flip-board") { Width = 38, Height = 32, AccessibleName = "Brett drehen" };
    private List<Position> positions = [Position.FromFen(Position.InitialFen)];
    private Position displayed = Position.FromFen(Position.InitialFen);
    private int index, selected = -1;
    private bool updating, exploring;
    private int? historyAnchor, assessmentTarget;
    private int navigationVersion;
    public int CurrentIndex => index;
    public bool BottomWhite => !board.Flip;
    public event Action? OrientationChanged;
    public bool Replaying => playback.IsPlaying;
    public List<string> ManualMoves { get; } = [];
    public bool Manual { get; set; }
    public Panel Details { get; } = new();

    private PersonalGame? loadedGame;
    public event Action<int>? SelectionChanged;
    public event Action<Position, Move, Position>? MoveTried;
    public void SetFlip(bool flip)
    {
        board.Flip = flip;
        RefreshPosition();
        OrientationChanged?.Invoke();
    }

    public void ExploreMove(string u)
    {
        var before = displayed.Clone();
        var move = MoveGenerator.GenerateLegal(displayed).FirstOrDefault(m => m.Uci == u);
        if (move is null)
            return;
        StopPreviewPlayback();
        displayed.Apply(move);
        exploring = true;
        RefreshPosition();
        MoveTried?.Invoke(before, move, displayed.Clone());
    }

    public void PreviewPositionIndex(int i)
    {
        StopPreviewPlayback();
        displayed = positions[Math.Clamp(i, 0, positions.Count - 1)].Clone();
        exploring = displayed.ToFen() != positions[index].ToFen();
        board.InputEnabled = true;
        RefreshPosition();
    }

    private void StopPreviewPlayback()
    {
        navigationVersion++;
        playback.Cancel();
        assessmentTarget = null;
        board.InputEnabled = true;
    }

    public StudyBoard(StockfishEngine engine)
    {
        DoubleBuffered = true;
        BackColor = UiPalette.FromArgb(31, 35, 39);
        ForeColor = UiPalette.Gainsboro;
        presenter = new(engine, evaluation, lines);
        playback = new(board);
        datasetBinding = new(board, datasetStats, () => showDatasetStats);
        Controls.Add(datasetStats);
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 35
        };
        toolbar.Controls.Add(flip);
        Controls.AddRange([board, evaluation, lines, history, Details, toolbar, current]);
        current.Click += (_, _) => ShowIndex(historyAnchor ?? index);
        flip.Click += (_, _) =>
        {
            board.Flip = !board.Flip;
            RefreshPosition();
            OrientationChanged?.Invoke();
        };
        undo.Click += async (_, _) =>
        {
            if (Manual && positions.Count > 1)
            {
                positions.RemoveAt(positions.Count - 1);
                ManualMoves.RemoveAt(ManualMoves.Count - 1);
                history.Items.RemoveAt(history.Items.Count - 1);
                ShowIndex(positions.Count - 1);
            }
            else
                await PreviousMoveAsync();
        };
        history.SelectedIndexChanged += async (_, _) =>
        {
            if (!updating && !history.IsNavigationInput && history.SelectedIndex >= 0)
                await NavigateHistory(history.SelectedIndex, HistoryNavigationKind.Animated);
        };
        history.NavigationRequested += async (target, kind) =>
        {
            await NavigateHistory(target, kind);
        };
        lines.Navigate += p =>
        {
            if (Manual)
                return;
            StopPreviewPlayback();
            displayed = p.Clone();
            exploring = displayed.ToFen() != positions[index].ToFen();
            RefreshPosition();
        };
        bool Try(int from, int to)
        {
            if (Replaying) return false;
            var options = MoveGenerator.GenerateLegal(displayed).Where(m => m.From == from && m.To == to).ToList();
            if (options.Count == 0)
                return false;
            var move = options.Count == 1 ? options[0] : PromotionPicker.Choose(FindForm() ?? (IWin32Window)this, options, displayed.WhiteToMove);
            if (move is null)
                return false;
            if (Manual)
                while (positions.Count > index + 1)
                {
                    positions.RemoveAt(positions.Count - 1);
                    ManualMoves.RemoveAt(ManualMoves.Count - 1);
                    history.Items.RemoveAt(history.Items.Count - 1);
                }

            var before = displayed.Clone();
            string san = $"{before.Fullmove}{(before.WhiteToMove ? "." : "…")} {SanNotation.Format(before, move)}";
            displayed.Apply(move);
            AppSounds.PlayMove(before, move, displayed);
            if (Manual)
            {
                ManualMoves.Add(move.Uci);
                positions.Add(displayed.Clone());
                index = positions.Count - 1;
                updating = true;
                history.Items.Add(san);
                history.SelectedIndex = index;
                updating = false;
            }
            else exploring = true;

            RefreshPosition();
            MoveTried?.Invoke(before, move, displayed.Clone());
            return true;
        }

        board.SquareClicked += sq =>
        {
            if (Replaying) return;
            if (selected >= 0 && Try(selected, sq))
                return;
            selected = sq;
            board.SelectedSquare = sq;
            board.CandidateMoves = MoveGenerator.GenerateLegal(displayed).Where(m => m.From == sq).ToList();
            board.Invalidate();
        };
        board.SquareDragged += (from, to) => Try(from, to);
        board.BlankClicked += () =>
        {
            selected = -1;
            BoardPresentation.ClearSelection(board);
        };
        Resize += (_, _) => LayoutBoard();
        LayoutBoard();
        ShowIndex(0);
    }

    private void LayoutBoard()
    {
        int side = Math.Max(200, Math.Min(Height - 46 - (showDatasetStats ? 44 : 0), Width - 360));
        board.MinimumSize = Size.Empty;
        board.SetBounds(48, 40, side, side);
        datasetStats.SetBounds(48, board.Bottom + 2, side, 42);
        evaluation.SetBounds(5, 62, 38, Math.Max(50, side - 44));
        int x = side + 62, w = Math.Max(160, Width - x - 5);
        int detailHeight = Math.Clamp(Height / 3 + 20, 235, 255);
        Details.SetBounds(x, 38, w, detailHeight);
        lines.SetBounds(x, Details.Bottom + 8, w, 140);
        int footer = Math.Max(0, Height - 44);
        history.SetBounds(x, lines.Bottom + 10, w, Math.Max(60, footer - lines.Bottom - 16));
        current.SetBounds(x, footer, w, 38);
        current.BringToFront();
    }

    public void LoadGame(PersonalGame game)
    {
        loadedGame = game;
        board.WhitePlayerName = game.White;
        board.BlackPlayerName = game.Black;
        Manual = false;
        ManualMoves.Clear();
        positions = game.Positions();
        updating = true;
        history.Items.Clear();
        history.Items.Add("Startstellung");
        for (int i = 0; i < game.Moves.Count; i++)
        {
            var p = positions[i];
            var m = MoveGenerator.GenerateLegal(p).First(m => m.Uci == game.Moves[i]);
            history.Items.Add($"{p.Fullmove}{(p.WhiteToMove ? "." : "…")} {SanNotation.Format(p, m)}");
        }

        updating = false;
        ShowIndex(0);
    }

    public void ShowIndex(int i)
    {
        navigationVersion++;
        playback.Cancel();
        historyAnchor = assessmentTarget = null;
        exploring = false;
        board.InputEnabled = true;
        index = Math.Clamp(i, 0, positions.Count - 1);
        displayed = positions[index].Clone();
        updating = true;
        history.SelectedIndex = index;
        updating = false;
        RefreshPosition();
        SelectionChanged?.Invoke(index);
    }

    private async Task NavigateHistory(int target, HistoryNavigationKind kind)
    {
        int anchor = historyAnchor ?? index;
        historyAnchor = anchor;
        int version = ++navigationVersion;
        if (kind == HistoryNavigationKind.Immediate) { ShowIndex(target); version = navigationVersion; }
        else await ReplayTo(target, false, kind, preserveHistoryAnchor: true);
        await board.OnAnimationThread(() =>
        {
            if (version != navigationVersion || IsDisposed) return;
            historyAnchor = anchor == index ? null : anchor;
            RefreshPosition();
        });
    }

    public async Task ReplayTo(int target, bool fromStart = false, HistoryNavigationKind kind = HistoryNavigationKind.Animated, bool highlightOnly = false, bool preserveHistoryAnchor = false)
    {
        target = Math.Clamp(target, 0, positions.Count - 1);
        if (!preserveHistoryAnchor) { historyAnchor = null; navigationVersion++; }
        // A history click and a highlight replay both review one selected move.
        // Intervening animations retain their piece sounds, not assessment
        // badges or success/error feedback for moves the user did not select.
        assessmentTarget = target;
        if (!fromStart && target == index)
        {
            ShowIndex(target);
            return;
        }
        int start = fromStart ? 0 : index;
        // Commit the destination immediately. Another navigation request snaps this
        // animation to its destination and advances from there, rather than repeating it.
        index = target;
        exploring = false;
        updating = true;
        history.SelectedIndex = target;
        updating = false;
        board.InputEnabled = false;
        // The selected ply and its assessment are committed independently from
        // the intermediate boards of a multi-move highlight animation.
        current.Visible = false;
        SelectionChanged?.Invoke(target);
        await playback.NavigateAsync(positions, target, kind, (p, _) =>
        {
            displayed = p;
            board.InputEnabled = !playback.IsPlaying;
            RefreshPosition();
        }, start, _ =>
        {
            if (board.Assessment is not { } badge) return;
            AppSounds.Play(badge.Category is "Patzer" or "Schlecht" or "Verpasste Chance" or "Verloren" ? "error" : "success");
        });
    }

    public Task PreviousMoveAsync()
    {
        if (index <= 0) { ShowIndex(0); return Task.CompletedTask; }
        return ReplayTo(index - 1, false, HistoryNavigationKind.Reverse);
    }

    public Task NavigateToHistoryIndexAsync(int target, HistoryNavigationKind kind = HistoryNavigationKind.Animated)
        => NavigateHistory(Math.Clamp(target, 0, positions.Count - 1), kind);

    private void RefreshPosition()
    {
        selected = -1;
        board.Position = displayed;
        board.SelectedSquare = null;
        board.CandidateMoves = [];
        board.Arrows.Clear();
        board.MarkedSquares.Clear();
        board.Assessment = null;
        board.HintMove = null;
        int displayedIndex = positions.FindIndex(p => p.ToFen() == displayed.ToFen());
        board.Outcome = null;
        if (loadedGame is not null && displayedIndex == positions.Count - 1 && loadedGame.Tags.GetValueOrDefault("Result") == "1/2-1/2")
            board.Outcome = "Remis";
        if (loadedGame is not null && displayedIndex > 0)
        {
            board.HintMove = MoveGenerator.GenerateLegal(positions[displayedIndex - 1]).FirstOrDefault(m => m.Uci == loadedGame.Moves[displayedIndex - 1]);
            string state = MoveGenerator.GameState(displayed, new Dictionary<string, int>());
            string? category = state.Contains("matt", StringComparison.OrdinalIgnoreCase) ? "Matt"
                : state is not ("" or "Schach") ? "Remis"
                : loadedGame.HasCompleteAnalysis ? loadedGame.Analysis.FirstOrDefault(m => m.Ply == displayedIndex - 1)?.Category : null;
            if (category == "Lektion") category = null;
            if (category is not null && category != "Remis" && (assessmentTarget is null || displayedIndex == assessmentTarget) && board.HintMove is { } move) board.Assessment = (move.To, category);
        }
        board.Invalidate();
        evaluation.WhiteAtBottom = !board.Flip;
        lines.Flip = board.Flip;
        history.SetPositions(positions);
        current.Visible = (exploring || historyAnchor is not null) && displayed.ToFen() != positions[historyAnchor ?? index].ToFen();
        undo.Visible = Manual ? positions.Count > 1 : index > 0 || current.Visible;
        LayoutBoard();
        int saved = positions.FindIndex(p => p.ToFen() == displayed.ToFen());
        if (!Manual && saved >= 0 && loadedGame?.HasCompleteAnalysis == true)
            presenter.ShowSaved(displayed, loadedGame.PositionAnalyses[saved]);
        else
            presenter.Show(displayed, true, true, true, MoveGenerator.GameState(displayed, new Dictionary<string, int>()));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            playback.Cancel();
            presenter.Dispose();
        }
        base.Dispose(disposing);
    }
}
