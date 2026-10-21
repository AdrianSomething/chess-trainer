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
        void Click(Func<Form?> window, Func<Form, Control?> target, string text, Action? enter = null, Action? leave = null, Func<bool>? complete = null, Action? space = null) => steps.Add(new(window, target, text, true, complete, enter, leave, SpaceAction: space));
        Control? HomeCard(Form f) => TutorialSession.Descendants(f).OfType<Label>().FirstOrDefault(l => l.Text == example.Name)?.Parent;
        Control? HomeLearn(Form f) => HomeCard(f)?.Controls.Cast<Control>().FirstOrDefault(c => c.Text == L.T("Lernen"));
        Info(root, _ => home, "Trainiere Openings, löse Aufgaben und verstehe deine Partien. Alles läuft lokal auf deinem Computer.");
        Info(root, HomeCard, "Hier stehen deine Openings. Die vier Farben zeigen gelernt, fällig, angeschaut und unbekannt.");
        // Use a private practice profile; the real Learn button is highlighted, but
        // its normal handler must not write the user's actual learning progress.
        Click(root, HomeLearn, "Öffne Lernen für das Beispiel-Opening.", enter: () => tutorialLearning = (example, tutorialProgress), leave: () =>
        {
            lesson = Application.OpenForms.OfType<MainForm>().LastOrDefault(f => f.learningOnly && !existing.Contains(f));
            tutorialLearning = null;
        });
        void MarkLessonMove()
        {
            if (lesson is { question: true } && lesson.node?.Moves.FirstOrDefault() is { } m && ChessOpeningTrainer.Move.TryParseUci(m.Uci, out var move))
            { lesson.board.HintMove = move; lesson.board.Invalidate(); }
        }
        Info(() => lesson, _ => lesson?.board, "Du lernst immer aus einer festen Perspektive. Die Markierung zeigt dir den ersten gesuchten Zug.",
            enter: MarkLessonMove);
        Click(() => lesson, _ => lesson?.board, "Spiele jetzt den markierten Zug auf dem Brett.", complete: () => { MarkLessonMove(); return lesson is { question: false }; },
            leave: () => { if (lesson is not null) lesson.board.HintMove = null; });
        string? firstKey = null;
        Click(() => lesson, _ => lesson?.next, "Gut! Gehe mit Nächste Karte zur nächsten Lernstellung.", enter: () => firstKey = lesson?.targetKey, complete: () => lesson?.targetKey != firstKey, space: () => lesson?.next.PerformClick());
        Click(() => lesson, _ => lesson?.next, "Lass dir diesmal die Lösung zeigen. Die Karte wird später erneut abgefragt.", complete: () => lesson is { question: false }, space: () => lesson?.next.PerformClick());
        Info(() => lesson, _ => lesson?.variations, "Die Enginevarianten helfen beim Verstehen. Du kannst ihre Züge anklicken und eigene Fortsetzungen ausprobieren.");
        Info(() => lesson, _ => lesson?.history, "Die Zugfolge zeigt, wie die Stellung entstanden ist. Zur aktuellen Stellung bringt dich zurück.");
        Click(() => lesson, _ => lesson?.branch, "Öffne diese Stellung im Branch.", leave: () =>
        {
            branchWindow = Application.OpenForms.OfType<BranchForm>().LastOrDefault(f => !existing.Contains(f));
            branchWindow?.CollapseTutorialSettings();
        });
        Info(() => branchWindow, f => TutorialSession.Of<BranchCanvas>(f), "Der Branch zeigt Antworten und Abzweigungen. Ziehe zum Verschieben und nutze das Mausrad zum Zoomen.",
            enter: () => branchWindow?.ClearTutorialSelection(), allowInput: true);
        Click(() => branchWindow, f => TutorialSession.Of<BranchCanvas>(f), "Klicke auf einen Knoten, um seine Stellung und Varianten anzusehen.",
            enter: () => branchWindow?.ClearTutorialSelection(),
            complete: () => branchWindow is not null && (TutorialSession.Of<BranchCanvas>(branchWindow) as BranchCanvas)?.HasSelection == true);
        Info(() => branchWindow, f => TutorialSession.Of<BoardControl>(f)?.Parent, "Rechts findest du Brett, Stellungsvarianten, Lernpriorität und Aktionen: gezielt lernen, tiefer erforschen oder als eigenes Opening speichern.");
        Info(() => branchWindow, f => TutorialSession.Descendants(f).OfType<ComboBox>().FirstOrDefault(c => c.Items.Count == 3 && c.Items.Cast<object>().Any(i => i.ToString() is "Bearbeitungsmodus" or "Edit mode")),
            "Lernmodus ist die Standardansicht. Bearbeitungsmodus verändert den Baum; Analysemodus verknüpft ihn mit deinen Partien.", leave: () => { branchWindow?.Hide(); lesson?.Hide(); Activate(); });
        Info(root, f => HomeCard(f), "Jedes Opening hat auch direkt einen Branch-Knopf. Freeplay lässt dich dein Repertoire zusammenhängend spielen.");
        Click(root, f => Find(f, "Opening-Branch-Generator"), "Öffne den Opening-Branch-Generator.", leave: () => generator = generatorWindow);
        Info(() => generator, f => TutorialSession.Of<BoardControl>(f), "Hier erstellst du eigene Repertoires. Links spielst du die Startsequenz für beide Seiten.");
        Info(() => generator, f => TutorialSession.Of<SectionEditor>(f), "Rechts bestimmst du Perspektive, Tiefen und Filter. Schätze zuerst die Kartenanzahl; für den Einstieg sind höchstens etwa 1.000 Karten sinnvoll.");
        Info(() => generator, f => Find(f, "Auftrag erstellen – Lernkarten generieren"), "Generieren startet einen Auftrag. Du kannst weitere Aufträge hinzufügen und bei einem Kartenlimit gezielt fortsetzen.", leave: () => { generator?.Hide(); Activate(); });
        Click(root, f => Find(f, "Rätselmodus"), "Öffne den Rätselmodus.", leave: () => puzzle = puzzleWindow);
        Info(() => puzzle, f => Find(f, "Taktik"), "Taktik trainiert Matt, Materialgewinn und gute Mittelspielzüge. Zeit und Leben sind optional.");
        Info(() => puzzle, f => Find(f, "Endspiel"), "Endspieltraining übt konkrete Ziele wie Mattsetzen, Umwandeln und Verteidigen.");
        Info(() => puzzle, f => Find(f, "Aus Fehlern lernen"), "Aus Fehlern lernen macht Aufgaben aus deinen analysierten Partien.");
        Info(() => puzzle, f => Find(f, "Gegnerfehler ausnutzen"), "Gegnerfehler ausnutzen trainiert Antworten auf schwache Abweichungen von deinen Openings.", leave: () => { puzzle?.Hide(); Activate(); });
        Click(root, f => Find(f, "Freies Spiel"), "Öffne Freies Spiel.", leave: () => free = Application.OpenForms.OfType<FreeGameForm>().LastOrDefault(f => !existing.Contains(f)));
        Info(() => free, f => TutorialSession.Of<PositionSetupControl>(f), "Stelle rechts Figuren und Regeln ein. Mit dieser Stellung spielen startet die Partie; danach kannst du allein oder gegen die Engine spielen.", leave: () => { free?.Hide(); Activate(); });
        Info(root, f => Find(f, "Lokales Spiel"), "Lokales Spiel ist für zwei Menschen an einem Gerät, mit optionalen Schachuhren.");
        Click(root, f => Find(f, "Eigene Spiele und Analysen"), "Öffne Eigene Spiele und Analysen. Für den Überblick verwenden wir eine separate Beispielpartie.", enter: () => tutorialGamesDirectory = tutorialData,
            leave: () => { games = gamesWindow; tutorialGamesDirectory = null; });
        Info(() => games, f => TutorialSession.Of<GameList>(f), "In der Partienliste wählst du eine Partie aus. Das Beispiel wird im Hintergrund analysiert.");
        Info(() => games, f => Find(f, "PGN importieren"), "Importiere eine PGN-Datei mit einer oder mehreren Partien oder schreibe eine eigene PGN.");
        Info(() => games, f => Find(f, "Highlights durchgehen") ?? Find(f, "Analyse abbrechen"), "Highlights zeigen wichtige Entscheidungen. Warte bei der Beispielpartie kurz auf das Ende der Analyse.");
        Click(() => games, f => Find(f, "Evaluationsgraph anzeigen"), "Öffne den Evaluationsgraphen, sobald die Analyse fertig ist.", leave: () => graph = Application.OpenForms.OfType<GameEvaluationForm>().LastOrDefault());
        Info(() => graph, _ => graph?.Graph, "Klicke auf eine Stelle im Graphen: Das Brett springt zum zugehörigen Zug.",
            leave: () => { graph?.Hide(); games?.Hide(); Activate(); }, allowInput: true);
        Click(root, f => Find(f, "Schachblick"), "Öffne Schachblick.", leave: () => vision = Application.OpenForms.OfType<VisionForm>().LastOrDefault(f => !existing.Contains(f)));
        string[] visionInfo = ["Angreifer & Verteidiger: Erkenne, welche Figuren ein Feld kontrollieren.", "Angriffe im nächsten Zug: Finde mögliche neue Angriffe und Verteidigungen.", "Gegnerische Angriffe: Markiere die direkten Bedrohungen deiner Figuren.", "Figur retten: Finde das einzige sichere Zielfeld.", "Zugvergleich: Entscheide, welche von zwei Fortsetzungen besser ist.", "Figuren merken: Präge dir eine Stellung ein und baue sie wieder auf."];
        for (int i = 0; i < visionInfo.Length; i++)
        {
            int mode = i;
            Info(() => vision, f => TutorialSession.Descendants(f).OfType<VisionModeButton>().Where(b => b.Visible).ElementAtOrDefault(mode), visionInfo[i]);
        }
        Click(() => vision, f => TutorialSession.Descendants(f).OfType<VisionModeButton>().LastOrDefault(b => b.Visible), "Öffne die Statistik. Für den Überblick zeigen wir vorübergehend 400 Tage Demodaten.",
            enter: () => { Preferences.Current.VisionStatisticsDemo = true; Preferences.Save(); },
            leave: () =>
            {
                statistics = Application.OpenForms.OfType<VisionStatisticsForm>().LastOrDefault();
                if (statistics is not null) foreach (var c in TutorialSession.Descendants(statistics).OfType<ComboBox>().Where(c => c.Items.Count == 4)) c.SelectedIndex = 2;
            });
        int oldTab = -1, oldMetric = -1;
        Click(() => statistics, f => TutorialSession.Of<TabControl>(f), "Wähle einen anderen Submodus-Tab, um dessen Statistik zu sehen.",
            enter: () => oldTab = (TutorialSession.Of<TabControl>(statistics!) as TabControl)?.SelectedIndex ?? -1,
            complete: () => (TutorialSession.Of<TabControl>(statistics!) as TabControl)?.SelectedIndex != oldTab);
        ComboBox? Metric() => statistics is null ? null : TutorialSession.Descendants(statistics).OfType<ComboBox>().FirstOrDefault(c => c.Visible && c.Items.Count == 5);
        Click(() => statistics, _ => Metric(), "Wähle einen Statistiktyp, etwa Geschwindigkeit oder Fehlerzusammensetzung.",
            enter: () => oldMetric = Metric()?.SelectedIndex ?? -1, complete: () => Metric()?.SelectedIndex != oldMetric);
        Info(() => statistics, f => TutorialSession.Descendants(f).OfType<ComboBox>().FirstOrDefault(c => c.Visible && c.Items.Count == 4), "Die Zeitspanne und Tage, Wochen oder Monate bestimmen die Gruppierung. Die Demo wird nach diesem Überblick wieder zurückgestellt.");
        Info(root, _ => home, "Du bist startklar. Den Überblick kannst du jederzeit oben in den allgemeinen Einstellungen erneut öffnen.", enter: () => { statistics?.Hide(); vision?.Hide(); Activate(); });
        tutorial = new TutorialSession(steps, () =>
        {
            tutorialLearning = null; tutorialGamesDirectory = null;
            Preferences.Current.VisionStatisticsDemo = oldDemo; Preferences.Current.LastLearningOpening = oldLearningOpening; Preferences.Save();
            foreach (var f in Application.OpenForms.Cast<Form>().Where(f => !existing.Contains(f)).ToArray()) f.Close();
            tutorial = null; if (!IsDisposed) Activate();
        });
    }
    private (OpeningDefinition Opening, ProgressStore Progress)? tutorialLearning;
    private string? tutorialGamesDirectory;
}
