namespace ChessOpeningTrainer;

internal sealed class DatasetInlineProgress(Action<string> report) : IProgress<string>
{
    public void Report(string value) => report(value);
}
