using System.Diagnostics;
using System.Text.Json;
namespace ChessOpeningTrainer;

internal sealed class NeuralEvaluationSession : IDisposable
{
    private readonly SemaphoreSlim gate=new(1,1);
    private Process? process;private string? identity;private Task<string>? errors;
    private static object Encode(Position p)=>new {board=new string(p.Squares.Select(c=>c=='\0'?'.':c).ToArray()),white=p.WhiteToMove,castling=p.Castling};
    public async Task<EngineResult> Analyze(SavedEngine engine,Position p,int count,CancellationToken token)
    {
        await gate.WaitAsync(token);
        try
        {
            string key=engine.Weights+"|"+File.GetLastWriteTimeUtc(engine.Weights).Ticks;
            if(process is null || process.HasExited || identity!=key)
            {
                Stop();var start=new ProcessStartInfo(engine.Python){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true};
                foreach(string a in new[]{Path.Combine(AppContext.BaseDirectory,"Addons","EngineLab","inference.py"),"--weights",engine.Weights,"--model",engine.Model,"--device",engine.Device})start.ArgumentList.Add(a);
                process=Process.Start(start)??throw new IOException("Could not start neural engine.");identity=key;errors=process.StandardError.ReadToEndAsync();
            }
            var running=process;
            var legal=MoveGenerator.GenerateLegal(p);
            var children=legal.Select(move=>{var after=p.Clone();after.Apply(move);return new{move=move.Uci,index=TrainingGame.ActionIndex(move),state=Encode(after)};}).ToArray();
            await running.StandardInput.WriteLineAsync(JsonSerializer.Serialize(new{state=Encode(p),children}).AsMemory(),token);await running.StandardInput.FlushAsync(token);
            string? reply=await running.StandardOutput.ReadLineAsync(token);
            if(reply is null)throw new IOException(errors is not null?await errors:"Neural engine stopped.");
            using var result=JsonDocument.Parse(reply);var root=result.RootElement;
            if(!root.GetProperty("ok").GetBoolean())throw new IOException(root.GetProperty("error").GetString());
            var lines=root.GetProperty("lines").EnumerateArray().Take(count).Select((line,i)=>new EngineLine{Rank=i+1,Depth=0,BestMove=line.GetProperty("move").GetString(),ScoreCentipawns=line.GetProperty("cp").GetInt32(),Pv=[line.GetProperty("move").GetString()!],ContinuationChecked=true,ContinuationNote=engine.Model=="neat"?"NEAT policy · material estimate":"Neural policy · no tree search"}).ToList();
            return new(){BestMove=lines.FirstOrDefault()?.BestMove,ScoreCentipawns=root.GetProperty("cp").GetInt32(),Lines=lines};
        }
        catch(OperationCanceledException){Stop();throw;}
        catch(Exception ex) when(ex is IOException or InvalidOperationException or JsonException or System.ComponentModel.Win32Exception){Stop();return new(){Error=ex.Message};}
        finally{gate.Release();}
    }
    private void Stop(){if(process is null)return;try{if(!process.HasExited)process.Kill(true);}catch(InvalidOperationException){}process.Dispose();process=null;identity=null;}
    public void Dispose()=>Stop();
}
