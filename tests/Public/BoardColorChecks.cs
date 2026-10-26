using ChessOpeningTrainer;
using static Program;

internal static class BoardColorChecks
{
    internal static void Run()
    {
        Check(Square.IsDark(Square.At(0, 0)), "a1 is dark");
        Check(!Square.IsDark(Square.At(3, 0)), "White queen begins on light d1");
        Check(Square.IsDark(Square.At(3, 7)), "Black queen begins on dark d8");
        string previous = Preferences.Current.Skin;
        try
        {
            foreach (var skin in Skins.All)
                foreach (bool flip in new[] { false, true })
                {
                    Preferences.Current.Skin = skin.Id;
                    using var board = new BoardControl { Size = new(528, 556), Flip = flip };
                    using var image = new Bitmap(board.Width, board.Height);
                    board.DrawToBitmap(image, board.ClientRectangle);
                    var bounds = board.PlayingBounds;
                    for (int rank = 0; rank < 8; rank++)
                        for (int file = 0; file < 8; file++)
                        {
                            int col = flip ? 7 - file : file, row = flip ? rank : 7 - rank;
                            int x = (int)(bounds.Left + (col + .5f) * bounds.Width / 8);
                            int y = (int)(bounds.Top + (row + .5f) * bounds.Height / 8);
                            var expected = Skins.Color((file + rank) % 2 == 0 ? skin.Dark : skin.Light, Color.Magenta);
                            Check(image.GetPixel(x, y).ToArgb() == expected.ToArgb(), $"Rendered square {Square.Name(Square.At(file, rank))}, {skin.Id}, flipped={flip}");
                        }
                }
        }
        finally { Preferences.Current.Skin = previous; }
    }
}
