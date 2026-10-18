using System.Diagnostics;

namespace ChessOpeningTrainer;
public sealed partial class LocalMatchForm : LocalizedForm
{
    private readonly BoardControl board = new();
    private readonly MoveHistory history = new();
    private readonly Label clocks = new()
    {
        ForeColor = UiPalette.Gainsboro,
        Font = new Font("Segoe UI", 19, FontStyle.Bold)
    }, status = new()
    {
        ForeColor = UiPalette.Gainsboro
    };
    private readonly Panel setup = new()
    {
        Dock = DockStyle.Fill
    }, play = new()
    {
        Dock = DockStyle.Fill,
        Visible = false
    };
    private readonly TextBox whiteName = new()
    {
        Width = 300
    }, blackName = new()
    {
        Width = 300
    };
    private readonly ComboBox owner = new()
    {
        Width = 300,
        DropDownStyle = ComboBoxStyle.DropDownList
    };
    private readonly ScrollNumber whiteTime = new()
    {
        Minimum = .1m,
        Maximum = 180,
        DecimalPlaces = 1,
        Value = 5,
        Width = 100
    }, blackTime = new()
    {
        Minimum = .1m,
        Maximum = 180,
        DecimalPlaces = 1,
        Value = 5,
        Width = 100
    }, whiteIncrement = new()
    {
        Maximum = 120,
        Value = 3,
        Width = 100
    }, blackIncrement = new()
    {
        Maximum = 120,
        Value = 3,
        Width = 100
    };
    private readonly CheckBox useClock = new()
    {
        Text = "Schachuhr verwenden",
        Checked = true,
        AutoSize = true
    }, autoFlip = new()
    {
        Text = "Nach jedem Zug Ansicht wechseln",
        AutoSize = true
    };
    private readonly PersonalGameStore store;
    private readonly string directory;
    private PersonalGame? game;
    private Position position = Position.FromFen(Position.InitialFen);
    private readonly List<Position> positions = [];
    private readonly Dictionary<string, int> repetitions = [];
    private readonly System.Windows.Forms.Timer timer = new()
    {
        Interval = 50
    };
    private readonly Stopwatch watch = Stopwatch.StartNew();
    private long last;
    private MatchClock clock = new(300, 300, 3, 3);
    private bool running, paused, review, anonymous;
    private readonly Label lowerClock = new() { Font = new Font("Segoe UI", 19, FontStyle.Bold) };
    private Action? refreshActions;
    private int? selected;
    public LocalMatchForm(string dataDirectory, StockfishEngine? analysisEngine = null)
    {
        historyPlayback = new(board);
        this.analysisEngine = analysisEngine;
        directory = dataDirectory;
        store = new(directory);
        Text = "Schach zu zweit · an einem Gerät";
        Size = new(1220, 880);
        MinimumSize = new(1000, 720);
        BackColor = UiPalette.FromArgb(31, 35, 39);
        ForeColor = UiPalette.Gainsboro;
        Controls.Add(play);
        Controls.Add(setup);
        var fields = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new(24)
        };
        setup.Controls.Add(fields);
        void Field(string text, Control input)
        {
            var row = new FlowLayoutPanel
            {
                Width = 600,
                Height = 38
            };
            row.Controls.AddRange([new Label { Text = text, Width = 260 }, input]);
            fields.Controls.Add(row);
        }

