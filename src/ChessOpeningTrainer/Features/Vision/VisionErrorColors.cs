namespace ChessOpeningTrainer;

/// <summary>Stable piece hues and widely separated error tones, independent of date/filter.</summary>
internal static class VisionErrorColors
{
    private static readonly (double Saturation, double Value)[] tones =
        [(.94, .92), (.38, .96), (.91, .46)];
    public static Color For(VisionErrorCategory category, IEnumerable<VisionErrorCategory>? canonical = null)
    {
        if (category.Piece == '\0') return UiPalette.FromArgb(155, 163, 171);
        double hue = category.Piece switch { 'P' => 38, 'N' => 215, 'B' => 130, 'R' => 2, 'Q' => 280, _ => 180 };
        string pattern = category.Pattern;
        int tone;
        if (pattern == "Falsche Figurenfarbe" || pattern == "Röntgenlinie" || pattern == "Illegales Zielfeld" || pattern == "Nicht vorhandene Angriffslinie markiert") tone = 2;
        else if (pattern.StartsWith("Falsch", StringComparison.Ordinal)) tone = 1;
        else if (pattern.StartsWith("Zusätzliche", StringComparison.Ordinal) || pattern == "Eigene Figur als Angreifer markiert") tone = 2;
        else if (pattern == "Gegnerische Figur als Ziel markiert") tone = 1;
        else if (pattern == "Freiziehen übersehen") tone = 2;
        else if (pattern == "Umpositionierung übersehen") tone = 0;
        else if (pattern == "Schlechterer Vergleichszug") tone = 0;
        else if (pattern is "Linienangriff" or "Bauernangriff" or "Königsnähe" or "Springersprung" or "Umpositionierung / Freiziehen" or "Angriff übersehen" or "Figur nicht wiederaufgebaut" or "Zielfeld bleibt angegriffen") tone = 0;
        else
        {
            uint hash = 2166136261;
            foreach (char letter in pattern) hash = unchecked((hash ^ letter) * 16777619);
            tone = (int)(hash % tones.Length);
        }
        var known = canonical?.Where(c => c.Piece == category.Piece).Select(c => c.Pattern).Distinct().Order(StringComparer.Ordinal).ToArray() ?? [];
        int order = Array.IndexOf(known, pattern);
        if (order >= 0 && pattern != "Schlechterer Vergleichszug")
            tone = known.Length == 1 ? 0 : known.Length == 2 ? (order == 0 ? 0 : 2) : order == 0 ? 1 : order == 1 ? 0 : 2;
        var (saturation, value) = tones[tone];
        double chroma = value * saturation, x = chroma * (1 - Math.Abs(hue / 60 % 2 - 1)), m = value - chroma;
        (double r, double g, double b) = hue switch { < 60 => (chroma, x, 0d), < 120 => (x, chroma, 0d), < 180 => (0d, chroma, x), < 240 => (0d, x, chroma), < 300 => (x, 0d, chroma), _ => (chroma, 0d, x) };
        return Color.FromArgb((int)Math.Round((r + m) * 255), (int)Math.Round((g + m) * 255), (int)Math.Round((b + m) * 255));
    }
}
