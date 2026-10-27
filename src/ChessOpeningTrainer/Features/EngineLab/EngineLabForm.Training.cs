using System.Text.Json;
namespace ChessOpeningTrainer;
public sealed partial class EngineLabForm
{
    private readonly TableLayoutPanel boards=new(){Dock=DockStyle.Fill};
    private readonly List<(BoardControl Board,Label Name)> displayed=[];
    private readonly Label runInfo=new(){Dock=DockStyle.Top,Height=48,Padding=new(16,5,16,0),ForeColor=UiPalette.Gainsboro},roundInfo=new(){Dock=DockStyle.Top,Height=70,Padding=new(16,4,16,4),ForeColor=UiPalette.Gainsboro};
    private readonly GeneratorProgressBar runProgress=new(){Dock=DockStyle.Top,Height=23,Maximum=1000},roundProgress=new(){Dock=DockStyle.Top,Height=23,Maximum=1000};
    private readonly Button stop=new(){Text="Training anhalten",Dock=DockStyle.Bottom,Height=42};
    private readonly Queue<double> roundDurations=[];
    private double roundStarted;private int roundNumber,roundTotal,roundFirst=1,runNumber=1,runTotal=1,taskNumber,taskTotal=1;
    private double taskFraction;private string currentTask="",bestSummary="";private readonly List<string> outputLines=[];
    private void BuildTraining()
    {
        trainingView.Controls.Add(boards);trainingView.Controls.Add(trainingChart);trainingView.Controls.Add(roundProgress);trainingView.Controls.Add(roundInfo);trainingView.Controls.Add(runProgress);trainingView.Controls.Add(runInfo);trainingView.Controls.Add(stop);stop.Click+=(_,_)=>RequestStop();
    }
    private void PrepareTraining()
    {
        foreach(Control child in boards.Controls.Cast<Control>().ToArray())child.Dispose();boards.Controls.Clear();boards.ColumnStyles.Clear();boards.RowStyles.Clear();displayed.Clear();
        int count=plan["display_boards"]?.GetValue<int>()??1,columns=(int)Math.Ceiling(Math.Sqrt(count));boards.ColumnCount=Math.Max(1,columns);boards.RowCount=Math.Max(1,(int)Math.Ceiling(count/(double)Math.Max(1,columns)));
        for(int i=0;i<boards.ColumnCount;i++)boards.ColumnStyles.Add(new(SizeType.Percent,100f/boards.ColumnCount));for(int i=0;i<boards.RowCount;i++)boards.RowStyles.Add(new(SizeType.Percent,100f/boards.RowCount));
        for(int i=0;i<count;i++){var host=new Panel{Dock=DockStyle.Fill,Padding=new(8)};var board=new BoardControl{Dock=DockStyle.Fill,InputEnabled=false,Position=Position.FromFen(Position.InitialFen)};var name=new Label{Dock=DockStyle.Top,Height=25,TextAlign=ContentAlignment.MiddleCenter,ForeColor=UiPalette.Gainsboro};host.Controls.Add(board);host.Controls.Add(name);boards.Controls.Add(host,i%columns,i/columns);displayed.Add((board,name));}
        trainingChart.Points.Clear();trainingChart.Invalidate();runNumber=1;runTotal=plan["runs"]?.GetValue<int>()??1;roundNumber=0;roundTotal=plan["rounds"]?.GetValue<int>()??100;taskFraction=0;taskNumber=0;taskTotal=1;roundDurations.Clear();outputLines.Clear();bestSummary="";stop.Enabled=true;roundStarted=0;elapsed.Restart();
    }
    private void DrainMessages()
    {
        string[] batch;lock(messageLock){batch=messages.ToArray();messages.Clear();}
        foreach(string line in batch)
        {
            outputLines.Add(line);if(outputLines.Count>1000)outputLines.RemoveAt(0);
            try{using var document=JsonDocument.Parse(line);ApplyMessage(document.RootElement);}catch(Exception ex) when(ex is JsonException or KeyNotFoundException or InvalidDataException){currentTask=line;}
        }
        if(state==LabState.Training)
        {
            double completed=Math.Max(0,roundNumber-roundFirst)+(taskNumber-1+taskFraction)/Math.Max(1,taskTotal);int rounds=Math.Max(1,roundTotal-roundFirst+1);completed=Math.Clamp(completed,0,rounds);
            double remaining=roundDurations.Count==0?double.NaN:roundDurations.Average()*(rounds-completed+(runTotal-runNumber)*rounds);
            runInfo.Text=L.T($"Durchgang {runNumber}/{runTotal}")+" · "+L.T("Vergangen")+": "+elapsed.Elapsed.ToString(@"hh\:mm\:ss")+" · "+L.T("Restzeit")+": "+(double.IsFinite(remaining)?TimeSpan.FromSeconds(Math.Max(0,remaining)).ToString(@"hh\:mm\:ss"):"…");
            roundInfo.Text=L.T($"Runde {roundNumber}/{roundTotal}")+" · "+L.T(currentTask)+"\n"+bestSummary;
            runProgress.Value=(int)Math.Clamp(1000*((runNumber-1)+completed/rounds)/Math.Max(1,runTotal),0,1000);
            roundProgress.Value=(int)Math.Clamp(1000*(taskNumber-1+taskFraction)/Math.Max(1,taskTotal),0,1000);
        }
    }
    private void ApplyMessage(JsonElement value)
    {
        string kind=value.GetProperty("event").GetString()??"";
        if(kind=="run_start"){runNumber=value.GetProperty("run").GetInt32();runTotal=value.GetProperty("runs").GetInt32();if(value.TryGetProperty("directory",out var directory)){activeDirectory=directory.GetString();File.WriteAllText(Path.Combine(storage,"latest-run.txt"),activeDirectory);}roundNumber=0;roundFirst=1;LoadExistingScores();}
        if(kind=="round_start"){if(roundNumber==0)roundFirst=value.GetProperty("round").GetInt32();roundNumber=value.GetProperty("round").GetInt32();roundTotal=value.GetProperty("total").GetInt32();roundStarted=elapsed.Elapsed.TotalSeconds;taskNumber=0;taskFraction=0;}
        if(kind=="task_start"){currentTask=value.GetProperty("task").GetString()??"";taskFraction=0;if(value.TryGetProperty("task_index",out var i))taskNumber=i.GetInt32();if(value.TryGetProperty("tasks",out var n))taskTotal=n.GetInt32();}
        if(kind=="round_complete")
        {
            if(roundStarted>0){roundDurations.Enqueue(elapsed.Elapsed.TotalSeconds-roundStarted);if(roundDurations.Count>10)roundDurations.Dequeue();}
            if(value.TryGetProperty("best_score",out var pointScore)&&pointScore.TryGetDouble(out var best)){trainingChart.Points.Add(new(runNumber,value.GetProperty("round").GetInt32(),best,value.TryGetProperty("mean_score",out var mean)&&mean.TryGetDouble(out var average)?average:null));trainingChart.Invalidate();}
            string id=value.TryGetProperty("best_engine",out var identity)?identity.ToString():"1";
            bestSummary=L.T("Bestes Modell der letzten Runde")+$": #{id} · "+L.T("Punkte")+": "+(value.TryGetProperty("best_score",out var score)?score.ToString():"—");taskFraction=1;
        }
        if(value.TryGetProperty("completed",out var done)&&value.TryGetProperty("total",out var total)){taskFraction=total.GetDouble()>0?done.GetDouble()/total.GetDouble():0;if(value.TryGetProperty("stage",out var stage))currentTask=stage.GetString()+$" · {done}/{total}";}
        if(value.TryGetProperty("boards",out var positions))
        {
            int i=0;foreach(var item in positions.EnumerateArray()){if(i>=displayed.Count)break;var view=displayed[i++];view.Board.Position=Position.FromFen(item.GetProperty("fen").GetString()!);view.Name.Text=$"{EngineName(item.GetProperty("white"))} – {EngineName(item.GetProperty("black"))}";view.Board.Invalidate();}
        }
        else if(value.TryGetProperty("fen",out var fen)&&fen.ValueKind==JsonValueKind.String&&displayed.Count>0){displayed[0].Board.Position=Position.FromFen(fen.GetString()!);displayed[0].Board.Invalidate();}
        if(kind is "error" or "stopped")currentTask=value.GetProperty("message").GetString()??kind;
        if(kind=="complete")currentTask=L.T("Experiment abgeschlossen. Bericht im Experimentordner.");
    }
    private void LoadExistingScores()
    {
        if(activeDirectory is null)return;string file=Path.Combine(activeDirectory,"history.json");if(!File.Exists(file))return;
        try { using var history=JsonDocument.Parse(File.ReadAllText(file));foreach(var point in history.RootElement.EnumerateArray())
            if(point.TryGetProperty("best_score",out var score)&&point.TryGetProperty("round",out var round))trainingChart.Points.Add(new(runNumber,round.GetInt32(),score.GetDouble(),point.TryGetProperty("mean_score",out var mean)?mean.GetDouble():null));trainingChart.Invalidate();
        }catch(Exception ex) when(ex is JsonException or IOException){}
    }
    private static string EngineName(JsonElement id)=>id.ValueKind==JsonValueKind.Null?"Stockfish":"Engine #"+id.ToString().PadLeft(5,'0');
}
