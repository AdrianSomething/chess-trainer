using System.Text.Json;

namespace ChessOpeningTrainer;
public sealed partial class OpeningGeneratorForm : LocalizedForm
{
    private sealed class BufferedFlowPanel : FlowLayoutPanel
    {
        public BufferedFlowPanel() => DoubleBuffered = true;
    }
    private sealed class ForecastProgress(GeneratorForecast forecast, IProgress<GeneratorGraphSnapshot> display) : IProgress<GeneratorGraphSnapshot>
    {
        public void Report(GeneratorGraphSnapshot value)
        {
            // Record completion synchronously; UI delivery is intentionally queued.
            forecast.Update(value);
            display.Report(value);
        }
    }
    public event Action<OpeningDefinition>? Generated;
    public void PrepareFrom(OpeningDefinition opening, Position target)
    {
        var path = GraphOperations.Positions(opening).First(w => OpeningStore.PositionKey(w.Position) == OpeningStore.PositionKey(target)).Path;
        undo.Clear();
        position = Position.FromFen(opening.StartFen ?? Position.InitialFen);
        foreach (var u in path)
        {
            undo.Push(position.Clone());
            position.Apply(MoveGenerator.GenerateLegal(position).First(m => m.Uci == u));
        }

        color.SelectedIndex = opening.LearnsWhite ? 1 : 0;
        RefreshPosition();
    }

    public bool IsWorking => operation is not null || workQueue.Jobs.Any(j => !j.Finished);

