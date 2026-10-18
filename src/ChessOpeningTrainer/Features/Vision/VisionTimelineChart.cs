using System.Drawing.Drawing2D;

namespace ChessOpeningTrainer;

public sealed class VisionTimelineChart : Control
{
    public VisionStatistics.Run[] Runs { get; set; } = [];
    // All-time mode categories keep tone assignment stable when the visible date range changes.
    public VisionErrorCategory[]? ColorCategories { get; set; }
    public int Period { get; set; }
    public int Metric { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ThroughDate { get; set; }
    public bool GraphMode { get; set; }
    public bool ConnectBars { get; set; }
    public bool ShowSum { get; set; } = true;
    private readonly HashSet<string> selectedKeys = [];
    private readonly List<(Rectangle Bounds, string Key)> legendHits = [];
    private readonly Dictionary<char, Bitmap> icons = [];
    private string iconSkin = "";
    private int legendOffset;
    private int legendWidth;
    public event Action? SelectionChanged;
    public VisionErrorCategory[] Categories => VisionTimelineData.Categories(Runs);
    public VisionTimelineSample[] Samples => VisionTimelineData.Build(Runs, Period, FromDate, ThroughDate);
    public bool CanShowSum
    {
        get
        {
            var categories = Categories;
            int count = categories.Count(c => IsCategorySelected(c.Key));
            return count > 1 && Metric is 0 or 1 or 4;
        }
    }
    public bool IsCategorySelected(string key) => selectedKeys.Count == 0 || selectedKeys.Contains(key);
    public void ToggleCategory(string key)
    {
        if (!selectedKeys.Add(key)) selectedKeys.Remove(key);
        Invalidate(); SelectionChanged?.Invoke();
    }
    public void SelectAllCategories() { selectedKeys.Clear(); Invalidate(); SelectionChanged?.Invoke(); }
    public VisionTimelineChart() { DoubleBuffered = true; SetStyle(ControlStyles.ResizeRedraw, true); }

    // Piece determines hue; exactly the same error pattern has the same saturation across pieces.
    public static Color CategoryColor(VisionErrorCategory category, IEnumerable<VisionErrorCategory> all)
    {
        return VisionErrorColors.For(category, all);
    }
    public double[] AxisTicks
    {
        get
        {
            double max = Metric == 0 ? 100 : Math.Max(1, Samples.Select(s => VisionTimelineData.Value(s, Metric) ?? 0).DefaultIfEmpty(0).Max());
            int divisions = 4;
            if (Metric is 2 or 4) { divisions = Math.Min(4, (int)Math.Ceiling(max)); max = Math.Ceiling(max / divisions) * divisions; }
            return Enumerable.Range(0, divisions + 1).Select(n => max * n / divisions).ToArray();
        }
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var categories = Categories; var samples = Samples;
        legendHits.Clear();
        bool errorsMetric = Metric is 0 or 1 or 4;
        legendWidth = errorsMetric ? Math.Min(290, Math.Max(190, Width / 3)) : 0;
        var plot = new RectangleF(legendWidth + 44, 30, Math.Max(30, Width - legendWidth - 64), Math.Max(80, Height - 80));
        using var ink = new SolidBrush(ForeColor);
        if (samples.Length == 0) { e.Graphics.DrawString(L.T("Keine datierten Durchgänge im gewählten Zeitraum."), Font, ink, 16, 20); return; }
        var selected = categories.Where(c => IsCategorySelected(c.Key)).ToArray();
        var ticks = AxisTicks; double max = ticks[^1];
        using var grid = new Pen(UiPalette.FromArgb(58, 66, 74));
        using var rightAligned = new StringFormat { Alignment = StringAlignment.Far };
        foreach (double tick in ticks)
        {
            float y = plot.Bottom - plot.Height * (float)(tick / max);
            e.Graphics.DrawLine(grid, plot.Left, y, plot.Right, y);
            string label = tick.ToString(Metric is 2 or 4 ? "0" : "0.#") + (Metric == 0 ? "%" : "");
            e.Graphics.DrawString(label, Font, ink, new RectangleF(plot.Left - 42, y - 10, 38, 20), rightAligned);
        }
        float column = plot.Width / samples.Length;
        float BarX(int i) => plot.Left + i * column + column * .15f;
        float BarWidth = Math.Max(1, column * .7f);
        float Y(double value) => plot.Bottom - (float)(value / max * plot.Height);
        var colors = categories.ToDictionary(c => c.Key, c => CategoryColor(c, ColorCategories ?? categories));
        if (GraphMode)
        {
            if (errorsMetric)
            {
                foreach (var category in selected)
                    DrawSeries(e.Graphics, samples, plot, max, i => VisionTimelineData.Value(samples[i], Metric, category.Key), colors[category.Key]);
                if (ShowSum && CanShowSum)
                {
                    DrawSeries(e.Graphics, samples, plot, max, i => Sum(samples[i], selected), ForeColor, dashed: true);
                    e.Graphics.DrawString(L.T(Metric == 1 && selected.Length == categories.Length ? "Gestrichelt: gesamte Fehler pro Durchgang" : "Gestrichelt: Summe der Auswahl"), Font, ink, plot.Left + 4, 4);
                }
            }
            else DrawSeries(e.Graphics, samples, plot, max, i => VisionTimelineData.Value(samples[i], Metric), UiPalette.LightSkyBlue);
        }
        else if (errorsMetric)
        {
            // Reversed draw order matches the top-to-bottom P,N,B,R,Q,K legend.
            var ordered = selected.Reverse().ToArray();
            var levels = samples.Select(sample =>
            {
                double bottom = 0;
                return ordered.Select(category =>
                {
                    double value = VisionTimelineData.Value(sample, Metric, category.Key) ?? 0;
                    var range = (Category: category, Bottom: bottom, Top: bottom + value); bottom += value; return range;
                }).ToArray();
            }).ToArray();
            if (ConnectBars)
            {
                int previous = -1;
                for (int i = 0; i < samples.Length; i++)
                {
                    if (samples[i].RunCount == 0) continue;
                    if (previous >= 0)
                    for (int c = 0; c < ordered.Length; c++)
                    {
                        var before = levels[previous][c]; var after = levels[i][c];
                        using var fill = new SolidBrush(Color.FromArgb(210, colors[after.Category.Key]));
                        e.Graphics.FillPolygon(fill, new PointF[] { new(BarX(previous) + BarWidth, Y(before.Top)), new(BarX(i), Y(after.Top)), new(BarX(i), Y(after.Bottom)), new(BarX(previous) + BarWidth, Y(before.Bottom)) });
                    }
                    previous = i;
                }
            }
            for (int i = 0; i < samples.Length; i++)
                foreach (var range in levels[i])
                {
                    float height = Y(range.Bottom) - Y(range.Top); if (height <= 0) continue;
                    using var fill = new SolidBrush(colors[range.Category.Key]);
                    e.Graphics.FillRectangle(fill, BarX(i), Y(range.Top), BarWidth, height);
                    if (BarWidth >= 70 && height >= 25)
                        TextRenderer.DrawText(e.Graphics, L.T(range.Category.Pattern), Font, Rectangle.Round(new RectangleF(BarX(i) + 2, Y(range.Top) + 2, BarWidth - 4, height - 4)), Color.Black, TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);
                }
        }
        else
        {
            using var fill = new SolidBrush(UiPalette.LightSkyBlue);
            for (int i = 0; i < samples.Length; i++)
                if (VisionTimelineData.Value(samples[i], Metric) is double value)
                    e.Graphics.FillRectangle(fill, BarX(i), Y(value), BarWidth, plot.Bottom - Y(value));
        }
        int stride = Math.Max(1, (int)Math.Ceiling(68 / column));
        for (int i = 0; i < samples.Length; i += stride)
            e.Graphics.DrawString(samples[i].Date.ToString("dd.MM.yy"), Font, ink, plot.Left + i * column - 12, plot.Bottom + 8);
        if (errorsMetric) DrawLegend(e.Graphics, categories, colors, plot);
        if (errorsMetric && selected.Length == 0) e.Graphics.DrawString(L.T("Keine Fehlertypen ausgewählt."), Font, ink, plot.Left + 12, plot.Top + 16);
    }
    private double? Sum(VisionTimelineSample sample, VisionErrorCategory[] selected)
    {
        if (VisionTimelineData.Value(sample, Metric) is null) return null;
        return selected.Sum(c => VisionTimelineData.Value(sample, Metric, c.Key) ?? 0);
    }
    private void DrawSeries(Graphics graphics, VisionTimelineSample[] samples, RectangleF plot, double max, Func<int, double?> value, Color color, bool dashed = false)
    {
        using var pen = new Pen(color, dashed ? 2.7f : 2) { DashStyle = dashed ? DashStyle.Dash : DashStyle.Solid };
        PointF? previous = null;
        for (int i = 0; i < samples.Length; i++)
        {
            if (value(i) is not double number) continue;
            var point = new PointF(plot.Left + (i + .5f) * plot.Width / samples.Length, plot.Bottom - (float)(number / max * plot.Height));
            if (previous is PointF before) graphics.DrawLine(pen, before, point);
            else if (samples.Length < 100) { using var dot = new SolidBrush(color); graphics.FillEllipse(dot, point.X - 2, point.Y - 2, 4, 4); }
            previous = point;
        }
    }
    private void DrawLegend(Graphics graphics, VisionErrorCategory[] categories, Dictionary<string, Color> colors, RectangleF plot)
    {
        const int rowHeight = 32;
        int available = Math.Max(1, Height - 42), content = categories.Length * rowHeight;
        legendOffset = Math.Clamp(legendOffset, 0, Math.Max(0, content - available));
        var state = graphics.Save(); graphics.SetClip(new Rectangle(0, 30, legendWidth, available));
        if (iconSkin != Skins.Current.Id) { foreach (var icon in icons.Values) icon.Dispose(); icons.Clear(); iconSkin = Skins.Current.Id; }
        foreach (var (category, index) in categories.Select((category, index) => (category, index)))
        {
            var bounds = new Rectangle(5, 32 + index * rowHeight - legendOffset, legendWidth - 14, rowHeight - 2);
            if (bounds.Bottom < 30 || bounds.Top > Height) continue;
            bool selected = IsCategorySelected(category.Key);
            using var patch = new SolidBrush(selected ? colors[category.Key] : UiPalette.FromArgb(83, 91, 99));
            graphics.FillRectangle(patch, bounds.Left + 2, bounds.Top + 8, 10, 15);
            if (category.Piece != '\0')
            {
                if (!icons.TryGetValue(category.Piece, out var icon))
                {
                    icon = new(27, 29); using var g = Graphics.FromImage(icon); BoardControl.DrawPiece(g, category.Piece, new(13, 14), 29); icons[category.Piece] = icon;
                }
                using var attributes = new System.Drawing.Imaging.ImageAttributes();
                attributes.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix { Matrix33 = selected ? 1 : .3f });
                graphics.DrawImage(icon, new Rectangle(bounds.Left + 17, bounds.Top, 27, 29), 0, 0, 27, 29, GraphicsUnit.Pixel, attributes);
            }
            TextRenderer.DrawText(graphics, L.T(category.Pattern), Font, new Rectangle(bounds.Left + 49, bounds.Top, bounds.Width - 49, bounds.Height), selected ? ForeColor : UiPalette.FromArgb(155, 163, 171), TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            legendHits.Add((bounds, category.Key));
        }
        graphics.Restore(state);
        if (content > available)
        {
            using var track = new SolidBrush(UiPalette.FromArgb(32, 38, 44)); using var thumb = new SolidBrush(UiPalette.FromArgb(94, 111, 127));
            graphics.FillRectangle(track, legendWidth - 7, 30, 5, available);
            float height = Math.Max(18, available * available / (float)content);
            graphics.FillRectangle(thumb, legendWidth - 7, 30 + legendOffset * (available - height) / Math.Max(1, content - available), 5, height);
        }
    }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e); if (e.Button != MouseButtons.Left || e.Y < 30 || e.X >= legendWidth) return;
        var hit = legendHits.FirstOrDefault(h => h.Bounds.Contains(e.Location));
        if (hit.Key is not null) ToggleCategory(hit.Key);
    }
    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e); if (e.X > legendWidth) return;
        legendOffset = Math.Max(0, legendOffset - Math.Sign(e.Delta) * 64); Invalidate();
    }
    protected override void Dispose(bool disposing) { if (disposing) { foreach (var icon in icons.Values) icon.Dispose(); icons.Clear(); } base.Dispose(disposing); }
}
