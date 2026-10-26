namespace ChessOpeningTrainer;

public sealed partial class MainForm
{
    private CancellationTokenSource? supplyLifetime;
    private Task supplyRun = Task.CompletedTask;
    private bool openingEngineLab;
    private void StartSupply()
    {
        if (learningOnly || lifetime.IsCancellationRequested) return;
        supplyLifetime?.Dispose();
        supplyLifetime = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        supplyRun = supply.Maintain(supplyLifetime.Token);
    }
    public async void OpenEngineLab()
    {
        if (openingEngineLab) return;
        // A dedicated environment must not silently cancel the user's running jobs/games.
        if (Application.OpenForms.Cast<Form>().Any(f => f != this && f is not SettingsForm))
        {
            L.Message(this, "Bitte zuerst andere Spiel- und Generatorfenster schließen. Das Engine-Labor pausiert die automatische Rätselvorbereitung.", "Engine-Labor");
            return;
        }
        openingEngineLab = true;
        Enabled = false;
        try
        {
            supplyLifetime?.Cancel();
            try { await supplyRun; } catch (OperationCanceledException) { }
            if (IsDisposed) return;
            InvalidateWork(); AnalysisSearch.Shutdown();
            var laboratory = new EngineLabForm(dataDir);
            laboratory.FormClosed += (_, _) => { if (!IsDisposed) { Show(); Activate(); StartSupply(); } };
            Hide(); laboratory.Show();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        { if (!IsDisposed) { Show(); StartSupply(); L.Message(this, ex.Message, "Engine-Labor"); } }
        finally { openingEngineLab = false; if (!IsDisposed) Enabled = true; }
    }
}