    private readonly BoardControl board = new();
    private readonly Panel boardHost = new()
    {
        Dock = DockStyle.Fill
    };
    private readonly EvalScale evaluation = new();
    private readonly Label evalInfo = new()
    {
        ForeColor = UiPalette.Gainsboro,
        AutoSize = false
    };
    private readonly StockfishEngine engine;
    private readonly AnalysisPresenter presenter;
    private CancellationTokenSource evalCancellation = new();
    private Position position = Position.FromFen(Position.InitialFen);
    private readonly Stack<Position> undo = new();
    private int? selected;
    private bool moving;
    private bool nameEdited, updatingName;
    private readonly ToolTip tips = new()
    {
        AutoPopDelay = 18000
    };
    private readonly OpeningGenerator generator;
    private readonly GeneratorTimingHistory timingHistory;
    private TreeEstimate? cachedEstimate;
    private string? cachedEstimateKey;
    private readonly string directory;
    private CancellationTokenSource? operation;
    private readonly FlowLayoutPanel controls = new BufferedFlowPanel()
    {
        Dock = DockStyle.Right,
        Width = 620,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoScroll = true,
        Padding = new Padding(12)
    };
    private readonly TextBox name = new()
    {
        Text = "Mein Repertoire",
        Width = 320
    };
    private readonly ComboBox color = new()
    {
        Width = 320,
        DropDownStyle = ComboBoxStyle.DropDownList
    };
    private readonly NumericUpDown min = N(0, 60, 5), cards = N(1, 10000, 200), threads = N(1, 16, 2), workers = N(1, 16, Math.Clamp((Environment.ProcessorCount - 2) / 2, 1, 8)), humanDepth = N(1, 16, 8), sectionCount = N(1, 5, 1);
    private readonly SectionEditor sections = new();
    private readonly CheckBox merge = new()
    {
        Text = "Transpositionen über alle Tiefen vereinen",
        Checked = true,
        AutoSize = true
    }, human = new()
    {
        Text = "Human-Moves-Labels vorbereiten (keine Züge entfernen)",
        AutoSize = true
    };
    private readonly Label status = new()
    {
        AutoSize = false,
        Width = 325,
        Height = 105
    };
    private readonly Button generate = new()
    {
        Text = "Lernkarten generieren",
        Width = 320,
        Height = 36
    }, estimate = new()
    {
        Text = "Kartenzahl neu schätzen",
        Width = 320,
        Height = 32
    };
    public OpeningGeneratorForm(StockfishEngine engine, string appDirectory)
    {
        this.engine = engine;
        workQueue = new(engine);
        _ = new InputFocusCommit(this, includeTextBoxes: true);
        // Wheel input edits neither perspective nor numbers. Route it through
        // the same scroll path everywhere in the parameter column instead.
        var wheel = new SettingsWheelGuard(this);
        Application.AddMessageFilter(wheel);
        Disposed += (_, _) => wheel.Dispose();
        presenter = new(engine, evaluation, new VariationRows());
        cards.Increment = 10;
        generator = new(engine);
        timingHistory = new(Environment.GetEnvironmentVariable("CHESS_TRAINER_DATA") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChessOpeningTrainer"));
        directory = Path.Combine(appDirectory, "Openings");
        Text = "Opening-Branch-Generator";
        Size = new(1400, 900);
        MinimumSize = new(1100, 760);
        BackColor = UiPalette.FromArgb(31, 35, 39);
        ForeColor = UiPalette.Gainsboro;
        name.Text = L.T("Mein Repertoire");
        name.TextChanged += (_, _) =>
        {
            if (!updatingName)
                nameEdited = true;
        };
        var right = new Panel
        {
            Dock = DockStyle.Right,
            Width = 620
        };
        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 180
        };
        controls.Dock = DockStyle.Fill;
        right.Controls.Add(controls);
        right.Controls.Add(footer);
        controls.BringToFront();
        Controls.Add(boardHost);
        Controls.Add(right);
        boardHost.Controls.AddRange([board, evaluation, evalInfo]);
        board.Position = position;
        void LayoutBoard()
        {
            int size = Math.Max(300, Math.Min(boardHost.Width - 60, boardHost.Height - 205));
            board.SetBounds(52, 14, size, size);
            evaluation.SetBounds(4, 14, 42, size);
            evalInfo.SetBounds(52, size + 22, size, 40);
            LayoutQueueNavigation(size);
        }

        boardHost.Resize += (_, _) => LayoutBoard();
        LayoutBoard();
        Add("Startsequenz: beide Seiten selbst spielen.");
        var back = new Button
        {
            Text = "Zug zurück",
            Width = 155
        };
        back.Click += (_, _) =>
        {
            if (operation is null && selectedJob is null && undo.TryPop(out var p))
            {
                position = p;
                RefreshPosition();
            }
        };
        var reset = new Button
        {
            Text = "Grundstellung",
            Width = 155
        };
        reset.Click += (_, _) =>
        {
            if (operation is null && selectedJob is null)
            {
                position = Position.FromFen(Position.InitialFen);
                undo.Clear();
                RefreshPosition();
            }
        };
        controls.Controls.Add(back);
        controls.Controls.Add(reset);
        Add("Name");
        controls.Controls.Add(name);
        color.Items.AddRange(["Schwarz lernen", "Weiß lernen"]);
        color.SelectedIndex = 0;
        color.SelectedIndexChanged += (_, _) =>
        {
            board.Flip = color.SelectedIndex == 0;
            board.Invalidate();
            evaluation.WhiteAtBottom = color.SelectedIndex == 1;
            evaluation.Invalidate();
            Dirty();
        };
        var perspectiveLabel = Add("Lernperspektive");
        controls.Controls.Add(color);
        var countRow = Field("Anzahl Tiefenabschnitte", sectionCount);
        var countLabel = countRow.Controls.OfType<Label>().Single();
        bool matchingSpacing = false, spacingQueued = false;
        void MatchInputSpacing()
        {
            if (matchingSpacing || !IsHandleCreated || IsDisposed) return;
            int delta = perspectiveLabel.Top - name.Bottom - (countRow.Top + countLabel.Top - color.Bottom);
            if (delta == 0) return;
            matchingSpacing = true;
            try
            {
                countRow.Margin = new(countRow.Margin.Left, Math.Max(0, countRow.Margin.Top + delta), countRow.Margin.Right, countRow.Margin.Bottom);
                controls.PerformLayout();
            }
            finally { matchingSpacing = false; }
        }
        void QueueSpacing()
        {
            if (spacingQueued || !IsHandleCreated || IsDisposed) return;
            spacingQueued = true;
            BeginInvoke((Action)(() => { spacingQueued = false; MatchInputSpacing(); }));
        }
        name.SizeChanged += (_, _) => QueueSpacing();
        color.SizeChanged += (_, _) => QueueSpacing();
        Shown += (_, _) => MatchInputSpacing();
        MatchInputSpacing();
        sections.Width = 575;
        controls.Controls.Add(sections);
        sectionCount.ValueChanged += (_, _) =>
        {
            sections.SetCount((int)sectionCount.Value);
            sections.Width = 575;
            Dirty();
        };
        sections.Changed += Dirty;
        Field("Komplexitätsprüfung ab Tiefe (Halbzüge)", min);
        min.ValueChanged += (_, _) => sections.ComplexityStartDepth = (int)min.Value;
        Field("Kartenlimit je Bestätigungsblock", cards);
        controls.Controls.Add(pauseAllAtLimit);
        Field("Parallele Arbeiter (dauerhafte Engines)", workers);
        Field("Threads je Arbeiter", threads);
        Add("Threads je Arbeiter: CPU-Suchthreads innerhalb einer Engine. Nicht fest 2. Arbeiter × Threads = gesamtes CPU-Budget. Mehr Arbeiter: mehr Stellungen parallel; mehr Threads: aufwendigere Suche einer Stellung.");
        workers.ValueChanged += (_, _) => threads.Maximum = Math.Min(16, 128 / (int)workers.Value);
        L.SetToolTip(tips, threads, "CPU-Suchthreads innerhalb jeder Stockfish-Engine, nicht fest 2. Mehr Threads können eine einzelne Stellung schneller untersuchen; mehr Arbeiter untersuchen verschiedene Stellungen gleichzeitig. Gesamtbudget = Arbeiter × Threads. Zu viele Threads können durch Konkurrenz langsamer sein.");
        controls.Controls.Add(merge);
        controls.Controls.Add(human);
        Field("Human-Labels: flache Engine-Suchtiefe", humanDepth);
        foreach (var check in new[]
        {
            merge,
            human
        }

        )
            check.CheckedChanged += (_, _) => Dirty();
        footer.Controls.Add(estimate);
        footer.Controls.Add(generate);
        generate.SetBounds(12, 4, 585, 32);
        estimate.SetBounds(12, 74, 585, 30);
        var cancel = new Button
        {
            Text = "Abbrechen / Teilbaum behalten",
            Width = 320,
            Height = 32
        };
        cancelJobButton = cancel;
        cancel.Click += (_, _) => { if (selectedJob is not null) workQueue.Cancel(selectedJob); else operation?.Cancel(); };
        footer.Controls.Add(cancel);
        footer.Controls.Add(status);
        cancel.SetBounds(12, 40, 585, 30);
        status.SetBounds(12, 108, 585, 70);
        estimate.Click += async (_, _) => { if (selectedJob is { State: GeneratorJobState.AwaitingApproval }) workQueue.Resume(selectedJob); else if (selectedJob is null) await Estimate(); };
        generate.Click += async (_, _) => await Generate();
        var navigation = new GeneratorInputNavigation(name, color, (ScrollNumber)sectionCount, sections, generate,
            (ScrollNumber)min, (ScrollNumber)cards, (ScrollNumber)workers, (ScrollNumber)threads, (ScrollNumber)humanDepth);
        Disposed += (_, _) => navigation.Dispose();
        board.SquareClicked += sq =>
        {
            if (operation is not null || selectedJob is not null || moving)
                return;
            if (selected is int from && TryBoardMove(from, sq))
                return;
            selected = sq;
            board.SelectedSquare = sq;
            board.CandidateMoves = MoveGenerator.GenerateLegal(position).Where(m => m.From == sq).ToList();
            board.Invalidate();
        };
        board.SquareDragged += (from, to) =>
        {
            if (operation is null && selectedJob is null && !moving)
                TryBoardMove(from, to, false);
        };
        board.BlankClicked += () =>
        {
            selected = null;
            board.SelectedSquare = null;
            board.CandidateMoves = [];
            board.Invalidate();
        };
        FormClosing += (_, e) =>
        {
            if (operation is not null)
            {
                operation.Cancel();
                e.Cancel = true;
                status.Text = "Abbruch läuft; danach kannst du das Fenster schließen.";
            }
        };
        FormClosed += (_, _) =>
        {
            queueClock.Stop(); queueClock.Dispose();
            queuePreview?.Close();
            draft?.Cancellation.Dispose();
            _ = workQueue.DisposeAsync();
            evalCancellation.Cancel();
            presenter.Dispose();
            tips.Dispose();
        };
        InitializeQueue(footer);
        RefreshPosition();
    }

