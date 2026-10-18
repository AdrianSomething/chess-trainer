namespace ChessOpeningTrainer;

[BranchFeature(7)]
internal sealed class DeleteWholeNodeFeature : IBranchFeature
{
    public void Install(BranchFeatureContext context)
    {
        if (context.Owner is BranchForm form) form.InstallDeleteWholeNodeAction(context);
    }
}
