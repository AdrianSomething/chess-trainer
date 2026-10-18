namespace ChessOpeningTrainer;

public sealed partial class PuzzleSessionForm
{
    private readonly FlowLayoutPanel puzzleHub = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new(0, 16, 0, 16), WrapContents = false, AutoScroll = true };
    private TabPage? lastSettingsPage;
    private Action? showTrainingPage;
    private Action? returnToPuzzleHub;
    private bool switchingPages;
    private bool quickStartSession;

    // Navigation wraps the registered pages; existing session controls and model stay shared.
    [PuzzleTab(100)]
    private void BuildNavigation(string dataDirectory)
    {
        var top = new Panel { Dock = DockStyle.Top, Height = 54, Visible = false };
        var back = new MenuIconButton("back") { Left = 12, Top = 7, Width = 42, Height = 38, AccessibleName = "Zurück" };
        var title = new Label { Left = 68, Top = 12, AutoSize = true, ForeColor = ForeColor, Font = new Font(Font.FontFamily, 16, FontStyle.Bold) };
        top.Controls.AddRange([back, title]);
        var hubTitle = new Label { Text = "Rätselmodus", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, UseMnemonic = false, Font = new Font(Font.FontFamily, 24, FontStyle.Bold) };
        var hubHeading = new Panel { Dock = DockStyle.Top, Height = ModeHubLayout.HeaderHeight };
        var hubNavigation = new Panel { Dock = DockStyle.Top, Height = ModeHubLayout.NavigationHeight };
        hubHeading.Controls.AddRange([hubTitle, hubNavigation]);
        var settings = new MenuIconButton("settings") { Width = 38, Height = 38, AccessibleName = "Rätsel-Einstellungen" };
        settings.Click += (_, _) => SettingsForm.OpenPuzzles();
        void PositionSettings(Control header)
        {
            // Keep the gear inside the active header, not underneath a sibling Dock=Fill page.
            if (settings.Parent != header) header.Controls.Add(settings);
            settings.Location = new(Math.Max(12, header.ClientSize.Width - settings.Width - 12), 7);
            settings.BringToFront();
        }
        hubNavigation.SizeChanged += (_, _) => { if (settings.Parent == hubNavigation) PositionSettings(hubNavigation); };
        top.SizeChanged += (_, _) => { if (settings.Parent == top) PositionSettings(top); };
        Controls.Add(top);
        Controls.Add(hubHeading);
        Controls.Add(puzzleHub);
        puzzleHub.BringToFront();
        PositionSettings(hubNavigation);
        tabs.Visible = false;
        lastSettingsPage = setup;
        showTrainingPage = () =>
        {
            puzzleHub.Visible = hubHeading.Visible = false;
            top.Visible = tabs.Visible = true;
            tabs.SelectedTab = play;
            title.Text = play.Text;
            PositionSettings(top);
        };
        void OpenPage(TabPage page)
        {
            lastSettingsPage = page;
            switchingPages=true; SuspendLayout(); tabs.SuspendLayout(); page.SuspendLayout();
            try
            {
            tabs.Visible = false;
            tabs.SelectedTab = page;
            // Prepare native editors while the page is hidden, never reveal intermediate layouts.
            void Prepare(Control c) { _=c.Handle; foreach(Control child in c.Controls) Prepare(child); }
            Prepare(page);
            }
            finally { page.ResumeLayout(true); tabs.ResumeLayout(true); ResumeLayout(true); switchingPages=false; }
            puzzleHub.Visible = hubHeading.Visible = false;
            top.Visible = tabs.Visible = true;
            title.Text = page.Text;
            page.Select();
            PositionSettings(top);
        }
        void Link(string text, string icon, string description, Action action)
        {
            var button = new ModeMenuButton(icon, text, description);
            button.Click += (_, _) => action();
            puzzleHub.Controls.Add(button);
        }
        Link("Schnelle Rätsel", "puzzle-quick", "Mit den letzten Einstellungen starten", QuickStart);
        Link("Taktik", "puzzles", "Matt, Materialgewinn und Stellungsgefühl", () => OpenPage(setup));
        Link("Endspiel", "endgame-queen", "Matttechnik und Bauernendspiele", () => OpenPage(endgameSetup));
        Link("Aus Fehlern lernen", "puzzle-mistakes", "Eigene Partien gezielt verbessern", () => OpenPage(tabs.TabPages["personal-mistakes"]!));
        Link("Gegnerfehler ausnutzen", "puzzle-exploit", "Abweichungen deiner Openings bestrafen", () => OpenPage(tabs.TabPages["opening-mistakes"]!));
        var stock = new Label { AutoSize = true, MaximumSize = new(800, 0), Margin = new(0, 18, 0, 0), ForeColor = UiPalette.Gainsboro };
        puzzleHub.Controls.Add(stock);
        puzzleHub.SizeChanged += (_, _) => ModeHubLayout.Center(puzzleHub);
        ModeHubLayout.Center(puzzleHub);
        void RefreshStock()
        {
            if (!puzzleHub.Visible) return;
            var counts = bank.Stock();
            stock.Text = L.T("Vorbereitete Taktikaufgaben") + ":\n" + string.Join(" · ", counts.Select(p => L.T(p.Key) + ": " + p.Value));
            if (Preferences.Current.DebugInformation) stock.Text += "\n" + bank.Diagnostics + $"\nAktive Stockfish-Suchen: {supply.Budget.Active}";
            ModeHubLayout.Center(puzzleHub);
        }
        var stockTimer = new System.Windows.Forms.Timer { Interval = 2000 };
        stockTimer.Tick += (_, _) => RefreshStock();
        stockTimer.Start(); RefreshStock();
        FormClosed += (_, _) => stockTimer.Dispose();
        tabs.SelectedIndexChanged += (_, _) =>
        {
            if(switchingPages) return;
            if (tabs.SelectedTab is not { } page) return;
            if (page != play) lastSettingsPage = page;
            puzzleHub.Visible = hubHeading.Visible = false;
            top.Visible = tabs.Visible = true;
            title.Text = page.Text;
            PositionSettings(top);
        };
        returnToPuzzleHub = () =>
        {
            quickStartSession = false;
            active = question = showingSolution = waitingReply = false; autoNextAt = 0;
            revision++; work.Cancel(); analysis.Cancel();
            busy = loading = false; resultVisible = resultDismissed = false; sessionResult.Visible = restartBelow.Visible = false;
            historyPlayback.Cancel(); board.StopAnimation(); Clear(); SaveScore();
            tabs.Visible = top.Visible = false;
            puzzleHub.Visible = hubHeading.Visible = true;
            puzzleHub.BringToFront(); PositionSettings(hubNavigation);
        };
        back.Click += (_, _) =>
        {
            if (tabs.SelectedTab == play)
            {
                if (quickStartSession) { returnToPuzzleHub?.Invoke(); return; }
                active = question = showingSolution = waitingReply = false;
                autoNextAt = 0;
                revision++;
                work.Cancel(); analysis.Cancel();
                busy = loading = false;
                historyPlayback.Cancel(); board.StopAnimation(); Clear(); SaveScore();
                OpenPage(lastSettingsPage ?? setup);
            }
            else
            {
                tabs.Visible = top.Visible = false;
                puzzleHub.Visible = hubHeading.Visible = true;
                puzzleHub.BringToFront();
                PositionSettings(hubNavigation);
            }
        };
    }
}
