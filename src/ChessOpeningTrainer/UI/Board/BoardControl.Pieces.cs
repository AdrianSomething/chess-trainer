using System.Drawing.Drawing2D;
using ChessMove = ChessOpeningTrainer.Move;

namespace ChessOpeningTrainer;
public partial class BoardControl
{
    public static void DrawPiece(Graphics g, char piece, PointF center, float cell, bool historyIcon = false)
    {
        if (Skins.Piece(piece)is Image image)
        {
            g.DrawImage(image, center.X - cell * .46f, center.Y - cell * .46f, cell * .92f, cell * .92f);
            return;
        }

        bool rounded = Skins.Current.PieceStyle == "rounded";
        using var path = new GraphicsPath(FillMode.Winding);
        void Polygon(params PointF[] points) => path.AddPolygon(points);
        if (!rounded) switch (char.ToLowerInvariant(piece))
        {
            case 'p':
                path.StartFigure();
                path.AddBezier(38, 37, 20, 12, 80, 12, 62, 37);
                path.AddBezier(62, 37, 56, 44, 57, 60, 67, 70);
                path.AddLines([new(67, 70), new(71, 78), new(29, 78), new(33, 70)]);
                path.AddBezier(33, 70, 43, 60, 44, 44, 38, 37);
                path.CloseFigure();
                break;
            case 'r':
                Polygon(new(25, 20), new(37, 20), new(37, 30), new(44, 30), new(44, 20), new(56, 20), new(56, 30), new(63, 30), new(63, 20), new(75, 20), new(75, 40), new(65, 45), new(65, 65), new(72, 73), new(72, 79), new(28, 79), new(28, 73), new(35, 65), new(35, 45), new(25, 40));
                break;
            case 'b':
                path.StartFigure();
                path.AddBezier(50, 13, 30, 26, 28, 40, 42, 48);
                path.AddLines([new(42, 48), new(38, 64), new(27, 76), new(27, 79), new(73, 79), new(73, 76), new(62, 64), new(58, 48)]);
                path.AddBezier(58, 48, 70, 43, 71, 31, 61, 24);
                path.AddLines([new(61, 24), new(48, 39), new(44, 35), new(57, 20), new(50, 13)]);
                path.CloseFigure();
                break;
            case 'q':
                Polygon(new(23, 27), new(38, 42), new(39, 20), new(50, 38), new(61, 20), new(62, 42), new(77, 27), new(65, 62), new(73, 76), new(73, 79), new(27, 79), new(27, 76), new(35, 62));
                foreach (var point in new PointF[]
                {
                    new(23, 24),
                    new(39, 18),
                    new(61, 18),
                    new(77, 24)
                }

                )
                    path.AddEllipse(point.X - 4, point.Y - 4, 8, 8);
                break;
            case 'k':
                Polygon(new(46, 12), new(54, 12), new(54, 20), new(62, 20), new(62, 27), new(54, 27), new(54, 35), new(67, 31), new(74, 39), new(64, 61), new(73, 75), new(73, 79), new(27, 79), new(27, 75), new(36, 61), new(26, 39), new(33, 31), new(46, 35), new(46, 27), new(38, 27), new(38, 20), new(46, 20));
                break;
            default:
                path.StartFigure();
                path.AddLines([new(29, 79), new(72, 79), new(72, 71)]);
                path.AddBezier(72, 71, 79, 48, 67, 28, 52, 24);
                path.AddLines([new(52, 24), new(44, 14), new(42, 27), new(31, 38), new(21, 51), new(27, 60), new(43, 54), new(49, 45)]);
                path.AddBezier(49, 45, 57, 54, 37, 58, 29, 72);
                path.CloseFigure();
                break;
        }

        using var transform = new Matrix();
        transform.Translate(center.X - cell / 2, center.Y - cell * .47f);
        transform.Scale(cell / 100, cell / 100);
        using var fill = new SolidBrush(Skins.Color(char.IsUpper(piece) ? Skins.Current.WhitePiece : Skins.Current.BlackPiece, Color.Gray));
        using var outline = new Pen(Skins.Color(char.IsUpper(piece) ? Skins.Current.WhiteOutline : Skins.Current.BlackOutline, Color.Gray), Math.Max(1, cell * .018f))
        {
            LineJoin = LineJoin.Round
        };
        var layers = rounded ? RoundedPieceShape.CreateLayers(piece) : [path];
        try
        {
            using var expanded = new Pen(Color.Black, outline.Width) { LineJoin = LineJoin.Round };
            foreach (var layer in layers)
            {
                layer.Transform(transform);
                g.FillPath(fill, layer);
                g.DrawPath(historyIcon && char.IsUpper(piece) ? expanded : outline, layer);
            }
        }
        finally { if (rounded) foreach (var layer in layers) layer.Dispose(); }
    }
}
