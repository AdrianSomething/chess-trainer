namespace ChessOpeningTrainer;

/// <summary>One game's progress stream, shared by its local board and analysis view.</summary>
public sealed class AnalysisProgressChannel : IProgress<string>
{
    private string latest = "";
    public string Latest => Volatile.Read(ref latest);
    public event Action<string>? Changed;
    public void Report(string value) { Volatile.Write(ref latest, value); Changed?.Invoke(value); }
}
