namespace ChessOpeningTrainer;
[BranchFeature(1)]
internal sealed class SubbranchStatusFeature : IBranchFeature
{
    public void Install(BranchFeatureContext context)
    {
        var info = context.Info;
        var Rebuild = context.Rebuild;
        var ActionButton = context.AddButton;
        var subbranch = ActionButton("Subbranch-Status umschalten", 738);
        subbranch.Click += (_, _) =>
        {
            var opening = context.Opening;
            var selectedVertex = context.Selection();
            if (selectedVertex is null)
                return;
            if (!selectedVertex.IsLearnerMove)
            {
                info.Text = "Subbranches können nur an eigenen Zugknoten markiert werden.";
                return;
            }

            bool value = !selectedVertex.Nodes.All(n => n.IsSubbranch);
            foreach (var n in selectedVertex.Nodes)
                n.IsSubbranch = value;
            OpeningStore.Save(opening, opening.FilePath);
            Rebuild();
        };
    }
}
