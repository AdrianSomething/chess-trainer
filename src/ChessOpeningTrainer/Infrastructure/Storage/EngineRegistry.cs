using System.Text.Json;
namespace ChessOpeningTrainer;

public sealed record SavedEngine(string Id,string Name,string Model,string Python,string Weights,string Device="cuda")
{ public override string ToString()=>Name; }
public static class EngineRegistry
{
    public static string DirectoryPath=>Path.Combine(Preferences.DataDirectory,"Engines");
    public static IReadOnlyList<SavedEngine> List()
    {
        if(!Directory.Exists(DirectoryPath))return [];
        var result=new List<SavedEngine>();
        foreach(string path in Directory.EnumerateFiles(DirectoryPath,"engine.json",SearchOption.AllDirectories))
            try { var item=JsonSerializer.Deserialize<SavedEngine>(File.ReadAllText(path));if(item is not null && Guid.TryParseExact(item.Id,"N",out _) && IsModel(item.Model)) {string weights=Path.Combine(DirectoryPath,item.Id,item.Model=="neat"?"best-neat.json":"best.pt");if(File.Exists(weights))result.Add(item with {Weights=weights});} }catch(Exception ex) when(ex is IOException or JsonException){}
        return result.OrderBy(e=>e.Name,StringComparer.CurrentCultureIgnoreCase).ToArray();
    }
    public static bool IsModel(string model)=>model is "compact" or "balanced-8gb" or "backprop-8gb" or "neat";
    public static SavedEngine? Selected(StockfishEngine engine)=>engine.NeuralOverride??List().FirstOrDefault(e=>e.Id==Preferences.Current.EvaluationEngine);
    public static async Task<SavedEngine> Save(string weights,string model,string python,string name,CancellationToken token,string device="cuda")
    {
        if(!IsModel(model)||!File.Exists(weights))throw new InvalidDataException("Unknown model or missing weights.");
        string id=Guid.NewGuid().ToString("N"),directory=Path.Combine(DirectoryPath,id);Directory.CreateDirectory(directory);
        string target=Path.Combine(directory,model=="neat"?"best-neat.json":"best.pt");
        try
        {
            await using(var input=new FileStream(weights,FileMode.Open,FileAccess.Read,FileShare.Read,1024*1024,true))
            await using(var output=new FileStream(target,FileMode.CreateNew,FileAccess.Write,FileShare.None,1024*1024,true)) await input.CopyToAsync(output,token);
            var saved=new SavedEngine(id,name,model,python,target,device);
            await File.WriteAllTextAsync(Path.Combine(directory,"engine.json"),JsonSerializer.Serialize(saved),token);return saved;
        }
        catch {File.Delete(Path.Combine(directory,"engine.json"));if(File.Exists(target))File.Delete(target);if(!Directory.EnumerateFileSystemEntries(directory).Any())Directory.Delete(directory);throw;}
    }
    public static void Delete(SavedEngine engine)
    {
        if(!Guid.TryParseExact(engine.Id,"N",out _))throw new InvalidDataException("Invalid engine ID.");
        string folder=Path.Combine(DirectoryPath,engine.Id);
        foreach(string file in new[]{"best.pt","best-neat.json","engine.json"})File.Delete(Path.Combine(folder,file));
        if(Directory.Exists(folder)&&!Directory.EnumerateFileSystemEntries(folder).Any())Directory.Delete(folder);
        if(Preferences.Current.EvaluationEngine==engine.Id){Preferences.Current.EvaluationEngine="stockfish";Preferences.Save();}
    }
}
