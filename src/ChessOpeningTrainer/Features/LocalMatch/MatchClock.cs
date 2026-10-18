using System.Diagnostics;

namespace ChessOpeningTrainer;
public sealed class MatchClock
{
    public double White { get; private set; }
    public double Black { get; private set; }
    public double WhiteIncrement { get; }
    public double BlackIncrement { get; }

    public MatchClock(double white, double black, double whiteIncrement, double blackIncrement)
    {
        White = white;
        Black = black;
        WhiteIncrement = whiteIncrement;
        BlackIncrement = blackIncrement;
    }

    public void Elapse(bool white, double seconds)
    {
        if (white)
            White = Math.Max(0, White - seconds);
        else
            Black = Math.Max(0, Black - seconds);
    }

    public void CompleteMove(bool white)
    {
        if (white)
            White += WhiteIncrement;
        else
            Black += BlackIncrement;
    }
}
