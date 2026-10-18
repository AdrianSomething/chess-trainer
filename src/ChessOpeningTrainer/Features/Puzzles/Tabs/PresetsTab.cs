namespace ChessOpeningTrainer;
public sealed partial class PuzzleSessionForm
{
    [PuzzleTab(20)]
    private void BuildPresetsTab(string dataDirectory)
    {
        var tips = new ToolTip { AutoPopDelay = 18000 };
        FormClosed += (_, _) => tips.Dispose();
        var columns = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
        columns.ColumnStyles.Add(new(SizeType.Percent, 34));
        columns.ColumnStyles.Add(new(SizeType.Percent, 33));
        columns.ColumnStyles.Add(new(SizeType.Percent, 33));
        columns.RowStyles.Add(new(SizeType.Percent, 100));
        var settings = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new(16)
        };
        var types = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new(16) };
        columns.Controls.Add(settings, 0, 0);
        columns.Controls.Add(types, 1, 0);
        columns.Controls.Add(presetCards, 2, 0);
        setup.Controls.Add(columns);
        var fields = settings;
        void Label(string text) => fields.Controls.Add(new Label { Text = text, AutoSize = true, MaximumSize = new(320, 0), Margin = new(0, 8, 0, 4) });
        void Field(string text, Control field)
        {
            var row = new FlowLayoutPanel
            {
                Width = 290,
                Height = 38,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
            int note = text.IndexOf('(');
            var label = new Label { Text = note < 0 ? text : text[..note].Trim(), Width = 180, Height = 34 };
            row.Controls.Add(label);
            field.Width = 100;
            L.SetToolTip(tips, label, text);
            L.SetToolTip(tips, field, text);
            row.Controls.Add(field);
            fields.Controls.Add(row);
        }

        Label("Presets werden nur über Speichern angelegt. Training starten merkt sich die Einstellungen separat.");
        settings.Controls.Add(presets);
        settings.Controls.Add(stats);
        Label("Name des Presets");
        settings.Controls.Add(name);
        name.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.Handled = e.SuppressKeyPress = true;
            ActiveControl = null;
        };
        name.Margin = new(3, 3, 3, 16);
        difficulty.Items.AddRange(["Alle", "Leicht", "Normal", "Schwer"]);
        Field("Rätsel-Schwierigkeit (Taktikfilter)", difficulty);
        Field("Zeit pro eigenem Zug (Sekunden, 0 = ohne Limit)", perMove);
        Field("Zeit insgesamt (Sekunden, 0 = ohne Limit)", total);
        Field("Leben (0 = unbegrenzt)", lives);
        settings.Controls.Add(revealError);
        revealError.Margin = new(3, 3, 3, 14);
        settings.Controls.Add(continueTimeout);
        void TimeoutOption() => continueTimeout.Visible = perMove.Value > 0 && total.Value > 0;
        perMove.ValueChanged += (_, _) => TimeoutOption(); total.ValueChanged += (_, _) => TimeoutOption(); TimeoutOption();
        L.SetToolTip(tips, continueTimeout, "Wenn beide Uhren aktiv sind: bei Ablauf der Zugzeit einen Punkt abziehen und mit dem nächsten Rätsel fortfahren. Ausgeschaltet endet der Durchgang sofort.");
        fields = types;
        Label("Aufgabentypen");
        types.Controls.AddRange([mate, material, positional, positionalSubtle]);
        L.SetToolTip(tips, mate, "Mattaufgaben mit einer vorgegebenen Zugzahl.");
        L.SetToolTip(tips, material, "Taktische Zugfolgen, die Material gewinnen.");
        L.SetToolTip(tips, positional, "Ruhige Mittelspielzüge mit einem deutlichen Bewertungsvorsprung gegenüber Alternativen.");
        L.SetToolTip(tips, positionalSubtle, "Ruhige Mittelspielzüge mit kleineren, tiefer geprüften Vorteilen.");
        L.SetToolTip(tips, revealError, "Nach einem Fehler den Lösungszug für die aktuelle Stellung anzeigen.");
        Field("Mittelspiel: mindestens gute Züge", minGood);
        Field("Mittelspiel: höchstens gute Züge", maxGood);
        fields = settings;
        modes.Items.AddRange(EndgameFactory.Kinds);
        var buttons = new FlowLayoutPanel
        {
            Width = 290,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Margin = new(3, 14, 3, 3)
        };
        var save = new Button
        {
            Text = "Als neues Preset speichern",
            Width = 260,
            Height = 36
        };
        var start = new Button
        {
            Text = "Training starten",
            Width = 260,
            Height = 36
        };
        buttons.Controls.AddRange([save, start]);
        settings.Controls.Add(buttons);
        save.Click += (_, _) => SavePreset();
        start.Click += (_, _) =>
        {
            var p = ReadProfile();
            if (SameSettings(p, profiles.Last) && name.Text == profiles.Last.Name)
                p = profiles.Last;
            StartSession(p);
        };
        buttons.Controls.Add(updatePreset);
        updatePreset.Click += (_, _) => UpdatePreset();

        presets.SelectionChangeCommitted += (_, _) =>
        {
            if (presets.SelectedItem is PuzzleProfile p)
            {
                profiles.LastId = p.Id;
                profiles.Save();
                LoadPreset(p);
            }
        };
        ReloadPresets();
        LoadPreset(profiles.Last);
        foreach (var b in new[]
        {
            save,
            start
        }

        )
        {
            b.BackColor = UiPalette.FromArgb(57, 91, 119);
            b.ForeColor = Color.White;
            b.FlatStyle = FlatStyle.Flat;
        }
        void ResizeColumn(FlowLayoutPanel panel)
        {
            int width = Math.Max(220, panel.ClientSize.Width - 48);
            foreach (Control c in panel.Controls)
            {
                c.MaximumSize = new(width, int.MaxValue);
                if (c is FlowLayoutPanel row)
                {
                    row.Width = width;
                    if (!row.AutoSize)
                    {
                        row.Height = panel == types ? 46 : 38;
                        foreach (Control child in row.Controls)
                            if (child is Label) child.Width = Math.Max(90, width - 112);
                    }
                }
                else
                {
                    c.Width = width;
                    if (c is System.Windows.Forms.Label or CheckBox) c.Height = c.GetPreferredSize(new(width, 0)).Height;
                    else if (c is ComboBox or TextBox) c.Height = 26;
                }
            }
            if (panel == settings)
                foreach (Control button in buttons.Controls) button.Width = Math.Max(180, width - 6);
        }
        settings.Resize += (_, _) => ResizeColumn(settings);
        types.Resize += (_, _) => ResizeColumn(types);
        presetCards.Resize += (_, _) => ResizePresetCards();
        ResizeColumn(settings); ResizeColumn(types);
    }
}
