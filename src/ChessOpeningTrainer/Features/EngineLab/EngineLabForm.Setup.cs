using System.Text.Json.Nodes;
namespace ChessOpeningTrainer;
public sealed partial class EngineLabForm
{
    private readonly List<string?> templateFiles=[];
    private static readonly string[] ModelIds=["compact","balanced-8gb","backprop-8gb","neat"];
    private static readonly string[] ModeIds=["evolution","distillation","selfplay","neat"];
    private void BuildSetup()
    {
        setupView.Controls.Add(taskCards);setupView.Controls.Add(setupOptions);
        mode.Items.AddRange(["Evolutionäres Populationstraining","Backpropagation / Lehrer-Imitation","Self-Play Reinforcement Learning","NEAT (CPU)"]);model.Items.AddRange(["GPU · Kompakt (~0,1 GB)","GPU · Ausgewogen (8-GB-Karte)","GPU · Backpropagation (8-GB-Karte)","NEAT · variable Topologie (CPU)"]);device.Items.AddRange(["GPU (CUDA)","CPU"]);
        void Add(string label,Control input,string help){setupOptions.Controls.Add(new Label{Text=label,Width=285,Height=24,Margin=new(0,12,0,2)});setupOptions.Controls.Add(input);L.SetToolTip(tips,input,help);}
        Add("Trainingsverfahren",mode,"Evolution bewertet und mutiert eine Population. Backpropagation imitiert Stockfish. Selbstspiel lernt aus Partien; NEAT entwickelt die Netztopologie auf der CPU. Alle verwenden dieselben Schachregeln.");
        Add("Vorlage",templates,"Lädt die vollständige Aufgaben- und Kurvenkonfiguration. Eigene Vorlagen werden lokal gespeichert und sind als JSON editierbar.");
        Add("Modell",model,"Kompakt: schneller Funktionstest. Ausgewogen: größeres Modell für Population oder Gradientenlernen. Backpropagation: etwa 1,54 GiB Gewichte und bis etwa 7 GiB Trainingsbedarf; keine 6-GiB-Gewichte. Populationen liegen auf der Festplatte.");
        Add("Rechengerät",device,"CUDA benötigt eine passende PyTorch-Installation. CPU ist für kleine Tests verfügbar; große Modelle können sehr langsam sein.");
        setupOptions.Controls.Add(requirements);
        Add("Angezeigte Bretter",boardCount,"0 deaktiviert die Brettdarstellung. Sonst werden bis zu so viele laufende Partien angezeigt; die beste bekannte Engine wird bevorzugt.");
        Add("Unabhängige Durchgänge",runCount,"Anzahl unabhängiger Trainingsläufe mit jeweils eigenem Zufallsstart, Checkpoint und Ergebnis.");
        Add("Python",python,"Python-Programm mit installiertem PyTorch. Das normale Schachtraining benötigt Python nicht.");
        setupOptions.Controls.Add(templateName);var save=new Button{Text="Vorlage speichern",Width=280,Height=34};save.Click+=(_,_)=>SaveTemplate();setupOptions.Controls.Add(save);
        start.Click+=async (_,_)=>await StartExperiment(continuing);setupOptions.Controls.Add(start);
        var resume=new Button{Text="Gespeichertes Training fortsetzen",Width=280,Height=36};resume.Click+=(_,_)=>LoadLastExperiment();setupOptions.Controls.Add(resume);
        mode.SelectionChangeCommitted+=(_,_)=>{plan=templates.SelectedIndex==1?LabConfiguration.Minimal(ModeIds[Math.Max(0,mode.SelectedIndex)]):LabConfiguration.Default(ModeIds[Math.Max(0,mode.SelectedIndex)]);continuing=false;LoadPlan();};
        model.SelectionChangeCommitted+=(_,_)=>{if(model.SelectedIndex==3&&mode.SelectedIndex!=3){model.SelectedIndex=0;return;}plan["model"]=ModelIds[model.SelectedIndex];UpdateRequirements();};
        device.SelectionChangeCommitted+=(_,_)=>plan["device"]=device.SelectedIndex==0?"cuda":"cpu";
        boardCount.ValueChanged+=(_,_)=>{if(!loading){plan["display_boards"]=(int)boardCount.Value;plan["visualize"]=boardCount.Value>0;}};
        runCount.ValueChanged+=(_,_)=>{if(!loading)plan["runs"]=(int)runCount.Value;};
        templates.SelectionChangeCommitted+=(_,_)=>{int index=templates.SelectedIndex;if(index<0)return;try{plan=templateFiles[index] is {} file?JsonNode.Parse(File.ReadAllText(file))!.AsObject():index==1?LabConfiguration.Minimal(ModeIds[Math.Max(0,mode.SelectedIndex)]):LabConfiguration.Default(ModeIds[Math.Max(0,mode.SelectedIndex)]);continuing=false;LoadPlan();}catch(Exception ex) when(ex is IOException or System.Text.Json.JsonException){L.Message(this,ex.Message,"Vorlage");}};
        RefreshTemplates();LoadPlan();
        taskCards.SizeChanged+=(_,_)=>{foreach(Control card in taskCards.Controls)card.Width=Math.Max(450,taskCards.ClientSize.Width-42);};
    }
    private void UpdateRequirements()
    {
        requirements.Text=L.T(model.SelectedIndex switch{0=>"Kompakt: ungefähr 11 MiB Gewichte je Modell. Für schnelle Tests geeignet.",1=>"Ausgewogen: ungefähr 224 MiB Gewichte je Modell. 100 Modelle benötigen mindestens etwa 22 GiB Festplattenspeicher.",3=>"NEAT: kleine CPU-Netze mit veränderlicher Topologie. Keine CUDA-Abhängigkeit für die Netzauswertung.",_=>"Backpropagation: ungefähr 1,54 GiB Gewichte, etwa 6,7–7 GiB GPU-Speicher mit Gradienten und AdamW. Empfohlen: 8-GB-GPU. Eine Population von 100 Modellen benötigt über 154 GiB Festplattenspeicher."});
    }
    private void LoadPlan()
    {
        loading=true;
        try{mode.SelectedIndex=Math.Max(0,Array.IndexOf(ModeIds,plan["mode"]?.GetValue<string>()));model.SelectedIndex=Math.Max(0,Array.IndexOf(ModelIds,plan["model"]?.GetValue<string>()));device.SelectedIndex=plan["device"]?.GetValue<string>()=="cpu"?1:0;boardCount.Value=Math.Clamp(plan["display_boards"]?.GetValue<int>()??1,0,16);runCount.Value=Math.Clamp(plan["runs"]?.GetValue<int>()??1,1,1000);}
        finally{loading=false;}
        model.Enabled=!continuing&&mode.SelectedIndex!=3;device.Enabled=mode.SelectedIndex!=3;mode.Enabled=!continuing;UpdateRequirements();RenderTaskCards();start.Text=L.T(continuing?"Training fortsetzen":"Training starten");
    }
    private void RefreshTemplates()
    {
        templates.Items.Clear();templateFiles.Clear();templates.Items.Add(L.T("Standardvorlage"));templateFiles.Add(null);templates.Items.Add(L.T("Minimale Testvorlage (3 Runden)"));templateFiles.Add(null);
        string folder=Path.Combine(storage,"templates");Directory.CreateDirectory(folder);
        foreach(string file in Directory.EnumerateFiles(folder,"*.json").Order()){templates.Items.Add(Path.GetFileNameWithoutExtension(file));templateFiles.Add(file);}templates.SelectedIndex=0;
    }
    private void SaveTemplate()
    {
        string name=templateName.Text.Trim();if(name.Length==0||name.IndexOfAny(Path.GetInvalidFileNameChars())>=0){L.Message(this,"Bitte einen gültigen Vorlagennamen eingeben.","Vorlage");return;}
        File.WriteAllText(Path.Combine(storage,"templates",name+".json"),plan.ToJsonString(new(){WriteIndented=true}));RefreshTemplates();
    }
    private void LoadLastExperiment()
    {
        string? file=activeDirectory is null?null:Path.Combine(activeDirectory,"config.json");if(file is null||!File.Exists(file)){L.Message(this,"Noch kein gespeichertes Experiment vorhanden.","Engine-Labor");return;}
        plan=JsonNode.Parse(File.ReadAllText(file))!.AsObject();continuing=true;plan["runs"]=1;LoadPlan();SwitchState(LabState.Setup);
    }
}
