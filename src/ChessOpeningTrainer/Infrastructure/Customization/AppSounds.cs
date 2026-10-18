using System.Windows.Threading;
using System.Text.Json;

namespace ChessOpeningTrainer;
public static class AppSounds
{
    private static string? settings;
    private static readonly Dictionary<string, List<SoundVoice>> players = [];
    public sealed class SoundPack
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public bool Synthesized { get; set; }
        public Dictionary<string, string[]> Files { get; set; } = [];

        [System.Text.Json.Serialization.JsonIgnore]
        public string Directory { get; set; } = "";

        public override string ToString() => Name;
    }

    public static List<SoundPack> Packs { get; } = [];

    private static string loadedPack = "";
    private static bool legacyMode;
    public static string? LastError { get; private set; }
    public static string? LastEvent { get; private set; }

    private static Dispatcher? dispatcher;
    private static Thread? audioThread;
    private static readonly object threadLock = new();
    private static DispatcherTimer? watchdog;
    private static int readyFiles, loadedFiles, loadSerial;
    public static int ReadyFiles => Volatile.Read(ref readyFiles);
    public static int LoadedFiles => Volatile.Read(ref loadedFiles);
    public static double AppliedVolume { get; private set; }

    private static void EnsureAudioThread()
    {
        lock (threadLock)
        {
        // Never have two STAs touching the shared pack. During shutdown skip an
        // event; the next one creates a session after the old thread has exited.
        if (audioThread?.IsAlive == true) return;
        using var ready = new ManualResetEventSlim();
        audioThread = new Thread(() =>
        {
            dispatcher = Dispatcher.CurrentDispatcher;
            dispatcher.UnhandledException += (_, e) => { LastError = e.Exception.Message; e.Handled = true; };
            // A replacement dispatcher must not reuse MediaPlayers owned by the old STA.
            players.Clear(); loadedPack = "";
            watchdog = new DispatcherTimer(TimeSpan.FromMilliseconds(350), DispatcherPriority.Background, (_, _) =>
            {
                bool active = false;
                foreach (var voice in players.Values.SelectMany(x => x))
                {
                    voice.CheckPlayback(AppliedVolume);
                    active |= voice.IsPlaying;
                }
                if (!active) watchdog?.Stop();
            }, dispatcher);
            watchdog.Stop();
            ready.Set();
            Dispatcher.Run();
        })
        {
            IsBackground = true,
            Name = "Chess audio"
        };
        audioThread.SetApartmentState(ApartmentState.STA);
        audioThread.Start();
        ready.Wait();
        }
    }

    private static string cache = "";
    public static bool Muted { get; private set; }

    public static void Initialize(string directory)
    {
        settings = Path.Combine(directory, "sound.json");
        try
        {
            if (File.Exists(settings))
                Muted = JsonSerializer.Deserialize<bool>(File.ReadAllText(settings));
        }
        catch
        {
        }

        Preferences.Initialize(directory);
        cache = Path.Combine(directory, "SoundCache");
        Directory.CreateDirectory(cache);
        EnsureAudioThread();
        if (Packs.Count == 0)
        {
            var root = Path.Combine(AppContext.BaseDirectory, "Customization", "sounds");
            if (Directory.Exists(root))
                foreach (var file in Directory.EnumerateFiles(root, "pack.json", SearchOption.AllDirectories))
                    try
                    {
                        var pack = JsonSerializer.Deserialize<SoundPack>(File.ReadAllText(file), OpeningStore.Json)!;
                        pack.Directory = Path.GetDirectoryName(file)!;
                        Packs.Add(pack);
                    }
                    catch
                    {
                    }

            Preferences.Changed += Update;
        }

        Update();
    }

    private static void Update()
    {
        if (cache == "" || dispatcher is null)
            return;
        try
        {
        EnsureAudioThread();
        var target = dispatcher!;
        if (target.HasShutdownStarted || target.HasShutdownFinished) return;
        target.Invoke(() =>
        {
            bool reload = loadedPack != Preferences.Current.SoundPack || players.Count == 0;
            if (reload)
            {
                foreach (var p in players.Values.SelectMany(x => x))
                    p.Dispose();
                players.Clear();
                loadedPack = Preferences.Current.SoundPack;
                readyFiles = loadedFiles = 0;
                loadSerial++;
                Load();
            }

            double volume = Muted ? 0 : Preferences.Current.Volume / 100d;
            if (!reload && AppliedVolume == volume) return;
            AppliedVolume = volume;
            foreach (var p in players.Values.SelectMany(x => x))
                p.SetVolume(AppliedVolume);
        });
        }
        catch (Exception e) { LastError = e.Message; }
    }

    private static SoundVoice Open(string path)
    {
        int serial = loadSerial;
        Interlocked.Increment(ref loadedFiles);
        return new(path, () => { if (serial == loadSerial) Interlocked.Increment(ref readyFiles); },
            error => { if (serial == loadSerial) LastError = error; });
    }

    private static void Load()
    {
        var pack = Packs.FirstOrDefault(p => p.Id == loadedPack);
        legacyMode = pack is null || pack.Synthesized || pack.Id == "legacy";
        LastError = null;
        if (pack is not null && !pack.Synthesized)
        {
            foreach (var(kind, files)in pack.Files)
            {
                players[kind] = [];
                foreach (var relative in files)
                {
                    string path = Path.GetFullPath(Path.Combine(pack.Directory, relative));
                    if (!path.StartsWith(Path.GetFullPath(pack.Directory) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
                    {
                        LastError = "Sounddatei fehlt: " + relative;
                        continue;
                    }

                    players[kind].Add(Open(path));
                }
            }

            return;
        }

        foreach (var kind in new[]
        {
            "move",
            "capture",
            "check",
            "mate",
            "error",
            "success"
        }

        )
            try
            {
                int rate = 44100;
                double seconds = kind == "mate" ? .3 : kind == "check" ? .18 : .11;
                int samples = (int)(rate * seconds);
                double hz = kind switch
                {
                    "capture" => 230,
                    "check" => 780,
                    "mate" => 520,
                    "error" => 190,
                    "success" => 880,
                    _ => 340
                };
                var bytes = new MemoryStream();
                using (var w = new BinaryWriter(bytes, System.Text.Encoding.ASCII, true))
                {
                    w.Write("RIFF"u8);
                    w.Write(36 + samples * 2);
                    w.Write("WAVEfmt "u8);
                    w.Write(16);
                    w.Write((short)1);
                    w.Write((short)1);
                    w.Write(rate);
                    w.Write(rate * 2);
                    w.Write((short)2);
                    w.Write((short)16);
                    w.Write("data"u8);
                    w.Write(samples * 2);
                    var random = new Random(23);
                    double noise = 0;
                    for (int i = 0; i < samples; i++)
                    {
                        double t = i / (double)rate;
                        noise = .55 * noise + .45 * (random.NextDouble() * 2 - 1);
                        double envelope = Math.Min(1, t / .0007) * Math.Min(1, (samples - i) / 220d);
                        double wave = (.75 * noise + .5 * Math.Sin(2 * Math.PI * hz * t) + .22 * Math.Sin(2 * Math.PI * hz * 2.73 * t)) * Math.Exp(-t * (kind == "capture" ? 48 : 65));
                        if (kind is "check" or "mate" or "success")
                            wave += .18 * (Math.Sin(2 * Math.PI * hz * t) + .5 * Math.Sin(2 * Math.PI * hz * 1.5 * t)) * Math.Exp(-t * 18);
                        w.Write((short)(wave * envelope * 6500));
                    }
                }

                string path = Path.Combine(cache, kind + ".wav");
                if (!File.Exists(path))
                    File.WriteAllBytes(path, bytes.ToArray());
                players[kind] = [Open(path)];
            }
            catch
            { /* Sound is optional; no audio device must not affect training. */
            }
    }

    public static void Toggle()
    {
        Muted = !Muted;
        try
        {
            var target = PreparePlayback();
            target?.Invoke(() => { foreach (var p in players.Values.SelectMany(x => x)) p.Stop(); watchdog?.Stop(); });
        }
        catch (Exception e) { LastError = e.Message; }
        Update();
        try
        {
            if (settings is not null)
                File.WriteAllText(settings, JsonSerializer.Serialize(Muted));
        }
        catch
        {
        }
    }

    public static string MoveKind(Position before, Move move, Position after)
    {
        if (MoveGenerator.IsKingInCheck(after, after.WhiteToMove))
            return MoveGenerator.GenerateLegal(after).Count == 0 ? "mate" : "check";
        return before.Squares[move.To] != '\0' || char.ToLowerInvariant(before.Squares[move.From]) == 'p' && move.From % 8 != move.To % 8 ? "capture" : "move";
    }

    public static void PlayMove(Position before, Move move, Position after, bool royalKings = true)
    {
        string kind = royalKings ? MoveKind(before, move, after) : before.Squares[move.To] != '\0' || move.IsEnPassant ? "capture" : "move";
        if (!legacyMode && (kind == "move" || kind == "capture" && !HasCaptureSound))
            kind = char.ToLowerInvariant(before.Squares[move.From]) == 'p' ? "light" : "heavy";
        Play(kind);
    }

    // Optional future pack extension. Existing manifests contain no capture file,
    // so captures keep exactly their current light/heavy placement sound.
    public static bool HasCaptureSound => Packs.FirstOrDefault(p => p.Id == Preferences.Current.SoundPack) is { Synthesized: false } pack
        && pack.Files is not null && pack.Files.TryGetValue("capture", out var files) && files is not null && files.Any(relative =>
        {
            try
            {
            string path = Path.GetFullPath(Path.Combine(pack.Directory, relative));
            return path.StartsWith(Path.GetFullPath(pack.Directory) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && File.Exists(path);
            }
            catch (Exception e) when (e is ArgumentException or IOException or NotSupportedException) { return false; }
        });

    private static Dispatcher? PreparePlayback()
    {
        try
        {
            EnsureAudioThread();
            var target = dispatcher!;
            if (target.HasShutdownStarted || target.HasShutdownFinished) return null;
            target.Invoke(() => { if (players.Count == 0) Update(); });
            return target;
        }
        catch (Exception e) { LastError = e.Message; return null; }
    }

    private static Task StartPlayback(string kind)
    {
        if (Muted || Preferences.Current.Volume == 0 || !players.TryGetValue(kind, out var choices) || choices.Count == 0)
            return Task.CompletedTask;
        var playing = choices[Random.Shared.Next(choices.Count)].Play(AppliedVolume);
        watchdog?.Start();
        return playing;
    }

    public static async Task PlayAndWait(string kind)
    {
        LastEvent = kind;
        if (Muted || Preferences.Current.Volume == 0 || cache == "")
        {
            await Task.Delay(250);
            return;
        }

        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            var target = PreparePlayback();
            if (target is null) return;
            _ = target.BeginInvoke(async () =>
            {
                try { await StartPlayback(kind); }
                catch (Exception ex) { LastError = ex.Message; }
                finally { finished.TrySetResult(); }
            });
            await Task.WhenAny(finished.Task, Task.Delay(5000));
        }
        catch (Exception e) { LastError = e.Message; }
    }

    public static void Play(string kind)
    {
        LastEvent = kind;
        if (Muted || Preferences.Current.Volume == 0 || cache == "")
            return;
        try
        {
            PreparePlayback()?.BeginInvoke(() =>
            {
                try { _ = StartPlayback(kind); }
                catch (Exception ex) { LastError = ex.Message; }
            });
        }
        catch (Exception e) { LastError = e.Message; }
    }
}
