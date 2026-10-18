namespace ChessOpeningTrainer;
public sealed partial class PuzzleSessionForm
{
    [PuzzleTab(30)]
    private void BuildPersonalMistakesTab(string dataDirectory)
    {
        var errorsTab = new TabPage("Aus eigenen Fehlern lernen")
        {
            Name = "personal-mistakes",
            BackColor = BackColor
        };
        tabs.TabPages.Insert(2, errorsTab);
        var errorsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new(24)
        };
        errorsTab.Controls.Add(errorsPanel);
        var username = new ComboBox
        {
            Width = 420
        };
        var exclude = Number(0, 1000, 10);
        var excludeOn = Check("Letzte X Partien ausschließen", true);
        var importGames = new Button
        {
            Text = "Spiele importieren / analysieren",
            Width = 420,
            Height = 36
        };
        var startMistakes = new Button
        {
            Text = "Training starten",
            Width = 420,
            Height = 40
        };
        var errorInfo = new Label
        {
            Text = "Zuerst Partien im Bereich Eigene Spiele analysieren. Dann deinen Spielernamen auswählen.",
            Width = 550,
            Height = 70
        };
        errorsPanel.Controls.AddRange([errorInfo, username, excludeOn, exclude, importGames, startMistakes]);
        void Names()
        {
            var store = new PersonalGameStore(dataDirectory);
            username.Items.Clear();
            username.Items.AddRange(store.Games.SelectMany(g => new[] { g.White, g.Black }).GroupBy(n => n).OrderByDescending(g => g.Count()).Select(g => (object)g.Key).ToArray());
            if (username.SelectedIndex < 0 && username.Items.Count > 0)
                username.SelectedIndex = 0;
        }

        errorsTab.Enter += (_, _) => Names();
        importGames.Click += (_, _) => new GameLibraryForm(engine, dataDirectory).Show();
        startMistakes.Click += (_, _) =>
        {
            var store = new PersonalGameStore(dataDirectory);
            mistakes = store.Mistakes(username.Text.Trim(), excludeOn.Checked ? (int)exclude.Value : 0).OrderBy(_ => Random.Shared.Next()).ToList();
            mistakeIndex = 0;
            if (mistakes.Count == 0)
            {
                errorInfo.Text = "Keine analysierten eigenen Fehler für diese Auswahl. Namen, Analyse und Ausschluss prüfen.";
                return;
            }

            StartSession(new PuzzleProfile { Name = "Eigene Fehler", Tactics = true, Draw = false, Material = false, Mate = true });
        };
        var analyzeMissing = new Button
        {
            Text = "Fehlende Partien automatisch analysieren",
            Width = 420,
            Height = 38
        };
        errorsPanel.Controls.Add(analyzeMissing);
        analyzeMissing.Click += async (_, _) =>
        {
            analyzeMissing.Enabled = false;
            try
            {
                var store = new PersonalGameStore(dataDirectory);
                var pending = store.Games.Where(g => g.PositionAnalyses.Count != g.Moves.Count + 1).ToList();
                for (int i = 0; i < pending.Count; i++)
                {
                    int count = i + 1;
                    await store.Analyze(pending[i], engine, new Progress<string>(s =>
                    {
                        if (!IsDisposed)
                            errorInfo.Text = $"Partie {count}/{pending.Count}: {s}";
                    }), lifetime.Token);
                }

                if (!IsDisposed)
                {
                    errorInfo.Text = "Fehlende Analysen fertig. Training neu starten, um alle Fehler zu mischen.";
                    Names();
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                if (!IsDisposed)
                    errorInfo.Text = ex.Message;
            }
            finally
            {
                if (!IsDisposed)
                    analyzeMissing.Enabled = true;
            }
        };
    }
}
