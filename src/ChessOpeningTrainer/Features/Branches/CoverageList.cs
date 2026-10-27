namespace ChessOpeningTrainer;

/// <summary>Virtual rows and one painted scrollbar; no native ListBox scrollbar underneath.</summary>
internal sealed class CoverageList : Control
{
    public IReadOnlyList<CoverageGap> Rows { get; set; } = [];
    public event Action<CoverageGap>? Chosen;
    private const int RowHeight = 36;
    private int offset, hovered = -1, selected = -1;
    private bool dragging; private float grab;
    private int Extent => Math.Max(0, Rows.Count * RowHeight - Height);
    private RectangleF Thumb { get { float h = Math.Min(Height, Math.Max(28, Height * (float)Height / Math.Max(1, Rows.Count * RowHeight))); return new(Width - 9, Extent == 0 ? 0 : offset * (Height - h) / Extent, 6, h); } }
    public CoverageList() { DoubleBuffered = true; SetStyle(ControlStyles.ResizeRedraw | ControlStyles.Selectable, true); BackColor = UiPalette.FromArgb(31,35,39); ForeColor = UiPalette.Gainsboro; }
    private void ScrollTo(int value) { offset = Math.Clamp(value,0,Extent); hovered = Index(PointToClient(Cursor.Position)); Invalidate(); Update(); }
    private int Index(Point p) { int i=(p.Y+offset)/RowHeight; return ClientRectangle.Contains(p) && p.X < Width-14 && i<Rows.Count ? i : -1; }
    protected override void OnMouseWheel(MouseEventArgs e) { base.OnMouseWheel(e); if(e is HandledMouseEventArgs h)h.Handled=true; ScrollTo(offset-(int)(e.Delta/120d)*RowHeight*3); }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e); if(e.Button!=MouseButtons.Left)return; Focus();
        if(e.X>=Width-14 && Extent>0){grab=Thumb.Contains(e.Location)?e.Y-Thumb.Y:Thumb.Height/2;dragging=Capture=true;OnMouseMove(e);return;}
        selected=Index(e.Location); if(selected>=0)Chosen?.Invoke(Rows[selected]); Invalidate();
    }
    protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); if(dragging){ScrollTo((int)((e.Y-grab)*Extent/Math.Max(1,Height-Thumb.Height)));return;} int i=Index(e.Location);if(i!=hovered){hovered=i;Invalidate();} }
    protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);hovered=-1;Invalidate();}
    protected override void OnMouseUp(MouseEventArgs e){dragging=Capture=false;base.OnMouseUp(e);}
    protected override void OnMouseCaptureChanged(EventArgs e){if(!Capture)dragging=false;base.OnMouseCaptureChanged(e);}
    protected override void OnResize(EventArgs e){base.OnResize(e);offset=Math.Clamp(offset,0,Extent);}
    protected override bool IsInputKey(Keys keyData)=>keyData is Keys.Up or Keys.Down || base.IsInputKey(keyData);
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if(Rows.Count>0 && e.KeyCode is Keys.Up or Keys.Down){selected=Math.Clamp(selected+(e.KeyCode==Keys.Down?1:-1),0,Rows.Count-1);ScrollTo(selected*RowHeight<offset?selected*RowHeight:selected*RowHeight+RowHeight>offset+Height?selected*RowHeight+RowHeight-Height:offset);e.Handled=true;}
        if(e.KeyCode==Keys.Enter && selected>=0){Chosen?.Invoke(Rows[selected]);e.SuppressKeyPress=true;}base.OnKeyDown(e);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); var g=e.Graphics;
        using var hover=new SolidBrush(UiPalette.FromArgb(54,61,67));using var choice=new SolidBrush(UiPalette.FromArgb(69,77,83));
        for(int i=offset/RowHeight;i<Rows.Count && i*RowHeight-offset<Height;i++)
        {
            var rect=new Rectangle(0,i*RowHeight-offset,Math.Max(0,Width-14),RowHeight);if(i==selected||i==hovered)g.FillRectangle(i==selected?choice:hover,rect);
            var row=Rows[i];string text=$"+{row.Gain:0.00} · {row.Games:N0} "+L.T("Partien")+" · "+row.Sequence;
            TextRenderer.DrawText(g,text,Font,new Rectangle(8,rect.Y,Math.Max(0,rect.Width-16),rect.Height),ForeColor,TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
        }
        if(Extent>0){using var track=new SolidBrush(UiPalette.FromArgb(32,38,44));using var thumb=new SolidBrush(UiPalette.FromArgb(94,111,127));g.FillRectangle(track,Width-10,0,9,Height);g.FillRectangle(thumb,Thumb);}
    }
}
