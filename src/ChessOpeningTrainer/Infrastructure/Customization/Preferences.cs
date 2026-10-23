using System.Text.Json;

namespace ChessOpeningTrainer;
public static class Preferences
{
    public static UserPreferences Current { get; private set; } = new();

    private static string? file;
    public static string DataDirectory => file is not null ? Path.GetDirectoryName(file)! : Environment.GetEnvironmentVariable("CHESS_TRAINER_DATA") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChessOpeningTrainer");
    public static event Action? Changed;
    // Transient live previews repaint controls without writing a file for every mouse move.
    public static void PreviewAppearance() => Changed?.Invoke();
    public static void Initialize(string directory)
    {
        string path = Path.Combine(directory, "preferences.json");
        if (file == path)
            return;
        file = path;
        try
        {
            if (File.Exists(path))
                Current = JsonSerializer.Deserialize<UserPreferences>(File.ReadAllText(path), OpeningStore.Json) ?? new();
        }
        catch
        {
        }

        Current.Volume = Math.Clamp(Current.Volume, 0, 100);
        Changed?.Invoke();
    }

    public static void Save()
    {
        if (file is not null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(file, JsonSerializer.Serialize(Current, OpeningStore.Json));
        }

        Changed?.Invoke();
    }
}
