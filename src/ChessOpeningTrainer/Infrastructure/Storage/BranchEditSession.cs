using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ChessOpeningTrainer;

/// <summary>Owns a private graph/progress storage transaction. Checkpoints never target the user's opening.</summary>
public sealed class BranchEditSession
{
    private readonly OpeningDefinition original;
    private readonly ProgressStore originalProgress;
    private readonly ProgressData baselineProgress;
    private readonly string baselineSignature;
    private readonly string? originalFileHash;
    private readonly string stateFile;
    public OpeningDefinition Working { get; private set; }
    public ProgressStore Progress { get; }
    public string CheckpointDirectory { get; }
    public bool Changed { get; private set; }
    public string? CheckpointWarning { get; private set; }
    public string? RestoredDraftFen { get; private set; }
    public List<string> RestoredDraftMoves { get; private set; } = [];
    private Dictionary<string, string> transfers = [];
    private sealed record Checkpoint(string OriginalSignature, string State, string? DraftFen, List<string> DraftMoves, Dictionary<string, string>? Transfers = null);

    public BranchEditSession(OpeningDefinition opening, ProgressStore progress)
    {
        original = opening; originalProgress = progress;
        baselineSignature = Signature(opening); originalFileHash = File.Exists(opening.FilePath) ? Hash(File.ReadAllBytes(opening.FilePath)) : null;
        string prefix = opening.Id + "/";
        baselineProgress = CopyProgress(new ProgressData { GroupSimilarReplies = progress.Data.GroupSimilarReplies, LearningTurns = progress.Data.LearningTurns,
            Moves = progress.Data.Moves.Where(p => p.Key.StartsWith(prefix, StringComparison.Ordinal)).ToDictionary(p => p.Key, p => p.Value),
            BranchPriority = progress.Data.BranchPriority.Where(p => p.Key.StartsWith(prefix, StringComparison.Ordinal)).ToDictionary(p => p.Key, p => p.Value) });
        string id = Hash(Encoding.UTF8.GetBytes(Path.GetFullPath(string.IsNullOrWhiteSpace(opening.FilePath) ? opening.Id : opening.FilePath)))[..24];
        CheckpointDirectory = Path.Combine(progress.DataDirectory, "branch-edits", id);
        stateFile = Path.Combine(CheckpointDirectory, "session.json");
        Progress = new ProgressStore(CheckpointDirectory); AssignProgress(Progress.Data, baselineProgress);
        Working = Clone(opening); Working.FilePath = Path.Combine(CheckpointDirectory, "opening.json");
    }

