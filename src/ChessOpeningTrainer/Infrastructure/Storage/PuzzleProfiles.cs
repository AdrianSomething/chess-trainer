using System.Text.Json;

namespace ChessOpeningTrainer;
public sealed class PuzzleProfiles
{
    public const string DefaultId = "builtin-default-tactics";
    public List<PuzzleProfile> Items { get; set; } = [];
    public List<PuzzleProfile> LegacyItems { get; set; } = [];
    public PuzzleProfile LastSettings { get; set; } = new();
    public string? LastId { get; set; }

    private string file = "";
    public static PuzzleProfiles Load(string directory)
    {
        string file = Path.Combine(directory, "puzzle-presets.json");
        PuzzleProfiles store;
        try
        {
            store = JsonSerializer.Deserialize<PuzzleProfiles>(File.ReadAllText(file)) ?? new();
        }
        catch
        {
            store = new();
        }

        store.file = file;
        var old = store.Items.Where(p => !p.ExplicitlySaved).ToList();
        if (old.Count > 0)
        {
            store.LastSettings = store.Items.FirstOrDefault(p => p.Id == store.LastId)?.Copy() ?? store.LastSettings;
            store.LegacyItems.AddRange(old);
            store.Items.RemoveAll(p => !p.ExplicitlySaved);
        }

        if (!store.Items.Any(p => p.Id == DefaultId))
            store.Items.Insert(0, new PuzzleProfile { Id = DefaultId, Name = "Standard", ExplicitlySaved = true });
        store.Items.Single(p => p.Id == DefaultId).Draw = false;
        return store;
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        string tmp = file + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(tmp, file, true);
    }

    [System.Text.Json.Serialization.JsonIgnore]
    public PuzzleProfile Last => Items.FirstOrDefault(p => p.Id == LastId) ?? LastSettings;
}
