using System.Text.Json;

namespace ChessOpeningTrainer;
public sealed class EndgameExercise
{
    public string Kind { get; set; } = "";
    public Puzzle Puzzle { get; set; } = new();
}
