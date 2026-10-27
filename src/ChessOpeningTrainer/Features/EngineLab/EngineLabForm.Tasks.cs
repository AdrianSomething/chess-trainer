using System.Text.Json.Nodes;
namespace ChessOpeningTrainer;
public sealed partial class EngineLabForm
{
    private void RenderTaskCards()
    {
        taskCards.SuspendLayout();foreach(Control control in taskCards.Controls.Cast<Control>().ToArray()){taskCards.Controls.Remove(control);control.Dispose();}
        var schedules=plan["schedules"] as JsonObject??new();if(plan["schedules"] is not JsonObject)plan["schedules"]=schedules;
        AddSection("Allgemein");AddCard("general",null,null,mode.SelectedIndex==0?LabTaskCatalog.Fields("general"):mode.SelectedIndex==3?LabTaskCatalog.Fields("neat"):[LabTaskCatalog.Fields("general")[0]],p=>p.Key=="rounds"?plan:schedules);
        AddSection(mode.SelectedIndex is 0 or 3?"Fitness-Aufgaben":"Lernaufgaben");
        var tasks=plan["tasks"]!.AsArray();foreach(var task in tasks.OfType<JsonObject>().ToArray())AddTaskCard(task,tasks);
        AddTaskChoice(tasks,mode.SelectedIndex is 0 or 3?["tournament","stockfish","mate","teacher_tactics"]:mode.SelectedIndex==2?["selfplay"]:["distillation"]);
        if(mode.SelectedIndex==0){AddSection("Selektion und Reproduktion");AddCard("selection",null,null,LabTaskCatalog.Fields("selection"),_=>schedules);}
        AddSection("Ergebnisanalyse");var results=plan["results"]!.AsArray();foreach(var task in results.OfType<JsonObject>().ToArray())AddTaskCard(task,results);AddTaskChoice(results,["benchmark","speed","mate","teacher_tactics"]);
        taskCards.ResumeLayout(true);
    }
    private void AddSection(string text)=>taskCards.Controls.Add(new Label{Text=text,Font=new(Font.FontFamily,14,FontStyle.Bold),ForeColor=UiPalette.Gainsboro,Height=36,Width=600,Margin=new(3,16,3,3)});
    private static string TaskName(string kind)=>kind switch{"tournament"=>"Engine-Turnier","stockfish"=>"Gegen Stockfish","mate"=>"Mattaufgaben","teacher_tactics"=>"Engine-Zugaufgaben","distillation"=>"Stockfish-Imitation","selfplay"=>"Actor-Critic-Selbstspiel","benchmark"=>"Stockfish-Tiefenvergleich","speed"=>"Rechengeschwindigkeit","general"=>"Allgemeine Parameter",_=>"Selektion"};
    private void AddTaskChoice(JsonArray array,string[] kinds)
    {
        var row=new FlowLayoutPanel{Width=600,Height=38};var selection=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=260};selection.Items.AddRange(kinds.Select(k=>(object)L.T(TaskName(k))).ToArray());selection.SelectedIndex=0;
        var add=new Button{Text="Aufgabe hinzufügen",Width=175,Height=32};add.Click+=(_,_)=>{array.Add(LabConfiguration.Task(kinds[selection.SelectedIndex],TaskName(kinds[selection.SelectedIndex])));RenderTaskCards();};
        L.SetToolTip(tips,add,"Fügt eine eigenständige Aufgabe hinzu. Mehrere Aufgaben desselben Typs dürfen unterschiedliche Parameter und Gewichte verwenden.");row.Controls.AddRange([selection,add]);taskCards.Controls.Add(row);
    }
    private void AddTaskCard(JsonObject task,JsonArray collection)
    {
        string kind=task["kind"]!.GetValue<string>();var parameters=task["parameters"] as JsonObject;
        if(parameters is null){parameters=new();task["parameters"]=parameters;}
        AddCard(kind,task,collection,LabTaskCatalog.Fields(kind),_=>parameters);
    }
    private void AddCard(string kind,JsonObject? task,JsonArray? collection,IReadOnlyList<LabParameter> fields,Func<LabParameter,JsonObject> target)
    {
        var card=new FlowLayoutPanel{Width=Math.Max(450,taskCards.ClientSize.Width-42),AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new(12),Margin=new(3,5,3,8),BackColor=UiPalette.FromArgb(43,49,55)};
        var header=new FlowLayoutPanel{Width=card.Width-28,Height=40,WrapContents=false};
        var toggle=new Button{Text=(kind=="general"?"▾ ":"▸ ")+L.T(task?["name"]?.GetValue<string>()??TaskName(kind)),FlatStyle=FlatStyle.Flat,Height=36,Width=300,TextAlign=ContentAlignment.MiddleLeft,Font=new(Font.FontFamily,12,FontStyle.Bold)};toggle.FlatAppearance.BorderSize=0;
        var body=new FlowLayoutPanel{Width=card.Width-28,AutoSize=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,Visible=kind=="general"};
        toggle.Click+=(_,_)=>{body.Visible=!body.Visible;toggle.Text=(body.Visible?"▾ ":"▸ ")+L.T(task?["name"]?.GetValue<string>()??TaskName(kind));};header.Controls.Add(toggle);
        if(task is not null)
        {
            var active=new CheckBox{Text="Aktiv",AutoSize=true,Checked=task["enabled"]?.GetValue<bool>()??true};active.CheckedChanged+=(_,_)=>task["enabled"]=active.Checked;header.Controls.Add(active);
            var remove=new Button{Text="×",Width=34,Height=32};remove.Click+=(_,_)=>{collection!.Remove(task);RenderTaskCards();};header.Controls.Add(remove);
        }
        foreach(var field in fields){var row=ParameterRow(field,target(field));row.Width=body.Width-8;body.Controls.Add(row);}
        card.Controls.AddRange([header,body]);taskCards.Controls.Add(card);
        card.SizeChanged+=(_,_)=>{header.Width=body.Width=Math.Max(400,card.Width-28);toggle.Width=Math.Max(230,header.Width-160);foreach(Control row in body.Controls)row.Width=body.Width-8;};
    }
    private Control ParameterRow(LabParameter field,JsonObject target)
    {
        var row=new Panel{Width=590,Height=44};var label=new Label{Text=field.Title,Left=0,Top=4,Width=310,Height=35,TextAlign=ContentAlignment.MiddleLeft,ForeColor=UiPalette.Gainsboro};row.Controls.Add(label);L.SetToolTip(tips,label,LabTaskCatalog.Description(field));
        Control input;
        if(field.Choices is {} options)
        {
            var values=options.Split(';').Select(x=>x.Split('|')).ToArray();var combo=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=220};combo.Items.AddRange(values.Select(x=>(object)L.T(x[1])).ToArray());combo.SelectedIndex=Math.Max(0,Array.FindIndex(values,x=>x[0]==target[field.Key]?.GetValue<string>()));if(target[field.Key] is null)target[field.Key]=values[combo.SelectedIndex][0];combo.SelectionChangeCommitted+=(_,_)=>target[field.Key]=values[combo.SelectedIndex][0];input=combo;
        }
        else if(field.Domain=="bool")
        {var check=new CheckBox{Checked=target[field.Key]?.GetValue<bool>()??false,Width=40};check.CheckedChanged+=(_,_)=>target[field.Key]=check.Checked;input=check;}
        else if(field.Domain is "file" or "text")
        {
            var text=new TextBox{Text=target[field.Key]?.GetValue<string>()??"",Width=180};text.TextChanged+=(_,_)=>target[field.Key]=text.Text;input=text;
            if(field.Domain=="file"){var browse=new Button{Text="…",Width=32,Height=28,Anchor=AnchorStyles.Right|AnchorStyles.Top,Left=row.Width-34};browse.Click+=(_,_)=>{using var dialog=new OpenFileDialog{Filter="Position files|*.json;*.jsonl;*.cotidx|All files|*.*"};if(dialog.ShowDialog(this)==DialogResult.OK)text.Text=dialog.FileName;};row.Controls.Add(browse);}
        }
        else
        {
            if(target[field.Key] is null)target[field.Key]=field.Domain=="constant"?JsonValue.Create((int)field.Default):field.Domain=="scalar"?JsonValue.Create(field.Default):LabConfiguration.Curve(field.Default);
            var number=new ScrollNumber{Minimum=(decimal)field.Min,Maximum=(decimal)field.Max,DecimalPlaces=field.Decimals,Increment=field.Decimals==0?1:field.Decimals>3?.0001m:.01m,Value=Math.Clamp((decimal)LabConfiguration.Value(target[field.Key],field.Default),(decimal)field.Min,(decimal)field.Max),Width=120};
            number.ValueChanged+=(_,_)=>{target[field.Key]=field.Domain=="constant"?JsonValue.Create((int)number.Value):field.Domain=="scalar"?JsonValue.Create((double)number.Value):LabConfiguration.Curve((double)number.Value);};input=number;
            if(field.Domain is not ("constant" or "scalar"))
            {
                var graph=new Button{Text="↗",Width=54,Height=30,Left=row.Width-56,Anchor=AnchorStyles.Right|AnchorStyles.Top};
                void PaintCurve(){bool plotted=target[field.Key] is JsonObject c && c["points"] is JsonArray pts && pts.Count>0;number.Enabled=!plotted;graph.Text=plotted?"▥":"↗";}
                graph.Click+=(_,_)=>new LabCurveEditor(field,target[field.Key],plan["rounds"]?.GetValue<int>()??100,curve=>{target[field.Key]=curve;if(!row.IsDisposed)PaintCurve();}).Show();row.Controls.Add(graph);L.SetToolTip(tips,graph,LabTaskCatalog.Description(field));PaintCurve();
            }
        }
        input.Left=330;input.Top=5;input.Anchor=AnchorStyles.Top|AnchorStyles.Right;row.Controls.Add(input);L.SetToolTip(tips,input,LabTaskCatalog.Description(field));
        row.SizeChanged+=(_,_)=>{label.Width=Math.Max(170,row.Width-260);input.Left=Math.Max(180,row.Width-235);};return row;
    }
}
