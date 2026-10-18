using System.Globalization;
using System.Runtime.InteropServices;

namespace ChessOpeningTrainer;

/// <summary>
/// Keeps the ListBox selection/items contract, but paints the viewport in one
/// buffered pass. Native scrolling must not copy partially repainted hover rows.
/// </summary>
public sealed class GameList : ListBox
{
    private int hover = -1, previousSelection = -1, scrollOffset;
    private Point pointer;
    private bool pointerInside, sliding;
    private float grabOffset;
    private int Extent => Math.Max(0, Items.Count * ItemHeight - ClientSize.Height);
    private RectangleF Thumb
    {
        get
        {
            float track = Math.Max(1, ClientSize.Height - 4);
            float length = Math.Min(track, Math.Max(28, track * track / Math.Max(1, Items.Count * ItemHeight)));
            return new(ClientSize.Width - 8, 2 + (Extent == 0 ? 0 : scrollOffset * (track - length) / Extent), 6, length);
        }
    }

    public GameList()
    {
        DrawMode = DrawMode.OwnerDrawFixed;
        ItemHeight = 34;
        IntegralHeight = false;
        BorderStyle = BorderStyle.None;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    protected override CreateParams CreateParams
    {
        get { var value = base.CreateParams; value.Style &= ~0x200000; return value; }
    }

    // ListBox can re-add its scrollbar after item changes. Keep the native one
    // hidden; the managed thumb below is the same style as history/PV controls.
    [DllImport("user32.dll")]
    private static extern bool ShowScrollBar(IntPtr handle, int bar, bool show);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong(IntPtr handle, int index);
    [StructLayout(LayoutKind.Sequential)]
    private struct MouseTracking { public int Size, Flags; public IntPtr Window; public int HoverTime; }
    [DllImport("user32.dll")]
    private static extern bool TrackMouseEvent(ref MouseTracking tracking);
    protected override void WndProc(ref Message message)
    {
        if (message.Msg is 0x201 or 0x202 or 0x200 or 0x20A)
        {
            // Dispatch managed input without the native list changing a second,
            // differently scrolled selection underneath the custom viewport.
            message.Result = IntPtr.Zero;
            int x = unchecked((short)(long)message.LParam), y = unchecked((short)((long)message.LParam >> 16));
            if (message.Msg == 0x201) OnMouseDown(new(MouseButtons.Left, 1, x, y, 0));
            else if (message.Msg == 0x202) OnMouseUp(new(MouseButtons.Left, 1, x, y, 0));
            else if (message.Msg == 0x200) OnMouseMove(new(Capture ? MouseButtons.Left : MouseButtons.None, 0, x, y, 0));
            else OnMouseWheel(new(MouseButtons.None, 0, x, y, unchecked((short)((long)message.WParam >> 16))));
            return;
        }
        if (message.Msg == 0x2A3) { OnMouseLeave(EventArgs.Empty); message.Result = IntPtr.Zero; return; }
        if (message.Msg == 0x14) { message.Result = (IntPtr)1; return; }
        base.WndProc(ref message);
        if (IsHandleCreated && (GetWindowLong(Handle, -16) & 0x200000) != 0)
            ShowScrollBar(Handle, 1, false);
    }

    private int Hit(Point point) => point.X >= 0 && point.X < ClientSize.Width - (Extent > 0 ? 12 : 0) &&
        point.Y >= 0 && point.Y < ClientSize.Height && (point.Y + scrollOffset) / ItemHeight < Items.Count
        ? (point.Y + scrollOffset) / ItemHeight : -1;
    private Rectangle Row(int index) => new(0, index * ItemHeight - scrollOffset, ClientSize.Width, ItemHeight);
    private void Hover(int next)
    {
        if (hover == next) return;
        if (hover >= 0) Invalidate(Row(hover));
        hover = next;
        if (hover >= 0) Invalidate(Row(hover));
    }
    private void ScrollTo(int value)
    {
        int next = Math.Clamp(value, 0, Extent);
        if (next == scrollOffset) return;
        scrollOffset = next;
        Hover(pointerInside ? Hit(pointer) : -1);
        Invalidate();
    }

    protected override void OnSelectedIndexChanged(EventArgs e)
    {
        if (previousSelection >= 0) Invalidate(Row(previousSelection));
        previousSelection = SelectedIndex;
        if (SelectedIndex >= 0)
        {
            var row = Row(SelectedIndex);
            if (row.Top < 0) ScrollTo(SelectedIndex * ItemHeight);
            else if (row.Bottom > ClientSize.Height) ScrollTo((SelectedIndex + 1) * ItemHeight - ClientSize.Height);
            Invalidate(Row(SelectedIndex));
        }
        base.OnSelectedIndexChanged(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        using var background = new SolidBrush(BackColor);
        e.Graphics.FillRectangle(background, e.ClipRectangle);
        scrollOffset = Math.Clamp(scrollOffset, 0, Extent);
        int first = Math.Max(0, (scrollOffset + e.ClipRectangle.Top) / ItemHeight);
        int last = Math.Min(Items.Count - 1, (scrollOffset + e.ClipRectangle.Bottom) / ItemHeight);
        for (int i = first; i <= last; i++)
            if (Items[i] is PersonalGame game) DrawGame(e.Graphics, Row(i), game, i);
        if (Extent > 0)
        {
            using var track = new SolidBrush(UiPalette.FromArgb(32, 38, 44));
            using var thumb = new SolidBrush(UiPalette.FromArgb(94, 111, 127));
            e.Graphics.FillRectangle(track, ClientSize.Width - 9, 0, 8, ClientSize.Height);
            e.Graphics.FillRectangle(thumb, Thumb);
        }
    }

    private void DrawGame(Graphics graphics, Rectangle rect, PersonalGame game, int index)
    {
        Color color = index == SelectedIndex ? UiPalette.FromArgb(70, 78, 85)
            : index == hover ? UiPalette.FromArgb(58, 66, 74) : BackColor;
        using var fill = new SolidBrush(color);
        graphics.FillRectangle(fill, rect);
        int available = rect.Width - (Extent > 0 ? 12 : 0);
        int dateWidth = 64, playerWidth = Math.Max(40, (available - dateWidth) / 2);
        string date = game.Tags.GetValueOrDefault("Date", "");
        if (DateTime.TryParseExact(date, "yyyy.MM.dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)) date = parsed.ToString("dd.MM.yy");
        var flags = TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix;
        TextRenderer.DrawText(graphics, date, Font, new Rectangle(rect.X + 3, rect.Y, dateWidth - 4, rect.Height), ForeColor, flags);
        string result = game.Tags.GetValueOrDefault("Result", "*");
        for (int i = 0; i < 2; i++)
        {
            bool winner = i == 0 ? result == "1-0" : result == "0-1";
            using var font = new Font(Font, winner ? FontStyle.Bold : FontStyle.Regular);
            TextRenderer.DrawText(graphics, i == 0 ? game.White : game.Black, font,
                new Rectangle(rect.X + dateWidth + i * playerWidth + 4, rect.Y, playerWidth - 7, rect.Height),
                winner || result == "*" ? ForeColor : UiPalette.FromArgb(155, 163, 171), flags);
        }
        using var line = new Pen(UiPalette.FromArgb(83, 91, 99));
        graphics.DrawLine(line, rect.X + dateWidth, rect.Top, rect.X + dateWidth, rect.Bottom);
        graphics.DrawLine(line, rect.Left, rect.Bottom - 1, rect.Right, rect.Bottom - 1);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        pointer = e.Location;
        pointerInside = ClientRectangle.Contains(pointer);
        if (pointerInside && IsHandleCreated)
        {
            var tracking = new MouseTracking { Size = Marshal.SizeOf<MouseTracking>(), Flags = 2, Window = Handle };
            TrackMouseEvent(ref tracking);
        }
        if (sliding)
        {
            var thumb = Thumb;
            ScrollTo((int)((e.Y - grabOffset - 2) * Extent / Math.Max(1, ClientSize.Height - 4 - thumb.Height)));
        }
        else Hover(Hit(pointer));
        base.OnMouseMove(e);
    }
    protected override void OnMouseLeave(EventArgs e) { pointerInside = false; Hover(-1); base.OnMouseLeave(e); }
    protected override void OnMouseWheel(MouseEventArgs e) => ScrollTo(scrollOffset - Math.Sign(e.Delta) * ItemHeight * 3);
    protected override void OnMouseDown(MouseEventArgs e)
    {
        Focus();
        if (e.Button != MouseButtons.Left) return;
        if (Extent > 0 && e.X >= ClientSize.Width - 13)
        {
            var thumb = Thumb;
            grabOffset = thumb.Contains(e.Location) ? e.Y - thumb.Y : thumb.Height / 2;
            sliding = true;
            Capture = true;
            OnMouseMove(e);
        }
        else if (Hit(e.Location) is int hit && hit >= 0) SelectedIndex = hit;
    }
    protected override void OnMouseUp(MouseEventArgs e) { sliding = false; Capture = false; base.OnMouseUp(e); }
    protected override void OnResize(EventArgs e) { base.OnResize(e); ScrollTo(scrollOffset); }
}
