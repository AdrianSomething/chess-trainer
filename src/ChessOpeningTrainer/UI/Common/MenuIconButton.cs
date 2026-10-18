using System.Drawing.Drawing2D;

namespace ChessOpeningTrainer;

/// <summary>Shared monochrome navigation icons; optional PNG overrides are loaded once per button.</summary>
public class MenuIconButton : Button
{
    private string iconName = "";
    public string IconName
    {
        get => iconName;
        set
        {
            if (iconName == value) return;
            iconName = value;
            LoadCustomIcon();
            Invalidate();
        }
    }
    private Image? custom;
    private Bitmap? fallback;
    private readonly ToolTip tip = new();
    protected bool HasCustomIcon => custom is not null;
    public MenuIconButton(string icon, string caption = "")
    {
        IconName = icon;
        Text = caption;
        AccessibleName = caption.Length > 0 ? caption : icon;
        L.SetToolTip(tip, this, AccessibleName ?? Text);
        FlatStyle = FlatStyle.Flat;
        BackColor = UiPalette.FromArgb(43, 49, 55);
        ForeColor = Color.White;
        TextAlign = ContentAlignment.MiddleLeft;
        Padding = new(42, 0, 0, 0);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        // Most icon-only buttons set AccessibleName in their object initializer,
        // after the constructor has supplied its generic icon key.
        L.SetToolTip(tip, this, AccessibleName ?? Text);
        base.OnMouseEnter(e);
    }

