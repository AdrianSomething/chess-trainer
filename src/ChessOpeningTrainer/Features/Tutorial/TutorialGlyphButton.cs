using System.Drawing.Drawing2D;

namespace ChessOpeningTrainer;

// Vector glyphs have geometric centers, independent of font baseline/DPI.
internal sealed class TutorialGlyphButton(string glyph) : Button
{
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(ClientRectangle.Contains(PointToClient(Cursor.Position)) ? Color.FromArgb(55, 59, 65) : BackColor);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        float x = ClientSize.Width / 2f, y = ClientSize.Height / 2f;
        float r = Math.Min(Width, Height) * .21f;
        using var pen = new Pen(Enabled ? ForeColor : Color.Gray, 2) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        if (glyph == "×")
        {
            e.Graphics.DrawLine(pen, x - r, y - r, x + r, y + r);
            e.Graphics.DrawLine(pen, x - r, y + r, x + r, y - r);
        }
        else
        {
            float direction = glyph == "‹" ? -1 : 1;
            e.Graphics.DrawLines(pen, new PointF[] { new(x - direction * r / 2, y - r), new(x + direction * r / 2, y), new(x - direction * r / 2, y + r) });
        }
    }
}
