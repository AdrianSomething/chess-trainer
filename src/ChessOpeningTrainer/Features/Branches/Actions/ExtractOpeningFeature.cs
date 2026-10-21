namespace ChessOpeningTrainer;

[BranchFeature(6)]
internal sealed class ExtractOpeningFeature : IBranchFeature
{
    public void Install(BranchFeatureContext context)
    {
        var button = context.AddButton("Variante als eigenes Opening speichern", 854);
        button.Click += (_, _) =>
        {
            if (context.Selection() is null || context.Board.Position is not { } position) return;
            try
            {
                var result = BranchExtraction.Extract(context.Opening, position,
                    context.Opening.Name + " · " + L.T("Auszug"));
                var directory = Path.GetDirectoryName(context.Opening.FilePath)!;
                OpeningStore.Save(result, Path.Combine(directory, result.Id + ".json"));
                L.Message(context.Owner, L.T("Neues Opening gespeichert. Das ursprüngliche Opening bleibt unverändert."), L.T("Auszug"));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
            { L.Message(context.Owner, ex.Message, L.T("Auszug")); }
        };
    }
}
