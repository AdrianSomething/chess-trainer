using System.Drawing.Drawing2D;

namespace ChessOpeningTrainer;

/// <summary>A legible disclosure arrow without enlarging the caption or its settings row.</summary>
internal sealed class BranchDisclosureButton : Button
{
    private bool expanded;
    public bool Expanded { get => expanded; set { expanded = value; Invalidate(); } }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        float x = ClientSize.Width - 17, y = ClientSize.Height / 2f;
        using var ink = new Pen(Enabled ? ForeColor : SystemColors.GrayText, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        e.Graphics.DrawLines(ink, expanded
            ? [new PointF(x - 5, y - 2.5f), new(x, y + 2.5f), new(x + 5, y - 2.5f)]
            : [new PointF(x - 2.5f, y - 5), new(x + 2.5f, y), new(x - 2.5f, y + 5)]);
    }
}
