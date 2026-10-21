using ChessMove = ChessOpeningTrainer.Move;

namespace ChessOpeningTrainer;
public sealed partial class MainForm
{
    private readonly PuzzleSupply supply;
    private void OpenPuzzles(bool quick = false)
    {
        if (puzzleWindow is null || puzzleWindow.IsDisposed)
            puzzleWindow = new PuzzleSessionForm(engine, puzzles, dataDir, supply);
        // Select the training page before Windows paints the first frame.
        if (quick) puzzleWindow.QuickStart();
        else puzzleWindow.Show();
        if (puzzleWindow.WindowState == FormWindowState.Minimized)
            puzzleWindow.WindowState = FormWindowState.Normal;
        puzzleWindow.Activate();
    }

    internal void OpenBranch(OpeningDefinition item, Position? current = null)
    {
        var window = new BranchForm(item, progress, engine, current);
        window.LearnRequested += p =>
        {
            var plan = BranchLearning.Plan(item, p);
            OpenLearning(item, plan);
        };
        window.Show();
    }

    internal void OpenLearning(OpeningDefinition item, List<OpeningStore.LearningTarget>? plan = null)
    {
        Preferences.Current.LastLearningOpening = item.Id;
        Preferences.Save();
        var window = new MainForm(true, tutorialLearning?.Progress ?? progress);
        window.Show();
        if (plan is null)
            window.Start(item, "Lernen");
        else
            window.StartFocused(item, "Lernen", plan);
    }

