using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
namespace ChessOpeningTrainer;

/// <summary>Three views over one experiment: configuration, live monitoring and saved results.</summary>
public sealed partial class EngineLabForm : LocalizedForm
{
    private enum LabState { Setup,Training,Result }
    private readonly Panel content=new(){Dock=DockStyle.Fill};
    private readonly Panel setupView=new(){Dock=DockStyle.Fill},trainingView=new(){Dock=DockStyle.Fill},resultView=new(){Dock=DockStyle.Fill};
    private readonly Label title=new(){Dock=DockStyle.Top,Height=65,Padding=new(70,14,10,8),Font=new("Segoe UI",19,FontStyle.Bold),Text="Engine-Labor · Einrichtung"};
    private readonly FlowLayoutPanel setupOptions=new(){Dock=DockStyle.Left,Width=340,Padding=new(18),FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true};
    private readonly FlowLayoutPanel taskCards=new(){Dock=DockStyle.Fill,Padding=new(14),FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true};
    private readonly ComboBox mode=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=280},model=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=280},device=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=280},templates=new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=280};
    private readonly TextBox python=new(){Text="python",Width=280},templateName=new(){Width=280,PlaceholderText="Vorlagenname"};
    private readonly ScrollNumber boardCount=new(){Minimum=0,Maximum=16,Value=1,Width=110},runCount=new(){Minimum=1,Maximum=1000,Value=1,Width=110};
    private readonly Label requirements=new(){Width=280,Height=115,ForeColor=UiPalette.Gainsboro};
    private readonly Button start=new(){Text="Training starten",Width=280,Height=42};
    private readonly ToolTip tips=new(){AutoPopDelay=25000};
    private readonly System.Windows.Forms.Timer refresh=new(){Interval=250};
    private readonly object messageLock=new();
    private readonly Queue<string> messages=new();
    private readonly Stopwatch elapsed=new();
    private readonly string storage;
    private JsonObject plan=LabConfiguration.Default();
    private string? runDirectory,activeDirectory;
    private Process? process;
    private LabState state;
    private bool loading,closing,continuing;
    private CancellationTokenSource saveLifetime=new();
    public EngineLabForm(string dataDirectory)
    {
        storage=Path.Combine(dataDirectory,"EngineLab");Directory.CreateDirectory(storage);
        string previous=Path.Combine(storage,"latest-run.txt");if(File.Exists(previous)){string path=File.ReadAllText(previous);if(Directory.Exists(path))runDirectory=activeDirectory=path;}
        Text="Engine-Labor · Esc: Hauptmenü";Size=new(1300,900);MinimumSize=new(1050,700);BackColor=UiPalette.FromArgb(31,35,39);ForeColor=UiPalette.Gainsboro;
        var back=new MenuIconButton("back"){Size=new(42,36),Location=new(16,14)};back.Click+=(_,_)=>Close();
        Controls.Add(content);Controls.Add(title);title.Controls.Add(back);back.BringToFront();content.Controls.AddRange([setupView,trainingView,resultView]);
        BuildSetup();BuildTraining();BuildResults();SwitchState(LabState.Setup);
        refresh.Tick+=(_,_)=>DrainMessages();refresh.Start();
        FormClosing+=async (_,e)=>
        {
            if(process is not null&&!closing)
            {
                if(L.Message(this,"Training anhalten und das Engine-Labor verlassen? Gespeicherte Runden bleiben erhalten.","Engine-Labor",MessageBoxButtons.YesNo)!=DialogResult.Yes){e.Cancel=true;return;}
                e.Cancel=true;closing=true;RequestStop();var running=process;
                try{await running.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(15));}catch(TimeoutException){try{running.Kill(true);}catch(InvalidOperationException){}}
                Close();
            }
        };
        FormClosed+=(_,_)=>{saveLifetime.Cancel();refresh.Dispose();tips.Dispose();};
    }
    private void SwitchState(LabState next)
    {
        state=next;setupView.Visible=next==LabState.Setup;trainingView.Visible=next==LabState.Training;resultView.Visible=next==LabState.Result;
        title.Text=L.T(next switch{LabState.Setup=>"Engine-Labor · Einrichtung",LabState.Training=>"Engine-Labor · Training",_=>"Engine-Labor · Ergebnis"});
        (next==LabState.Setup?setupView:next==LabState.Training?trainingView:resultView).BringToFront();
    }
    protected override bool ProcessCmdKey(ref Message msg,Keys keyData){if(keyData==Keys.Escape){Close();return true;}return base.ProcessCmdKey(ref msg,keyData);}
}
