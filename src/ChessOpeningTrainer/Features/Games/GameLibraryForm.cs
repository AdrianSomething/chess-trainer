namespace ChessOpeningTrainer;
public sealed class GameLibraryForm : LocalizedForm
{
    private readonly PersonalGameStore store;
    private readonly StockfishEngine engine;
    private readonly GameList games = new()
    {
        Dock = DockStyle.Fill,
        BackColor = UiPalette.FromArgb(43, 49, 55),
        ForeColor = UiPalette.Gainsboro
    };
    private readonly PlayerSelector player = new()
    {
        Width = 210
    };
    private readonly StudyBoard study;
    private readonly Label status = new()
    {
        Dock = DockStyle.Bottom,
        Height = 55
    }, detail = new()
    {
        Dock = DockStyle.Fill,
        Height = 136
    };
    private readonly Button cancel = new()
    {
        Text = "Analyse abbrechen",
        AutoSize = true,
        Visible = false
    }, best = new()
    {
        Text = "Besten Zug zeigen",
        AutoSize = false,
        Dock = DockStyle.Fill
    }, improve = new()
    {
        Text = "Selbst verbessern",
        AutoSize = false,
        Dock = DockStyle.Fill
    };
    private readonly CancellationTokenSource lifetime = new();
    private readonly GameAnalysisQueue analysisQueue;
    private bool working;
    private bool selecting, improving;
    private GameMoment? moment;
    private GameEvaluationForm? evaluationGraph;
    private int highlightIndex = -1;
    public GameLibraryForm(StockfishEngine engine, string dataDir, string? selectedGameId = null, bool analyzeOnOpen = false, Task? preparedAnalysis = null, int initialPositionIndex = 0, AnalysisProgressChannel? preparedProgress = null, string? initialPlayer = null, Action? cancelPreparedAnalysis = null)
    {
        this.engine = engine;
        Task? attachedPreparedCompletion=null;
        store = new(dataDir);
        analysisQueue = new((g,p,t)=>store.Analyze(g,engine,p,t));
        Disposed += (_,_)=>analysisQueue.Dispose();
        Text = "Eigene Spiele & Analyse";
        Size = new(1500, 920);
        MinimumSize = new(1150, 740);
        BackColor = UiPalette.FromArgb(31, 35, 39);
        ForeColor = UiPalette.Gainsboro;
        study = new(engine)
        {
            Dock = DockStyle.Fill
        };
        var left = new Panel
        {
            Dock = DockStyle.Left,
            Width = 310
        };
        left.Controls.AddRange([games, status]);
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 58
        };
        Button Button(string text) => new()
        {
            Text = text,
            AutoSize = true
        };
        var import = Button("PGN importieren");
        var write = Button("PGN schreiben");
        var remove = Button("Partie entfernen");
        var all = Button("Fehlende Partien analysieren");
        var graph = Button("Evaluationsgraph anzeigen");
        var highlights = Button("Highlights durchgehen");
        var settings = new MenuIconButton("settings") { Width = 40, Height = 34, AccessibleName = "Analyse-Einstellungen" };
        toolbar.Controls.AddRange([import, write, remove, all, graph, new Label { Text = "Standardspieler:", AutoSize = true, Margin = new(22, 6, 3, 0) }, player, cancel]);
        var frequencies = new CheckBox { Text = "Partiestatistik", AutoSize = true, Margin = new(8,6,3,0) };
        frequencies.CheckedChanged += (_, _) => study.ShowDatasetStatistics = frequencies.Checked;
        toolbar.Controls.Add(frequencies);
        settings.Dock = DockStyle.Right;
        Controls.Add(study);
        Controls.Add(left);
        Controls.Add(toolbar);
        var headerHost = new Panel { Dock = DockStyle.Top, Height = 58 };
        Controls.Remove(toolbar); toolbar.Dock = DockStyle.Fill;
        settings.Dock = DockStyle.Right;
        headerHost.Controls.Add(toolbar); headerHost.Controls.Add(settings); Controls.Add(headerHost);
        toolbar.Layout += (_, _) =>
        {
            int height = Math.Max(58, toolbar.Controls.Cast<Control>().Where(c => c.Visible).Select(c => c.Bottom + 5).DefaultIfEmpty(58).Max());
            if (headerHost.Height != height) headerHost.Height = height;
        };
        study.Details.Controls.Add(detail);
        highlights.Dock = DockStyle.Top;
        highlights.AutoSize = false;
        highlights.Height = 88;
        highlights.Font = new Font(Font.FontFamily, 14, FontStyle.Bold);
        study.Details.Controls.Add(highlights);
        var actionHost = new Panel { Dock = DockStyle.Bottom, Height = 112 };
        var actions = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            ColumnCount = 4,
            RowCount = 1
        };
        foreach (float width in new[] { 15f, 35f, 35f, 15f })
            actions.ColumnStyles.Add(new(SizeType.Percent, width));
        var previousMove = new Button { Text = "←", Dock = DockStyle.Fill, AccessibleName = "Vorheriger Zug" };
        var nextMove = new Button { Text = "→", Dock = DockStyle.Fill, AccessibleName = "Nächster Zug" };
        var nextHighlight = Button("Nächster Highlightzug");
        nextHighlight.AutoSize = false;
        nextHighlight.Dock = DockStyle.Top;
        nextHighlight.Height = 62;
        nextHighlight.Font = new Font(Font.FontFamily, 13, FontStyle.Bold);
        previousMove.Click += async (_, _) => await study.PreviousMoveAsync();
        nextMove.Click += async (_, _) => await study.ReplayTo(study.CurrentIndex + 1);
        actions.Controls.Add(previousMove, 0, 0);
        actions.Controls.Add(best, 1, 0);
        actions.Controls.Add(improve, 2, 0);
        actions.Controls.Add(nextMove, 3, 0);
        actionHost.Controls.Add(actions);
        actionHost.Controls.Add(nextHighlight);
        var categoryIcon = new MenuIconButton("assessment-good") { Width = 36, Height = 36, Dock = DockStyle.Top, Enabled = false };
        var iconColumn = new Panel { Width = 38, Dock = DockStyle.Right };
        iconColumn.Controls.Add(categoryIcon);
        study.Details.Controls.Add(iconColumn);
        study.Details.Controls.Add(actionHost);
        settings.Click += (_, _) => SettingsForm.OpenGameAnalysis();
        cancel.Click += (_, _) => { if(games.SelectedItem is PersonalGame g) analysisQueue.Cancel(g.Id); };
        void Reload(string? id = null)
        {
            selecting = true;
            games.Items.Clear();
            games.Items.AddRange(store.Games.OrderByDescending(g => g.Tags.GetValueOrDefault("Date")).Cast<object>().ToArray());
            games.SelectedItem = games.Items.Cast<PersonalGame>().FirstOrDefault(g => g.Id == id) ?? games.Items.Cast<PersonalGame>().FirstOrDefault();
            string previous = player.SelectedItem?.ToString() ?? initialPlayer ?? "";
            player.Items.Clear();
            player.Items.AddRange(store.Games.SelectMany(g => new[] { g.White, g.Black }).Where(s => !string.IsNullOrWhiteSpace(s)).GroupBy(s => s).OrderByDescending(g => g.Count()).Select(g => (object)g.Key).ToArray());
            player.SelectedItem = player.Items.Cast<string>().FirstOrDefault(s => s == previous) ?? player.Items.Cast<string>().FirstOrDefault();
            selecting = false;
            LoadSelected();
        }

        string? perspectiveGame = null;
        static bool HasAnalysis(PersonalGame game) => game.HasCompleteAnalysis;
        void SynchronizeGraph()
        {
            if (evaluationGraph is not { IsDisposed: false }) return;
            if (games.SelectedItem is PersonalGame g) evaluationGraph.Synchronize(g, study.CurrentIndex, study.BottomWhite);
            else evaluationGraph.Close();
        }
        graph.Click += (_, _) =>
        {
            if (games.SelectedItem is not PersonalGame g || !HasAnalysis(g)) return;
            if (evaluationGraph is not { IsDisposed: false })
            {
                evaluationGraph = new();
                evaluationGraph.PositionSelected += async i => await study.NavigateToHistoryIndexAsync(i);
                evaluationGraph.FormClosed += (_, _) => evaluationGraph = null;
                evaluationGraph.Synchronize(g, study.CurrentIndex, study.BottomWhite);
                evaluationGraph.Show(this);
            }
            else { SynchronizeGraph(); evaluationGraph.Activate(); }
        };
        void LoadSelected()
        {
            improving = false;
            highlightIndex = -1;
            moment = null;
            if (games.SelectedItem is PersonalGame g)
            {
                if (perspectiveGame != g.Id && (initialPlayer is null || g.Id != selectedGameId) && g.Tags.TryGetValue("Owner", out var own) && player.Items.Contains(own))
                    player.SelectedItem = own;
                perspectiveGame = g.Id;
                study.LoadGame(g);
                study.SetFlip(g.Black == player.SelectedItem?.ToString());
            }

            ShowDetail();
        }

        void ShowDetail()
        {
            bool analyzed = games.SelectedItem is PersonalGame selectedGame && HasAnalysis(selectedGame);
            moment = analyzed && games.SelectedItem is PersonalGame rated ? rated.Analysis.FirstOrDefault(m => m.Ply == study.CurrentIndex - 1) : null;
            graph.Enabled = analyzed;
            SynchronizeGraph();
            var selectedId=(games.SelectedItem as PersonalGame)?.Id;
            bool pending=selectedId is not null && analysisQueue.IsPending(selectedId);
            iconColumn.Visible = moment is not null && !pending;
            highlights.Text = pending ? analysisQueue.IsRunning(selectedId!) ? "Analyse abbrechen" : "Aus Warteschlange entfernen" : analyzed ? "Highlights durchgehen" : "Fehler analysieren";
            highlights.Visible = games.SelectedItem is PersonalGame && (pending || !analyzed || study.CurrentIndex == 0);
            highlights.Enabled = true;
            previousMove.Visible = study.CurrentIndex > 0;
            nextHighlight.Visible = analyzed && !pending && study.CurrentIndex > 0;
            actionHost.Height = nextHighlight.Visible ? 112 : 44;
            best.Visible = improve.Visible = false;
            if (games.SelectedItem is not PersonalGame g)
            {
                detail.Text = "Partie importieren oder PGN schreiben.";
                return;
            }
            if(pending) { detail.Text=analysisQueue.Describe(g.Id)+$"\n{analysisQueue.PendingCount} Partien in Arbeit / Warteschlange"; return; }

            if (moment is null)
            {
                detail.Text = "Zug in der Historie auswählen oder Highlights durchgehen.";
                return;
            }

            var p = g.Positions()[moment.Ply];
            var move = MoveGenerator.GenerateLegal(p).First(m => m.Uci == g.Moves[moment.Ply]);
            double sign = p.WhiteToMove ? 1 : -1;
            categoryIcon.IconName = moment.Category switch { "Patzer" => "assessment-blunder", "Verpasste Chance" => "assessment-missed", "Schlecht" => "assessment-mistake", "Bester Zug" => "assessment-best", _ => "assessment-good" };
            if (study.CurrentIndex == g.Moves.Count && g.Tags.GetValueOrDefault("Result") is string result && result != "*")
                categoryIcon.IconName = result == "1/2-1/2" ? "assessment-draw"
                    : (result == "1-0") == study.BottomWhite ? "assessment-mate" : "assessment-lost";
            string category = moment.Category == "Lektion" ? "Gut" : moment.Category;
            detail.Text = GameHighlights.Describe(moment) + "\n" + $"{p.Fullmove}{(p.WhiteToMove ? "." : "…")} {SanNotation.Format(p, move)} · {category}\nBestzug {sign * moment.Before:+0.00;-0.00;0.00} → gespielt {sign * moment.After:+0.00;-0.00;0.00} (Weißsicht)\nVerlust {moment.Loss:0.00} · gewichtet {moment.WeightedLoss:0.00}\nGespeicherte Tiefe {g.AnalysisOptions?.Depth.ToString() ?? "alt"}";
            best.Visible = moment.Category != "Bester Zug";
            improve.Visible = moment.Category is "Patzer" or "Schlecht" or "Verpasste Chance";
        }

        games.SelectedIndexChanged += (_, _) =>
        {
            if (!selecting)
                LoadSelected();
        };
        player.SelectionChangeCommitted += (_, _) => LoadSelected();
        study.SelectionChanged += index =>
        {
            if (selecting)
                return;
            improving = false;
            if (games.SelectedItem is PersonalGame g)
                moment = HasAnalysis(g) ? g.Analysis.FirstOrDefault(m => m.Ply == index - 1) : null;
            ShowDetail();
        };
        study.MoveTried += (before, move, after) =>
        {
            if (!improving || moment is null || games.SelectedItem is not PersonalGame g)
                return;
            bool correct = moment.BestMoves.Contains(move.Uci);
            detail.Text = correct ? "✓ Verbesserung gefunden. Die Originalpartie bleibt unverändert." : "Dieser Zug gehört nicht zu den gespeicherten gleichwertigen Verbesserungen. Erneut versuchen.";
            if (!correct)
            {
                selecting = true;
                study.PreviewPositionIndex(moment.Ply);
                selecting = false;
            }
            else
                improving = false;
        };
        best.Click += (_, _) =>
        {
            if (moment?.BestMoves.FirstOrDefault()is not string u)
                return;
            improving = false;
            selecting = true;
            study.PreviewPositionIndex(moment.Ply);
            study.ExploreMove(u);
            selecting = false;
        };
        improve.Click += (_, _) =>
        {
            if (moment is null)
                return;
            selecting = true;
            study.PreviewPositionIndex(moment.Ply);
            selecting = false;
            improving = true;
            detail.Text = "Finde eine Verbesserung für diesen Zug. Alle gespeicherten gleichwertigen Antworten werden akzeptiert.";
        };
        async void Highlight(object? sender, EventArgs args)
        {
            if (games.SelectedItem is not PersonalGame g) return;
            if (!HasAnalysis(g)) return;
            string username = study.BottomWhite ? g.White : g.Black;
            var relevant = GameHighlights.Build(g, username);
            if (relevant.Count == 0) return;
            int nextHighlight = relevant.FindIndex(h => h.PositionIndex > study.CurrentIndex);
            if (nextHighlight < 0) nextHighlight = 0;
            highlightIndex = nextHighlight;
            var target = relevant[nextHighlight];
            Task animation = study.ReplayTo(target.PositionIndex, study.CurrentIndex == 0, highlightOnly: true);
            // ReplayTo commits its destination before the first animation frame.
            // Show the final highlight's rating now, not after its entire prefix.
            moment = g.Analysis.FirstOrDefault(m => m.Ply == target.PositionIndex - 1);
            improving = false;
            ShowDetail();
            detail.Text = target.Description + "\n" + string.Join("\n", detail.Text.Split('\n').Skip(1));
            status.Text = $"Perspektive: {username} · Highlight {nextHighlight + 1}/{relevant.Count}";
            await animation;
        }
        highlights.Click += async (sender, args) =>
        {
            if (games.SelectedItem is not PersonalGame g) return;
            if (analysisQueue.IsPending(g.Id)) analysisQueue.Cancel(g.Id);
            else if (!HasAnalysis(g)) await Analyze([g]);
            else Highlight(sender, args);
        };
        nextHighlight.Click += Highlight;
        study.OrientationChanged += () => { highlightIndex = -1; ShowDetail(); };
        async Task Import(string pgn)
        {
            try
            {
                var parsed = await Task.Run(() => PgnImport.Parse(pgn));
                if (IsDisposed)
                    return;
                int count = store.ImportGames(parsed);
                Reload();
                status.Text = $"{count} neue Partien importiert. Doppelte übersprungen.";
            }
            catch (Exception ex)
            {
                if (!IsDisposed)
                    L.Message(this, ex.Message, "PGN-Import");
            }
        }

        import.Click += async (_, _) =>
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "Schachpartien (*.pgn)|*.pgn",
                Multiselect = true
            };
            if (dialog.ShowDialog(this) == DialogResult.OK)
                foreach (string file in dialog.FileNames)
                    await Import(await File.ReadAllTextAsync(file));
        };
        write.Click += async (_, _) =>
        {
            using var dialog = new PgnComposer(store.Games);
            if (dialog.ShowDialog(this) == DialogResult.OK)
                await Import(dialog.Pgn);
        };
        remove.Click += (_, _) =>
        {
            if (games.SelectedItem is not PersonalGame g)
                return;
            if (L.Message(this, "Partie samt gespeicherter Analyse entfernen? Die ursprüngliche PGN-Datei bleibt unverändert.", "Partie entfernen", MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;
            analysisQueue.Cancel(g.Id); store.Remove(g);
            Reload();
        };
        async Task Analyze(List<PersonalGame> pending, bool usePrepared = false)
        {
            var tasks=new List<Task>();
            foreach(var game in pending)
            {
                var prepared=game.Id==selectedGameId ? preparedAnalysis : null;
                var completion=analysisQueue.Enqueue(game,prepared,prepared is null ? null : cancelPreparedAnalysis);
                tasks.Add(completion);
                if(prepared is not null)attachedPreparedCompletion=completion;
                if(prepared is not null)preparedAnalysis=null;
            }
            ShowDetail(); await Task.WhenAll(tasks);
        }
        analysisQueue.Changed += id => _ = OnUi(()=>
        {
            working=analysisQueue.PendingCount>0;
            status.Text=analysisQueue.Describe(id)+$"\n{analysisQueue.PendingCount} Partien in Arbeit / Warteschlange";
            cancel.Visible=false; // cancellation lives in the selected game's large action button
            if(games.SelectedItem is PersonalGame selected && selected.Id==id) ShowDetail();
        });

        all.Click += async (_, _) => await Analyze(store.Games.Where(g => !HasAnalysis(g)).ToList());
        FormClosed += (_, _) => lifetime.Cancel();
        Reload(selectedGameId);
        if (initialPositionIndex > 0 && games.SelectedItem is PersonalGame initialGame) study.ShowIndex(Math.Clamp(initialPositionIndex, 0, initialGame.Moves.Count));
        if (preparedProgress is not null)
        {
            void ProgressUpdate(string message) => _ = OnUi(() =>
            {
                if(games.SelectedItem is PersonalGame selected && selected.Id==selectedGameId
                    && (attachedPreparedCompletion is null || attachedPreparedCompletion is { IsCompleted:false } && analysisQueue.IsPending(selected.Id))) status.Text=message;
            });
            preparedProgress.Changed += ProgressUpdate;
            if (!string.IsNullOrEmpty(preparedProgress.Latest)) status.Text = preparedProgress.Latest;
            Disposed += (_, _) => preparedProgress.Changed -= ProgressUpdate;
        }
        if (analyzeOnOpen) Shown += async (_, _) => { if (games.SelectedItem is PersonalGame g) await Analyze([g], true); };
    }

    private async Task OnUi(Action update)
    {
        if (IsDisposed || Disposing) return;
        try
        {
            if (InvokeRequired) await this.InvokeAsync(() => { if (!IsDisposed && !Disposing) update(); });
            else update();
        }
        catch (Exception ex) when ((ex is ObjectDisposedException or InvalidOperationException or OperationCanceledException) && (IsDisposed || Disposing)) { }
    }
}
