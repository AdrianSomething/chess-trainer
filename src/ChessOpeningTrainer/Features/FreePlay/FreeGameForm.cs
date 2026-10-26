namespace ChessOpeningTrainer;
public sealed partial class FreeGameForm : LocalizedForm
{
    private readonly Panel gameHost = new() { Dock = DockStyle.Fill };
    private PositionSetupControl? setupEditor;
    private readonly BoardControl board = new();
    private readonly DatasetResultsStrip datasetStats = new();
    private readonly CheckBox showDataset = new() { Text = "Partiestatistik", AutoSize = true };
    private readonly EvalScale evaluation = new();
    private readonly VariationRows variants = new();
    private readonly CheckBox opponent = new()
    {
        Text = "Automatisches Engine-Spiel",
        AutoSize = true
    };
    private readonly Button resume = new()
    {
        Text = "Zur aktuellen Stellung",
        AutoSize = true
    };
    private readonly Button continueHere = new() { Text = "Ab hier weiterspielen", AutoSize = false };
    private bool flipped, humanWhite = true, review;
    private Position? explored;
    private Position Display => review && explored is not null ? explored : position;

    private readonly Stack<Position> exploration = [];
    private readonly CheckBox showEval = new()
    {
        Text = "Bewertung",
        AutoSize = true
    }, showLines = new()
    {
        Text = "Top 3",
        AutoSize = true
    };
    private readonly Label status = new()
    {
        AutoSize = false
    }, book = new()
    {
        AutoSize = false
    };
    private readonly MoveHistory history = new()
    {
        BackColor = UiPalette.FromArgb(43, 49, 55),
        ForeColor = UiPalette.Gainsboro
    };
    private readonly HistoryPlayback historyPlayback;
    private readonly StockfishEngine engine;
    private readonly List<OpeningDefinition> openings;
    private string startFen = Position.InitialFen;
    private Position position = Position.FromFen(Position.InitialFen);
    private readonly List<Position> states = [];
    private readonly List<string> moves = [];
    private readonly Dictionary<string, int> repetitions = [];
    private CancellationTokenSource analysis = new(), play = new();
    private int? selected;
    private bool busy, engineThinking;
    private int revision;
    private Move? premove;
    private readonly AnalysisPresenter presenter;
    private string? setupWarning;
    private KingCaptureRules? kingRules;
    private readonly NumericUpDown bestMovePercent = new() { Minimum = 0, Maximum = 100, Value = 100, Width = 55 };
    private readonly NumericUpDown engineDepth = new() { Minimum = 1, Maximum = 40, Value = 22, Width = 65 };
    private readonly ToolTip tips = new();
    public FreeGameForm(StockfishEngine engine, List<OpeningDefinition> openings, bool setup = false)
    {
        this.engine = engine;
        this.openings = openings;
        historyPlayback = new(board);
        presenter = new(engine, evaluation, variants);
        Text = "Freies Spiel · unabhängig vom Opening";
        Size = new(1220, 860);
        MinimumSize = new(1000, 700);
        BackColor = UiPalette.FromArgb(31, 35, 39);
        ForeColor = UiPalette.Gainsboro;
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 42
        };
        var flip = new MenuIconButton("flip-board") { Width = 38, Height = 32, AccessibleName = "Brett drehen" };
        flip.Click += (_, _) =>
        {
            flipped = !flipped;
            RefreshPosition();
        };
        var reset = new Button
        {
            Text = "Neue Partie",
            AutoSize = true
        };
        reset.Click += (_, _) => Reset();
        toolbar.Controls.Add(reset);
        var edit = new MenuIconButton("back") { Width = 36, Height = 30, AccessibleName = "Stellung bearbeiten" };
        edit.Click += (_, _) =>
        {
            if (moves.Count > 0 && L.Message(this, "Partie verlassen und Stellung bearbeiten?", "Stellung bearbeiten", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            BeginSetup(Display.Clone());
        };
        var header = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44 };
        header.Controls.AddRange([edit, new Label { Text = "Freies Spiel", AutoSize = true, Font = new Font(Font.FontFamily, 18, FontStyle.Bold) }]);
        var options = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false };
        var displayRow = new FlowLayoutPanel { Height = 26, Width = 300, WrapContents = false };
        displayRow.Controls.AddRange([showLines, showDataset]);
        options.Controls.AddRange([showEval, displayRow, opponent]);
        var frequencies = new DatasetFrequencyBinding(board, datasetStats, () => showDataset.Checked);
        showDataset.CheckedChanged += (_, _) => { frequencies.Refresh(); LayoutBoard(); };
        options.Controls.Add(EngineOpponentControls.Create(engineDepth, bestMovePercent));
        Controls.Add(gameHost);
        gameHost.Controls.AddRange([board, evaluation, variants, status, book, history, resume, continueHere, options, toolbar, header, flip]);
        gameHost.Controls.Add(datasetStats);
        header.SendToBack();
        resume.AutoSize = false;
        foreach (var button in new[] { resume, continueHere })
        {
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = UiPalette.FromArgb(31, 35, 39);
            button.ForeColor = UiPalette.Gainsboro;
        }
        continueHere.Click += async (_, _) => await ContinueFromDisplayedPosition();
        foreach (Button b in toolbar.Controls.OfType<Button>())
        {
            b.BackColor = UiPalette.FromArgb(57, 91, 119);
            b.ForeColor = Color.White;
            b.FlatStyle = FlatStyle.Flat;
        }

