using System.Text.Json;

namespace ChessOpeningTrainer;
public static class Skins
{
    private static readonly Dictionary<string, Image> images = [];
    public static List<SkinDefinition> All { get; } = Load();

    private static List<SkinDefinition> Load()
    {
        var list = new List<SkinDefinition>();
        var directory = Path.Combine(AppContext.BaseDirectory, "Customization", "skins");
        if (System.IO.Directory.Exists(directory))
            foreach (var file in System.IO.Directory.EnumerateFiles(directory, "*.json"))
                try
                {
                    var skin = JsonSerializer.Deserialize<SkinDefinition>(File.ReadAllText(file), OpeningStore.Json)!;
                    skin.Directory = Path.GetDirectoryName(file)!;
                    list.Add(skin);
                }
                catch
                {
                }

        if (list.Count == 0)
            list.Add(new() { Id = "classic", Name = "Classic" });
        return list;
    }

    public static SkinDefinition Current => All.FirstOrDefault(s => s.Id == Preferences.Current.Skin) ?? All[0];

    public static Color Color(string html, Color fallback)
    {
        try
        {
            return ColorTranslator.FromHtml(html);
        }
        catch
        {
            return fallback;
        }
    }

    public static Image? Piece(char piece)
    {
        string key = (char.IsUpper(piece) ? "w" : "b") + char.ToLowerInvariant(piece);
        if (!Current.Pieces.TryGetValue(key, out var relative))
            return null;
        string path = Path.GetFullPath(Path.Combine(Current.Directory, relative));
        if (!path.StartsWith(Path.GetFullPath(Current.Directory) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            return null;
        if (images.TryGetValue(path, out var image))
            return image;
        try
        {
            using var source = Image.FromFile(path);
            return images[path] = new Bitmap(source);
        }
        catch
        {
            return null;
        }
    }
}