    private void LoadHome()
    {
        home.Controls.Remove(difficulty);
        foreach (Control old in home.Controls.Cast<Control>().ToArray())
            old.Dispose();
        home.Controls.Clear();
        var header = new Panel { Dock = DockStyle.Top, Height = 80 };
        var sidebar = new FlowLayoutPanel { Dock = DockStyle.Left, Width = 270, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new(12, 12, 12, 0) };
        var content = new DarkScrollPanel { Dock = DockStyle.Fill, Padding = new(12) };
        home.Controls.Add(content);
        home.Controls.Add(sidebar);
        home.Controls.Add(header);
        var heading = Label(24);
        heading.Text = "Dein Schachtraining";
        heading.SetBounds(30, 24, 600, 44);
        heading.Width = 510;
        header.Controls.Add(heading);
        var soundButton = new MenuIconButton(AppSounds.Muted ? "sound-off" : "sound-on");
        soundButton.SetBounds(795, 25, 42, 36);
        soundButton.Click += (_, _) =>
        {
            AppSounds.Toggle();
            soundButton.IconName = AppSounds.Muted ? "sound-off" : "sound-on";
            soundButton.Invalidate();
        };
        header.Controls.Add(soundButton);
        var settingsButton = new MenuIconButton("settings") { AccessibleName = "Einstellungen" };
        settingsButton.SetBounds(635, 25, 42, 36);
        settingsButton.Click += (_, _) => SettingsForm.Open();
        header.Controls.Add(settingsButton);
        var quickPuzzle = Button("Schnelle Rätsel");
        quickPuzzle.SetBounds(12, 12, 165, 36);
        quickPuzzle.Click += (_, _) => OpenPuzzles(true);
        content.Controls.Add(quickPuzzle);
        var puzzleButton = new MenuIconButton("puzzles", "Rätselmodus");
        puzzleButton.SetBounds(30, 82, 300, 42);
        puzzleButton.Click += (_, _) => OpenPuzzles();
        var generator = new MenuIconButton("generator", "Opening-Branch-Generator");
        generator.SetBounds(575, 82, 355, 42);
        generator.Click += (_, _) =>
        {
            if (generatorWindow is null || generatorWindow.IsDisposed)
            {
                generatorWindow = new OpeningGeneratorForm(engine, AppContext.BaseDirectory);
                string Before() => string.Join("|", Directory.EnumerateFiles(Path.Combine(AppContext.BaseDirectory, "Openings"), "*.json").Order()
                    .Select(p => p + File.GetLastWriteTimeUtc(p).Ticks));
                string openingVersion = Before();
                generatorWindow.FormClosed += (_, _) =>
                {
                    if (!IsDisposed && home.Visible && openingVersion != Before())
                        LoadHome();
                };
                generatorWindow.Show();
            }
            else
            {
                generatorWindow.Show();
                generatorWindow.WindowState = FormWindowState.Normal;
                generatorWindow.Activate();
            }
        };
        var reload = new MenuIconButton("reload") { AccessibleName = "Openings neu laden" };
        reload.SetBounds(350, 12, 42, 36);
        reload.Click += (_, _) => LoadHome();
        content.Controls.Add(reload);
        var openings = OpeningStore.Load(Path.Combine(AppContext.BaseDirectory, "Openings"), out var errors);
        var quickLearn = Button("Schnelles Lernen");
        quickLearn.SetBounds(187, 12, 170, 36);
        var eligible = openings.Where(o => OpeningStore.LearnerMoves(o).Any(m => progress.Eligible(m.Key))).ToList();
        var last = eligible.FirstOrDefault(o => o.Id == Preferences.Current.LastLearningOpening) ?? eligible.FirstOrDefault();
        quickLearn.Enabled = last is not null;
        quickLearn.Click += (_, _) => { if (last is not null) OpenLearning(last); };
        content.Controls.Add(quickLearn);
        var freeGame = new MenuIconButton("custom-game", "Freies Spiel");
        freeGame.SetBounds(575, 130, 355, 32);
        freeGame.Click += (_, _) => new FreeGameForm(engine, openings, true).Show();
        var ownGames = new MenuIconButton("games", "Eigene Spiele und Analysen");
        ownGames.SetBounds(575, 172, 355, 32);
        ownGames.Click += (_, _) =>
        {
            if (tutorialGamesDirectory is { } tutorialDirectory)
            {
                var sample = TutorialSampleGame.Load(tutorialDirectory);
                gamesWindow = new GameLibraryForm(engine, tutorialDirectory, sample.Id);
            }
            if (gamesWindow is null || gamesWindow.IsDisposed)
                gamesWindow = new GameLibraryForm(engine, dataDir);
            gamesWindow.Show();
            gamesWindow.Activate();
        };
        var local = new MenuIconButton("local-game", "Lokales Spiel");
        local.SetBounds(575, 212, 355, 32);
        local.Click += (_, _) => new LocalMatchForm(dataDir, engine).Show();
        var vision = new MenuIconButton("vision", "Schachblick");
        vision.Click += (_, _) => new VisionForm(dataDir, engine, supply.Comparisons).Show();
        var navigation = new[] { generator, puzzleButton, freeGame, local, ownGames, vision };
        string[] captions = ["Opening-Branch-Generator", "Rätselmodus", "Freies Spiel", "Lokales Spiel", "Eigene Spiele und Analysen", "Schachblick"];
        foreach (var button in navigation) { button.Size = new(244, 52); button.Margin = new(0, 0, 0, 10); sidebar.Controls.Add(button); }
        var cards = new List<Panel>();
        int y = 66;
        foreach (var item in OpeningMenuOrder.Sort(openings, Preferences.Current.BookmarkedOpenings))
        {
            var panel = new Panel
            {
                BackColor = UiPalette.FromArgb(43, 49, 55)
            };
            panel.SetBounds(12, y, 900, 187);
            var name = Label(15);
            name.Text = item.Name;
            name.SetBounds(16, 12, 575, 30);
            OpeningNameEditor.Attach(name, item);
            var count = Label(10);
            count.Text = item.ColorName + " · " + OpeningStore.LearnerMoves(item).Count() + " Lernzüge";
            count.SetBounds(16, 47, 530, 24);
            var bar = new ProgressStrip
            {
                Counts = progress.Counts(item)
            };
            bar.SetBounds(16, 88, 570, 48);
            var learn = Button("Lernen");
            learn.SetBounds(710, 15, 175, 35);
            learn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            learn.Click += (_, _) => OpenLearning(item);
            bool bookmarked = Preferences.Current.BookmarkedOpenings.Contains(item.Id);
            var bookmark = new MenuIconButton(bookmarked ? "bookmark-filled" : "bookmark")
            {
                AccessibleName = bookmarked ? "Lesezeichen entfernen" : "Opening als Lesezeichen markieren",
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = panel.BackColor
            };
            bookmark.FlatAppearance.BorderSize = 0;
            bookmark.SetBounds(668, 15, 34, 35);
            bookmark.Click += (_, _) =>
            {
                if (!Preferences.Current.BookmarkedOpenings.Add(item.Id)) Preferences.Current.BookmarkedOpenings.Remove(item.Id);
                Preferences.Save(); LoadHome();
            };
            var free = Button("Freeplay");
            free.SetBounds(710, 60, 175, 35);
            free.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            free.Click += (_, _) => Start(item, "Freeplay");
            var clear = Button("Fortschritt zurücksetzen");
            clear.SetBounds(16, 143, 230, 32);
            clear.Click += (_, _) =>
            {
                if (L.Message(this, "Fortschritt für „" + item.Name + "“ zurücksetzen?", "Bestätigen", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    progress.ResetOpening(item);
                    LoadHome();
                }
            };
            var graph = Button("Branch Modus");
            graph.SetBounds(710, 105, 175, 35);
            graph.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            graph.Click += (_, _) => OpenBranch(item);
            var delete = Button("Opening löschen");
            delete.SetBounds(255, 143, 175, 32);
            delete.Visible = !IsDefault(item);
            delete.Click += (_, _) => DeleteOpening(item);
            panel.Controls.AddRange([name, count, bar, learn, bookmark, free, clear, graph, delete]);
            panel.Resize += (_, _) =>
            {
                name.Width = Math.Max(160, panel.Width - 264);
                count.Width = bar.Width = Math.Max(160, panel.Width - 220);
            };
            cards.Add(panel);
            content.Controls.Add(panel);
            y += 203;
        }

        var error = Label(10);
        error.Text = string.Join("\r\n", errors);
        error.SetBounds(30, y, 900, 100);
        content.Controls.Add(error);
        void Arrange(object? sender, EventArgs args)
        {
            bool narrow = home.ClientSize.Width < 1180;
            sidebar.Width = narrow ? 76 : 270;
            for (int i = 0; i < navigation.Length; i++)
            {
                navigation[i].Width = narrow ? 50 : 244;
                navigation[i].Text = narrow ? "" : captions[i];
            }
            settingsButton.Left = header.ClientSize.Width - 112;
            soundButton.Left = header.ClientSize.Width - 60;
            reload.Left = Math.Max(370, content.ClientSize.Width - 65);
            foreach (var card in cards) card.Width = Math.Max(640, content.ClientSize.Width - 42);
        }
        header.SizeChanged += Arrange;
        content.SizeChanged += Arrange;
        Arrange(null, EventArgs.Empty);
    }

    private static bool IsDefault(OpeningDefinition item) => item.Id is "example_english_white" or "example_pirc_black"
        || Path.GetFileName(item.FilePath).Equals("example_pirc_defense.json", StringComparison.OrdinalIgnoreCase)
        || item.Id == "pirc_defence_complete_black_repertoire";
    private void DeleteOpening(OpeningDefinition item)
    {
        if (IsDefault(item))
            return;
        if (L.Message(this, "„" + item.Name + "“ aus der Auswahl entfernen? Die Datei wird zur Wiederherstellung archiviert.", "Opening löschen", MessageBoxButtons.YesNo) != DialogResult.Yes)
            return;
        try
        {
            var archive = Path.Combine(dataDir, "DeletedOpenings");
            Directory.CreateDirectory(archive);
            File.Move(item.FilePath, Path.Combine(archive, Guid.NewGuid().ToString("N") + "_" + Path.GetFileName(item.FilePath)));
            LoadHome();
        }
        catch (Exception ex)
        {
            L.Message(this, "Löschen fehlgeschlagen: " + ex.Message, "Opening löschen");
        }
    }
}
