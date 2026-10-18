using System.Text.Json;
using System.Text.RegularExpressions;

namespace ChessOpeningTrainer;
public static class L
{
    public sealed class Language
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";

        public override string ToString() => Name;
    }

    public sealed class Catalog
    {
        public List<Language> Languages { get; set; } = [];
        public List<Dictionary<string, string>> Rows { get; set; } = [];
    }

    private static readonly Catalog catalog = Load();
    private static readonly Dictionary<string, Dictionary<string, string>> exact = catalog.Rows.Where(r => r.ContainsKey("de")).GroupBy(r => r["de"]).ToDictionary(g => g.Key, g => g.First());
    private static readonly List<(Dictionary<string, string> Row, Regex Pattern)> templates = catalog.Rows.Where(r => r.GetValueOrDefault("de")?.Contains("{0}") == true)
        .GroupBy(r => r["de"]).Select(g => g.First())
        .OrderByDescending(r => Regex.Replace(r["de"], @"\{\d+\}", "").Length).Select(r =>
    {
        string p = Regex.Escape(r["de"]);
        for (int i = 0; i < 8; i++)
            p = p.Replace("\\{" + i + "}", "(?<p" + i + ">.*?)");
        return (r, new Regex("^" + p + "$", RegexOptions.Singleline | RegexOptions.Compiled, TimeSpan.FromMilliseconds(50)));
    }).ToList();
    private static readonly object memoSync = new();
    private static readonly Dictionary<(string Language, string Text), string> memo = [];
    public static IReadOnlyList<Language> Languages => catalog.Languages;

    private static Catalog Load()
    {
        try
        {
            return JsonSerializer.Deserialize<Catalog>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Customization", "languages.json")), OpeningStore.Json) ?? new();
        }
        catch
        {
            return new()
            {
                Languages = [new()
                {
                    Id = "de",
                    Name = "Deutsch"
                }, new()
                {
                    Id = "en",
                    Name = "English"
                }

                ]
            };
        }
    }

    public static string T(string text)
    {
        string language = Preferences.Current.Language;
        if (language == "de" || text.Length == 0) return text;
        // Repaint-heavy views reuse short labels/SAN fragments. Never retain
        // entire games, long explanations or unbounded dynamic status histories.
        bool remember = text.Length <= 512;
        var key = (language, text);
        if (remember) lock (memoSync) if (memo.TryGetValue(key, out string? cached)) return cached;
        string translated = Translate(text, 0, language);
        if (remember) lock (memoSync)
        {
            if (memo.Count >= 4096) memo.Clear();
            memo[key] = translated;
        }
        return translated;
    }

    /// <summary>Keep the original tooltip text so an open window follows language changes.</summary>
    public static void SetToolTip(ToolTip tip, Control control, string text) => LocalizedToolTips.Set(tip, control, text);
    private static string Translate(string text, int depth, string language)
    {
        if (language == "de" || text.Length == 0 || depth > 8)
            return text;
        if (exact.TryGetValue(text, out var entry))
            return entry.GetValueOrDefault(language) ?? text;
        foreach (var(row, pattern)in templates)
        {
            if (!row.TryGetValue(language, out var target))
                continue;
            Match match;
            try { match = pattern.Match(text); }
            catch (RegexMatchTimeoutException) { continue; }
            if (!match.Success)
                continue;
            for (int i = 0; i < 8; i++)
                target = target.Replace("{" + i + "}", Translate(match.Groups["p" + i].Value, depth + 1, language));
            return target;
        }

        if (text.Contains('\n'))
            return string.Join("\n", text.Split('\n').Select(t => Translate(t, depth + 1, language)));
        if (text.Contains(" · "))
            return string.Join(" · ", text.Split(" · ").Select(t => Translate(t, depth + 1, language)));
        return text;
    }

    public static DialogResult Message(IWin32Window owner, string text, string title, MessageBoxButtons buttons = MessageBoxButtons.OK) => MessageBox.Show(owner, T(text), T(title), buttons);
}
