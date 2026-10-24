namespace ChessOpeningTrainer;

internal static class RenameConfirmation
{
    public static void Style(Button button)
    {
        button.Text = "✓";
        button.Font = new Font(SystemFonts.MessageBoxFont!.FontFamily, 12, FontStyle.Bold);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = UiPalette.FromArgb(85, 92, 99);
        button.BackColor = UiPalette.FromArgb(43, 49, 55);
        button.ForeColor = UiPalette.Gainsboro;
        button.AccessibleName = L.T("Namen bestätigen");
    }
}
