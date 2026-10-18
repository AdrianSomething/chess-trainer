namespace ChessOpeningTrainer;
[BranchFeature(4)]
internal sealed class CombineOpeningsFeature : IBranchFeature
{
    public void Install(BranchFeatureContext context)
    {
        var info = context.Info;
        var Rebuild = context.Rebuild;
        var ActionButton = context.AddButton;
        var owner = context.Owner;
        var combine = ActionButton("Mit anderem Opening kombinieren", 776);
        combine.Click += (_, _) =>
        {
            var opening = context.Opening;
            try
            {
                string folder = owner is BranchForm branch ? branch.OpeningDirectory : Path.GetDirectoryName(opening.FilePath)!;
                var candidates = OpeningStore.Load(folder, out _).Where(o => o.Id != opening.Id && o.LearnsWhite == opening.LearnsWhite).ToList();
                using var dialog = new LocalizedForm
                {
                    Text = "Gespeichertes Opening auswählen",
                    Width = 650,
                    Height = 450
                };
                var list = new ListBox
                {
                    Dock = DockStyle.Fill,
                    DisplayMember = "Name"
                };
                list.Items.AddRange(candidates.Cast<object>().ToArray());
                var ok = new Button
                {
                    Text = "Kombinieren",
                    Dock = DockStyle.Bottom,
                    DialogResult = DialogResult.OK
                };
                dialog.Controls.AddRange([list, ok]);
                if (list.Items.Count > 0)
                    list.SelectedIndex = 0;
                if (dialog.ShowDialog(owner) != DialogResult.OK || list.SelectedItem is not OpeningDefinition other)
                    return;
                if (L.Message(owner, "Neues Opening erstellen? Bei widersprüchlichen eigenen Antworten hat dieses Opening Vorrang. Beide Originale bleiben erhalten.", "Kombinieren", MessageBoxButtons.YesNo) != DialogResult.Yes)
                    return;
                var merged = GraphOperations.Combine(opening, other);
                OpeningStore.Save(merged, Path.Combine(folder, merged.Id + ".json"));
                info.Text = "Neues Opening erstellt: " + merged.Description;
            }
            catch (Exception ex)
            {
                info.Text = ex.Message;
            }
        };
    }
}
