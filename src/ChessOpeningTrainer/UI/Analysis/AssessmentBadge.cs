namespace ChessOpeningTrainer;

/// <summary>Assessment symbols shared with the analysis sidebar; color is an additional cue.</summary>
public static class AssessmentBadge
{
    private static readonly Lazy<Bitmap?> Sheet = new(() =>
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Customization", "icons", "assessment-sheet.png");
        try { return File.Exists(path) ? new Bitmap(path) : null; }
        catch (Exception ex) when (ex is IOException or ArgumentException) { return null; }
    });
    public static string Symbol(string category) => category switch
    {
        "Patzer" => "??", "Verpasste Chance" => "?!", "Schlecht" => "?",
        "Bester Zug" => "★", "Lektion" => "▤", "Matt" => "#", "Remis" => "½", _ => "✓"
    };
    public static void Draw(Graphics g, RectangleF bounds, string category)
    {
        int tile = category switch { "Bester Zug" => 0, "Gut" => 1, "Schlecht" => 2, "Patzer" => 3, "Verpasste Chance" => 4, "Matt" => 5, "Verloren" => 6, "Remis" => 7, _ => -1 };
        if (Preferences.Current.UseCustomIcons && tile >= 0 && Sheet.Value is { } sheet)
        {
            float cell = sheet.Width / 8f;
            // The supplied sheet has generous transparent margins around each badge.
            g.DrawImage(sheet, bounds, new RectangleF(tile * cell + cell * .24f, sheet.Height * .24f, cell * .52f, sheet.Height * .52f), GraphicsUnit.Pixel);
            return;
        }
        var color = category switch
        {
            "Patzer" => Color.Firebrick, "Verpasste Chance" => Color.DarkOrange,
            "Schlecht" => Color.Sienna, "Bester Zug" => Color.ForestGreen,
            "Lektion" => Color.SteelBlue, "Matt" => Color.DarkRed, "Remis" => Color.SlateGray,
            _ => Color.SeaGreen
        };
        using var fill = new SolidBrush(color);
        using var edge = new Pen(Color.White, Math.Max(1, bounds.Width / 24));
        g.FillEllipse(fill, bounds); g.DrawEllipse(edge, bounds);
        using var font = new Font("Segoe UI", bounds.Height * .48f, FontStyle.Bold, GraphicsUnit.Pixel);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(Symbol(category), font, Brushes.White, bounds, format);
    }
}
