using System.Text.Json;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;

namespace ChessOpeningTrainer;
public sealed class GameMoment
{
    public double WeightedLoss { get; set; }
    public int Ply { get; set; }
    public string Category { get; set; } = "";
    public double Loss { get; set; }
    public double Before { get; set; }
    public double After { get; set; }
    public List<string> BestMoves { get; set; } = [];
    public List<string> Pv { get; set; } = [];

    public override string ToString() => $"{Ply / 2 + 1}{(Ply % 2 == 0 ? "." : "…")} {Category} · Verlust {Loss:0.00}";
}
