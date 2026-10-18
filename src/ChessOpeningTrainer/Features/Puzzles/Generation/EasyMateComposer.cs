namespace ChessOpeningTrainer;

/// <summary>Random tactical positions with an exact one-ply mate proof, not engine-score guesses.</summary>
public static class EasyMateComposer
{
    public static Puzzle Generate(CancellationToken token)
    {
        while (true)
        {
            token.ThrowIfCancellationRequested();
            var p = new Position { Castling = "", WhiteToMove = true };
            int king = Random.Shared.Next(2) == 0 ? Random.Shared.Next(56, 64) : Random.Shared.Next(8) * 8 + 7;
            p.Squares[king] = 'k';
            void Place(char piece)
            {
                var squares = Enumerable.Range(0, 64).Where(s => p.Squares[s] == '\0' && (char.ToLowerInvariant(piece) != 'p' || s / 8 is > 0 and < 7)).ToArray();
                p.Squares[squares[Random.Shared.Next(squares.Length)]] = piece;
            }
            Place('K'); Place('Q'); Place(Random.Shared.Next(2) == 0 ? 'R' : 'B');
            Place('r'); Place('n');
            for (int i = 0; i < Random.Shared.Next(2, 6); i++) { Place('p'); Place('P'); }
            if (MoveGenerator.IsKingInCheck(p, false) || MoveGenerator.IsKingInCheck(p, true)) continue;
            var legal = MoveGenerator.GenerateLegal(p);
            var mates = legal.Where(m =>
            {
                var n = p.Clone(); n.Apply(m);
                return MoveGenerator.IsKingInCheck(n, false) && MoveGenerator.GenerateLegal(n).Count == 0;
            }).ToList();
            if (mates.Count == 0 || mates.Count > 3) continue;
            if (Random.Shared.Next(2) == 0) p = PawnTablebase.SwapColors(p);
            var puzzle = new Puzzle { Id = Guid.NewGuid().ToString("N"), Fen = p.ToFen(), Theme = "Matt in 1", Origin = "Zufällige Taktikkomposition · vollständig regelgeprüft" };
            puzzle.PreparedMateRoutes = MatePreparation.Prepare(puzzle, token);
            puzzle.AcceptedMoves = puzzle.PreparedMateRoutes[MatePreparation.Key(p, 1)].Select(r => r.Move).ToList();
            puzzle.Moves = [puzzle.AcceptedMoves[0]];
            return puzzle;
        }
    }
}
