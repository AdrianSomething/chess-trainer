using System.Drawing.Drawing2D;

namespace ChessOpeningTrainer;

/// <summary>A clickable graph of stored per-position evaluation, shared by the game review window.</summary>
public sealed class GameEvaluationGraph : Control
{
    private IReadOnlyList<GameEvaluationPoint> samples = [];
    private int currentIndex;
    private bool whiteAtBottom = true;
    public event Action<int>? PositionSelected;
    public IReadOnlyList<GameEvaluationPoint> Samples
    {
        get => samples;
        set { samples = value; currentIndex = Math.Clamp(currentIndex, 0, Math.Max(0, samples.Count - 1)); Invalidate(); }
    }
    public int CurrentIndex
    {
        get => currentIndex;
        set { currentIndex = Math.Clamp(value, 0, Math.Max(0, samples.Count - 1)); Invalidate(); }
    }
    public bool WhiteAtBottom
    {
        get => whiteAtBottom;
        set { whiteAtBottom = value; Invalidate(); }
    }
    public RectangleF PlotBounds => new(58, 30, Math.Max(1, Width - 78), Math.Max(1, Height - 84));
    public double AxisMaximum => Math.Max(2, Math.Min(12, Math.Ceiling(samples.Select(s => Math.Abs(s.WhitePawns)).DefaultIfEmpty(2).Max())));
    public PointF SampleLocation(int index)
    {
        var plot = PlotBounds;
        int i = Math.Clamp(index, 0, Math.Max(0, samples.Count - 1));
        double value = samples.Count == 0 ? 0 : samples[i].WhitePawns;
        float direction = whiteAtBottom ? 1 : -1;
        return new(plot.Left + plot.Width * i / Math.Max(1, samples.Count - 1),
            plot.Top + plot.Height / 2 + direction * (float)Math.Clamp(value / AxisMaximum, -1, 1) * plot.Height / 2);
    }
    public int PositionIndexAt(float x)
    {
        var plot = PlotBounds;
        return Math.Clamp((int)Math.Round((x - plot.Left) / plot.Width * Math.Max(0, samples.Count - 1)), 0, Math.Max(0, samples.Count - 1));
    }

    public GameEvaluationGraph()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.ResizeRedraw, true);
        BackColor = UiPalette.FromArgb(31, 35, 39);
        ForeColor = UiPalette.Gainsboro;
        Cursor = Cursors.Hand;
        AccessibleName = "Evaluationsgraph";
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left && samples.Count > 0 && PlotBounds.Contains(e.Location))
            PositionSelected?.Invoke(PositionIndexAt(e.X));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        var plot = PlotBounds;
        float middle = plot.Top + plot.Height / 2;
        using var black = new SolidBrush(Color.FromArgb(38, 40, 43));
        using var white = new SolidBrush(Color.FromArgb(226, 228, 225));
        g.FillRectangle(whiteAtBottom ? black : white, plot.Left, plot.Top, plot.Width, plot.Height / 2);
        g.FillRectangle(whiteAtBottom ? white : black, plot.Left, middle, plot.Width, plot.Height / 2);
        using var grid = new Pen(UiPalette.FromArgb(110, 133, 141, 151));
        using var axis = new Pen(ForeColor, 1);
        using var text = new SolidBrush(ForeColor);
        using var right = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
        for (int n = -2; n <= 2; n++)
        {
            float y = middle + n * plot.Height / 4;
            double value = n * AxisMaximum / 2 * (whiteAtBottom ? 1 : -1);
            g.DrawLine(grid, plot.Left, y, plot.Right, y);
            g.DrawString($"{value:+0.#;-0.#;0}", Font, text, new RectangleF(0, y - 10, plot.Left - 8, 20), right);
        }
        g.DrawRectangle(axis, plot.X, plot.Y, plot.Width, plot.Height);
        if (samples.Count == 0)
        {
            using var panel = new SolidBrush(BackColor);
            g.FillRectangle(panel, plot);
            g.DrawString(L.T("Partie zuerst vollständig analysieren."), Font, text, plot.Left + 12, middle - 8);
            return;
        }
        if (samples.Count > 1)
        {
            using var under = new Pen(Color.FromArgb(160, 0, 0, 0), 4);
            using var line = new Pen(UiPalette.Orange, 2);
            var points = Enumerable.Range(0, samples.Count).Select(SampleLocation).ToArray();
            g.DrawLines(under, points); g.DrawLines(line, points);
        }
        var selected = SampleLocation(currentIndex);
        using var markerUnder = new Pen(Color.Black, 4);
        using var marker = new Pen(Color.White, 2);
        g.DrawLine(markerUnder, selected.X, plot.Top, selected.X, plot.Bottom);
        g.DrawLine(marker, selected.X, plot.Top, selected.X, plot.Bottom);
        using var dot = new SolidBrush(UiPalette.Orange);
        g.FillEllipse(dot, selected.X - 4, selected.Y - 4, 8, 8);
        int steps = Math.Max(1, Math.Min(8, samples.Count - 1));
        using var centered = new StringFormat { Alignment = StringAlignment.Center };
        for (int n = 0; n <= steps; n++)
        {
            int i = (int)Math.Round((samples.Count - 1) * n / (double)steps);
            float x = SampleLocation(i).X;
            g.DrawString(i == 0 ? "0" : $"{(i + 1) / 2}{(i % 2 == 1 ? "." : "…")}", Font, text, new RectangleF(x - 25, plot.Bottom + 7, 50, 22), centered);
        }
        g.DrawString(L.T(whiteAtBottom ? "Schwarz" : "Weiß"), Font, text, plot.Left + 2, 5);
        g.DrawString(L.T(whiteAtBottom ? "Weiß" : "Schwarz"), Font, text, plot.Left + 2, plot.Bottom + 31);
    }
}