    public bool CanRestore
    {
        get
        {
            try { var state = JsonSerializer.Deserialize<Checkpoint>(File.ReadAllText(stateFile), OpeningStore.Json); return state is { State: "pending" } && state.OriginalSignature == baselineSignature; }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException) { return false; }
        }
    }

    public bool Restore()
    {
        if (!CanRestore) return false;
        try
        {
            var saved = JsonSerializer.Deserialize<OpeningDefinition>(File.ReadAllText(Working.FilePath), OpeningStore.Json)!;
            OpeningStore.ResolveGraph(saved); OpeningStore.Validate(saved); saved.FilePath = Working.FilePath;
            var state = JsonSerializer.Deserialize<Checkpoint>(File.ReadAllText(stateFile), OpeningStore.Json)!;
            var data = JsonSerializer.Deserialize<ProgressData>(File.ReadAllText(Path.Combine(CheckpointDirectory, "progress.json")), OpeningStore.Json)!;
            Working = saved; AssignProgress(Progress.Data, data); Changed = Signature(saved) != baselineSignature || ProgressChanged();
            RestoredDraftFen = state.DraftFen; RestoredDraftMoves = state.DraftMoves;
            transfers = state.Transfers ?? [];
            return true;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException or InvalidDataException) { return false; }
    }

    public void MarkChanged() => Changed = Signature(Working) != baselineSignature || ProgressChanged();
    public void SaveCheckpoint(string? draftFen = null, IReadOnlyList<string>? draftMoves = null)
    {
        MarkChanged(); OpeningStore.Save(Working, Working.FilePath); Progress.Save();
        WriteState(new(baselineSignature, "pending", draftFen, draftMoves?.ToList() ?? [], transfers));
    }
    public void Discard() => WriteState(new(baselineSignature, "discarded", null, []));

    public void Commit()
    {
        if (string.IsNullOrWhiteSpace(original.FilePath)) throw new InvalidOperationException("Für dieses Opening fehlt der Dateipfad.");
        if (originalFileHash is not null && (!File.Exists(original.FilePath) || Hash(File.ReadAllBytes(original.FilePath)) != originalFileHash))
            throw new InvalidOperationException("Das Original wurde zwischenzeitlich geändert. Bitte den Zwischenstand behalten und das Opening neu öffnen.");
        var saved = Clone(Working); saved.FilePath = original.FilePath;
        var merged = PrepareMergedProgress(saved);
        string tag = ".edit-" + Guid.NewGuid().ToString("N") + ".tmp";
        string openingTemporary = original.FilePath + tag;
        string progressFile = Path.Combine(originalProgress.DataDirectory, "progress.json"), progressTemporary = progressFile + tag;
        byte[]? beforeOpening = File.Exists(original.FilePath) ? File.ReadAllBytes(original.FilePath) : null;
        bool openingPublished = false;
        string? publishedHash = null;
        try
        {
            // Validate and serialize both outputs before changing either original.
            OpeningStore.Save(saved, openingTemporary);
            File.WriteAllText(progressTemporary, JsonSerializer.Serialize(merged, OpeningStore.Json));
            publishedHash = Hash(File.ReadAllBytes(openingTemporary));
            if (beforeOpening is not null && Hash(File.ReadAllBytes(original.FilePath)) != Hash(beforeOpening)
                || beforeOpening is null && File.Exists(original.FilePath))
                throw new IOException("Das Original wurde während des Speicherns geändert.");
            File.Move(openingTemporary, original.FilePath, true); openingPublished = true;
            try { File.Move(progressTemporary, progressFile, true); }
            catch (Exception saveFailure) when (saveFailure is IOException or UnauthorizedAccessException)
            {
                // Never roll back someone else's subsequent edit. Restore only
                // the exact opening bytes just published by this transaction.
                bool stillOurs = File.Exists(original.FilePath) && Hash(File.ReadAllBytes(original.FilePath)) == publishedHash;
                if (!stillOurs) throw new IOException("Der Fortschritt konnte nicht gespeichert werden; die Openingdatei wurde inzwischen anderweitig geändert. Bitte das Opening neu laden.", saveFailure);
                try
                {
                    if (beforeOpening is null) File.Delete(original.FilePath);
                    else { File.WriteAllBytes(openingTemporary, beforeOpening); File.Move(openingTemporary, original.FilePath, true); }
                    openingPublished = false;
                }
                catch (Exception rollbackFailure) when (rollbackFailure is IOException or UnauthorizedAccessException)
                {
                    BranchGraphEdit.Apply(original, saved);
                    throw new IOException("Die Openingänderung wurde bereits geschrieben, der Fortschritt jedoch nicht. Die Rücknahme scheiterte; bitte das Opening neu laden. " + rollbackFailure.Message, saveFailure);
                }
                throw new IOException("Änderungen wurden nicht übernommen: Der Fortschritt konnte nicht gespeichert werden; die ursprüngliche Openingdatei wurde wiederhergestellt.", saveFailure);
            }
        }
        finally
        {
            if (File.Exists(openingTemporary)) File.Delete(openingTemporary);
            if (File.Exists(progressTemporary)) File.Delete(progressTemporary);
        }
        if (!openingPublished) return;
        // Install prepared objects only after both atomic file replacements succeeded.
        InstallProgress(originalProgress.Data, merged); BranchGraphEdit.Apply(original, saved); Changed = false;
        try { WriteState(new(baselineSignature, "committed", null, [])); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { CheckpointWarning = ex.Message; }
    }

    private ProgressData PrepareMergedProgress(OpeningDefinition saved)
    {
        var working = CopyProgress(Progress.Data);
        var merged = CopyProgress(originalProgress.Data);
        // Carry across the latest live progress, not the possibly older checkpoint snapshot.
        foreach (var (oldKey, newKey) in BranchGraphEdit.ProgressMapping(original, saved))
        {
            if (transfers.GetValueOrDefault(oldKey) == newKey && !working.Moves.ContainsKey(newKey))
            { merged.Moves.Remove(oldKey); continue; }
            if (oldKey != newKey && originalProgress.Data.Moves.TryGetValue(oldKey, out var record)
                && (!working.Moves.TryGetValue(newKey, out var current) || record.CountedStreak > current.CountedStreak))
                working.Moves[newKey] = CopyRecord(record);
        }
        foreach (string key in baselineProgress.Moves.Keys.Where(k => !working.Moves.ContainsKey(k))) merged.Moves.Remove(key);
        foreach (var (key, value) in working.Moves)
            if (!baselineProgress.Moves.TryGetValue(key, out var old) || JsonSerializer.Serialize(value, OpeningStore.Json) != JsonSerializer.Serialize(old, OpeningStore.Json)) merged.Moves[key] = value;
        foreach (string key in baselineProgress.BranchPriority.Keys.Where(k => !working.BranchPriority.ContainsKey(k))) merged.BranchPriority.Remove(key);
        foreach (var (key, value) in working.BranchPriority)
            if (!baselineProgress.BranchPriority.TryGetValue(key, out var old) || old != value) merged.BranchPriority[key] = value;
        if (working.GroupSimilarReplies != baselineProgress.GroupSimilarReplies) merged.GroupSimilarReplies = working.GroupSimilarReplies;
        return merged;
    }
    public void TrackProgressTransfer(string before, string after) { if (before != after) transfers[before] = after; }

    private bool ProgressChanged() => JsonSerializer.Serialize(Progress.Data, OpeningStore.Json) != JsonSerializer.Serialize(baselineProgress, OpeningStore.Json);
    private void WriteState(Checkpoint state)
    {
        File.WriteAllText(stateFile + ".tmp", JsonSerializer.Serialize(state, OpeningStore.Json)); File.Move(stateFile + ".tmp", stateFile, true);
    }
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    private static ProgressRecord CopyRecord(ProgressRecord record) => JsonSerializer.Deserialize<ProgressRecord>(JsonSerializer.Serialize(record, OpeningStore.Json), OpeningStore.Json)!;
    private static ProgressData CopyProgress(ProgressData data) => JsonSerializer.Deserialize<ProgressData>(JsonSerializer.Serialize(data, OpeningStore.Json), OpeningStore.Json)!;
    private static void AssignProgress(ProgressData target, ProgressData data)
    { InstallProgress(target, CopyProgress(data)); }
    private static void InstallProgress(ProgressData target, ProgressData data)
    { target.Moves = data.Moves; target.BranchPriority = data.BranchPriority; target.GroupSimilarReplies = data.GroupSimilarReplies; target.LearningTurns = data.LearningTurns; }

    public static OpeningDefinition Clone(OpeningDefinition source)
    {
        var nodes = new Dictionary<OpeningNode, OpeningNode>();
        OpeningNode Node(OpeningNode node)
        {
            if (nodes.TryGetValue(node, out var existing)) return existing;
            var copy = new OpeningNode { Id = node.Id, Fen = node.Fen, Comment = node.Comment, IsSubbranch = node.IsSubbranch, SubbranchColor = node.SubbranchColor };
            nodes[node] = copy;
            foreach (var edge in node.Moves) copy.Moves.Add(new OpeningMove { Uci = edge.Uci, Next = Node(edge.Next), NextId = edge.NextId, Name = edge.Name, Explanation = edge.Explanation, HumanUnfriendly = edge.HumanUnfriendly });
            return copy;
        }
        var result = new OpeningDefinition { SchemaVersion = source.SchemaVersion, Id = source.Id, Name = source.Name, Perspective = source.Perspective,
            Description = source.Description, Source = source.Source, StartFen = source.StartFen, FilePath = source.FilePath, RootNodeId = source.RootNodeId, Tree = Node(source.Tree) };
        if (source.SchemaVersion == 2) result.Nodes = nodes.Values.ToDictionary(n => n.Id!);
        return result;
    }
    private static string Signature(OpeningDefinition opening) => Hash(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { opening.Id, opening.Name, opening.Perspective, opening.StartFen, opening.Description, opening.Source }) + "\n" + string.Join('\n', GraphOperations.Positions(opening).Select(w =>
        string.Join('|', w.Path) + ":" + OpeningStore.PositionKey(w.Position) + ":" + w.Node.IsSubbranch + ":" + w.Node.SubbranchColor + ":" + string.Join(',', w.Node.Moves.Select(m => m.Uci + "/" + m.HumanUnfriendly))))));
}
