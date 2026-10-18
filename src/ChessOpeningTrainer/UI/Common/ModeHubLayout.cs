namespace ChessOpeningTrainer;

internal static class ModeHubLayout
{
    public const int HeaderHeight = 130, NavigationHeight = 48;
    public static void Center(FlowLayoutPanel hub, Control? separatedCard = null)
    {
        foreach (Control card in hub.Controls)
        {
            if (card is ModeMenuButton)
            {
                card.Width = Math.Min(ModeMenuButton.PreferredWidth, Math.Max(280, hub.ClientSize.Width - 40));
                card.Margin = new(Math.Max(12, (hub.ClientSize.Width - card.Width) / 2), card == separatedCard ? 24 : 0, 0, 12);
            }
            else if (card is Label label)
            {
                label.MaximumSize = new(Math.Max(120, Math.Min(800, hub.ClientSize.Width - 40)), 0);
                label.Margin = new(Math.Max(12, (hub.ClientSize.Width - label.Width) / 2), label.Margin.Top, 0, label.Margin.Bottom);
            }
        }
    }
}
