namespace ChessOpeningTrainer;
public sealed record EndgameDecision(IReadOnlyList<EngineLine> Lines, List<string> Accepted)
{
    public string? Best => Accepted.FirstOrDefault();

    public string? Reply(string move) => Lines.FirstOrDefault(l => l.BestMove == move)?.Pv.ElementAtOrDefault(1);
}