        void LayoutBoard()
        {
            int size = Math.Min(ClientSize.Height - (showDataset.Checked ? 155 : 110), ClientSize.Width - 430);
            board.SetBounds(58, 90, size, size);
            evaluation.SetBounds(8, 90, 40, size);
            datasetStats.SetBounds(58, board.Bottom + 2, size, 42);
            int x = size + 78, w = ClientSize.Width - x - 16;
            flip.SetBounds(x + w - 38, 90, 38, 32);
            flip.BringToFront();
            displayRow.Width = w;
            options.SetBounds(x, 90, w, 155);
            status.SetBounds(x, 250, w, 42);
            book.SetBounds(x, 295, w, 65);
            variants.SetBounds(x, 365, w, 126);
            history.SetBounds(x, 505, w, Math.Max(0, ClientSize.Height - 601));
            resume.SetBounds(x, ClientSize.Height - 90, w, 38);
            continueHere.SetBounds(x, ClientSize.Height - 48, w, 38);
        }

        Resize += (_, _) => LayoutBoard();
        resume.VisibleChanged += (_, _) => LayoutBoard();
        LayoutBoard();
        board.SquareClicked += sq =>
        {
            if (busy && !engineThinking)
                return;
            if (selected is int from && TryMove(from, sq, true))
                return;
            selected = sq;
            board.SelectedSquare = sq;
            board.CandidateMoves = engineThinking ? Premoves.Candidates(Display, sq, humanWhite) : LegalMoves(Display).Where(m => m.From == sq).ToList();
            board.Invalidate();
        };
        board.SquareDragged += (a, b) =>
        {
            if (!busy || engineThinking)
                TryMove(a, b, false);
        };
        board.BlankClicked += () =>
        {
            premove = null;
            board.QueuedMove = null;
            Clear();
        };
        showEval.CheckedChanged += (_, _) => RefreshPosition();
        showLines.CheckedChanged += (_, _) => RefreshPosition();
        opponent.CheckedChanged += (_, _) =>
        {
            Cancel();
            Clear();
            humanWhite = !flipped;
            RefreshPosition();
            if (!review)
                _ = EngineTurn();
        };
        variants.NavigateRoute += route =>
        {
            PreviewRoute(route);
            if (!review)
                _ = EngineTurn();
        };
        resume.Click += (_, _) =>
        {
            Cancel();
            review = false;
            explored = null;
            exploration.Clear();
            Clear();
            history.SelectedIndex = states.Count;
            RefreshPosition();
            _ = EngineTurn();
        };
        history.NavigationRequested += async (target, kind) => await NavigateHistory(target, kind);
        history.Items.Add("Startstellung");
        FormClosed += (_, _) =>
        {
            Cancel();
            presenter.Dispose();
            tips.Dispose();
        };
        Repetitions();
        RefreshPosition();
        if (setup) Reset();
    }

    private bool EngineSide => setupWarning is null && opponent.Checked && position.WhiteToMove != humanWhite;

    private void PreviewHistory()
    {
        _ = NavigateHistory(history.SelectedIndex, HistoryNavigationKind.Animated);
    }

    private async Task NavigateHistory(int target, HistoryNavigationKind kind)
    {
        if (target < 0 || target > states.Count) return;
        int? start = historyPlayback.IsPlaying ? historyPlayback.CurrentIndex : null;
        Cancel();
        int version = revision;
        var snapshots = states.Append(position.Clone()).ToArray();
        await historyPlayback.NavigateAsync(snapshots, target, kind, (p, _) =>
        {
            explored = p; review = p.ToFen() != position.ToFen();
            busy = historyPlayback.IsPlaying; Clear(); RefreshPosition();
        }, startIndex: start);
        await board.OnAnimationThread(() => { if (version == revision && !review) _ = EngineTurn(); });
    }

    private IReadOnlyList<Move> LegalMoves(Position p) => kingRules?.Moves(p) ?? MoveGenerator.GenerateLegal(p);
    private string GameState => kingRules?.State(Display, review ? new Dictionary<string, int>() : repetitions)
        ?? MoveGenerator.GameState(Display, review ? new Dictionary<string, int>() : repetitions);
    private bool Terminal => GameState is not ("" or "Schach");

    private void Cancel()
    {
        historyPlayback.Cancel();
        revision++;
        engineThinking = false;
        premove = null;
        board.QueuedMove = null;
        analysis.Cancel();
        play.Cancel();
        play.Dispose();
        play = new();
        busy = false;
        board.StopAnimation();
    }

    public void PlayFrom(Position start, bool white)
    {
        flipped = !white;
        AcceptPosition(start);
        opponent.Checked = true;
    }

    private void Reset() => BeginSetup(null);

    private void BeginSetup(Position? initial)
    {
        Cancel();
        presenter.Suspend();
        Clear();
        gameHost.Visible = false;
        Text = "Brett aufstellen · Freies Spiel";
        setupEditor?.Dispose();
        var editor = new PositionSetupControl(initial, allowMultipleKings: true) { Dock = DockStyle.Fill };
        setupEditor = editor;
        Controls.Add(editor);
        editor.BringToFront();
        editor.Accepted += AcceptPosition;
    }

    private void AcceptPosition(Position chosen)
    {
        kingRules = setupEditor?.KingRules;
        setupEditor?.Dispose();
        setupEditor = null;
        gameHost.Visible = true;
        Text = "Freies Spiel · unabhängig vom Opening";
        review = false;
        explored = null;
        exploration.Clear();
        position = chosen.Clone();
        if (kingRules is null && KingCaptureRules.IsMultiple(position))
            kingRules = new(position.Squares.Count(c => c == 'K'), position.Squares.Count(c => c == 'k'));
        board.MoveProvider = kingRules is null ? null : LegalMoves;
        setupWarning = SetupMaterial.EngineWarning(position);
        opponent.Enabled = showEval.Enabled = showLines.Enabled = setupWarning is null;
        if (setupWarning is not null)
            opponent.Checked = showEval.Checked = showLines.Checked = false;
        startFen = position.ToFen();
        states.Clear();
        moves.Clear();
        history.Items.Clear();
        history.Items.Add("Startstellung");
        Repetitions();
        RefreshPosition();
        _ = EngineTurn();
    }

    private async Task Undo()
    {
        if (review)
        {
            if (exploration.TryPop(out var p))
            {
                explored = p;
                review = p.ToFen() != position.ToFen();
                Clear();
                RefreshPosition();
            }

            return;
        }

        if (states.Count == 0)
            return;
        var previousSnapshots = states.Append(position.Clone()).ToArray();
        int previousIndex = states.Count;
        Cancel();
        int undoVersion = revision;
        do
        {
            position = states[^1];
            states.RemoveAt(states.Count - 1);
            moves.RemoveAt(moves.Count - 1);
            history.Items.RemoveAt(history.Items.Count - 1);
        }
        while (states.Count > 0 && EngineSide);
        Clear();
        Repetitions();
        RefreshPosition();
        await historyPlayback.NavigateAsync(previousSnapshots, states.Count, HistoryNavigationKind.Reverse, (p, _) =>
        {
            explored = p; review = p.ToFen() != position.ToFen(); busy = historyPlayback.IsPlaying;
            Clear(); RefreshPosition();
        }, startIndex: previousIndex);
        await board.OnAnimationThread(() => { if (undoVersion == revision && EngineSide && !review) _ = EngineTurn(); });
    }

    private void Preview(Position p) => SetPreview(p, true);

    private void SetPreview(Position p, bool remember)
    {
        Cancel();
        if (remember && Display.ToFen() != p.ToFen())
            exploration.Push(Display.Clone());
        explored = p.Clone();
        review = p.ToFen() != position.ToFen();
        Clear();
        RefreshPosition();
    }

    private void Clear()
    {
        selected = null;
        board.SelectedSquare = null;
        board.CandidateMoves = [];
        board.Invalidate();
    }

    private bool TryMove(int from, int to, bool animate)
    {
        if (engineThinking && !review)
        {
            premove = Premoves.Candidates(Display, from, humanWhite).FirstOrDefault(m => m.To == to);
            board.QueuedMove = premove;
            Clear();
            return premove is not null;
        }

        if (!review && EngineSide || Terminal)
            return false;
        var choices = LegalMoves(Display).Where(m => m.From == from && m.To == to).ToList();
        if (choices.Count == 0)
            return false;
        var m = choices.Count == 1 ? choices[0] : board.ChoosePromotion(this, choices, Display.WhiteToMove);
        if (m is not null)
            _ = UserMove(m, animate);
        return true;
    }

    private async Task UserMove(Move m, bool animate)
    {
        if (review)
        {
            var before = Display.Clone();
            var after = before.Clone();
            after.Apply(m);
            AppSounds.PlayMove(before, m, after, kingRules is null);
            if (kingRules?.Winner(after) is not null) AppSounds.Play("mate");
            Preview(after);
            int v = revision;
            if (animate)
            {
                busy = true;
                RefreshPosition();
                await board.AnimateAsync(before, m, after);
                if (v == revision)
                {
                    busy = false;
                    RefreshPosition();
                }
            }

            if (v == revision && !review)
                await EngineTurn();
            return;
        }

        int version = revision;
        await Move(m, animate);
        if (version == revision)
            await EngineTurn();
    }

    private new async Task Move(Move m, bool animate)
    {
        int version = revision;
        var before = position.Clone();
        string san = $"{position.Fullmove}{(position.WhiteToMove ? "." : "…")} {kingRules?.Notation(position, m) ?? SanNotation.Format(position, m)}";
        states.Add(before);
        moves.Add(m.Uci);
        position.Apply(m);
        AppSounds.PlayMove(before, m, position, kingRules is null);
        if (kingRules?.Winner(position) is not null) AppSounds.Play("mate");
        history.Items.Add(san);
        history.SelectedIndex = history.Items.Count - 1;
        Clear();
        Repetitions();
        busy = true;
        RefreshPosition();
        if (animate)
            await board.AnimateAsync(before, m, position.Clone());
        if (version == revision)
        {
            busy = false;
            RefreshPosition();
        }
    }

    private async Task EngineTurn()
    {
        if (setupEditor is not null || !EngineSide || Terminal || IsDisposed || engineThinking || review)
            return;
        int version = revision;
        engineThinking = true;
        busy = true;
        RefreshPosition();
        var r = await EngineOpponentMove.Choose(engine, position.Clone(), (int)engineDepth.Value, (int)bestMovePercent.Value, play.Token);
        if (version != revision || IsDisposed)
            return;
        busy = false;
        var m = r.Move;
        if (m is not null)
            await Move(m, true);
        else
        {
            status.Text = r.Error ?? "Engine lieferte keinen Zug.";
            board.InputEnabled = false;
        }

        if (version != revision)
            return;
        engineThinking = false;
        var queued = premove;
        premove = null;
        board.QueuedMove = null;
        RefreshPosition();
        if (queued is not null)
        {
            var legal = MoveGenerator.GenerateLegal(position).FirstOrDefault(m => m.Uci == queued.Uci);
            if (legal is not null)
                await UserMove(legal, false);
            else
                status.Text = "Premove verworfen: nach der Antwort nicht legal.";
        }
    }

    private void Repetitions()
    {
        repetitions.Clear();
        foreach (var p in states.Append(position))
        {
            string k = p.RepetitionKey();
            repetitions[k] = repetitions.GetValueOrDefault(k) + 1;
        }
    }

    private void RefreshPosition()
    {
        if (IsDisposed || setupEditor is not null)
            return;
        board.Position = Display;
        board.PremoveSide = engineThinking ? humanWhite : null;
        board.Flip = flipped;
        variants.Flip = flipped;
        board.InputEnabled = (!busy || engineThinking) && (review || !EngineSide || engineThinking) && !Terminal;
        board.Outcome = kingRules is null ? GameState : "";
        if (kingRules?.Winner(Display) is not null && board.LastMoveDestination is int wonAt)
            board.Assessment = (wonAt, "Matt");
        board.Invalidate();
        evaluation.Visible = showEval.Checked;
        evaluation.WhiteAtBottom = !board.Flip;
        variants.Visible = showLines.Checked;
        resume.Visible = review;
        continueHere.Visible = review;
        continueHere.Enabled = !busy;
        status.Text = (Display.WhiteToMove ? "Weiß" : "Schwarz") + " am Zug · " + GameState + (busy ? " · Engine / Bewegung" : "") + (opponent.Checked ? humanWhite ? " · Du spielst Weiß" : " · Du spielst Schwarz" : "");
        book.Text = setupWarning ?? (review ? "Freie Analyse · beide Seiten spielbar · Partie bleibt unverändert" : Recognize());
        history.SelectedIndex = states.Append(position).ToList().FindLastIndex(p => p.ToFen() == Display.ToFen());
        Analyze();
    }

    private string Recognize()
    {
        var matches = new List<(string Name, int Plies, bool Inside)>();
        foreach (var o in openings)
        {
            if (OpeningStore.PositionKey(Position.FromFen(o.StartFen ?? Position.InitialFen)) != OpeningStore.PositionKey(Position.FromFen(startFen)))
                continue;
            var n = o.Tree;
            int i = 0;
            foreach (var u in moves)
            {
                var d = n.Moves.FirstOrDefault(d => d.Uci == u);
                if (d is null)
                    break;
                n = d.Next;
                i++;
            }

            if (i > 0)
                matches.Add((o.Name, i, i == moves.Count));
        }

        if (matches.Count == 0)
            return "Noch kein gespeichertes Opening erkannt.";
        int best = matches.Max(m => m.Plies);
        return string.Join("\n", matches.Where(m => m.Plies == best).Take(3).Select(m => m.Name + (m.Inside ? " · im Baum" : $" · verlassen bei Halbzug {m.Plies + 1}")));
    }

    private void Analyze()
    {
        history.SetPositions(states.Append(position));
        if (setupWarning is not null) { presenter.Suspend(); evaluation.Visible = variants.Visible = false; return; }
        presenter.Show(Display, showEval.Checked, showLines.Checked, review || !EngineSide, GameState);
    }
}
