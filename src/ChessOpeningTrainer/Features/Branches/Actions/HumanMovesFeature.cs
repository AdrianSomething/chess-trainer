namespace ChessOpeningTrainer;
[BranchFeature(0)]
internal sealed class HumanMovesFeature : IBranchFeature
{
    public void Install(BranchFeatureContext context)
    {
        var engine = context.Engine;
        var info = context.Info;
        var Rebuild = context.Rebuild;
        var ActionButton = context.AddButton;
        var owner = context.Owner;
        var humanAgain = ActionButton("Human Moves neu generieren", 700);
        humanAgain.Click += async (_, _) =>
        {
            var opening = context.Opening;
            var token = context.Cancellation.Token;
            humanAgain.Enabled = false;
            try
            {
                await GraphOperations.LabelHuman(opening, engine, 8, new Progress<string>(s => { if (!token.IsCancellationRequested) info.Text = s; }), token);
                Rebuild();
                info.Text = "Human-Labels aktualisiert (Tiefe 8, Verlustgrenze 2 Bauern).";
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                info.Text = ex.Message;
            }
            finally
            {
                if (!owner.IsDisposed)
                    humanAgain.Enabled = true;
            }
        };
    }
}
