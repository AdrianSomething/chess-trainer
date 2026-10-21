using System.Drawing.Drawing2D;

namespace ChessOpeningTrainer;

/// <summary>Window-local dimming with a real input hole. Movement, minimization
/// and occlusion follow the host without synchronizing a second native window.</summary>
internal sealed class TutorialOverlay : Control
{
    private readonly Label information = new() { AutoSize = false, ForeColor = Color.White, BackColor = Color.FromArgb(8, 9, 11), Font = new("Segoe UI", 12) };
    private readonly Label counter = new() { ForeColor = Color.White, BackColor = Color.FromArgb(8, 9, 11), TextAlign = ContentAlignment.MiddleLeft };
    private readonly Button next = MakeButton("Next · Space");
    private readonly Button cancel = MakeButton("×");
    private readonly Button previous = MakeButton("‹"), forward = MakeButton("›");
    private Bitmap? background;
    private readonly Dictionary<BoardControl, (string? Fen, int Frame, string? Hint)> boardFrames = [];
    private Rectangle focus, infoBounds;
    private string? selectorState;
    private (Size Host, Rectangle Focus, string Text, bool Advance)? lastView;
    public event Action? Advance;
    public event Action? Abort;
    public event Action? Previous;
    public event Action? Forward;
    internal static Button MakeButton(string text)
    {
        Button button = text is "×" or "‹" or "›" ? new TutorialGlyphButton(text) : new Button();
        button.Text = text; button.FlatStyle = FlatStyle.Flat; button.TabStop = false;
        button.BackColor = Color.FromArgb(36, 39, 43); button.ForeColor = Color.White;
        button.TextAlign = ContentAlignment.MiddleCenter; button.Padding = Padding.Empty;
        return button;
    }
    public TutorialOverlay()
    {
        Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        DoubleBuffered = true; TabStop = false;
        SetStyle(ControlStyles.Selectable, false);
        cancel.Font = new("Segoe UI", 17, FontStyle.Regular);
        previous.Font = forward.Font = new("Segoe UI", 18);
        Controls.AddRange([information, counter, next, cancel, previous, forward]);
        next.Click += (_, _) => Advance?.Invoke(); cancel.Click += (_, _) => Abort?.Invoke();
        previous.Click += (_, _) => Previous?.Invoke(); forward.Click += (_, _) => Forward?.Invoke();
    }
    public void UpdateView(Form host, Control? target, string message, bool canAdvance, bool canGoBack = false)
        => UpdateViewArea(host, target, message, canAdvance, canGoBack, null);
    public void UpdateViewArea(Form host, Control? target, string message, bool canAdvance, bool canGoBack, Func<Control, Rectangle>? area)
    {
        var targetBounds = target is null || !target.Visible ? Rectangle.Empty :
            host.RectangleToClient(target.RectangleToScreen(area?.Invoke(target) ?? target.ClientRectangle));
        var key = (host.ClientSize, targetBounds, message, canAdvance);
        bool numbered = message.Length > 0 && char.IsDigit(message[0]);
        previous.Visible = forward.Visible = numbered; previous.Enabled = canGoBack;
        if (lastView == key) { RefreshBoards(host); return; }
        lastView = key; Bounds = host.ClientRectangle;
        CaptureBackground(host);
        focus = Rectangle.Intersect(ClientRectangle, targetBounds);
        int desiredWidth = Math.Min(520, Math.Max(220, Width - 32));
        int height = Math.Min(host is GameEvaluationForm ? 145 : 210, Height - 24);
        int rightSpace = Width - focus.Right - 24, leftSpace = focus.Left - 24;
        int width = desiredWidth, x, y;
        if (!focus.IsEmpty && Math.Max(rightSpace, leftSpace) >= 240)
        {
            bool right = rightSpace >= leftSpace;
            width = Math.Min(desiredWidth, right ? rightSpace : leftSpace);
            x = right ? focus.Right + 12 : 12;
            y = Math.Clamp(focus.Top + 20, 12, Math.Max(12, Height - height - 12));
        }
        else
        {
            x = Math.Max(12, (Width - width) / 2);
            y = focus.Top >= height + 24 ? focus.Top - height - 12 : Height - height - 12;
        }
        int newline = message.IndexOf('\n');
        string body = numbered && newline >= 0 ? message[(newline + 1)..] : message;
        int textHeight = TextRenderer.MeasureText(body, information.Font, new Size(width - 32, int.MaxValue), TextFormatFlags.WordBreak).Height;
        height = Math.Min(Height - 24, Math.Max(height, textHeight + 94));
        y = Math.Clamp(y, 12, Math.Max(12, Height - height - 12));
        infoBounds = new(x, Math.Max(12, y), width, height);
        counter.SetBounds(x + 16, infoBounds.Y + 8, 58, 26);
        counter.Text = numbered && newline >= 0 ? message[..newline] : "";
        information.Bounds = new(x + 16, infoBounds.Y + 43, width - 32, height - 94);
        information.Text = body;
        cancel.SetBounds(x + width - 36, infoBounds.Y + 6, 28, 28);
        previous.SetBounds(x + 76, infoBounds.Y + 8, 26, 26);
        forward.SetBounds(x + 108, infoBounds.Y + 8, 26, 26);
        next.SetBounds(x + 50, infoBounds.Bottom - 48, width - 100, 34);
        next.Text = L.T("Weiter · Leertaste"); next.Visible = canAdvance;
        var region = new Region(ClientRectangle);
        if (!focus.IsEmpty) region.Exclude(focus);
        region.Union(infoBounds);
        var oldRegion = Region; Region = region; oldRegion?.Dispose();
        boardFrames.Clear(); RefreshBoards(host);
        BringToFront(); Invalidate();
    }
    internal bool IsAdvanceAt(Point screen) => next.Visible && next.RectangleToScreen(next.ClientRectangle).Contains(screen);
    internal int NavigationAt(Point screen) => previous.Visible && previous.Enabled && previous.RectangleToScreen(previous.ClientRectangle).Contains(screen) ? -1 : forward.Visible && forward.RectangleToScreen(forward.ClientRectangle).Contains(screen) ? 1 : 0;
    private void RefreshBoards(Form host)
    {
        if (background is null) return;
        string selectors = string.Join("|", TutorialSession.Descendants(host).Select(c => c switch
        { ComboBox combo => combo.SelectedIndex.ToString(), TabControl tabs => tabs.SelectedIndex.ToString(), _ => "" }).Where(s => s.Length > 0));
        bool changed = selectorState != selectors; selectorState = selectors;
        foreach (var board in TutorialSession.Descendants(host).OfType<BoardControl>().Where(b => b.Visible))
        {
            var frame = (board.Position?.ToFen(), board.TutorialFrame, board.HintMove?.Uci);
            if (boardFrames.TryGetValue(board, out var previousFrame) && previousFrame == frame) continue;
            boardFrames[board] = frame;
            changed = true;
        }
        // The game-library side panel changes with the displayed board too.
        // Refresh its snapshot, not just the board pixels, after navigation.
        if (changed) { CaptureBackground(host); Invalidate(); }
    }
    private void CaptureBackground(Form host)
    {
        background?.Dispose(); background = new Bitmap(Math.Max(1, Width), Math.Max(1, Height));
        using var g = Graphics.FromImage(background);
        g.Clear(host.BackColor);
        // Never capture this layer, or toggle visibility (which would flash).
        foreach (Control c in host.Controls.Cast<Control>().Reverse())
        {
            if (c == this || !c.Visible || c.Width <= 0 || c.Height <= 0) continue;
            using var bitmap = new Bitmap(c.Width, c.Height);
            c.DrawToBitmap(bitmap, new Rectangle(Point.Empty, c.Size));
            g.DrawImageUnscaled(bitmap, c.Location);
        }
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        if (background is not null) e.Graphics.DrawImageUnscaled(background, Point.Empty);
        using var dim = new SolidBrush(Color.FromArgb(172, 24, 27, 30));
        e.Graphics.FillRectangle(dim, ClientRectangle);
        using var card = new SolidBrush(Color.FromArgb(8, 9, 11));
        e.Graphics.FillRectangle(card, infoBounds);
        if (focus.IsEmpty) return;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var line = new Pen(Color.FromArgb(240, 187, 79), 3);
        var outline = focus; outline.Inflate(2, 2); e.Graphics.DrawRectangle(line, outline);
        // Large targets can surround the information card. No connecting arrow
        // can avoid crossing that card in this case (home/graph/statistics).
        if (focus.IntersectsWith(infoBounds)) return;
        // Nearest facing edges, never a fixed left origin through the text.
        Point start, end;
        if (infoBounds.Right <= focus.Left)
        { int y = Math.Clamp(infoBounds.Top + infoBounds.Height / 2, focus.Top, focus.Bottom); start = new(infoBounds.Right, Math.Clamp(y, infoBounds.Top, infoBounds.Bottom)); end = new(focus.Left, y); }
        else if (infoBounds.Left >= focus.Right)
        { int y = Math.Clamp(infoBounds.Top + infoBounds.Height / 2, focus.Top, focus.Bottom); start = new(infoBounds.Left, Math.Clamp(y, infoBounds.Top, infoBounds.Bottom)); end = new(focus.Right, y); }
        else
        {
            int x = Math.Clamp(infoBounds.Left + infoBounds.Width / 2, focus.Left, focus.Right);
            bool above = infoBounds.Top + infoBounds.Height / 2 < focus.Top + focus.Height / 2;
            start = new(Math.Clamp(x, infoBounds.Left, infoBounds.Right), above ? infoBounds.Bottom : infoBounds.Top);
            end = new(x, above ? focus.Top : focus.Bottom);
        }
        using var arrow = new AdjustableArrowCap(5, 6); line.CustomEndCap = arrow;
        e.Graphics.DrawLine(line, start, end);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) background?.Dispose();
        base.Dispose(disposing);
    }
}
