namespace ChessOpeningTrainer;
[BranchFeature(2)]
internal sealed class CollapseSubbranchFeature : IBranchFeature
{
    public void Install(BranchFeatureContext context)
    {
        var collapsed = context.Collapsed;
        var hideSubs = context.HideSubbranches;
        var Rebuild = context.Rebuild;
        var ActionButton = context.AddButton;
        var collapse = ActionButton("Diesen Subbranch auf-/zuklappen", 894);
        collapse.Click += (_, _) =>
        {
            var selectedVertex = context.Selection();
            if (selectedVertex is null || !selectedVertex.Nodes.Any(n => n.IsSubbranch))
                return;
            var keys = selectedVertex.Alternatives.Select(OpeningStore.PositionKey).ToList();
            bool close = keys.Any(k => !collapsed.Contains(k));
            foreach (var key in keys)
                if (close)
                    collapsed.Add(key);
                else
                    collapsed.Remove(key);
            hideSubs.Checked = false;
            Rebuild();
        };
    }
}
