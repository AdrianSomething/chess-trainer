namespace ChessOpeningTrainer;
public sealed partial class PuzzleSessionForm
{
    private OpeningDefinition? mistakeOpening;
    private Action<string>? openingProgress;
    [PuzzleTab(40)]
    private void BuildOpeningMistakesTab(string dataDirectory)
    {
        var opponentTab = new TabPage("Gegnerfehler ausnutzen")
        {
            Name = "opening-mistakes",
            BackColor = BackColor
        };
        tabs.TabPages.Insert(tabs.TabPages.Count - 1, opponentTab);
        var opponentPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            Padding = new(24),
            WrapContents = false
        };
        opponentTab.Controls.Add(opponentPanel);
        var openingChoice = new ComboBox
        {
            Width = 550,
            DropDownStyle = ComboBoxStyle.DropDownList,
            DisplayMember = "Name"
        };
        var opponentInfo = new Label
        {
            Text = "Ausgelassene, schwache Gegnerzüge deines Repertoires bestrafen. Fortsetzung bis zur nächsten nicht eindeutigen Entscheidung; Vorbereitung kann bis zu drei Minuten dauern.",
            Width = 650,
            Height = 75
        };
        var opponentStart = new Button
        {
            Text = "Training starten",
            Width = 400,
            Height = 40
        };
        var anyDeviation = Check("Jede Abweichung vom besten Enginezug (sonst nur klarer eigener Vorteil)", false);
        opponentPanel.Controls.AddRange([opponentInfo, openingChoice, anyDeviation, opponentStart]);
        opponentTab.Enter += (_, _) =>
        {
            string? id = (openingChoice.SelectedItem as OpeningDefinition)?.Id;
            openingChoice.Items.Clear();
            openingChoice.Items.AddRange(OpeningStore.Load(Path.Combine(AppContext.BaseDirectory, "Openings"), out _).Cast<object>().ToArray());
            openingChoice.SelectedItem = openingChoice.Items.Cast<OpeningDefinition>().FirstOrDefault(o => o.Id == id);
            if (openingChoice.SelectedIndex < 0 && openingChoice.Items.Count > 0)
                openingChoice.SelectedIndex = 0;
        };
        opponentStart.Click += (_, _) =>
        {
            if (openingChoice.SelectedItem is not OpeningDefinition opening)
                return;
            mistakeOpening = opening;
            if (openingMistakes is not null && openingProgress is not null) openingMistakes.Progress -= openingProgress;
            openingMistakes = supply.Opening(opening, anyDeviation.Checked);
            openingProgress = message =>
            {
                if (!IsDisposed && IsHandleCreated && openingMistakesMode && loading)
                    BeginInvoke(() => info.Text = message);
            };
            openingMistakes.Progress += openingProgress;
            StartSession(new PuzzleProfile { Name = "Gegnerfehler ausnutzen", Tactics = true, Mate = true, Material = false, Draw = false });
        };
        FormClosed += (_, _) => { if (openingMistakes is not null && openingProgress is not null) openingMistakes.Progress -= openingProgress; };
    }
}
