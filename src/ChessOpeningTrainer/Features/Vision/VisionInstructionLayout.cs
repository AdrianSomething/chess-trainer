namespace ChessOpeningTrainer;

internal static class VisionInstructionLayout
{
    public static void Center(Label cover, Rectangle area)
    {
        int width = Math.Max(120, Math.Min(900, area.Width - 76));
        int height = Math.Max(120, Math.Min(700, area.Height - 76));
        if (cover.Dock != DockStyle.None) cover.Dock = DockStyle.None;
        Rectangle bounds = new(area.Left + (area.Width - width) / 2, area.Top + (area.Height - height) / 2, width, height);
        if (cover.Bounds != bounds) cover.Bounds = bounds;
        if (cover.Parent is Control parent && parent.Controls.GetChildIndex(cover) != 0) cover.BringToFront();
    }
}
