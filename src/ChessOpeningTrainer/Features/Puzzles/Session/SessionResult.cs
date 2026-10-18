namespace ChessOpeningTrainer;

public sealed partial class PuzzleSessionForm
{
    private readonly Panel sessionResult = new() { Visible = false, BackColor = UiPalette.FromArgb(43, 49, 55) };
    private readonly Label sessionResultText = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 17, FontStyle.Bold) };
    private bool resultVisible;
    private bool resultDismissed;
    private Action? layoutSessionResult;
    private readonly Button restartBelow = new() {Text="Neuer Durchgang", Visible=false, Height=40};
    private void BuildSessionResult()
    {
        var restart = new Button { Text = "Neuer Durchgang", Dock = DockStyle.Bottom, Height = 48 };
        var menu = new Button { Text = "Zurück zum Menü", Dock = DockStyle.Bottom, Height = 42 };
        var close = new MenuIconButton("close") { AccessibleName="Ergebnis schließen", Dock=DockStyle.Right, Width=32, Height=28 };
        var heading = new Panel {Dock=DockStyle.Top, Height=30}; heading.Controls.Add(close);
        close.Click += (_,_)=> { resultVisible=false; resultDismissed=true; sessionResult.Visible=false; restartBelow.Visible=true; RefreshBoard(); };
        restartBelow.Click += (_,_)=> { if(profile is not null) BeginSession(profile); };
        play.Controls.Add(restartBelow);
        menu.Click += (_, _) => returnToPuzzleHub?.Invoke();
        restart.Click += (_, _) => { if (profile is not null) BeginSession(profile); };
        sessionResult.Controls.AddRange([sessionResultText, restart, menu, heading]);
        board.Controls.Add(sessionResult);
        void Center()
        {
            int width = Math.Min(470, Math.Max(280, board.Width - 65));
            int textHeight = TextRenderer.MeasureText(sessionResultText.Text, sessionResultText.Font,
                new Size(width - 32, int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height;
            int height = Math.Min(board.Height - 16, Math.Max(250, textHeight + heading.Height + restart.Height + menu.Height + 24));
            sessionResult.SetBounds((board.Width - width) / 2, (board.Height - height) / 2, width, height);
        }
        layoutSessionResult = Center;
        board.Resize += (_, _) => Center(); Center();
    }
    private void ShowSessionResult(string message)
    {
        resultVisible = true;
        resultDismissed=false; restartBelow.Visible=false;
        sessionResultText.Text = message + "\n\n" + L.T("Punkte") + $": {score}\nHighscore: {profile?.HighScore ?? 0}";
        layoutSessionResult?.Invoke();
        sessionResult.Visible = true; sessionResult.BringToFront();
    }
    private async Task RevealWholeSolution()
    {
        if (showingSolution) { showingSolution = false; return; }
        if (!question || busy || loading) return;
        if (profile?.Tactics == true && profile.Lives > 0 && !Mistake("Lösung angezeigt · ein Leben verloren.")) return;
        failed = true; showingSolution = true; whole.Text = "Lösung stoppen";
        int version = revision;
        try
        {
            for (int i = 0; i < 160 && showingSolution && question && version == revision; i++)
            {
                await Reveal();
                if (version != revision) return;
                await Task.Delay(100);
            }
        }
        finally { showingSolution = false; if (!IsDisposed) whole.Text = "Gesamte Lösung anzeigen"; }
    }
}
