namespace ChessOpeningTrainer;
public sealed partial class PuzzleSessionForm
{
    private PuzzleProfile ReadProfile() => new()
    {
        Name = string.IsNullOrWhiteSpace(name.Text) ? "Training" : name.Text.Trim(),
        MoveSeconds = (int)perMove.Value,
        TotalSeconds = (int)total.Value,
        Lives = (int)lives.Value,
        RevealOnError = revealError.Checked,
        ContinueAfterMoveTimeout = continueTimeout.Checked,
        Difficulty = difficulty.SelectedItem?.ToString() ?? "Alle",
        Mate = mate.Checked,
        Material = material.Checked,
        Draw = false,
        Positional = positional.Checked,
        PositionalSubtle = positionalSubtle.Checked,
        MinGoodMoves = (int)minGood.Value,
        MaxGoodMoves = (int)maxGood.Value,
        Tactics = true,
        DefenseMoves = (int)defense.Value,
        FullEndgames = true,
        Endgames = []
    };
    private static bool SameSettings(PuzzleProfile a, PuzzleProfile b) => a.ContinueAfterMoveTimeout == b.ContinueAfterMoveTimeout && a.PositionalSubtle == b.PositionalSubtle && a.Positional == b.Positional && a.MinGoodMoves == b.MinGoodMoves && a.MaxGoodMoves == b.MaxGoodMoves && a.FullEndgames == b.FullEndgames && a.MoveSeconds == b.MoveSeconds && a.TotalSeconds == b.TotalSeconds && a.Lives == b.Lives && a.RevealOnError == b.RevealOnError && a.Difficulty == b.Difficulty && a.Mate == b.Mate && a.Material == b.Material && a.Draw == b.Draw && a.Tactics == b.Tactics && a.DefenseMoves == b.DefenseMoves && a.Endgames.SequenceEqual(b.Endgames);
    private PuzzleProfile SavePreset()
    {
        var p = ReadProfile();
        p.ExplicitlySaved = true;
        profiles.Items.Add(p);
        profiles.LastId = p.Id;
        profiles.Save();
        ReloadPresets();
        LoadPreset(p);
        return p;
    }

    private void ReloadPresets()
    {
        presets.Items.Clear();
        presets.Items.AddRange(profiles.Items.Cast<object>().ToArray());
        presets.SelectedItem = profiles.Last;
        RefreshPresetCards();
    }

    private void LoadPreset(PuzzleProfile p)
    {
        editingPreset = p;
        updatePreset.Enabled = p.Id != PuzzleProfiles.DefaultId && profiles.Items.Contains(p);
        updatePreset.Visible = updatePreset.Enabled;
        fullEndgames.Checked = p.FullEndgames;
        name.Text = p.Name;
        perMove.Value = Math.Clamp(p.MoveSeconds, 0, 3600);
        total.Value = Math.Clamp(p.TotalSeconds, 0, 86400);
        lives.Value = Math.Clamp(p.Lives, 0, 7);
        continueTimeout.Checked = p.ContinueAfterMoveTimeout;
        defense.Value = Math.Clamp(p.DefenseMoves, 3, 50);
        difficulty.SelectedItem = p.Difficulty;
        revealError.Checked = p.RevealOnError;
        mate.Checked = p.Mate;
        material.Checked = p.Material;
        draw.Checked = false;
        positional.Checked = p.Positional;
        positionalSubtle.Checked = p.PositionalSubtle;
        minGood.Value = Math.Clamp(p.MinGoodMoves, 2, 8);
        maxGood.Value = Math.Clamp(p.MaxGoodMoves, 2, 8);
        tactics.Checked = p.Tactics;
        for (int i = 0; i < modes.Items.Count; i++)
            modes.SetItemChecked(i, p.Endgames.Contains((string)modes.Items[i]));
        UpdateStats(p);
    }

    private void UpdateStats(PuzzleProfile p) => stats.Text = $"Gelöste Rätsel insgesamt: {p.Completed}" + (p.CanLose ? $" · Highscore: {p.HighScore}" : " · Ohne Verlustbedingung: kein Highscore");
    public void QuickStart()
    {
        var p = profiles.Last;
        if (!p.Tactics || p.Endgames.Count > 0)
        {
            p = p.Copy();
            p.Tactics = true;
            p.Endgames = [];
            p.ExplicitlySaved = false;
            if (!p.Mate && !p.Material && !p.Positional && !p.PositionalSubtle)
                p.Mate = true;
        }

        if (!p.Mate && !p.Material && !p.Positional && !p.PositionalSubtle)
        {
            p = p.Copy(); p.Mate = true; p.Draw = false; p.ExplicitlySaved = false;
        }
        quickStartSession = true;
        BeginSession(p);
        if (!Visible) Show();
        Activate();
    }
}
