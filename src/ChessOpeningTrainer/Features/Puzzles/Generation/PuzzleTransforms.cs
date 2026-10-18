using System.Text.Json;

namespace ChessOpeningTrainer;
public static class PuzzleTransforms
{
    public static string Move(string u) => Square.Name(((u[1] - '1') * 8 + u[0] - 'a') ^ 56) + Square.Name(((u[3] - '1') * 8 + u[2] - 'a') ^ 56) + (u.Length > 4 ? u[4..] : "");
    public static Puzzle Swap(Puzzle source)
    {
        var p = JsonSerializer.Deserialize<Puzzle>(JsonSerializer.Serialize(source, OpeningStore.Json), OpeningStore.Json)!;
        p.Fen = PawnTablebase.SwapColors(Position.FromFen(source.Fen)).ToFen();
        p.Moves = source.Moves.Select(Move).ToList();
        p.AcceptedMoves = source.AcceptedMoves.Select(Move).ToList();
        if (source.PreparedMateRoutes is not null)
        {
            p.PreparedMateRoutes = [];
            foreach (var(key, routes)in source.PreparedMateRoutes)
            {
                int split = key.LastIndexOf('|');
                string mapped = PawnTablebase.SwapColors(Position.FromFen(key[..split])).ToFen() + key[split..];
                p.PreparedMateRoutes[mapped] = routes.Select(r => new MateRoute(Move(r.Move), r.Defense is null ? null : Move(r.Defense))).ToList();
            }
        }

        return p;
    }
}
