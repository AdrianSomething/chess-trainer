namespace ChessOpeningTrainer;

public sealed partial class MainForm
{
    private TutorialSession? tutorial;
    private TutorialWelcome? welcome;
    internal bool IsHomeWindow => !learningOnly;
    internal void OfferTutorial()
    {
        if (learningOnly || tutorial is not null) return;
        if (welcome is { IsDisposed: false }) { welcome.BringToFront(); return; }
        welcome = new TutorialWelcome(StartTutorial);
        Controls.Add(welcome); welcome.BringToFront();
    }
    private void StartTutorial()
    {
        var existing = Application.OpenForms.Cast<Form>().ToHashSet();
        bool oldDemo = Preferences.Current.VisionStatisticsDemo;
        string? oldLearningOpening = Preferences.Current.LastLearningOpening;
        string tutorialData = Path.Combine(dataDir, "Tutorial");
        var tutorialProgress = new ProgressStore(tutorialData);
        var openings = OpeningStore.Load(Path.Combine(AppContext.BaseDirectory, "Openings"), out _);
        var example = openings.FirstOrDefault(o => o.Name.StartsWith("Example_white", StringComparison.OrdinalIgnoreCase))
            ?? openings.FirstOrDefault(o => o.LearnsWhite) ?? openings.FirstOrDefault();
        if (example is null) { L.Message(this, "Bitte zuerst ein Beispiel-Opening hinzufügen.", "Überblick"); return; }
        tutorialProgress.ResetOpening(example);
        MainForm? lesson = null; BranchForm? branchWindow = null;
        OpeningGeneratorForm? generator = null; PuzzleSessionForm? puzzle = null;
        FreeGameForm? free = null; GameLibraryForm? games = null; VisionForm? vision = null;
        VisionStatisticsForm? statistics = null; GameEvaluationForm? graph = null;
        var steps = new List<TutorialStep>();
        Func<Form?> root = () => this;
        Control? Find(Form f, string text) => TutorialSession.Find(f, text);
        void Info(Func<Form?> window, Func<Form, Control?> target, string text, Action? enter = null, Action? leave = null, bool allowInput = false) => steps.Add(new(window, target, text, Enter: enter, Leave: leave, AllowInput: allowInput));
        void Click(Func<Form?> window, Func<Form, Control?> target, string text, Action? enter = null, Action? leave = null, Func<bool>? complete = null, Action? space = null, bool autoAdvance = true, Func<Control, Rectangle>? area = null)
        {
            // Explicit arrow navigation may skip an exercise, but still creates
            // the window/state needed by the following information cards.
            void Skip()
            {
                if (window() is not { } host) return;
                switch (target(host))
                {
                    case Button button: button.PerformClick(); break;
                    case BranchCanvas: branchWindow?.SelectTutorialCurrent(); break;
                    case BoardControl when host == lesson: lesson?.next.PerformClick(); break;
                    case ComboBox combo when combo.Items.Count > 1: combo.SelectedIndex = (combo.SelectedIndex + 1) % combo.Items.Count; break;
                    case TabControl tabs when tabs.TabCount > 1: tabs.SelectedIndex = (tabs.SelectedIndex + 1) % tabs.TabCount; break;
                }
            }
            steps.Add(new(window, target, text, true, complete, enter, leave, SpaceAction: space, Skip: Skip, AutoAdvance: autoAdvance, TargetArea: area));
        }
        Control? HomeCard(Form f) => TutorialSession.Descendants(f).OfType<Label>().FirstOrDefault(l => l.Text == example.Name)?.Parent;
        Control? HomeLearn(Form f) => HomeCard(f)?.Controls.Cast<Control>().FirstOrDefault(c => c.Text == L.T("Lernen"));
        Info(root, _ => home, "Trainiere Openings, löse Aufgaben und verstehe deine Partien. Alles läuft lokal auf deinem Computer.");
        Info(root, f => HomeCard(f)?.Parent, "Hier stehen deine Openings. Jede Fortschrittsleiste zeigt Karten in vier Farben: gelernt, zu wiederholen, angeschaut und unbekannt.");
        // Use a private practice profile; the real Learn button is highlighted, but
        // its normal handler must not write the user's actual learning progress.
        Click(root, HomeLearn, "Öffne „Lernen“ für das Beispiel-Opening.", enter: () => { tutorialProgress.ResetOpening(example); tutorialLearning = (example, tutorialProgress); }, leave: () =>
        {
            lesson = Application.OpenForms.OfType<MainForm>().LastOrDefault(f => f.learningOnly && !existing.Contains(f));
            tutorialLearning = null;
        });
        void MarkLessonMove()
        {
            if (lesson is { question: true } && lesson.node?.Moves.FirstOrDefault() is { } m && ChessOpeningTrainer.Move.TryParseUci(m.Uci, out var move))
            { lesson.board.HintMove = move; lesson.board.Invalidate(); }
        }
        Info(() => lesson, _ => lesson?.board, "Auf diesem Brett lernst du dein Opening. Jedes Opening hat eine feste Lernperspektive.");
        Click(() => lesson, _ => lesson?.board, "Spiele jetzt den markierten Zug auf dem Brett.", enter: MarkLessonMove, complete: () => { MarkLessonMove(); return lesson is { question: false }; },
            leave: () => { if (lesson is not null) lesson.board.HintMove = null; });
        string? firstKey = null;
        Click(() => lesson, _ => lesson?.next, "Gut! Gehe mit „Nächste Karte“ zur nächsten Lernstellung.", enter: () => firstKey = lesson?.targetKey, complete: () => lesson?.targetKey != firstKey, space: () => lesson?.next.PerformClick());
        Click(() => lesson, _ => lesson?.next, "Wenn du dir beim richtigen Zug unsicher bist, nutze „Lösung zeigen“. Die Karte wird später erneut abgefragt.", complete: () => lesson is { question: false }, space: () => lesson?.next.PerformClick());
        Info(() => lesson, _ => lesson?.variations, "Die Enginevarianten helfen beim Verstehen. Du kannst ihre Züge anklicken und eigene Fortsetzungen ausprobieren.");
        Info(() => lesson, _ => lesson?.history, "Die Zugfolge zeigt, wie die Stellung entstanden ist. „Zur aktuellen Stellung“ bringt dich zurück.");
        Click(() => lesson, _ => lesson?.branch, "Öffne diese Stellung mit „In Branch anzeigen“.", leave: () =>
        {
            branchWindow = Application.OpenForms.OfType<BranchForm>().LastOrDefault(f => !existing.Contains(f));
            branchWindow?.CollapseTutorialSettings();
        });
        Info(() => branchWindow, f => TutorialSession.Of<BranchCanvas>(f), "Der Branch zeigt Antworten und Abzweigungen. Ziehe zum Verschieben und nutze das Mausrad zum Zoomen.",
            enter: () => { branchWindow?.ClearTutorialSelection(); branchWindow?.SetTutorialPanOnly(true); },
            leave: () => branchWindow?.SetTutorialPanOnly(false), allowInput: true);
        Click(() => branchWindow, f => TutorialSession.Of<BranchCanvas>(f), "Klicke auf einen Knoten, um seine Stellung und Varianten anzusehen.",
            enter: () => branchWindow?.ClearTutorialSelection(),
            complete: () => branchWindow is not null && (TutorialSession.Of<BranchCanvas>(branchWindow) as BranchCanvas)?.HasSelection == true);
        Info(() => branchWindow, f => TutorialSession.Of<BoardControl>(f)?.Parent, "Rechts findest du Brett, Stellungsvarianten, Lernpriorität und Aktionen: gezielt lernen, tiefer erforschen oder als eigenes Opening speichern.");
        Info(() => branchWindow, f => TutorialSession.Descendants(f).OfType<ComboBox>().FirstOrDefault(c => c.Items.Count == 3 && c.Items.Cast<object>().Any(i => i.ToString() is "Bearbeitungsmodus" or "Edit mode")),
            "Lernmodus ist die Standardansicht. Bearbeitungsmodus verändert den Baum; Analysemodus verknüpft ihn mit deinen Partien.");
        Info(root, f => HomeCard(f), "Jedes Opening hat auch direkt einen Branch-Knopf. Freeplay lässt dich dein Repertoire zusammenhängend spielen.");
        Click(root, f => Find(f, "Opening-Branch-Generator"), "Öffne den „Opening-Branch-Generator“.", leave: () => generator = generatorWindow);
        Info(() => generator, f => TutorialSession.Of<BoardControl>(f), "Erstelle hier eigene Repertoires durch automatische Engineanalyse. Links spielst du die Startsequenz für beide Seiten.");
        Info(() => generator, f => TutorialSession.Of<SectionEditor>(f)?.Parent, "Rechts bestimmst du Perspektive, Tiefen und Filter. Schätze zuerst die Kartenanzahl; für den Einstieg sind höchstens etwa 1.000 Karten sinnvoll.");
        Info(() => generator, f => Find(f, "Auftrag erstellen – Lernkarten generieren"), "Starte hier einen Auftrag. Du kannst weitere Aufträge hinzufügen und bei einem Kartenlimit gezielt fortsetzen.");
        Click(root, f => Find(f, "Rätselmodus"), "Öffne „Rätselmodus“. Hier trainierst du Eröffnung, Mittelspiel und Endspiel.", leave: () => puzzle = puzzleWindow);
        Info(() => puzzle, f => Find(f, "Taktik"), "Taktik trainiert Matt, Materialgewinn und gute Mittelspielzüge. Zeit und Leben sind optional.");
        Info(() => puzzle, f => Find(f, "Endspiel"), "Endspieltraining übt konkrete Ziele wie Mattsetzen, Umwandeln und Verteidigen.");
        Info(() => puzzle, f => Find(f, "Aus Fehlern lernen"), "Aus Fehlern lernen macht Aufgaben aus deinen analysierten Partien.");
        Info(() => puzzle, f => Find(f, "Gegnerfehler ausnutzen"), "Gegnerfehler ausnutzen trainiert Antworten auf schwache Abweichungen von deinen Openings.");
        Click(root, f => Find(f, "Freies Spiel"), "Öffne „Freies Spiel“. Spiele beliebige Stellungen mit integrierter Engineanalyse.", leave: () => free = Application.OpenForms.OfType<FreeGameForm>().LastOrDefault(f => !existing.Contains(f)));
        Info(() => free, f => TutorialSession.Of<PositionSetupControl>(f), "Stelle rechts Figuren und Regeln ein. „Mit dieser Stellung spielen“ startet die Partie; danach kannst du allein oder gegen die Engine spielen.");
        Info(root, f => Find(f, "Lokales Spiel"), "Lokales Spiel ist für zwei Menschen an einem Gerät, mit optionalen Schachuhren.");
        Click(root, f => Find(f, "Eigene Spiele und Analysen"), "Öffne „Eigene Spiele und Analysen“. Für den Überblick verwenden wir eine separate Beispielpartie.", enter: () => tutorialGamesDirectory = tutorialData,
            leave: () => { games = gamesWindow; tutorialGamesDirectory = null; });
        Info(() => games, f => TutorialSession.Of<GameList>(f), "In der Partienliste wählst du eine Partie aus. Das Beispiel ist bereits analysiert.");
        Info(() => games, f => Find(f, "PGN importieren"), "Importiere eine PGN-Datei mit einer oder mehreren Partien oder schreibe eine eigene PGN.");
        Info(() => games, f => Find(f, "Highlights durchgehen") ?? Find(f, "Analyse abbrechen"), "Highlights zeigen wichtige Entscheidungen der bereits analysierten Beispielpartie.");
        Click(() => games, f => Find(f, "Evaluationsgraph anzeigen"), "Öffne „Evaluationsgraph anzeigen“.", leave: () => graph = Application.OpenForms.OfType<GameEvaluationForm>().LastOrDefault());
        int graphIndex = 0;
        Click(() => graph, _ => graph?.Graph, "Klicke auf eine Stelle im Graphen: Das Brett springt zum zugehörigen Zug.",
            enter: () => graphIndex = graph?.Graph.CurrentIndex ?? 0, complete: () => graph?.Graph.CurrentIndex != graphIndex, autoAdvance: false);
        Click(root, f => Find(f, "Schachblick"), "Öffne „Schachblick“. Hier verbessern besondere Schachübungen deine allgemeinen Schachfähigkeiten.", leave: () => vision = Application.OpenForms.OfType<VisionForm>().LastOrDefault(f => !existing.Contains(f)));
        string[] visionInfo = ["Angreifer & Verteidiger: Erkenne, welche Figuren ein Feld kontrollieren.", "Angriffe im nächsten Zug: Finde mögliche neue Angriffe und Verteidigungen.", "Gegnerische Angriffe: Markiere die direkten Bedrohungen deiner Figuren.", "Figur retten: Finde das einzige sichere Zielfeld.", "Zugvergleich: Entscheide, welche von zwei Fortsetzungen besser ist.", "Figuren merken: Präge dir eine Stellung ein und baue sie wieder auf."];
        for (int i = 0; i < visionInfo.Length; i++)
        {
            int mode = i;
            Info(() => vision, f => TutorialSession.Descendants(f).OfType<VisionModeButton>().Where(b => b.Visible).ElementAtOrDefault(mode), visionInfo[i]);
        }
        Click(() => vision, f => TutorialSession.Descendants(f).OfType<VisionModeButton>().LastOrDefault(b => b.Visible), "Öffne „Statistik“. Analysiere deine Leistung pro Spielmodus im Zeitverlauf. Der Überblick zeigt vorübergehend 400 Tage Demodaten.",
            enter: () => { Preferences.Current.VisionStatisticsDemo = true; Preferences.Save(); },
            leave: () =>
            {
                statistics = Application.OpenForms.OfType<VisionStatisticsForm>().LastOrDefault();
                if (statistics is not null) foreach (var c in TutorialSession.Descendants(statistics).OfType<ComboBox>().Where(c => c.Items.Count == 4)) c.SelectedIndex = 2;
            });
        int oldTab = -1, oldMetric = -1;
        Click(() => statistics, f => TutorialSession.Of<TabControl>(f), "Wähle einen anderen Submodus-Tab, um dessen Statistik zu sehen.",
            enter: () => oldTab = (TutorialSession.Of<TabControl>(statistics!) as TabControl)?.SelectedIndex ?? -1,
            complete: () => (TutorialSession.Of<TabControl>(statistics!) as TabControl)?.SelectedIndex != oldTab, autoAdvance: false,
            area: c => new Rectangle(0, 0, c.Width, ((TabControl)c).GetTabRect(0).Bottom + 3));
        ComboBox? Metric() => statistics is null ? null : TutorialSession.Descendants(statistics).OfType<ComboBox>().FirstOrDefault(c => c.Visible && c.Items.Count == 5);
        Click(() => statistics, _ => Metric(), "Wähle einen Statistiktyp, etwa Geschwindigkeit oder Fehlerzusammensetzung.",
            enter: () => oldMetric = Metric()?.SelectedIndex ?? -1, complete: () => Metric()?.SelectedIndex != oldMetric, autoAdvance: false);
        ComboBox? Period() => statistics is null ? null : TutorialSession.Descendants(statistics).OfType<ComboBox>().FirstOrDefault(c => c.Visible && c.Items.Count == 4);
        int oldPeriod = -1;
        Click(() => statistics, _ => Period(), "Wähle eine andere Zeitgruppierung: Tage, Wochen oder Monate.",
            enter: () => oldPeriod = Period()?.SelectedIndex ?? -1, complete: () => Period()?.SelectedIndex != oldPeriod, autoAdvance: false);
        Info(() => statistics, f => TutorialSession.Of<VisionTimelineChart>(f), "Jetzt kannst du deine Statistik erkunden.", allowInput: true);
        Info(root, f => Find(f, "Datensätze"), "Datensätze zeigen, wie häufig Stellungen und Züge in gespielten Partien vorkommen und wie diese Partien ausgegangen sind.");
        Info(root, f => Find(f, "Datensätze"), "Im Datensatzmodus kannst du Lichess-Monatsarchive herunterladen oder PGN-Dateien importieren, filtern und zu einem Häufigkeitsbaum zusammenfassen. Große Archive benötigen Zeit und freien Speicher.");
        Info(root, _ => home, "Du bist startklar. Den Überblick kannst du jederzeit oben in den allgemeinen Einstellungen erneut öffnen.");
        var lessonTargets = OpeningStore.LearningTargets(example).Take(2).ToArray();
        Position branchPosition = Position.FromFen(example.StartFen ?? Position.InitialFen);
        void Prepare(int destination, int previous)
        {
            // Each step is a reproducible checkpoint, not merely a text index.
            // Close completed tutorial windows; backwards navigation recreates
            // only the window(s) required by its destination.
            tutorialLearning = null; tutorialGamesDirectory = null;
            var keep = new HashSet<Form> { this };
            bool Missing(Form? form) => form is null || form.IsDisposed || existing.Contains(form);
            if (destination is >= 3 and <= 9)
            {
                if (Missing(lesson)) { lesson = new MainForm(true, tutorialProgress); lesson.Show(); }
                bool second = destination >= 6;
                var target = lessonTargets[Math.Min(second ? 1 : 0, lessonTargets.Length - 1)];
                lesson!.RestoreTutorialLesson(example, target, destination == 5 || destination >= 7);
                if (destination == 5) lesson.forcedTarget = lessonTargets[Math.Min(1, lessonTargets.Length - 1)];
                branchPosition = lesson.position.Clone(); keep.Add(lesson);
            }
            else if (destination is >= 10 and <= 13)
            {
                if (Missing(branchWindow))
                { branchWindow = new BranchForm(example, tutorialProgress, engine, branchPosition); branchWindow.Show(); }
                branchWindow!.CollapseTutorialSettings();
                branchWindow.SetTutorialPanOnly(destination == 10);
                if (destination <= 11) branchWindow.ClearTutorialSelection(); else branchWindow.EnsureTutorialSelection();
                keep.Add(branchWindow);
            }
            else if (destination is >= 16 and <= 18)
            {
                if (Missing(generator)) { generator = new OpeningGeneratorForm(engine, AppContext.BaseDirectory); generator.Show(); }
                keep.Add(generator!);
            }
            else if (destination is >= 20 and <= 23)
            {
                if (Missing(puzzle)) { puzzle = new PuzzleSessionForm(engine, puzzles, tutorialData, supply); puzzle.Show(); }
                keep.Add(puzzle!);
            }
            else if (destination == 25)
            {
                if (Missing(free)) { free = new FreeGameForm(engine, openings, true); free.Show(); }
                keep.Add(free!);
            }
            else if (destination is >= 28 and <= 32)
            {
                if (Missing(games))
                { var sample = TutorialSampleGame.Load(tutorialData); games = new GameLibraryForm(engine, tutorialData, sample.Id); games.Show(); }
                keep.Add(games!);
                if (destination == 32)
                {
                    if (Missing(graph))
                    {
                        (Find(games!, "Evaluationsgraph anzeigen") as Button)?.PerformClick();
                        graph = Application.OpenForms.OfType<GameEvaluationForm>().LastOrDefault(f => !existing.Contains(f));
                    }
                    if (graph is not null) keep.Add(graph);
                }
                else if (previous >= destination && TutorialSession.Of<StudyBoard>(games!) is StudyBoard study) study.ShowIndex(0);
            }
            else if (destination is >= 34 and <= 40)
            {
                if (Missing(vision)) { vision = new VisionForm(tutorialData, engine, supply.Comparisons); vision.Show(); }
                keep.Add(vision!);
            }
            else if (destination is >= 41 and <= 44)
            {
                Preferences.Current.VisionStatisticsDemo = true;
                if (Missing(statistics))
                {
                    statistics = new VisionStatisticsForm(new VisionStatistics(tutorialData).Values); statistics.Show();
                    foreach (var c in TutorialSession.Descendants(statistics).OfType<ComboBox>().Where(c => c.Items.Count == 4)) c.SelectedIndex = 2;
                }
                if (destination == 41 && TutorialSession.Of<TabControl>(statistics!) is TabControl tabs) tabs.SelectedIndex = 0;
                if (destination == 42 && Metric() is ComboBox metric) metric.SelectedIndex = 0;
                if (destination == 43 && Period() is ComboBox period) period.SelectedIndex = 2;
                keep.Add(statistics!);
            }
            foreach (var window in Application.OpenForms.Cast<Form>().Where(f => !existing.Contains(f) && !keep.Contains(f) && f is not HoverBoard).Reverse().ToArray()) window.Close();
        }
        tutorial = new TutorialSession(steps, () =>
        {
            tutorialLearning = null; tutorialGamesDirectory = null;
            Preferences.Current.VisionStatisticsDemo = oldDemo; Preferences.Current.LastLearningOpening = oldLearningOpening; Preferences.Save();
            foreach (var f in Application.OpenForms.Cast<Form>().Where(f => !existing.Contains(f)).ToArray()) f.Close();
            tutorial = null; if (!IsDisposed) Activate();
        }, Prepare);
    }
    private (OpeningDefinition Opening, ProgressStore Progress)? tutorialLearning;
    private string? tutorialGamesDirectory;
}