    private static NumericUpDown N(decimal min, decimal max, decimal value, int places = 0) => new ScrollNumber
    {
        Minimum = min,
        Maximum = max,
        Value = value,
        DecimalPlaces = places,
        Increment = places == 0 ? 1 : .1m,
        EmptyDefaultValue = value,
        Width = 150
    };
    private Label Add(string text)
    {
        var label = new Label { Text = text, AutoSize = true, MaximumSize = new Size(570, 0), Margin = new Padding(0, 8, 0, 2) };
        controls.Controls.Add(label); return label;
    }
    private FlowLayoutPanel Field(string label, NumericUpDown field, int topMargin = 2)
    {
        var row = new FlowLayoutPanel
        {
            Width = 575,
            Height = 34,
            WrapContents = false,
            Margin = new(0, topMargin, 0, 2)
        };
        row.Controls.Add(new Label { Text = label, Width = 400, Height = 30 });
        row.Controls.Add(field);
        controls.Controls.Add(row);
        field.ValueChanged += (_, _) => Dirty();
        return row;
    }

    private void Dirty()
    {
        if (operation is null)
            status.Text = "Schätzung veraltet. Neu schätzen berücksichtigt diese Startstellung und die Filter.";
    }

    private GeneratorOptions Options() => new()
    {
        LearnWhite = color.SelectedIndex == 1,
        MinDepth = (int)min.Value,
        Sections = sections.Values,
        MaxDepth = sections.Values[^1].EndDepth,
        MaxPositions = (int)cards.Value,
        Threads = (int)(threads.Value * workers.Value),
        Workers = (int)workers.Value,
        MergeTranspositions = merge.Checked,
        HumanFilter = human.Checked,
        HumanDepth = (int)humanDepth.Value
    };
    private bool TryBoardMove(int from, int to, bool animate = true)
    {
        var moves = MoveGenerator.GenerateLegal(position).Where(m => m.From == from && m.To == to).ToList();
        if (moves.Count == 0)
            return false;
        var move = moves[0];
        if (moves.Count > 1)
        {
            var choice = PromotionPicker.Choose(this, moves, position.WhiteToMove);
            if (choice is null)
                return true;
            move = choice;
        }

        var before = position.Clone();
        undo.Push(before);
        position.Apply(move);
        AppSounds.PlayMove(before, move, position);
        RefreshPosition();
        if (animate)
            _ = Animate(before, move, position.Clone());
        return true;
    }

