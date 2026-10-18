using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace ChessOpeningTrainer;
// The public index remains a ply/snapshot index; only the visual rows are paired.
public sealed class MoveHistory : ScrollableControl
{
    public sealed class Entries(MoveHistory owner) : Collection<object>
    {
        protected override void InsertItem(int i, object item)
        {
            base.InsertItem(i, item);
            owner.Reflow();
        }

        protected override void RemoveItem(int i)
        {
            base.RemoveItem(i);
            owner.Reflow();
        }

        protected override void ClearItems()
        {
            base.ClearItems();
            owner.selected = -1;
            owner.Reflow();
        }

        public void AddRange(object[] items)
        {
            foreach (var item in items)
                Add(item);
        }
    }

    public Entries Items { get; }
    public HashSet<int> LessonIndices { get; } = [];

    private readonly Dictionary<int, List<MoveBadge>> badges = [];
    private string? badgeKey;
    public void SetPositions(IEnumerable<Position> source)
    {
        var positions = source.ToList();
        string key = string.Join('|', positions.Select(p => p.ToFen()));
        if (key == badgeKey)
            return;
        badgeKey = key;
        badges.Clear();
        for (int i = 1; i < positions.Count; i++)
            badges[i] = MoveBadges.Between(positions[i - 1], positions[i]);
        Invalidate();
    }

