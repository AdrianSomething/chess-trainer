using System.Text.Json;

namespace ChessOpeningTrainer;
public sealed class UiThemeDefinition
{
    public string Id { get; set; } = "classic";
    public string Name { get; set; } = "Standard - Dark";
    public Dictionary<string, string> Colors { get; set; } = [];
    public Dictionary<string, string> BranchNodes { get; set; } = [];

    [System.Text.Json.Serialization.JsonIgnore]
    internal Dictionary<int, int> Map { get; } = [];

    [System.Text.Json.Serialization.JsonIgnore]
    internal Dictionary<int, int> Reverse { get; } = [];

    public override string ToString() => Name;
    internal void Compile()
    {
        foreach (var(source, target)in Colors)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(source, "^#[0-9a-fA-F]{6}$") || !System.Text.RegularExpressions.Regex.IsMatch(target, "^#[0-9a-fA-F]{6}$"))
                throw new InvalidDataException("UI colors must use #RRGGBB.");
            int from = ColorTranslator.FromHtml(source).ToArgb() & 0xffffff;
            int to = ColorTranslator.FromHtml(target).ToArgb() & 0xffffff;
            Map[from] = to;
            Reverse.TryAdd(to, from);
        }
    }
}

/// <summary>Central paint/control palette. Empty overrides reproduce V10 exactly.</summary>
public static class UiPalette
{
    public static IReadOnlyList<UiThemeDefinition> All { get; } = Load();
    public static string? LastError { get; private set; }
    public static UiThemeDefinition Current => All.FirstOrDefault(t => t.Id == Preferences.Current.UiTheme) ?? All[0];

    private static List<UiThemeDefinition> Load()
    {
        var themes = new List<UiThemeDefinition>
        {
            new()
        };
        string directory = Path.Combine(AppContext.BaseDirectory, "Customization", "ui-themes");
        if (!Directory.Exists(directory))
            return themes;
        foreach (string file in Directory.EnumerateFiles(directory, "*.json").Order(StringComparer.Ordinal))
        {
            try
            {
                var theme = JsonSerializer.Deserialize<UiThemeDefinition>(File.ReadAllText(file), OpeningStore.Json) ?? throw new InvalidDataException("Empty theme");
                if (string.IsNullOrWhiteSpace(theme.Id) || themes.Any(t => t.Id == theme.Id))
                    continue;
                theme.Compile();
                themes.Add(theme);
            }
            catch (Exception ex)when (ex is IOException or JsonException or ArgumentException or InvalidDataException)
            {
                LastError = Path.GetFileName(file) + ": " + ex.Message;
            }
        }

        return themes;
    }

    public static Color Resolve(Color original)
    {
        int rgb = original.ToArgb() & 0xffffff;
        return Current.Map.TryGetValue(rgb, out int mapped) ? Color.FromArgb(original.A, Color.FromArgb(mapped | unchecked((int)0xff000000))) : original;
    }

    public static Color FromArgb(int r, int g, int b) => Resolve(Color.FromArgb(r, g, b));
    public static Color FromArgb(int a, int r, int g, int b) => Resolve(Color.FromArgb(a, r, g, b));
    public static Color FromArgb(int a, Color color) => Resolve(Color.FromArgb(a, color));
    public static Color Gainsboro => Resolve(Color.Gainsboro);
    public static Color LightSkyBlue => Resolve(Color.LightSkyBlue);
    public static Color Orange => Resolve(Color.Orange);
    public static bool LightBackground => FromArgb(31, 35, 39).GetBrightness() > .6f;
    public static Color ButtonText => LightBackground ? Resolve(Color.White) : Gainsboro;
    public static Color BranchNodeColor(string status, bool learner)
    {
        Color original = status switch
        {
            "excluded" => Color.FromArgb(65, 65, 65), "later" => Color.FromArgb(153, 109, 171),
            "learned" => Color.FromArgb(65, 178, 110), "due" => Color.FromArgb(225, 166, 64),
            "seen" => Color.FromArgb(76, 143, 208), _ => Color.FromArgb(133, 141, 151)
        };
        string key = learner ? status : status + "-opponent";
        if (Current.BranchNodes.TryGetValue(key, out string? value)) return Skins.Color(value, original);
        Color color = Resolve(original);
        return learner ? color : Color.FromArgb((int)(color.R * .64), (int)(color.G * .64), (int)(color.B * .64));
    }
    internal static Color InitialControlColor(Color color) => Current.Reverse.ContainsKey(color.ToArgb() & 0xffffff) ? color : Resolve(color);

    public static void ApplyTree(Control root, UiThemeDefinition previous)
    {
        Color Convert(Color color)
        {
            int rgb = color.ToArgb() & 0xffffff;
            if (previous.Reverse.TryGetValue(rgb, out int original))
                color = Color.FromArgb(color.A, Color.FromArgb(original | unchecked((int)0xff000000)));
            return Resolve(color);
        }

        // Snapshot first: WinForms propagates a parent's colors to its editor
        // children. Reading after mutation can map inherited colors twice
        // (especially a white input background in the light theme).
        var colors = new List<(Control Control, Color Back, Color Fore)>();
        void Collect(Control control)
        {
            colors.Add((control, Convert(control.BackColor), Convert(control.ForeColor)));
            foreach (Control child in control.Controls) Collect(child);
        }
        Collect(root);
        foreach (var (control, back, fore) in colors)
        {
            control.BackColor = back; control.ForeColor = fore; control.Invalidate();
        }
    }
}
