using System.Drawing.Drawing2D;
namespace ChessOpeningTrainer;

public readonly record struct LabScorePoint(int Run,int Round,double Best,double? Mean);
/// <summary>Run-separated round scores; points remain visible and are never silently interpolated across runs.</summary>
public sealed class LabScoreChart : Control
{
    public List<LabScorePoint> Points { get; }=[];
    private readonly ToolTip tip=new();
    private readonly List<(RectangleF Area,string Text)> hits=[];
    public LabScoreChart(){DoubleBuffered=true;BackColor=UiPalette.FromArgb(43,49,55);ForeColor=UiPalette.Gainsboro;MinimumSize=new(250,150);}
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);hits.Clear();var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
        var area=new RectangleF(65,35,Math.Max(1,Width-88),Math.Max(1,Height-72));
        TextRenderer.DrawText(g,L.T("Punkte über die Runden"),Font,new Point(12,8),ForeColor);
        using var axis=new Pen(ForeColor,1);g.DrawLine(axis,area.Left,area.Bottom,area.Right,area.Bottom);g.DrawLine(axis,area.Left,area.Top,area.Left,area.Bottom);
        var valid=Points.Where(p=>double.IsFinite(p.Best)).ToArray();if(valid.Length==0){TextRenderer.DrawText(g,L.T("Nach der ersten Runde erscheinen Datenpunkte."),Font,Rectangle.Round(area),ForeColor,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.WordBreak);return;}
        var values=valid.SelectMany(p=>p.Mean is {} m&&double.IsFinite(m)?new[]{p.Best,m}:new[]{p.Best}).ToArray();double low=values.Min(),high=values.Max();if(high-low<.000001){low-=1;high+=1;}double padding=(high-low)*.08;low-=padding;high+=padding;
        int minRound=valid.Min(p=>p.Round),maxRound=Math.Max(minRound+1,valid.Max(p=>p.Round));
        PointF Map(int round,double score)=>new(area.Left+(round-minRound)/(float)(maxRound-minRound)*area.Width,area.Bottom-(float)((score-low)/(high-low))*area.Height);
        for(int i=0;i<=4;i++){double v=low+(high-low)*i/4;float y=Map(minRound,v).Y;TextRenderer.DrawText(g,v.ToString("0.##"),Font,new Rectangle(0,(int)y-8,59,20),ForeColor,TextFormatFlags.Right);using var line=new Pen(Color.FromArgb(35,ForeColor));g.DrawLine(line,area.Left,y,area.Right,y);}
        Color[] colors=[Color.FromArgb(87,187,241),Color.FromArgb(235,180,75),Color.FromArgb(150,215,140),Color.FromArgb(202,144,230)];
        foreach(var group in valid.GroupBy(p=>p.Run))
        {
            var points=group.OrderBy(p=>p.Round).ToArray();var color=colors[(group.Key-1)%colors.Length];using var pen=new Pen(color,2);using var brush=new SolidBrush(color);using var meanPen=new Pen(Color.FromArgb(150,color),1.5f){DashStyle=DashStyle.Dash};
            if(points.Length>1)g.DrawLines(pen,points.Select(p=>Map(p.Round,p.Best)).ToArray());
            var means=points.Where(p=>p.Mean is {} m&&double.IsFinite(m)).ToArray();if(means.Length>1)g.DrawLines(meanPen,means.Select(p=>Map(p.Round,p.Mean!.Value)).ToArray());
            foreach(var p in points){var at=Map(p.Round,p.Best);g.FillEllipse(brush,at.X-3,at.Y-3,6,6);hits.Add((new(at.X-7,at.Y-7,14,14),L.T("Durchgang")+$" {p.Run} · "+L.T("Runde")+$" {p.Round}: {p.Best:0.####}"+(p.Mean is {} mean?" · "+L.T("Durchschnitt")+$": {mean:0.####}":"")));}
        }
        TextRenderer.DrawText(g,minRound.ToString(),Font,new Point((int)area.Left,Height-30),ForeColor);TextRenderer.DrawText(g,maxRound.ToString(),Font,new Point((int)area.Right-25,Height-30),ForeColor);
        TextRenderer.DrawText(g,L.T("Durchgezogen: bester Score · gestrichelt: Durchschnitt"),Font,new Rectangle(80,Height-28,Width-150,25),ForeColor,TextFormatFlags.HorizontalCenter|TextFormatFlags.EndEllipsis);
    }
    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);string text=hits.FirstOrDefault(h=>h.Area.Contains(e.Location)).Text??"";if(tip.GetToolTip(this)!=text)tip.SetToolTip(this,text);}
    protected override void OnResize(EventArgs e){base.OnResize(e);Invalidate();}
    protected override void Dispose(bool disposing){if(disposing)tip.Dispose();base.Dispose(disposing);}
}
