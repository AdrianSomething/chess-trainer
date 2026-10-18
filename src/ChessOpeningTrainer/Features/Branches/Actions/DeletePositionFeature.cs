namespace ChessOpeningTrainer;

[BranchFeature(6)]
internal sealed class DeletePositionFeature : IBranchFeature
{
    public void Install(BranchFeatureContext context)
    {
        if (context.Owner is BranchForm form) form.InstallDeleteAction(context);
    }
}
