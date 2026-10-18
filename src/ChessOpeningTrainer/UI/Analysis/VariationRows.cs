namespace ChessOpeningTrainer;
// Exactly three horizontally scrollable rows. Each SAN token is a position link.
public sealed class VariationRows : TableLayoutPanel
{
    public event Action<Position>? Navigate;
    public event Action<IReadOnlyList<Position>>? NavigateRoute;
    public bool Flip { get; set; }
    public int MaximumLines { get; set; } = 3;
    public HashSet<string> AcceptedMoves { get; set; } = [];

    private readonly System.Windows.Forms.Timer hoverTimer = new()
    {
        Interval = 180
    };
    private Position? hoverPosition;
    private HoverBoard? popup;
    private Position? pendingSource;
    private EngineResult? pendingResult;
    public VariationRows()
    {
        RowCount = 3;
        ColumnCount = 1;
        ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 3; i++)
            RowStyles.Add(new RowStyle(SizeType.Percent, 33.333f));
        VisibleChanged += (_, _) =>
        {
            if (!Visible)
                HidePreview();
        };
    }

    public void Clear()
    {
        pendingSource = null;
        pendingResult = null;
        hoverTimer.Stop();
        hoverPosition = null;
        popup?.Hide();
        foreach (Control c in Controls.Cast<Control>().ToArray())
            c.Dispose();
        Controls.Clear();
    }

    public void Placeholder()
    {
        SuspendLayout();
        Clear();
        for (int i = 0; i < 3; i++)
        {
            var row = new DarkVariationRow();
            row.Content.Controls.Add(new Label { Text = $"#{i + 1}   —   …", ForeColor = Color.Gray, AutoSize = true });
            Controls.Add(row, 0, i);
        }

        ResumeLayout();
    }

    private void HidePreview()
    {
        hoverTimer.Stop();
        hoverPosition = null;
        popup?.Hide();
        var p = pendingSource;
        var r = pendingResult;
        pendingSource = null;
        pendingResult = null;
        if (p is not null && r is not null)
            ShowLines(p, r);
    }

    private void Preview(Position p)
    {
        hoverPosition = p;
        hoverTimer.Stop();
        hoverTimer.Tick -= ShowPreview;
        hoverTimer.Tick += ShowPreview;
        hoverTimer.Start();
    }

    private void ShowPreview(object? sender, EventArgs e)
    {
        hoverTimer.Stop();
        if (hoverPosition is null || IsDisposed)
            return;
        popup ??= new HoverBoard();
        BoardPresentation.Preview(popup.Board, hoverPosition, Flip);
        var point = Cursor.Position;
        var area = Screen.FromPoint(point).WorkingArea;
        popup.Location = new(Math.Clamp(point.X - popup.Width / 2, area.Left, Math.Max(area.Left, area.Right - popup.Width)), Math.Clamp(point.Y - popup.Height - 12, area.Top, Math.Max(area.Top, area.Bottom - popup.Height)));
        popup.Show();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            hoverTimer.Dispose();
            popup?.Dispose();
        }

        base.Dispose(disposing);
    }

    public void ShowLines(Position source, EngineResult result)
    {
        if (hoverPosition is not null)
        {
            pendingSource = source;
            pendingResult = result;
            return;
        }

        SuspendLayout();
        Clear();
        RowCount = Math.Max(3, Math.Min(MaximumLines, result.Lines.Count));
        RowStyles.Clear();
        for (int i = 0; i < RowCount; i++) RowStyles.Add(new RowStyle(SizeType.Percent, 100f / RowCount));
        int rowIndex = 0;
        foreach (var line in result.Lines.Take(MaximumLines))
        {
            var host = new DarkVariationRow();
            var row = host.Content;
            var score = line.Mate is int mate ? $"M{(source.WhiteToMove ? mate : -mate)}" : $"{(source.WhiteToMove ? 1 : -1) * (line.ScoreCentipawns ?? 0) / 100d:+0.00;-0.00;0.00}";
            string verdict = AcceptedMoves.Count == 0 ? "" : AcceptedMoves.Contains(line.BestMove ?? "") ? "✓ " : "Nächstbester · ";
            row.Controls.Add(new Label { Text = verdict + $"#{line.Rank} {score}", AutoSize = true, ForeColor = UiPalette.Gainsboro, Margin = new Padding(3, 2, 5, 0) });
            var p = source.Clone();
            var back = new LinkLabel
            {
                Text = "↶",
                AutoSize = true,
                LinkColor = UiPalette.Gainsboro,
                VisitedLinkColor = UiPalette.Gainsboro,
                LinkBehavior = LinkBehavior.NeverUnderline,
                Margin = new Padding(2, 2, 2, 0)
            };
            back.Click += (_, _) => { NavigateRoute?.Invoke([source.Clone()]); Navigate?.Invoke(source.Clone()); };
            row.Controls.Add(back);
            var route = new List<Position> { source.Clone() };
            foreach (var u in line.Pv.Take(18))
            {
                var m = MoveGenerator.GenerateLegal(p).FirstOrDefault(m => m.Uci == u);
                if (m is null)
                    break;
                string san = $"{p.Fullmove}{(p.WhiteToMove ? "." : "…")}{SanNotation.Format(p, m)}";
                p.Apply(m);
                var target = p.Clone();
                route.Add(target.Clone());
                var targetRoute = route.Select(s => s.Clone()).ToArray();
                var link = new LinkLabel
                {
                    Text = san,
                    AutoSize = true,
                    LinkColor = UiPalette.Gainsboro,
                    VisitedLinkColor = UiPalette.Gainsboro,
                    LinkBehavior = LinkBehavior.NeverUnderline,
                    ActiveLinkColor = UiPalette.Orange,
                    Margin = new Padding(2, 2, 3, 0)
                };
                link.Click += (_, _) =>
                {
                    HidePreview();
                    NavigateRoute?.Invoke(targetRoute.Select(s => s.Clone()).ToArray());
                    Navigate?.Invoke(target.Clone());
                };
                link.MouseEnter += (_, _) => Preview(target);
                link.MouseLeave += (_, _) => HidePreview();
                row.Controls.Add(link);
            }

            if (line.ContinuationNote is not null)
                row.Controls.Add(new Label { Text = "· " + line.ContinuationNote, AutoSize = true, ForeColor = Color.Silver, Margin = new Padding(3, 2, 3, 0) });
            Controls.Add(host, 0, rowIndex++);
        }

        for (int rank = rowIndex + 1; rank <= RowCount; rank++)
            {
                var empty = new DarkVariationRow();
                empty.Content.Controls.Add(new Label { Text = $"#{rank}   —", ForeColor = Color.Gray, AutoSize = true });
                Controls.Add(empty, 0, rank - 1);
            }

        ResumeLayout();
    }
}
