using System.Drawing.Drawing2D;
using ChessMove = ChessOpeningTrainer.Move;

namespace ChessOpeningTrainer;
public partial class BoardControl
{
    /// <summary>Feature-owned overlays painted after pieces, in control coordinates.</summary>
    public event PaintEventHandler? ForegroundPaint;
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var size = BoardSize;
        var cell = size / 8f;
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        PaintCaptures(g);
        PaintTurnIndicator(g);
        if (WhitePlayerName is not null || BlackPlayerName is not null)
        {
            using var names = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
            using var ink = new SolidBrush(UiPalette.Gainsboro);
            // Captured pieces grow inwards from opposite ends; the middle is reserved for names.
            g.DrawString(Flip ? WhitePlayerName : BlackPlayerName, Font, ink, new RectangleF(BoardLeft + BoardSize * .3f, 0, BoardSize * .4f, 20), names);
            g.DrawString(Flip ? BlackPlayerName : WhitePlayerName, Font, ink, new RectangleF(BoardLeft + BoardSize * .3f, BoardSize + 24, BoardSize * .4f, 20), names);
        }
        g.TranslateTransform(BoardLeft, 22);
        string outcome = Position is null || UnrestrictedDrag ? "" : Outcome ?? MoveGenerator.GameState(Position, new Dictionary<string, int>());
        bool terminal = outcome is not ("" or "Schach");
        for (var row = 0; row < 8; row++)
            for (var col = 0; col < 8; col++)
            {
                var square = VisualToSquare(col, row);
                var rect = new RectangleF(col * cell, row * cell, cell + .5f, cell + .5f);
                var dark = Square.IsDark(square);
                using var brush = new SolidBrush(Skins.Color(dark ? Skins.Current.Dark : Skins.Current.Light, dark ? Color.Sienna : Color.Beige));
                g.FillRectangle(brush, rect);
                if (MarkedSquares.Contains(square))
                    using (var marked = new SolidBrush(Color.FromArgb(105, Skins.Color(Preferences.Current.SquareMarkColor, Color.DarkOrange))))
                        g.FillRectangle(marked, rect);
                if (SelectedSquare == square)
                    using (var selected = new SolidBrush(Color.FromArgb(165, Skins.Color(Preferences.Current.SelectionColor, Color.Khaki))))
                        g.FillRectangle(selected, rect);
                if (ErrorSquares.Contains(square))
                    using (var error = new SolidBrush(Color.FromArgb(175, 220, 45, 55)))
                        g.FillRectangle(error, rect);
                if (QueuedMove is not null && (QueuedMove.From == square || QueuedMove.To == square))
                    using (var queued = new SolidBrush(UiPalette.FromArgb(140, 75, 130, 240)))
                        g.FillRectangle(queued, rect);
                if (HintMove is not null && (HintMove.From == square || HintMove.To == square))
                    using (var hint = new SolidBrush(HintColor is Color hintColor ? Color.FromArgb(155, hintColor) : UiPalette.FromArgb(155, 67, 160, 71)))
                        g.FillRectangle(hint, rect);
                if (ShowDestinationHints && CandidateMoves.Any(x => x.To == square))
                {
                    var capture = Position?.Squares[square] != '\0';
                    using var target = new Pen(UiPalette.FromArgb(185, 35, 82, 52), capture ? Math.Max(3, cell / 12) : 1);
                    if (capture)
                        g.DrawEllipse(target, rect.X + cell * .12f, rect.Y + cell * .12f, cell * .76f, cell * .76f);
                    else
                        using (var dot = new SolidBrush(UiPalette.FromArgb(170, 35, 82, 52)))
                            g.FillEllipse(dot, rect.X + cell * .42f, rect.Y + cell * .42f, cell * .16f, cell * .16f);
                }

                if (Position is not null && Position.Squares[square] is var piece && piece != '\0' && !animated.Any(a => a.To == square) && !(_dragging && _leftStart == square && InputEnabled))
                {
                    var center = new PointF(rect.X + cell / 2, rect.Y + cell / 2);
                    bool mate = outcome.Contains("matt", StringComparison.OrdinalIgnoreCase), stalemate = outcome.Contains("Patt");
                    bool affected = terminal && char.ToLowerInvariant(piece) == 'k' && (!(mate || stalemate) || char.IsUpper(piece) == Position.WhiteToMove);
                    var pivot = new PointF(center.X, center.Y + cell * .31f);
                    var saved = g.Save();
                    if (affected)
                    {
                        g.TranslateTransform(pivot.X, pivot.Y);
                        g.RotateTransform(13);
                        g.TranslateTransform(-pivot.X, -pivot.Y);
                    }

                    DrawPiece(g, piece, center, cell);
                    g.Restore(saved);
                    if (affected)
                    {
                        AssessmentBadge.Draw(g, new RectangleF(rect.X + cell * .65f, rect.Y + cell * .04f, cell * .32f, cell * .32f), mate ? "Verloren" : "Remis");
                    }
                }

                if (row == 7)
                {
                    var text = ((char)('a' + square % 8)).ToString();
                    using var f = new Font(Font.FontFamily, Math.Max(8, cell * .16f), FontStyle.Bold, GraphicsUnit.Pixel);
                    using var b = new SolidBrush(Skins.Color(dark ? Skins.Current.Light : Skins.Current.Dark, Color.Gray));
                    g.DrawString(text, f, b, rect.Right - cell * .2f, rect.Bottom - cell * .22f);
                }

                if (col == 0)
                {
                    var text = (square / 8 + 1).ToString();
                    using var f = new Font(Font.FontFamily, Math.Max(8, cell * .16f), FontStyle.Bold, GraphicsUnit.Pixel);
                    using var b = new SolidBrush(Skins.Color(dark ? Skins.Current.Light : Skins.Current.Dark, Color.Gray));
                    g.DrawString(text, f, b, rect.X + cell * .04f, rect.Y + cell * .02f);
                }
            }

