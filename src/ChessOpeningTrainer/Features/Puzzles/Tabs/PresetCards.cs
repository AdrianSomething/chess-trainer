namespace ChessOpeningTrainer;

public sealed partial class PuzzleSessionForm
{
    private readonly FlowLayoutPanel presetCards = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new(16) };
    private readonly Button updatePreset = new() { Text = "Änderungen speichern", Width = 260, Height = 36, Enabled = false };
    private PuzzleProfile? editingPreset;

    private void RefreshPresetCards()
    {
        foreach (Control old in presetCards.Controls.Cast<Control>().ToArray()) old.Dispose();
        presetCards.Controls.Clear();
        presetCards.Controls.Add(new Label { Text = "Presets", AutoSize = true, Font = new Font(Font.FontFamily, 16, FontStyle.Bold) });
        foreach (var p in profiles.Items)
        {
            var card = new FlowLayoutPanel { Width = 300, Height = 185, Padding = new(10), FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = UiPalette.FromArgb(43, 49, 55), Margin = new(0, 6, 0, 10) };
            var title = new Label { Text = p.Name, AutoSize = true, MaximumSize = new(260, 40) };
            var status = new Label { Text = $"{p.Completed} Rätsel" + (p.CanLose ? $" · Highscore: {p.HighScore}" : ""), AutoSize = true };
            var start = new Button { Text = "Training starten", Width = 245, Height = 30 };
            start.Click += (_, _) => StartSession(p);
            var edit = new Button { Text = "Bearbeiten", Width = 245, Height = 30 };
            edit.Click += (_, _) => { presets.SelectedItem = p; LoadPreset(p); };
            var remove = new Button { Text = "Preset löschen", Width = 245, Height = 30, Enabled = p.Id != PuzzleProfiles.DefaultId };
            remove.Click += (_, _) =>
            {
                if (p.Id == PuzzleProfiles.DefaultId || L.Message(this, "Dieses gespeicherte Preset samt Statistik löschen?", "Preset", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
                profiles.Items.Remove(p);
                if (profiles.LastId == p.Id) profiles.LastId = null;
                profiles.Save(); ReloadPresets();
            };
            card.Controls.AddRange([title, status, start, edit, remove]);
            presetCards.Controls.Add(card);
        }
        ResizePresetCards();
    }

    private void ResizePresetCards()
    {
        int width = Math.Max(230, presetCards.ClientSize.Width - 48);
        foreach (var card in presetCards.Controls.OfType<FlowLayoutPanel>())
        {
            card.Width = width;
            foreach (Control c in card.Controls)
            {
                c.MaximumSize = new(width - 24, 40);
                if (c is Button) c.Width = width - 24;
            }
        }
    }

    private void UpdatePreset()
    {
        if (editingPreset is null || editingPreset.Id == PuzzleProfiles.DefaultId || !profiles.Items.Contains(editingPreset)) return;
        var replacement = ReadProfile();
        bool same = SameSettings(editingPreset, replacement);
        if (!same && L.Message(this, "Geänderte Trainingsregeln speichern und die Statistik dieses Presets zurücksetzen?", "Preset", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        replacement.Id = editingPreset.Id;
        replacement.ExplicitlySaved = true;
        replacement.HighScore = same ? editingPreset.HighScore : 0;
        replacement.Completed = same ? editingPreset.Completed : 0;
        profiles.Items[profiles.Items.IndexOf(editingPreset)] = replacement;
        profiles.LastId = replacement.Id;
        profiles.Save(); ReloadPresets(); LoadPreset(replacement);
    }
}