        Field("Name Weiß", whiteName);
        Field("Name Schwarz", blackName);
        var nameCommit = new NameInputCommit(this, whiteName, blackName);
        Disposed += (_, _) => nameCommit.Dispose();
        owner.Items.AddRange(["Ich spiele Weiß", "Ich spiele Schwarz"]);
        owner.SelectedIndex = 0;
        Field("Deine Seite (für die Partienanalyse)", owner);
        L.SetToolTip(analysisTip, owner, "Wird in der PGN als Owner/OwnerColor gespeichert und wählt die anfängliche Perspektive für Highlights. Die Engine analysiert trotzdem beide Seiten.");
        fields.Controls.Add(useClock);
        Field("Minuten für Weiß", whiteTime);
        Field("Minuten für Schwarz", blackTime);
        Field("Sekunden pro Zug für Weiß", whiteIncrement);
        Field("Sekunden pro Zug für Schwarz", blackIncrement);
        fields.Controls.Add(autoFlip);
        var duringPlay = new CheckBox { Text = "Partie während des Spielens analysieren", AutoSize = true, Checked = Preferences.Current.LocalContinuousAnalysis, Enabled = analysisEngine is not null };
        duringPlay.CheckedChanged += (_, _) => { Preferences.Current.LocalContinuousAnalysis = duringPlay.Checked; Preferences.Save(); };
        fields.Controls.Add(duringPlay);
        var begin = new Button
        {
            Text = "Partie starten",
            Width = 300,
            Height = 40
        };
        fields.Controls.Add(begin);
        begin.Click += (_, _) => { anonymous = false; StartMatch(); };
        var anonymousButton = new Button { Text = "Anonym spielen · nicht speichern", Width = 300, Height = 40 };
        fields.Controls.Add(anonymousButton);
        anonymousButton.Click += (_, _) => StartAnonymous();
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44
        };
        Button Button(string text, Action action)
        {
            var b = new Button
            {
                Text = text,
                AutoSize = true
            };
            b.Click += (_, _) => action();
            toolbar.Controls.Add(b);
            return b;
        }

        var flip = new MenuIconButton("flip-board") { Width = 38, Height = 32, AccessibleName = "Brett drehen" };
        flip.Click += (_, _) =>
        {
            board.Flip = !board.Flip;
            board.Invalidate();
            Tick();
        };
        var pause = new MenuIconButton("pause") { AccessibleName = "Uhr pausieren" };
        pause.Click += (_, _) =>
        {
            Tick();
            if (!running)
                return;
            SetPaused(!paused);
        };
        var current = Button("Zur aktuellen Stellung", () =>
        {
            historyPlayback.Cancel();
            review = false;
            history.SelectedIndex = positions.Count - 1;
            RefreshBoard();
        });
        var draw = new MenuIconButton("draw") { Width = 38, Height = 34, AccessibleName = "Remis vereinbaren" };
        draw.Click += (_, _) =>
        {
            if (running && L.Message(this, "Sind beide Spieler mit einem Remis einverstanden?", "Remis", MessageBoxButtons.YesNo) == DialogResult.Yes)
                Finish("1/2-1/2", "Remis vereinbart");
        };
        var resign = new MenuIconButton("resign") { Width = 38, Height = 34, AccessibleName = "Aufgeben (Seite am Zug)" };
        resign.Click += (_, _) =>
        {
            if (running && L.Message(this, "Die Seite am Zug gibt auf?", "Aufgeben", MessageBoxButtons.YesNo) == DialogResult.Yes)
                Finish(position.WhiteToMove ? "0-1" : "1-0", "Aufgabe");
        };
        var back = new MenuIconButton("back") { Width = 38, Height = 34, AccessibleName = "Zurück" };
        back.Click += (_, _) =>
        {
            if (!ConfirmLeave()) return;
            historyPlayback.Cancel();
            running = false; timer.Stop(); play.Visible = false; setup.Visible = true;
            pauseOverlay.SetPaused(false);
        };
        toolbar.Controls.Clear(); toolbar.Controls.Add(back);
        var newGame = new Button { Text = "Neues Spiel", Height = 38 };
        newGame.Click += (_, _) => StartMatch();
        analyzeGame.Click += async (_, _) => await OpenAnalysis();
        current.AutoSize = false;
        play.Controls.AddRange([board, history, clocks, lowerClock, status, toolbar, flip, pause, current, draw, resign, newGame, analyzeGame]);
        board.Controls.Add(pauseOverlay);
        board.Resize += (_, _) => pauseOverlay.Location = new((board.Width - pauseOverlay.Width) / 2, (board.Height - pauseOverlay.Height) / 2);
        void LayoutBoard()
        {
            int size = Math.Min(play.Height - 70, play.Width - 350);
            board.SetBounds(12, 52, size, size);
            int x = size + 30, w = Math.Max(250, play.Width - x - 12);
            flip.SetBounds(x, 46, 38, 32);
            clocks.SetBounds(x, 88, w - 145, 50);
            pause.SetBounds(x + w - 42, 88, 38, 38);
            status.SetBounds(x, 145, w, 78);
            int lower = size + 36;
            lowerClock.SetBounds(x, lower, w - 90, 50);
            draw.SetBounds(x + w - 82, lower, 38, 34); resign.SetBounds(x + w - 40, lower, 38, 34);
            history.SetBounds(x, 230, w, Math.Max(60, lower - 240 - (current.Visible || newGame.Visible ? 44 : 0) - (analyzeGame.Visible ? 38 : 0)));
            current.SetBounds(x, history.Bottom + 4, w, 38);
            newGame.SetBounds(x, history.Bottom + 4, w, 38);
            analyzeGame.SetBounds(x, history.Bottom + 46, w, 32);
        }

        refreshActions = () =>
        {
            pause.IconName = paused ? "play" : "pause";
            pause.AccessibleName = paused ? "Uhr fortsetzen" : "Uhr pausieren";
            pause.Visible = running && useClock.Checked;
            current.Visible = review;
            draw.Visible = resign.Visible = running && game?.Moves.Count > 0;
            newGame.Visible = !running && game is not null && !review;
            LayoutBoard();
        };

        play.Resize += (_, _) => LayoutBoard();
        LayoutBoard();
        board.SquareClicked += square =>
        {
            if (!running || paused || review)
                return;
            if (selected is int from && TryMove(from, square))
                return;
            selected = square;
            board.SelectedSquare = square;
            board.CandidateMoves = MoveGenerator.GenerateLegal(position).Where(m => m.From == square).ToList();
            board.Invalidate();
        };
        board.SquareDragged += (from, to) => TryMove(from, to);
        board.BlankClicked += () =>
        {
            selected = null;
            board.SelectedSquare = null;
            board.CandidateMoves = [];
            board.Invalidate();
        };
        history.NavigationRequested += async (target, kind) => await PreviewHistoryAsync(target, kind);
        history.SelectedIndexChanged += async (_, _) =>
        {
            if (!history.IsNavigationInput)
                await PreviewHistoryAsync(history.SelectedIndex, HistoryNavigationKind.Immediate);
        };
        timer.Tick += (_, _) => Tick();
        FormClosed += (_, _) =>
        {
            historyPlayback.Cancel();
            timer.Dispose();
            analysisLifetime.Cancel();
            if (liveAnalysis is not null) _ = liveAnalysis.DisposeAsync();
            analysisTip.Dispose();
            Save();
        };
    }

    private void StartMatch()
    {
        historyPlayback.Cancel();
        if (!anonymous && (string.IsNullOrWhiteSpace(whiteName.Text) || string.IsNullOrWhiteSpace(blackName.Text)))
        {
            L.Message(this, "Bitte beide Spielernamen eintragen.", "Spieler");
            return;
        }

        position = Position.FromFen(Position.InitialFen);
        matchAnalysis?.Cancel(); matchAnalysis?.Dispose(); matchAnalysis = null;
        if (liveAnalysis is not null) _ = liveAnalysis.DisposeAsync();
        liveAnalysis = null; analysisProgress = new();
        preparingAnalysis = null;
        analyzeGame.Visible = false;
        positions.Clear(); history.Items.Clear(); repetitions.Clear();
        review = paused = false;
        pauseOverlay.SetPaused(false);
        board.WhitePlayerName = anonymous ? null : whiteName.Text.Trim();
        board.BlackPlayerName = anonymous ? null : blackName.Text.Trim();
        game = new()
        {
            Id = "local_" + Guid.NewGuid().ToString("N"),
            Tags = new()
            {
                {
                    "Event",
                    "Schach zu zweit"
                },
                {
                    "Site",
                    "Lokal"
                },
                {
                    "Date",
                    DateTime.Now.ToString("yyyy.MM.dd")
                },
                {
                    "White",
                    whiteName.Text.Trim()
                },
                {
                    "Black",
                    blackName.Text.Trim()
                },
                {
                    "Result",
                    "*"
                },
                {
                    "Owner",
                    owner.SelectedIndex == 0 ? whiteName.Text.Trim() : blackName.Text.Trim()
                },
                {
                    "OwnerColor",
                    owner.SelectedIndex == 0 ? "white" : "black"
                },
                {
                    "WhiteTimeControl",
                    useClock.Checked ? $"{whiteTime.Value * 60}+{whiteIncrement.Value}" : "-"
                },
                {
                    "BlackTimeControl",
                    useClock.Checked ? $"{blackTime.Value * 60}+{blackIncrement.Value}" : "-"
                }
            }
        };
        if (!anonymous) store.Add(game);
        clock = new((double)whiteTime.Value * 60, (double)blackTime.Value * 60, (double)whiteIncrement.Value, (double)blackIncrement.Value);
        positions.Add(position.Clone());
        history.Items.Add("Startstellung");
        repetitions[position.RepetitionKey()] = 1;
        setup.Visible = false;
        play.Visible = true;
        running = true;
        last = watch.ElapsedMilliseconds;
        timer.Start();
        Tick();
        RefreshBoard();
        Save();
        QueueLiveAnalysis();
    }

    private void StartAnonymous()
    {
        anonymous = true;
        StartMatch();
    }

    private void Tick()
    {
        long now = watch.ElapsedMilliseconds;
        double elapsed = (now - last) / 1000d;
        last = now;
        if (running && !paused && useClock.Checked)
        {
            clock.Elapse(position.WhiteToMove, elapsed);
            if ((position.WhiteToMove ? clock.White : clock.Black) <= 0)
            {
                bool winner = !position.WhiteToMove;
                var men = position.Squares.Where(c => c != '\0' && char.IsUpper(c) == winner && char.ToLowerInvariant(c) != 'k').ToArray();
                bool bare = men.Length == 0;
                bool singleMinor = men.Length == 1 && char.ToLowerInvariant(men[0])is 'b' or 'n' && position.Squares.Count(c => c != '\0') == 3;
                Finish(bare || singleMinor ? "1/2-1/2" : position.WhiteToMove ? "0-1" : "1-0", bare || singleMinor ? "Zeit abgelaufen · kein Mattmaterial" : "Zeit abgelaufen");
            }
        }

        string Time(double t) => $"{(int)t / 60:00}:{(int)t % 60:00}.{(int)(t * 10) % 10}";
        clocks.Text = useClock.Checked ? Time(board.Flip ? clock.White : clock.Black) : "Ohne Uhr";
        lowerClock.Text = useClock.Checked ? Time(board.Flip ? clock.Black : clock.White) : "Ohne Uhr";
    }

    private bool TryMove(int from, int to)
    {
        if (!running || paused || review || historyPlayback.IsPlaying)
            return false;
        Tick();
        if (!running)
            return false;
        var choices = MoveGenerator.GenerateLegal(position).Where(m => m.From == from && m.To == to).ToList();
        if (choices.Count == 0)
            return false;
        var move = choices.Count == 1 ? choices[0] : PromotionPicker.Choose(this, choices, position.WhiteToMove);
        Tick();
        if (move is null || !running)
            return false;
        var before = position.Clone();
        string san = $"{before.Fullmove}{(before.WhiteToMove ? "." : "…")} {SanNotation.Format(before, move)}";
        position.Apply(move);
        game!.Moves.Add(move.Uci);
        QueueLiveAnalysis();
        if (useClock.Checked)
            clock.CompleteMove(before.WhiteToMove);
        last = watch.ElapsedMilliseconds;
        positions.Add(position.Clone());
        history.Items.Add(san);
        history.SetPositions(positions);
        history.SelectedIndex = history.Items.Count - 1;
        repetitions[position.RepetitionKey()] = repetitions.GetValueOrDefault(position.RepetitionKey()) + 1;
        AppSounds.PlayMove(before, move, position);
        selected = null;
        board.SelectedSquare = null;
        board.CandidateMoves = [];
        if (autoFlip.Checked)
            board.Flip = !position.WhiteToMove;
        string outcome = MoveGenerator.GameState(position, repetitions);
        if (outcome == "Schachmatt")
            Finish(position.WhiteToMove ? "0-1" : "1-0", outcome);
        else if (outcome.StartsWith("Remis"))
            Finish("1/2-1/2", outcome);
        RefreshBoard();
        Save();
        return true;
    }

    private void Finish(string result, string reason)
    {
        running = false;
        paused = false;
        pauseOverlay.SetPaused(false);
        if (game is not null)
        {
            game.Tags["Result"] = result;
            game.Tags["Termination"] = reason;
        }

        status.Text = reason + " · " + result;
        RefreshBoard();
        Save();
        PrepareAnalysis();
        refreshActions?.Invoke();
    }

    private void Save()
    {
        if (game is null || anonymous)
            return;
        try
        {
            store.Save();
            string folder = Path.Combine(directory, "LocalGames");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, game.Id + ".pgn"), PgnText.Write(game));
        }
        catch (Exception ex)
        {
            status.Text = "Speichern fehlgeschlagen: " + ex.Message;
        }
    }

    private void RefreshBoard()
    {
        refreshActions?.Invoke();
        board.Position = position;
        board.Outcome = MoveGenerator.GameState(position, repetitions);
        if (!running && game?.Tags.GetValueOrDefault("Result") == "1/2-1/2") board.Outcome = "Remis";
        board.InputEnabled = running && !paused && !review && !historyPlayback.IsPlaying;
        board.Invalidate();
        if (running)
            status.Text = (paused ? "Uhr pausiert" : position.WhiteToMove ? "Weiß am Zug" : "Schwarz am Zug") +
                (anonymous ? "\nAnonymes Spiel · wird nicht gespeichert." : "\nAutomatisch unter Eigene Spiele & Analyse gespeichert.");
    }
}
