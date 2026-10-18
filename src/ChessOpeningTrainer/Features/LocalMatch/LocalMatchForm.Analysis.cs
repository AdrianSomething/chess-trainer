namespace ChessOpeningTrainer;

public sealed partial class LocalMatchForm
{
    private readonly StockfishEngine? analysisEngine;
    private readonly CancellationTokenSource analysisLifetime = new();
    private Task? preparingAnalysis;
    private CancellationTokenSource? matchAnalysis;
    private LiveGameAnalysis? liveAnalysis;
    private AnalysisProgressChannel analysisProgress = new();
    private readonly Button analyzeGame = new() { Text = "Spiel analysieren", AutoSize = true, Visible = false };
    private readonly ToolTip analysisTip = new();

    private void PrepareAnalysis()
    {
        if (running || anonymous || game is null || analysisEngine is null || preparingAnalysis is not null) return;
        analyzeGame.Visible = true;
        matchAnalysis ??= CancellationTokenSource.CreateLinkedTokenSource(analysisLifetime.Token);
        preparingAnalysis = AnalyzeFinishedGame(game, matchAnalysis.Token);
    }

    private async Task AnalyzeFinishedGame(PersonalGame finishedGame, CancellationToken token)
    {
        var progress = analysisProgress;
        try
        {
            L.SetToolTip(analysisTip, analyzeGame, "Die Partieanalyse wird im Hintergrund vorbereitet.");
            var live = liveAnalysis;
            IReadOnlyDictionary<string, EngineResult>? cached = null;
            if (live is not null)
            {
                try { cached = await live.Finish(token); }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    progress.Report("Hintergrundanalyse wird vervollständigt …");
                }
                finally { await live.DisposeAsync(); }
            }
            await store.Analyze(finishedGame, analysisEngine!, progress, token, cached);
            await ShowAnalysisStatus(finishedGame, "Partieanalyse bereit.");
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            await ShowAnalysisStatus(finishedGame, "Analyse nicht abgeschlossen: " + ex.Message + " · In der Partienansicht erneut starten.");
        }
    }

    private Task ShowAnalysisStatus(PersonalGame finishedGame, string text)
    {
        if (IsDisposed || Disposing || !IsHandleCreated) return Task.CompletedTask;
        void Update() { if (!IsDisposed && !Disposing && ReferenceEquals(game, finishedGame)) L.SetToolTip(analysisTip, analyzeGame, text); }
        if (InvokeRequired) return this.InvokeAsync(Update);
        Update(); return Task.CompletedTask;
    }

    private Task OpenAnalysis()
    {
        if (game is null || analysisEngine is null || anonymous || running) return Task.CompletedTask;
        var preparedCancellation=matchAnalysis;
        new GameLibraryForm(analysisEngine, directory, game.Id, analyzeOnOpen: true, preparedAnalysis: preparingAnalysis, preparedProgress: analysisProgress,
            cancelPreparedAnalysis: ()=>preparedCancellation?.Cancel()).Show();
        return Task.CompletedTask;
    }
    private void QueueLiveAnalysis()
    {
        if (anonymous || game is null || analysisEngine is null || !Preferences.Current.LocalContinuousAnalysis) return;
        matchAnalysis ??= CancellationTokenSource.CreateLinkedTokenSource(analysisLifetime.Token);
        liveAnalysis ??= new LiveGameAnalysis(analysisEngine, analysisProgress, matchAnalysis.Token);
        liveAnalysis.Enqueue(position);
    }
}
