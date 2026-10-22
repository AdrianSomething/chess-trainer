namespace ChessOpeningTrainer;

/// <summary>Shared position-frequency display; unknown results remain visible in the total.</summary>
public sealed class DatasetResultsStrip : Control
{
    private DatasetCounts counts;
    public DatasetCounts Counts
    {
        get => counts; set
        {
            counts = value;
            Invalidate();
        }
    }
    public DatasetResultsStrip()
    {
        DoubleBuffered = true;
        Height = 65;
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        TextRenderer.DrawText(e.Graphics, $"{counts.Total:N0} " + L.T("Partien") + (counts.Unknown > 0 ? $" · ? {counts.Unknown:N0}" : ""), Font, new Rectangle(0, 0, Width, 25), UiPalette.Gainsboro);
        double total = Math.Max(1, counts.Total);
        float x = 0;
        foreach (var segment in new[] { (counts.White, Color.White, Color.Black), (counts.Draw, Color.Gray, Color.White), (counts.Black, Color.Black, Color.White), (counts.Unknown, Color.DimGray, Color.White) })
        {
            float width = (float)(Width * segment.Item1 / total);
            if (width <= 0)
                continue;
            using var brush = new SolidBrush(segment.Item2);
            e.Graphics.FillRectangle(brush, x, 28, width, 28);
            string text = $"{100 * segment.Item1 / total:0.#}%";
            if (TextRenderer.MeasureText(text, Font).Width < width)
                TextRenderer.DrawText(e.Graphics, text, Font, new Rectangle((int)x, 28, (int)width, 28), segment.Item3, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            x += width;
        }
    }
}
