using System.Diagnostics;
using System.Text.Json;
namespace ChessOpeningTrainer;
public sealed partial class EngineLabForm
{
    private readonly LabScoreChart trainingChart=new(){Dock=DockStyle.Bottom,Height=220},resultChart=new(){Dock=DockStyle.Fill};
    private readonly Label resultSummary=new(){Dock=DockStyle.Top,Height=125,Padding=new(14),ForeColor=UiPalette.Gainsboro};
    private readonly TextBox resultLog=new(){Dock=DockStyle.Bottom,Height=150,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical};
    private readonly FlowLayoutPanel resultActions=new(){Dock=DockStyle.Right,Width=300,Padding=new(15),FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true};
    private readonly TextBox engineName=new(){Width=260,Text="My chess engine"};
    private Button saveEngine=null!,defaultEngine=null!,playEngine=null!;
    private SavedEngine? savedEngine;
    private void BuildResults()
    {
        var left=new Panel{Dock=DockStyle.Fill};left.Controls.Add(resultChart);left.Controls.Add(resultLog);left.Controls.Add(resultSummary);resultView.Controls.Add(left);resultView.Controls.Add(resultActions);
        resultActions.Controls.Add(new Label{Text="Name der Engine",Width=260,Height=25});resultActions.Controls.Add(engineName);
        Button Action(string text,EventHandler click){var b=new Button{Text=text,Width=260,Height=42,Margin=new(3,8,3,3)};b.Click+=click;resultActions.Controls.Add(b);return b;}
        saveEngine=Action("Engine speichern",async (_,_)=>await SaveResult(false));defaultEngine=Action("Als Standardengine verwenden",async (_,_)=>await SaveResult(true));
        playEngine=Action("Freies Spiel gegen diese Engine",(_,_)=>
        {
            if(activeDirectory is null)return;var temporary=ResultEngine();var game=new FreeGameForm(new StockfishEngine(AppContext.BaseDirectory).WithNeural(temporary),[]);game.PlayFrom(Position.FromFen(Position.InitialFen),true);game.Show();
        });
        Action("Weitertrainieren / Einstellungen",(_,_)=>LoadLastExperiment());
        Action("Experimentdateien öffnen",(_,_)=>{if(runDirectory is not null)Process.Start(new ProcessStartInfo(runDirectory){UseShellExecute=true});});
        Action("Ergebnis verwerfen",(_,_)=>
        {
            if(L.Message(this,"Ergebnis schließen? Experimentdateien bleiben zur Nachvollziehbarkeit erhalten.","Engine-Labor",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
            savedEngine=null;continuing=false;SwitchState(LabState.Setup);
        });
        L.SetToolTip(tips,saveEngine,"Kopiert das fertige Modell in die lokale Engine-Bibliothek. Die Experimentdateien und Stockfish bleiben erhalten.");
        L.SetToolTip(tips,playEngine,"Öffnet ein unabhängiges Spiel mit diesem Modell. Das Ergebnisfenster bleibt geöffnet; die Standardengine wird nicht geändert.");
    }
    private SavedEngine ResultEngine()=>new("experiment",engineName.Text.Trim(),plan["model"]?.GetValue<string>()??"compact",python.Text.Trim(),Path.Combine(activeDirectory!,plan["mode"]?.GetValue<string>()=="neat"?"best-neat.json":"best.pt"),plan["device"]?.GetValue<string>()??"cuda");
    private async Task SaveResult(bool makeDefault)
    {
        if(activeDirectory is null)return;saveEngine.Enabled=defaultEngine.Enabled=false;
        try{if(savedEngine is null){var item=ResultEngine();savedEngine=await EngineRegistry.Save(item.Weights,item.Model,item.Python,string.IsNullOrWhiteSpace(item.Name)?"Chess engine":item.Name,saveLifetime.Token,item.Device);}if(makeDefault){Preferences.Current.EvaluationEngine=savedEngine.Id;Preferences.Save();}if(!IsDisposed)resultSummary.Text+= "\n"+L.T("Engine gespeichert.");}
        catch(OperationCanceledException){}catch(Exception ex) when(ex is IOException or InvalidDataException){if(!IsDisposed)L.Message(this,ex.Message,"Engine-Labor");}
        finally{if(!IsDisposed)saveEngine.Enabled=defaultEngine.Enabled=true;}
    }
    private void ShowResults(bool success)
    {
        savedEngine=null;resultChart.Points.Clear();resultChart.Points.AddRange(trainingChart.Points);resultChart.Invalidate();
        resultSummary.Text=L.T(success?"Training beendet":"Training mit Fehler beendet")+"\n"+bestSummary+"\n"+L.T("Vergangen")+": "+elapsed.Elapsed.ToString(@"hh\:mm\:ss")+"\n"+L.T("Experimentelle Scores sind keine Elo-Wertung.");
        if(activeDirectory is not null){string report=Path.Combine(activeDirectory,"report.json");if(File.Exists(report))
        {
            string text=File.ReadAllText(report);outputLines.Add(text);
            try { using var data=JsonDocument.Parse(text);var root=data.RootElement;
                if(root.TryGetProperty("results",out var results))foreach(var item in results.EnumerateArray())
                {
                    string kind=item.GetProperty("kind").GetString()??"";
                    if(kind=="speed")resultSummary.Text+="\n"+L.T("Mittlere Entscheidungszeit")+$": {item.GetProperty("mean_ms").GetDouble():0.###} ms";
                    else if(kind=="benchmark"&&item.TryGetProperty("rows",out var rows))foreach(var row in rows.EnumerateArray())resultSummary.Text+="\nStockfish "+L.T("Tiefe")+$" {row.GetProperty("depth")}: "+L.T("Siege / Remis / Niederlagen")+$" {row.GetProperty("wins")} / {row.GetProperty("draws")} / {row.GetProperty("losses")} · "+L.T("Zuglimit")+$" {row.GetProperty("capped")}";
                    else if(item.TryGetProperty("scores",out var scores))resultSummary.Text+="\n"+L.T(TaskName(kind))+": "+string.Join(", ",scores.EnumerateArray().Select(v=>v.ToString()));
                }
            } catch(JsonException){}
        }}
        resultSummary.Height=Math.Min(240,Math.Max(125,TextRenderer.MeasureText(resultSummary.Text,resultSummary.Font,new Size(Math.Max(300,resultView.Width-340),1000),TextFormatFlags.WordBreak).Height+28));
        resultLog.Text=string.Join(Environment.NewLine,outputLines.TakeLast(150));bool available=activeDirectory is not null&&File.Exists(ResultEngine().Weights);saveEngine.Enabled=defaultEngine.Enabled=playEngine.Enabled=available;SwitchState(LabState.Result);
    }
}
