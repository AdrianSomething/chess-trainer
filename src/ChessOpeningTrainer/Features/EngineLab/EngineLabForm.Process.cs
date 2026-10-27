using System.Diagnostics;
using System.Text.Json.Nodes;
namespace ChessOpeningTrainer;
public sealed partial class EngineLabForm
{
    private async Task StartExperiment(bool resume)
    {
        if(process is not null)return;
        string script=Path.Combine(AppContext.BaseDirectory,"Addons","EngineLab","train.py");if(!File.Exists(script)){L.Message(this,"Das optionale Engine-Labor-Add-on fehlt.","Engine-Labor");return;}
        if(resume&&(activeDirectory is null||!File.Exists(ResultEngine().Weights))){L.Message(this,"Noch kein gespeichertes Experiment vorhanden.","Engine-Labor");return;}
        if(!resume)runDirectory=Path.Combine(storage,"experiment-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N")[..6]);else runDirectory=activeDirectory;
        Directory.CreateDirectory(runDirectory!);activeDirectory=runDirectory;
        string marker=Path.Combine(runDirectory!,"stop.request");if(File.Exists(marker))File.Delete(marker);
        // Resolve dataset sources once for reproducible training; later active-selection changes do not alter a run.
        var launchPlan=plan.DeepClone().AsObject();
        try { ResolveDatasetSources(launchPlan); } catch (Exception ex) when (ex is IOException or InvalidDataException) { L.Message(this,ex.Message,"Engine-Labor");return; }
        string config=Path.Combine(runDirectory!,"launch-config.json");File.WriteAllText(config,launchPlan.ToJsonString(new(){WriteIndented=true}));File.WriteAllText(Path.Combine(storage,"latest-run.txt"),runDirectory);
        var command=new ProcessStartInfo(python.Text.Trim()){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=Path.GetDirectoryName(script)!};
        foreach(string argument in new[]{script,"--host",Path.Combine(AppContext.BaseDirectory,"ChessOpeningTrainer.exe"),"--output",runDirectory!,"--config",config})command.ArgumentList.Add(argument);if(resume)command.ArgumentList.Add("--resume");
        var running=new Process{StartInfo=command};
        try
        {
            running.Start();process=running;PrepareTraining();SwitchState(LabState.Training);
            async Task ReadLines(StreamReader reader){while(await reader.ReadLineAsync() is {} line){lock(messageLock){if(messages.Count>=1000)messages.Dequeue();messages.Enqueue(line);}}}
            await Task.WhenAll(ReadLines(running.StandardOutput),ReadLines(running.StandardError),running.WaitForExitAsync());
            if(!IsDisposed){DrainMessages();elapsed.Stop();ShowResults(running.ExitCode==0);}
        }
        catch(Exception ex) when(ex is IOException or InvalidOperationException or System.ComponentModel.Win32Exception){if(!IsDisposed){outputLines.Add(ex.Message);currentTask=ex.Message;ShowResults(false);}}
        finally{process=null;running.Dispose();}
    }
    private static void ResolveDatasetSources(JsonObject config)
    {
        foreach(string category in new[]{"tasks","results"})if(config[category] is JsonArray tasks)
            foreach(var task in tasks.OfType<JsonObject>())if((task["enabled"]?.GetValue<bool>()??true)&&task["parameters"] is JsonObject parameters&&parameters["source"]?.GetValue<string>()=="dataset")
            {
                var store=new DatasetStore(Preferences.DataDirectory);
                if(!store.HasIndex)throw new InvalidDataException(L.T("Zuerst einen Datensatzbaum erstellen."));parameters["file"]=store.IndexPath(store.Manifest.IndexFile!);
            }
    }
    private void RequestStop()
    {
        if(runDirectory is null||process is null)return;
        File.WriteAllText(Path.Combine(runDirectory,"stop.request"),"stop");stop.Enabled=false;currentTask=L.T("Anhalten nach der aktuellen Berechnung …");
    }
}