    private async Task Animate(Position before, ChessOpeningTrainer.Move move, Position after)
    {
        moving = true;
        board.InputEnabled = false;
        await board.AnimateAsync(before, move, after);
        moving = false;
        if (!IsDisposed)
            board.InputEnabled = operation is null && selectedJob is null;
    }

    private void RefreshPosition()
    {
        board.StopAnimation();
        board.Position = position;
        board.Flip = color.SelectedIndex == 0;
        selected = null;
        board.SelectedSquare = null;
        board.CandidateMoves = [];
        board.Invalidate();
        if (!nameEdited)
        {
            var p = Position.FromFen(Position.InitialFen);
            var sans = new List<string>();
            foreach (var u in SetupMoves())
            {
                var m = MoveGenerator.GenerateLegal(p).First(m => m.Uci == u);
                sans.Add(SanNotation.Format(p, m));
                p.Apply(m);
            }

            updatingName = true;
            name.Text = sans.Count == 0 ? L.T("Mein Repertoire") : string.Join(" ", sans);
            updatingName = false;
        }

        Dirty();
        AnalyzeSetup();
    }

    private IReadOnlyList<string> SetupMoves()
    {
        var states = undo.Reverse().Append(position).ToList();
        var moves = new List<string>();
        for (int i = 1; i < states.Count; i++)
        {
            var before = states[i - 1];
            var after = states[i];
            var m = MoveGenerator.GenerateLegal(before).First(m =>
            {
                var p = before.Clone();
                p.Apply(m);
                return p.ToFen() == after.ToFen();
            });
            moves.Add(m.Uci);
        }

        return moves;
    }

