using System.Drawing.Drawing2D;

namespace ChessOpeningTrainer;

/// <summary>Large mode name, independent record line and chess-native icon.</summary>
public sealed class VisionModeButton : ModeMenuButton
{
    public int Mode { get; }
    public string Record { get => Detail; set => Detail = value; }
    public VisionModeButton(int mode, string name) : base("vision-mode-" + mode, name)
    {
        Mode = mode; Tag = name;
        AccessibleName = name;
    }
    protected override void DrawCardIcon(Graphics graphics, Rectangle bounds)
    {
        if (HasCustomIcon) { base.DrawCardIcon(graphics, bounds); return; }
        var state = graphics.Save(); graphics.TranslateTransform(bounds.Left, bounds.Top); graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(UiPalette.Gainsboro, 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var ink = new SolidBrush(UiPalette.Gainsboro);
        switch (Mode)
        {
            case 0:
                graphics.DrawEllipse(pen, 16, 15, 25, 25); graphics.FillEllipse(ink, 25, 24, 7, 7);
                graphics.DrawLine(pen, 2, 4, 23, 22); graphics.DrawLine(pen, 23, 22, 15, 21); graphics.DrawLine(pen, 23, 22, 21, 14); break;
            case 1:
                BoardControl.DrawPiece(graphics, 'N', new(20, 25), 40);
                graphics.DrawLines(pen, [new(35, 37), new(45, 37), new(45, 8)]); graphics.DrawLines(pen, [new(40, 14), new(45, 8), new(50, 14)]); break;
            case 2:
                BoardControl.DrawPiece(graphics, 'R', new(18, 25), 40); graphics.DrawLine(pen, 32, 6, 48, 35);
                graphics.DrawLines(pen, [new(48, 25), new(48, 35), new(39, 31)]); break;
            case 3:
                BoardControl.DrawPiece(graphics, 'N', new(19, 25), 40);
                graphics.DrawPolygon(pen, [new(36, 7), new(49, 13), new(47, 30), new(36, 39), new(27, 30), new(26, 13)]); break;
            case 4:
                graphics.DrawRectangle(pen, 1, 7, 18, 32); graphics.DrawRectangle(pen, 28, 7, 18, 32);
                graphics.DrawLine(pen, 6, 16, 14, 27); graphics.DrawLine(pen, 14, 16, 6, 27);
                graphics.DrawLines(pen, [new(32, 25), new(37, 30), new(43, 18)]); break;
            case 5:
                BoardControl.DrawPiece(graphics, 'P', new(18, 26), 42);
                using (var question = new Font("Segoe UI", 22, FontStyle.Bold)) graphics.DrawString("?", question, ink, 30, 3); break;
            default:
                graphics.FillRectangle(ink, 4, 24, 8, 18); graphics.FillRectangle(ink, 20, 13, 8, 29); graphics.FillRectangle(ink, 36, 2, 8, 40); break;
        }
        graphics.Restore(state);
    }
}