    private void LoadCustomIcon()
    {
        custom?.Dispose();
        custom = null;
        fallback?.Dispose(); fallback = null;
        if (Preferences.Current.UseCustomIcons)
        {
            string file = Path.Combine(AppContext.BaseDirectory, "Customization", "icons", IconName + ".png");
            if (File.Exists(file))
                try
                {
                    using var source = new Bitmap(file);
                    var mask = new Bitmap(source.Width, source.Height);
                    for (int y = 0; y < source.Height; y++)
                        for (int x = 0; x < source.Width; x++)
                        {
                            var pixel = source.GetPixel(x, y);
                            mask.SetPixel(x, y, pixel.A > 127 && pixel.GetBrightness() >= .5 ? Color.White : Color.Transparent);
                        }
                    custom = mask;
                }
                catch (ArgumentException) { }
                catch (IOException) { }
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        PaintNavigationIcon(e.Graphics, new Rectangle(Text.Length == 0 ? (Width - 26) / 2 : 10, (Height - 26) / 2, 26, 26));
    }

    // Shared by large mode cards, including the normal custom-icon mask and palette tint.
    protected void PaintNavigationIcon(Graphics g, Rectangle target)
    {
        if (custom is null && fallback is null)
        {
            fallback = new Bitmap(26, 26);
            using var iconGraphics = Graphics.FromImage(fallback);
            iconGraphics.SmoothingMode = SmoothingMode.AntiAlias;
            DrawSymbol(iconGraphics, IconName);
        }
        using var attributes = new System.Drawing.Imaging.ImageAttributes();
        var ink = UiPalette.Gainsboro;
        attributes.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix(new float[][] {
            [ink.R / 255f, 0, 0, 0, 0], [0, ink.G / 255f, 0, 0, 0], [0, 0, ink.B / 255f, 0, 0],
            [0, 0, 0, Enabled ? 1 : .45f, 0], [0, 0, 0, 0, 1] }));
        var source = custom ?? fallback!;
        g.DrawImage(source, target, 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
    }

    private static void DrawSymbol(Graphics g, string name)
    {
        using var pen = new Pen(Color.White, 2.3f) { LineJoin = LineJoin.Round };
        switch (name)
        {
            case "pause":
                g.FillRectangle(Brushes.White, 6, 3, 5, 20); g.FillRectangle(Brushes.White, 16, 3, 5, 20); break;
            case "play":
                g.FillPolygon(Brushes.White, new Point[] { new(7, 2), new(24, 13), new(7, 24) }); break;
            case "close":
                g.DrawLine(pen, 5, 5, 21, 21); g.DrawLine(pen, 21, 5, 5, 21); break;
            case "draw":
                g.DrawLine(pen, 3, 9, 23, 9); g.DrawLine(pen, 3, 17, 23, 17); break;
            case "resign":
                g.DrawLine(pen, 5, 2, 5, 25); g.FillPolygon(Brushes.White, [new(6, 3), new(24, 3), new(18, 9), new(24, 15), new(6, 15)]); break;
            case "flip-board":
                g.DrawRectangle(pen, 7, 7, 12, 12);
                g.FillRectangle(Brushes.White, 7, 7, 6, 6); g.FillRectangle(Brushes.White, 13, 13, 6, 6);
                g.DrawArc(pen, 1, 1, 24, 24, 190, 130); g.DrawArc(pen, 1, 1, 24, 24, 10, 130);
                g.FillPolygon(Brushes.White, [new(23, 0), new(26, 8), new(18, 6)]);
                g.FillPolygon(Brushes.White, [new(3, 26), new(0, 18), new(8, 20)]);
                break;
            case "puzzle-quick":
                g.FillPolygon(Brushes.White, [new Point(16, 0), new(3, 15), new(12, 15), new(9, 26), new(24, 10), new(15, 10)]);
                break;
            case "puzzle-exploit":
                g.DrawEllipse(pen, 3, 3, 20, 20);
                g.DrawLine(pen, 0, 13, 26, 13); g.DrawLine(pen, 13, 0, 13, 26);
                g.FillEllipse(Brushes.White, 10, 10, 6, 6);
                break;
            case "puzzle-mistakes":
                g.DrawArc(pen, 2, 2, 22, 22, 40, 285);
                g.FillPolygon(Brushes.White, [new(24, 1), new(24, 10), new(16, 6)]);
                g.DrawLine(pen, 13, 7, 13, 15); g.FillEllipse(Brushes.White, 11.5f, 18, 3, 3);
                break;
            case "endgame-queen": case "endgame-rook": case "endgame-bishops":
            case "endgame-bishop-knight": case "endgame-pawn": case "endgame-pawns":
                string pieces = name switch { "endgame-queen" => "Q", "endgame-rook" => "R", "endgame-bishops" => "BB", "endgame-bishop-knight" => "BN", "endgame-pawn" => "P", _ => "PPP" };
                for (int i = 0; i < pieces.Length; i++)
                    BoardControl.DrawPiece(g, pieces[i], new(13 + (i - (pieces.Length - 1) / 2f) * 9, 13), pieces.Length == 1 ? 30 : 22);
                break;
            case "assessment-blunder": case "assessment-missed": case "assessment-mistake":
            case "assessment-mate": case "assessment-lost": case "assessment-draw":
            case "assessment-best": case "assessment-good": case "assessment-lesson": case "vision":
                if (name != "vision")
                {
                    AssessmentBadge.Draw(g, new(0, 0, 26, 26), name switch { "assessment-mate" => "Matt", "assessment-lost" => "Verloren", "assessment-draw" => "Remis", "assessment-blunder" => "Patzer", "assessment-missed" => "Verpasste Chance", "assessment-mistake" => "Schlecht", "assessment-best" => "Bester Zug", "assessment-lesson" => "Lektion", _ => "Gut" });
                    break;
                }
                string symbol = "◎";
                using (var font = new Font("Segoe UI", 14, FontStyle.Bold))
                    g.DrawString(symbol, font, Brushes.White, 0, 0);
                break;
            case "generator":
                g.DrawRectangle(pen, 1, 2, 23, 16); g.DrawLine(pen, 13, 18, 13, 23); g.DrawLine(pen, 6, 24, 20, 24); break;
            case "custom-game":
                g.FillRectangle(Brushes.White, 2, 2, 11, 11); g.FillRectangle(Brushes.White, 13, 13, 11, 11); g.DrawRectangle(pen, 2, 2, 22, 22); break;
            case "local-game":
                using (var font = new Font("Segoe UI", 11, FontStyle.Bold)) g.DrawString("VS", font, Brushes.White, -1, 3); break;
            case "games":
                g.DrawLines(pen, [new(13, 23), new(2, 20), new(2, 3), new(13, 6), new(24, 3), new(24, 20), new(13, 23), new(13, 6)]); break;
            case "reload":
                g.DrawArc(pen, 3, 3, 20, 20, 30, 290); g.FillPolygon(Brushes.White, [new(24, 1), new(24, 11), new(15, 6)]); break;
            case "settings":
                g.DrawEllipse(pen, 6, 6, 14, 14); g.DrawEllipse(pen, 10, 10, 6, 6);
                for (int i = 0; i < 8; i++) { var s = g.Save(); g.TranslateTransform(13, 13); g.RotateTransform(i * 45); g.FillRectangle(Brushes.White, -2, -13, 4, 6); g.Restore(s); } break;
            case "sound-on": case "sound-off":
                g.FillPolygon(Brushes.White, [new(2, 9), new(7, 9), new(13, 3), new(13, 23), new(7, 17), new(2, 17)]);
                if (name == "sound-on") { g.DrawArc(pen, 10, 4, 14, 18, -65, 130); }
                else { g.DrawLine(pen, 17, 9, 24, 17); g.DrawLine(pen, 24, 9, 17, 17); } break;
            case "puzzles":
                g.DrawLine(pen, 1, 7, 7, 7); g.DrawLine(pen, 1, 12, 5, 12);
                g.TranslateTransform(16, 16); g.RotateTransform(30);
                g.FillEllipse(Brushes.White, -4, -13, 8, 8);
                g.FillPolygon(Brushes.White, [new(-2, -5), new(2, -5), new(4, 4), new(7, 7), new(-7, 7), new(-4, 4)]); break;
            default:
                g.DrawLines(pen, [new(17, 3), new(7, 13), new(17, 23)]); break;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { custom?.Dispose(); fallback?.Dispose(); tip.Dispose(); }
        base.Dispose(disposing);
    }
}