    private void AnalyzeSetup()
    {
        evaluation.WhiteAtBottom = color.SelectedIndex == 1;
        presenter.Show(position, true, false, true, MoveGenerator.GameState(position, new Dictionary<string, int>()));
        evalInfo.Text = "Startstellung · Hintergrundanalyse · + Weiß / − Schwarz";
    }

    private void Working(bool working)
    {
        board.InputEnabled = !working;
        generate.Enabled = estimate.Enabled = !working;
        foreach (Control c in controls.Controls)
            c.Enabled = !working;
    }

    private async Task Estimate()
    {
        operation = new();
        evalCancellation.Cancel();
        Working(true);
        status.Text = "Kurze Graphprobe mit Startsequenz und Kompression …";
        try
        {
            var options = Options();
            var setup = SetupMoves();
            var range = await TreeEstimator.Estimate(engine, position, options, setup, operation.Token);
            cachedEstimate = range;
            cachedEstimateKey = EstimateKey(options, setup);
            int prefixCards = setup.Select((_, i) => (i % 2 == 0) == options.LearnWhite).Count(b => b);
            double? perCard = timingHistory.SecondsPerCard(GeneratorTimingHistory.Configuration(options));
            double? minutes = perCard * Math.Max(0, range.Estimate - prefixCards) / 60;
            string eta = minutes is double time ? $"Zeitprognose ≈ {(time >= 60 ? $"{time / 60:0.0} h" : $"{Math.Max(1, time):0} min")} aus früheren Generierungen." : "Zeitprognose nach ersten generierten Karten verfügbar.";
            status.Text = range.Samples == 0 ? "Noch kein vollständiger Probenpfad. Erneut schätzen oder Einstellungen anpassen." : $"Grobe Baumprognose ≈ {range.Estimate:N0} Karten ({range.Samples} Pfade; Streuung {range.Low:N0}–{range.High:N0}). {eta} Stichprobe bis Engine-Tiefe 8; Kompression kann die Zahl reduzieren. Blocklimit: {cards.Value}.";
        }
        catch (OperationCanceledException)
        {
            status.Text = "Schätzung abgebrochen.";
        }
        catch (Exception ex)
        {
            status.Text = ex.Message;
        }
        finally
        {
            operation.Dispose();
            operation = null;
            Working(false);
        }
    }

    private Task Generate() { SubmitSelectedJob(); return Task.CompletedTask; }

    private string EstimateKey(GeneratorOptions options, IReadOnlyList<string> setup) => position.ToFen() + "|" + string.Join(",", setup) + "|" + GeneratorTimingHistory.Configuration(options);
}
