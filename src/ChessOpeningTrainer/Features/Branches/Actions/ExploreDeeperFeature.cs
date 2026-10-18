namespace ChessOpeningTrainer;
[BranchFeature(5)]
internal sealed class ExploreDeeperFeature : IBranchFeature
{
    public void Install(BranchFeatureContext context)
    {
        var engine = context.Engine;
        var board = context.Board;
        var Rebuild = context.Rebuild;
        var ActionButton = context.AddButton;
        var owner = context.Owner;
        var explore = ActionButton("Tiefer erforschen", 814);
        explore.Click += (_, _) =>
        {
            var opening = context.Opening;
            var selectedVertex = context.Selection();
            if (selectedVertex is null || board.Position is null)
                return;
            if (!selectedVertex.IsLearnerMove)
            {
                L.Message(owner, "Bitte einen Knoten mit eigenem Zug auswählen.", "Subbranch");
                return;
            }

            var target = board.Position.Clone();
            var generator = new OpeningGeneratorForm(engine, AppContext.BaseDirectory);
            generator.PrepareFrom(opening, target);
            generator.Generated += result =>
            {
                if (!CanApplyResult(owner, opening, context.Opening)) return;
                GraphOperations.Extend(opening, result);
                foreach (var w in GraphOperations.Positions(opening).Where(w => OpeningStore.PositionKey(w.Position) == OpeningStore.PositionKey(target)))
                    w.Node.IsSubbranch = true;
                OpeningStore.Save(opening, opening.FilePath);
                if (!owner.IsDisposed)
                    Rebuild();
            };
            generator.Show();
        };
    }

    internal static bool CanApplyResult(Control owner, OpeningDefinition expected, OpeningDefinition current)
        => !owner.IsDisposed && !owner.Disposing && ReferenceEquals(expected, current);
}
