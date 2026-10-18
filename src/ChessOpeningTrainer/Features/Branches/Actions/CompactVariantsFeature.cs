namespace ChessOpeningTrainer;
[BranchFeature(3)]
internal sealed class CompactVariantsFeature : IBranchFeature
{
    public void Install(BranchFeatureContext context)
    {
        var compact = context.Compact;
        var Rebuild = context.Rebuild;
        var ActionButton = context.AddButton;
        var compactButton = ActionButton("Stellungsvarianten vergrößern/verkleinern", 932);
        compactButton.Click += (_, _) =>
        {
            var selectedVertex = context.Selection();
            if (selectedVertex is null || selectedVertex.Alternatives.Count < 2)
                return;
            var keys = selectedVertex.Alternatives.Select(OpeningStore.PositionKey).ToList();
            bool close = keys.Any(k => !compact.Contains(k));
            foreach (var key in keys)
                if (close)
                    compact.Add(key);
                else
                    compact.Remove(key);
            Rebuild();
        };
    }
}
