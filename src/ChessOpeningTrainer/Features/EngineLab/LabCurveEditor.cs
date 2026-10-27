using System.Text.Json.Nodes;
using System.Drawing.Drawing2D;
namespace ChessOpeningTrainer;

internal sealed class LabCurveEditor : LocalizedForm
{
    private sealed class Chart : Control
    {
        public readonly List<PointF> Points=[];
        public double XMax=100,YMin,YMax=100; public int Selected=-1;
        public event Action? Changed; private bool drag;
        private RectangleF Plot=>new(45,15,Math.Max(1,Width-65),Math.Max(1,Height-50));
        public Chart(){DoubleBuffered=true;SetStyle(ControlStyles.ResizeRedraw,true);}
        private PointF Pixel(PointF p)=>new(Plot.Left+(float)(p.X/XMax)*Plot.Width,Plot.Bottom-(float)((p.Y-YMin)/(YMax-YMin))*Plot.Height);
        private PointF Value(Point p)=>new((float)Math.Clamp((p.X-Plot.Left)/Plot.Width*XMax,0,XMax),(float)Math.Clamp(YMin+(Plot.Bottom-p.Y)/Plot.Height*(YMax-YMin),YMin,YMax));
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
            using var pen=new Pen(UiPalette.Gainsboro,1);g.DrawRectangle(pen,Plot.X,Plot.Y,Plot.Width,Plot.Height);
            using var line=new Pen(Color.FromArgb(76,155,226),2);
            if(Points.Count>1)g.DrawLines(line,Points.OrderBy(p=>p.X).Select(Pixel).ToArray());
            for(int i=0;i<Points.Count;i++){var p=Pixel(Points[i]);using var brush=new SolidBrush(i==Selected?Color.Orange:Color.FromArgb(76,155,226));g.FillEllipse(brush,p.X-5,p.Y-5,10,10);}
            TextRenderer.DrawText(g,$"0 → {XMax:0.##}",Font,new Point((int)Plot.Left,(int)Plot.Bottom+8),ForeColor);
            TextRenderer.DrawText(g,$"{YMax:0.###}",Font,new Point(0,10),ForeColor);TextRenderer.DrawText(g,$"{YMin:0.###}",Font,new Point(0,(int)Plot.Bottom-15),ForeColor);
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);if(e.Button!=MouseButtons.Left||!Plot.Contains(e.Location))return;
            Selected=Points.FindIndex(p=>Math.Abs(Pixel(p).X-e.X)+Math.Abs(Pixel(p).Y-e.Y)<16);
            if(Selected<0){Points.Add(Value(e.Location));Selected=Points.Count-1;}
            drag=Capture=true;Changed?.Invoke();Invalidate();
        }
        protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(!drag||Selected<0)return;Points[Selected]=Value(e.Location);Changed?.Invoke();Invalidate();}
        protected override void OnMouseCaptureChanged(EventArgs e){if(!Capture)drag=false;base.OnMouseCaptureChanged(e);}
        protected override void OnMouseUp(MouseEventArgs e){drag=Capture=false;base.OnMouseUp(e);}
    }
    public LabCurveEditor(LabParameter field,JsonNode? initial,int rounds,Action<JsonObject> save)
    {
        Text=L.T(field.Title);Size=new(780,500);MinimumSize=new(650,420);BackColor=UiPalette.FromArgb(31,35,39);ForeColor=UiPalette.Gainsboro;
        var source=initial as JsonObject;var chart=new Chart{Dock=DockStyle.Fill,ForeColor=ForeColor,XMax=field.Domain is "rank" or "distribution"?1:field.Domain=="move"?1000:Math.Max(1,rounds),YMin=field.Min,YMax=field.Max};
        if(source?["points"] is JsonArray points)foreach(var point in points)if(point is JsonArray p)chart.Points.Add(new((float)p[0]!.GetValue<double>(),(float)p[1]!.GetValue<double>()));
        var bottom=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=108,Padding=new(10),WrapContents=true};
        var x=new ScrollNumber{Width=100,Maximum=(decimal)chart.XMax,DecimalPlaces=field.Domain is "rank" or "distribution"?3:1};
        var y=new ScrollNumber{Width=110,Minimum=(decimal)field.Min,Maximum=(decimal)field.Max,DecimalPlaces=field.Decimals,Value=Math.Clamp((decimal)LabConfiguration.Value(initial,field.Default),(decimal)field.Min,(decimal)field.Max)};
        var cycle=new CheckBox{Text="Nach letztem Punkt wiederholen",AutoSize=true,Checked=source?["cycle"]?.GetValue<bool>()??false,Visible=field.Domain=="round"};
        bool editing=false;
        chart.Changed+=()=>{if(chart.Selected<0)return;editing=true;x.Value=Math.Clamp((decimal)chart.Points[chart.Selected].X,x.Minimum,x.Maximum);y.Value=Math.Clamp((decimal)chart.Points[chart.Selected].Y,y.Minimum,y.Maximum);editing=false;};
        void UpdatePoint(){if(editing||chart.Selected<0)return;chart.Points[chart.Selected]=new((float)x.Value,(float)y.Value);chart.Invalidate();}
        x.ValueChanged+=(_,_)=>UpdatePoint();y.ValueChanged+=(_,_)=>UpdatePoint();
        var add=new Button{Text="Punkt hinzufügen",AutoSize=true};add.Click+=(_,_)=>{chart.Points.Add(new((float)x.Value,(float)y.Value));chart.Selected=chart.Points.Count-1;chart.Invalidate();};
        var remove=new Button{Text="Punkt entfernen",AutoSize=true};remove.Click+=(_,_)=>{if(chart.Selected>=0){chart.Points.RemoveAt(chart.Selected);chart.Selected=-1;chart.Invalidate();}};
        var accept=new Button{Text="Übernehmen",Width=150,Height=34};accept.Click+=(_,_)=>{var ordered=chart.Points.OrderBy(p=>p.X).ToArray();if(ordered.Zip(ordered.Skip(1)).Any(pair=>pair.First.X==pair.Second.X)){L.Message(this,"Jeder X-Wert darf nur einmal vorkommen.","Kurve");return;}var curve=LabConfiguration.Curve((double)y.Value,ordered.Select(p=>((double)p.X,(double)p.Y)).ToArray());curve["cycle"]=cycle.Checked;save(curve);Close();};
        bottom.Controls.AddRange([new Label{Text="X",AutoSize=true},x,new Label{Text="Y",AutoSize=true},y,add,remove,cycle,accept]);
        var info=new Label{Text=L.T(LabTaskCatalog.Description(field)),Dock=DockStyle.Top,Height=65,Padding=new(12),ForeColor=ForeColor};Controls.Add(chart);Controls.Add(bottom);Controls.Add(info);
    }
}