    private readonly List<(int Index, Rectangle Rect, string Text)> cells = [];
    private int selected = -1;
    private int hovered = -1;
    private int scrollOffset, contentHeight;
    private bool sliding;
    private float grabOffset;
    private int Extent => Math.Max(0, contentHeight - ClientSize.Height);
    private RectangleF Thumb
    {
        get
        {
            float track = Math.Max(1, Height - 28);
            float length = Math.Min(track, Math.Max(28, track * track / Math.Max(1, contentHeight - 28)));
            return new(Width - 8, 28 + (Extent == 0 ? 0 : scrollOffset * (track - length) / Extent), 6, length);
        }
    }
    private void ScrollTo(int value) { scrollOffset = Math.Clamp(value, 0, Extent); hovered = -1; Invalidate(); }
    public event EventHandler? SelectedIndexChanged;
    public event Action<int, HistoryNavigationKind>? NavigationRequested;
    public bool IsNavigationInput { get; private set; }
    public int SelectedIndex
    {
        get => selected;
        set
        {
            if (value == selected)
                return;
            selected = value;
            var cell = cells.FirstOrDefault(c => c.Index == selected);
            if (selected >= 0)
            {
                int y = cell.Rect.Y - scrollOffset;
                if (y < 28)
                    ScrollTo(cell.Rect.Y - 28);
                else if (y + 28 > Height)
                    ScrollTo(cell.Rect.Bottom - Height);
            }

            Invalidate();
            SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool IntegralHeight { get; set; }
    public bool HorizontalScrollbar { get; set; }
    public BorderStyle BorderStyle { get; set; }

    public MoveHistory()
    {
        Items = new(this);
        DoubleBuffered = true;
        AutoScroll = false;
        SetStyle(ControlStyles.ResizeRedraw, true);
        BackColor = UiPalette.FromArgb(43, 49, 55);
        ForeColor = UiPalette.Gainsboro;
        Resize += (_, _) => Reflow();
        TabStop = true;
    }

    private void Reflow()
    {
        cells.Clear();
        int row = 0, lastNumber = -1;
        bool occupied = false;
        int width = Math.Max(80, (ClientSize.Width - 24) / 2);
        for (int i = 0; i < Items.Count; i++)
        {
            string text = Items[i].ToString() ?? "";
            var match = Regex.Match(text, @"^(\d+)(\.{3}|…|\.)\s*(.*)$");
            if (!match.Success)
            {
                if (occupied)
                {
                    row++;
                    occupied = false;
                }

                cells.Add((i, new(0, 28 + row++ * 28, width * 2, 28), text));
                lastNumber = -1;
                continue;
            }

            int number = int.Parse(match.Groups[1].Value);
            bool black = match.Groups[2].Value != ".";
            if (number != lastNumber)
            {
                if (occupied)
                    row++;
                lastNumber = number;
                occupied = true;
            }

            cells.Add((i, new(black ? width : 0, 28 + row * 28, width, 28), number + ". " + match.Groups[3].Value));
        }

        contentHeight = 28 + (row + 1) * 28;
        ScrollTo(scrollOffset);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.Clear(BackColor);
        var saved = g.Save();
        using var text = new SolidBrush(ForeColor);
        using var highlight = new SolidBrush(UiPalette.FromArgb(70, 90, 106));
        using var hover = new SolidBrush(ControlPaint.Light(BackColor, .3f));
        using var separator = new Pen(Color.White, 1);
        using var slash = new Pen(Color.IndianRed, 1.8f);
        using var format = new StringFormat
        {
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap
        };
        int width = Math.Max(80, (ClientSize.Width - 24) / 2);
        g.DrawString(L.T("Weiß"), Font, text, 5, 3);
        g.DrawString(L.T("Schwarz"), Font, text, width + 5, 3);
        g.SetClip(new Rectangle(0, 28, Width, Math.Max(0, Height - 28)));
        foreach (var cell in cells)
        {
            var rect = cell.Rect;
            rect.Offset(0, -scrollOffset);
            if (cell.Index == selected)
                g.FillRectangle(highlight, rect);
            else if (cell.Index == hovered)
                g.FillRectangle(hover, rect);
            if (LessonIndices.Contains(cell.Index))
            {
                using var lesson = new Pen(UiPalette.Orange, 2);
                g.DrawRectangle(lesson, Rectangle.Inflate(rect, -2, -2));
            }
            float right = rect.Right - 3;
            if (badges.TryGetValue(cell.Index, out var icons))
                foreach (var icon in icons.AsEnumerable().Reverse())
                {
                    right -= 19;
                    BoardControl.DrawPiece(g, icon.Piece, new(right + 9, rect.Y + 14), 23, true);
                    if (icon.Strike)
                        g.DrawLine(slash, right + 2, rect.Y + 22, right + 17, rect.Y + 5);
                    if (icon.ArrowBefore)
                    {
                        right -= 15;
                        g.DrawString("→", Font, text, right, rect.Y + 4);
                    }
                    else if (icon != icons[0])
                    {
                        right -= 5;
                        g.DrawLine(separator, right + 2, rect.Y + 6, right + 2, rect.Y + 22);
                    }
                }

            g.DrawString(L.T(cell.Text), Font, text, new RectangleF(rect.X + 5, rect.Y + 4, Math.Max(8, right - rect.X - 8), rect.Height - 4), format);
        }
        g.Restore(saved);
        if (Extent > 0)
        {
            using var track = new SolidBrush(UiPalette.FromArgb(32, 38, 44));
            using var thumb = new SolidBrush(UiPalette.FromArgb(94, 111, 127));
            g.FillRectangle(track, Width - 9, 28, 8, Math.Max(0, Height - 28));
            g.FillRectangle(thumb, Thumb);
        }
    }

    protected override void OnScroll(ScrollEventArgs se)
    {
        base.OnScroll(se);
        hovered = -1;
        // Native scrolling copies pixels, including our fixed header. Repaint all of it.
        Invalidate();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        ScrollTo(scrollOffset - Math.Sign(e.Delta) * 84);
        // ScrollableControl's wheel path can scroll native pixels without raising
        // OnScroll. Invalidate after that copy, not just on scrollbar messages.
        hovered = -1;
        Invalidate();
        Update();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (sliding)
        {
            var thumb = Thumb;
            ScrollTo((int)((e.Y - grabOffset - 28) * Extent / Math.Max(1, Height - 28 - thumb.Height)));
            return;
        }
        int next = -1;
        if (e.Y >= 28)
            foreach (var cell in cells)
            {
                var rect = cell.Rect;
                rect.Offset(0, -scrollOffset);
                if (rect.Contains(e.Location)) { next = cell.Index; break; }
            }
        if (next != hovered) { hovered = next; Invalidate(); }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        hovered = -1;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        Focus();
        if (e.Button == MouseButtons.Left && e.X >= Width - 13 && e.Y >= 28 && Extent > 0)
        {
            var thumb = Thumb;
            grabOffset = thumb.Contains(e.Location) ? e.Y - thumb.Y : thumb.Height / 2;
            sliding = true; Capture = true; OnMouseMove(e); return;
        }
        if (e.Button == MouseButtons.Left)
        foreach (var cell in cells)
        {
            var rect = cell.Rect;
            rect.Offset(0, -scrollOffset);
            if (e.Y >= 28 && rect.Contains(e.Location))
            {
                RequestNavigation(cell.Index, HistoryNavigationKind.Animated);
                break;
            }
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && e.Y >= 28 && e.X < Width - 13)
            foreach (var cell in cells)
            {
                var rect = cell.Rect; rect.Offset(0, -scrollOffset);
                if (!rect.Contains(e.Location)) continue;
                RequestNavigation(cell.Index, HistoryNavigationKind.Immediate);
                break;
            }
        base.OnMouseDoubleClick(e);
    }

    private void RequestNavigation(int target, HistoryNavigationKind kind)
    {
        IsNavigationInput = true;
        try { SelectedIndex = target; NavigationRequested?.Invoke(target, kind); }
        finally { IsNavigationInput = false; }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        sliding = false; Capture = false; base.OnMouseUp(e);
    }

    protected override bool IsInputKey(Keys keyData) => keyData is Keys.Up or Keys.Down or Keys.Left or Keys.Right || base.IsInputKey(keyData);
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Up or Keys.Left)
            RequestNavigation(Math.Max(0, selected - 1), HistoryNavigationKind.Reverse);
        if (e.KeyCode is Keys.Down or Keys.Right)
            RequestNavigation(Math.Min(Items.Count - 1, selected + 1), HistoryNavigationKind.Animated);
        if (e.KeyCode is Keys.Up or Keys.Down or Keys.Left or Keys.Right) e.Handled = e.SuppressKeyPress = true;
        base.OnKeyDown(e);
    }
}
