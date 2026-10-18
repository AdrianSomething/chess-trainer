namespace ChessOpeningTrainer;
public sealed record TreeEstimate(int Samples, long Estimate, long Low, long High, bool TimedOut);
