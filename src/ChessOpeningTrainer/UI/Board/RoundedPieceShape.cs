using System.Drawing.Drawing2D;

namespace ChessOpeningTrainer;

/// <summary>Slate's rounded silhouettes in the same 100-unit coordinate system as the classic set.</summary>
internal static class RoundedPieceShape
{
    public static GraphicsPath[] CreateLayers(char piece)
    {
        var path = new GraphicsPath(FillMode.Winding);
        var layers = new List<GraphicsPath>();
        void Shape(params PointF[] points) => path.AddPolygon(points);
        void Base()
        {
            layers.Add(path);
            path = new GraphicsPath(FillMode.Winding);
            path.StartFigure();
            path.AddBezier(29, 72, 23, 77, 24, 82, 31, 82);
            path.AddLine(31, 82, 69, 82);
            path.AddBezier(69, 82, 76, 82, 77, 77, 71, 72);
            path.CloseFigure();
        }
        switch (char.ToLowerInvariant(piece))
        {
            case 'p':
                path.StartFigure();
                path.AddBezier(42, 40, 45, 50, 41, 62, 31, 73);
                path.AddLine(31, 73, 69, 73);
                path.AddBezier(69, 73, 59, 62, 55, 50, 58, 40);
                path.CloseFigure(); Base(); path.AddEllipse(37, 17, 26, 26); break;
            case 'r':
                Shape(new(24, 18), new(36, 18), new(36, 30), new(44, 30), new(44, 18), new(56, 18), new(56, 30), new(64, 30), new(64, 18), new(76, 18), new(76, 40), new(62, 44), new(62, 63), new(70, 74), new(30, 74), new(38, 63), new(38, 44), new(24, 40));
                Base(); break;
            case 'n':
                path.StartFigure();
                path.AddBezier(29, 74, 25, 61, 49, 55, 51, 43);
                path.AddLine(51, 43, 39, 53);
                path.AddBezier(39, 53, 31, 60, 23, 57, 21, 51);
                path.AddLines([new(21, 51), new(33, 34), new(41, 28), new(43, 15), new(54, 25)]);
                path.AddBezier(54, 25, 75, 30, 79, 48, 71, 74);
                path.CloseFigure(); Base(); break;
            case 'b':
                path.StartFigure();
                path.AddBezier(50, 15, 26, 30, 31, 43, 43, 49);
                path.AddBezier(43, 49, 46, 59, 34, 66, 29, 73);
                path.AddLine(29, 73, 71, 73);
                path.AddBezier(71, 73, 65, 66, 54, 59, 57, 49);
                path.AddBezier(57, 49, 71, 43, 71, 32, 60, 24);
                path.AddLines([new(60, 24), new(45, 41), new(41, 36), new(55, 20), new(50, 15)]);
                path.CloseFigure(); Base(); break;
            case 'q':
                Shape(new(24, 24), new(37, 42), new(50, 20), new(63, 42), new(76, 24), new(65, 58), new(57, 63), new(68, 74), new(32, 74), new(43, 63), new(35, 58));
                Base(); path.AddEllipse(20, 18, 9, 9); path.AddEllipse(46, 13, 9, 9); path.AddEllipse(71, 18, 9, 9); break;
            default:
                Shape(new(46, 12), new(54, 12), new(54, 21), new(63, 21), new(63, 29), new(54, 29), new(54, 39), new(46, 39), new(46, 29), new(37, 29), new(37, 21), new(46, 21));
                layers.Add(path); path = new GraphicsPath(FillMode.Winding);
                path.StartFigure();
                path.AddBezier(46, 37, 22, 24, 24, 52, 37, 59);
                path.AddBezier(37, 59, 42, 65, 33, 67, 30, 74);
                path.AddLine(30, 74, 70, 74);
                path.AddBezier(70, 74, 67, 67, 58, 65, 63, 59);
                path.AddBezier(63, 59, 76, 52, 78, 24, 54, 37);
                path.CloseFigure(); Base(); break;
        }
        layers.Add(path);
        return layers.ToArray();
    }
}