        foreach (var arrow in Arrows)
            DrawArrow(g, arrow, cell, ErrorArrows.Contains(arrow) ? Color.FromArgb(235, 220, 45, 55) : Color.FromArgb(220, Skins.Color(Preferences.Current.ArrowColor, Color.DarkOrange)));
        if (PreviewArrow is not null)
            DrawArrow(g, PreviewArrow, cell, Color.FromArgb(150, Skins.Color(Preferences.Current.ArrowColor, Color.DarkOrange)));
        if (_dragging && _leftStart is int drag && InputEnabled && Position?.Squares[drag] is char p && p != '\0')
            DrawPiece(g, p, new PointF(_pointer.X - BoardLeft, _pointer.Y - 22), cell);
        foreach (var a in animated)
        {
            var from = SquareCenter(a.From, cell);
            var to = SquareCenter(a.To, cell);
            double t = animationFraction * animationFraction * (3 - 2 * animationFraction);
            DrawPiece(g, a.Piece, new PointF((float)(from.X + (to.X - from.X) * t), (float)(from.Y + (to.Y - from.Y) * t)), cell);
        }
        var displayedBadge = Assessment;
        if (outcome.Contains("matt", StringComparison.OrdinalIgnoreCase) && (HintMove?.To ?? LastMoveDestination) is int matingSquare)
            displayedBadge = (matingSquare, "Matt");
        if (displayedBadge is { } assessment && assessment.Category != "Remis" && animated.Count == 0)
        {
            var center = SquareCenter(assessment.Square, cell);
            AssessmentBadge.Draw(g, new RectangleF(center.X + cell * .17f, center.Y - cell * .48f, cell * .32f, cell * .32f), assessment.Category);
        }
        g.TranslateTransform(-BoardLeft, -22);
        PaintKeyboardSelection(g);
        ForegroundPaint?.Invoke(this, e);
    }

    private void PaintCaptures(Graphics g)
    {
        if (Position is null)
            return;
        using var ink = new SolidBrush(UiPalette.Gainsboro);
        for (int side = 0; side < 2; side++)
        {
            bool white = side == 1 ? !Flip : Flip;
            float y = side == 0 ? 11 : BoardSize + 33;
            bool right = side == 1;
            float cursor = right ? BoardLeft + BoardSize : BoardLeft;
            foreach (var(piece, count)in MaterialDisplay.Missing(Position, !white).Where(k => k.Value > 0))
            {
                bool pawn = char.ToLowerInvariant(piece) == 'p';
                int sprites = pawn ? Math.Min(3, count) : count;
                float size = 22, step = size * (pawn ? .3f : .5f), width = size + (sprites - 1) * step;
                string number = pawn && count > 3 ? count.ToString() : "";
                float numberWidth = number == "" ? 0 : g.MeasureString(number, Font).Width;
                float left = right ? cursor - width - numberWidth : cursor;
                if (number != "")
                    g.DrawString(number, Font, ink, left + 2, y - 8, StringFormat.GenericTypographic);
                left += numberWidth;
                int front = pawn ? sprites / 2 : sprites - 1;
                for (int i = 0; i < sprites; i++)
                    if (i != front)
                        DrawPiece(g, piece, new(left + size * .5f + i * step, y), size);
                DrawPiece(g, piece, new(left + size * .5f + front * step, y), sprites > 1 || char.ToLowerInvariant(piece) == 'q' ? size * 1.2f : size);
                cursor += (right ? -1 : 1) * (width + numberWidth - (numberWidth > 0 ? 0 : 4.4f));
            }

            cursor += right ? -6 : 6;
            string score = MaterialDisplay.Balance(Position, white).ToString("+0;-0;'+/- 0'");
            float textWidth = g.MeasureString(score, Font).Width;
            g.DrawString(score, Font, ink, right ? cursor - textWidth : cursor, y - 8);
        }
    }

    private void PaintTurnIndicator(Graphics g)
    {
        if (Position is null) return;
        double weight = TurnWeight;
        double colorWeight = turnInitialized ? turnFrom + (turnTarget - turnFrom) * Math.Clamp(turnClock.Elapsed.TotalMilliseconds * 2 / Math.Max(1, turnDuration), 0, 1) : weight;
        int tone = (int)Math.Round(255 * colorWeight);
        foreach (bool bottom in new[] { false, true })
        {
            float active = (float)(bottom != Flip ? weight : 1 - weight);
            float length = 17 + 18 * active;
            float thickness = 3.5f + active * 1.5f;
            using var edge = new Pen(Color.FromArgb(255 - tone, 255 - tone, 255 - tone), thickness + 3.5f) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
            using var ink = new Pen(Color.FromArgb(tone, tone, tone), thickness) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round };
            float x = bottom ? BoardLeft - 4 : BoardLeft + BoardSize + 4;
            float y = bottom ? BoardSize + 26 : 18;
            PointF[] points = bottom
                ? [new(x, y - length), new(x, y), new(x + length, y)]
                : [new(x - length, y), new(x, y), new(x, y + length)];
            g.DrawLines(edge, points);
            g.DrawLines(ink, points);
        }
    }

    private void DrawArrow(Graphics graphics, BoardArrow arrow, float cell, Color color)
    {
        var start = SquareCenter(arrow.From, cell);
        var end = SquareCenter(arrow.To, cell);
        float thickness = Math.Clamp(Preferences.Current.ArrowThicknessPercent, 30, 250) / 100f;
        using var pen = new Pen(color, Math.Max(2, cell * .075f * thickness))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
        var df = Math.Abs(arrow.From % 8 - arrow.To % 8);
        var dr = Math.Abs(arrow.From / 8 - arrow.To / 8);
        PointF lastStart = start;
        if (df == 2 && dr == 1 || df == 1 && dr == 2)
        {
            var elbow = df == 2 ? new PointF(end.X, start.Y) : new PointF(start.X, end.Y);
            graphics.DrawLine(pen, start, elbow);
            lastStart = elbow;
        }

        var angle = Math.Atan2(end.Y - lastStart.Y, end.X - lastStart.X);
        var head = cell * .18f * Math.Clamp(Preferences.Current.ArrowHeadPercent, 30, 250) / 100f;
        var left = new PointF(end.X - head * (float)Math.Cos(angle - Math.PI / 6), end.Y - head * (float)Math.Sin(angle - Math.PI / 6));
        var right = new PointF(end.X - head * (float)Math.Cos(angle + Math.PI / 6), end.Y - head * (float)Math.Sin(angle + Math.PI / 6));
        pen.EndCap = LineCap.Flat;
        var shaftEnd = new PointF((left.X + right.X) / 2, (left.Y + right.Y) / 2);
        graphics.DrawLine(pen, lastStart, shaftEnd);
        using var headBrush = new SolidBrush(color);
        graphics.FillPolygon(headBrush, [end, left, right]);
    }
}
