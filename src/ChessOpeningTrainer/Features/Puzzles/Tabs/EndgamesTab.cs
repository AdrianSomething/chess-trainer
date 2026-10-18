namespace ChessOpeningTrainer;
public sealed partial class PuzzleSessionForm
{
    private readonly Dictionary<string, CheckBox> endgameSelections = [];
    [PuzzleTab(10)]
    private void BuildEndgamesTab(string dataDirectory)
    {
        endgameSetup.BackColor = BackColor;
        var endgamePanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new(24),
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true
        };
        endgameSetup.Controls.Add(endgamePanel);
        for (int index = 0; index < EndgameFactory.Kinds.Length; index++)
        {
            string kind = EndgameFactory.Kinds[index];
            var row = new FlowLayoutPanel { Width = 700, Height = 48, WrapContents = false };
            var choice = new CheckBox { Width = 24, Height = 40, Checked = index == 0, AccessibleName = kind };
            endgameSelections[kind] = choice;
            string key = index switch { 0 => "queen", 1 => "rook", 2 => "bishops", 3 => "bishop-knight", 4 => "pawn", _ => "pawns" };
            var icon = new MenuIconButton("endgame-" + key) { Width = 66, Height = 42, AccessibleName = kind };
            icon.Click += (_, _) => choice.Checked = !choice.Checked;
            var label = new Label { Text = kind, AutoSize = true, Margin = new(6, 12, 0, 0) };
            label.Click += (_, _) => choice.Checked = !choice.Checked;
            row.Controls.AddRange([choice, icon, label]);
            endgamePanel.Controls.Add(row);
        }
        sideChoice.Items.AddRange(["Zufällige Farbe", "Weiß", "Schwarz"]);
        sideChoice.SelectedIndex = 0;
        var endgameStart = new Button
        {
            Text = "Training starten",
            Width = 450,
            Height = 38
        };
        endgamePanel.Controls.AddRange([sideChoice, new Label { Text = "Exakt bei kurzen Mattaufgaben und KPK-Umwandlung; sonst kürzestes bekanntes Engine-Matt oder gleichwertige Topzüge (Toleranz 0,10). Kein Beweis globaler Minimalität.", MaximumSize = new(540, 0), AutoSize = true }, optimal, fullEndgames, new Label { Text = "Bauernverteidigung: ruhige eigene Züge bis zum Trainingsziel", AutoSize = true }, defense, endgameStart]);
        endgameStart.Click += (_, _) => StartSession(new PuzzleProfile { Name = "Endspieltraining", Tactics = false, Endgames = endgameSelections.Where(p => p.Value.Checked).Select(p => p.Key).ToList(), FullEndgames = fullEndgames.Checked, StrictOptimal = optimal.Checked, DefenseMoves = (int)defense.Value });
        setup.BackColor = play.BackColor = BackColor;
    }
}
