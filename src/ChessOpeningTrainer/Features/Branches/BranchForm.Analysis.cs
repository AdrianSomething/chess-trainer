namespace ChessOpeningTrainer;

public sealed partial class BranchForm
{
    private readonly PlayerSelector analysisPlayer = new() { Width = 360 };
    private readonly BranchGameList matchedGames = new() { Width = 360, Height = 230 };
    private List<BranchGameExit> gameMatches = [];
    private BranchGameMatchIndex gameMatchIndex = new([]);
    private BranchVertex? listedVertex;
    private List<BranchGameExit>? listedMatches;
    private IReadOnlyDictionary<string, IReadOnlySet<string>> gameCaptions = new Dictionary<string, IReadOnlySet<string>>();
    private IReadOnlyList<PersonalGame> analysisGames = [];
    private readonly CheckBox hideSubbranchColors = new() { Text = "Subbranch-Farben ausblenden", Width = 230, Height = 44, ForeColor = UiPalette.Gainsboro };
    private Color analysisMarkerColor = Color.FromArgb(230, 72, 72);
    private Button? analysisColor;
    private sealed record MatchedGame(BranchGameExit Exit)
    {
        public override string ToString() => Exit.Game + " · " + L.T(Exit.TheoryEnd ? "Partieende in Theorie" : Exit.OpponentExit ? "Gegnerabweichung" : "Eigene Abweichung");
    }

    private void BuildAnalysisControls(FlowLayoutPanel panel)
    {
        panel.Controls.Add(new Label { Text = "Standardspieler · letzte gemeinsame Theoriestellung", Width = 360, Height = 28, ForeColor = UiPalette.Gainsboro });
        panel.Controls.Add(analysisPlayer); panel.Controls.Add(matchedGames);
        var refresh = new Button { Text = "Eigene Partien neu einlesen", Width = 360, Height = 32 };
        panel.Controls.Add(refresh);
        refresh.Click += (_, _) => LoadGameMatches();
        matchedGames.MouseClick += (_, e) => { if (matchedGames.IndexFromPoint(e.Location) >= 0) OpenMatchedGame(); };
        analysisPlayer.SelectionChangeCommitted += (_, _) => AnalyzeGames();
        analysisColor = new Button { Width = 28, Height = 28, FlatStyle = FlatStyle.Flat, BackColor = analysisMarkerColor, ForeColor=UiPalette.Gainsboro,
            AccessibleName = "Farbe der Partiemarkierungen", Visible = false };
        board.Parent!.Controls.Add(analysisColor);
        analysisColor.Click += (_, _) =>
        {
            Color before = analysisMarkerColor;
            using var picker = new ColorWheelDialog(before);
            picker.ColorChanged += color => { analysisMarkerColor = color; analysisColor.BackColor = color; ApplyModeCanvas(); };
            if (picker.ShowDialog(this) != DialogResult.OK) { analysisMarkerColor = before; analysisColor.BackColor = before; ApplyModeCanvas(); }
        };
    }

    private void LoadGameMatches()
    {
        try
        {
            string? previous = analysisPlayer.SelectedItem?.ToString();
            analysisGames = new PersonalGameStore(originalProgress.DataDirectory).Games;
            analysisPlayer.Items.Clear();
            analysisPlayer.Items.AddRange(analysisGames.SelectMany(g => new[] { g.White, g.Black }).Where(s => !string.IsNullOrWhiteSpace(s))
                .GroupBy(s => s).OrderByDescending(g => g.Count()).ThenBy(g => g.Key).Select(g => (object)g.Key).ToArray());
            analysisPlayer.SelectedItem = analysisPlayer.Items.Cast<string>().FirstOrDefault(s => s == previous) ?? analysisPlayer.Items.Cast<string>().FirstOrDefault();
            AnalyzeGames();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException) { info.Text = L.T("Partien konnten nicht geladen werden: ") + ex.Message; }
    }

    private void AnalyzeGames()
    {
        gameMatches = BranchGameAnalysis.Find(opening, analysisGames, analysisPlayer.SelectedItem?.ToString() ?? "");
        gameMatchIndex = new(gameMatches);
        gameCaptions = BranchAnalysisCaptions.Build(gameMatches);
        ApplyModeCanvas(); UpdateGameList();
        info.Text = L.T($"{gameMatches.Count} passende Partien · Abweichungen und Partieenden am Theorieende.");
    }

    private void UpdateGameList()
    {
        if (branchMode != BranchMode.Analysis) return;
        var selected = modeSelection?.Invoke();
        if (ReferenceEquals(selected, listedVertex) && ReferenceEquals(gameMatches, listedMatches)) return;
        listedVertex = selected; listedMatches = gameMatches;
        IEnumerable<string> keys = selected is null ? [] : canvas.PositionKeys(selected);
        matchedGames.BeginUpdate(); matchedGames.Items.Clear();
        matchedGames.Items.AddRange(gameMatchIndex.Find(keys).Select(m => (object)new MatchedGame(m)).ToArray());
        if (matchedGames.Items.Count > 0) matchedGames.SelectedIndex = 0;
        matchedGames.EndUpdate();
    }

    private void OpenMatchedGame()
    {
        if (matchedGames.SelectedItem is not MatchedGame item) return;
        new GameLibraryForm(engine, progress.DataDirectory, item.Exit.Game.Id, initialPositionIndex: item.Exit.PositionIndex, initialPlayer: analysisPlayer.SelectedItem?.ToString()).Show();
    }
}
