namespace ChessOpeningTrainer;

public partial class BoardControl
{
    private TextBox? notationInput;
    private IReadOnlyList<Move> keyboardMatches = [];
    private Move? keyboardMove, submittingKeyboardMove;
    public bool KeyboardInputOpen => notationInput?.Visible == true;
    public bool CanOpenKeyboard => InputEnabled && Enabled && Visible && Position is not null && (SquareDragged is not null || SquareClicked is not null);

    public void OpenKeyboardInput()
    {
        if (!CanOpenKeyboard) return;
        if (notationInput is null)
        {
            notationInput = new TextBox { BorderStyle = BorderStyle.FixedSingle, AccessibleName = L.T("Zug eingeben"), PlaceholderText = L.T("Zug · Pfeile: Ziel · Umschalt: Figur · Enter: spielen · Esc: schließen"),
                BackColor = UiPalette.FromArgb(43, 49, 55), ForeColor = UiPalette.Gainsboro };
            Controls.Add(notationInput);
            notationInput.TextChanged += (_, _) => UpdateKeyboardMatches();
            notationInput.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.ShiftKey) { CycleKeyboardPiece(); e.SuppressKeyPress = true; }
            };
        }
        LayoutNotationInput();
        notationInput.Visible = true;
        notationInput.BringToFront();
        notationInput.Text = "";
        UpdateKeyboardMatches();
        notationInput.Focus();
    }

    private void UpdateKeyboardMatches()
    {
        if (!KeyboardInputOpen || Position is null) return;
        IEnumerable<Move> available = UnrestrictedDrag
            ? Enumerable.Range(0, 64).Where(s => Position.Squares[s] != '\0').SelectMany(s => Enumerable.Range(0, 64).Where(t => t != s).Select(t => new Move { From = s, To = t }))
            : PremoveSide is bool side ? Enumerable.Range(0, 64).SelectMany(s => Premoves.Candidates(Position, s, side))
            : MoveProvider?.Invoke(Position) ?? MoveGenerator.GenerateLegal(Position);
        keyboardMatches = SimplifiedMoveNotation.Match(Position, notationInput!.Text, available);
        keyboardMove = keyboardMatches.OrderBy(m => VisualBottomLeftOrder(m.To)).ThenBy(m => VisualBottomLeftOrder(m.From))
            .ThenBy(m => m.Promotion == 'q' || m.Promotion == '\0' ? 0 : 1).FirstOrDefault();
        ShowKeyboardMatches();
    }

    private int VisualBottomLeftOrder(int square) => (Flip ? 7 - square / 8 : square / 8) * 8 + (Flip ? 7 - square % 8 : square % 8);
    private void LayoutNotationInput() => notationInput?.SetBounds((int)BoardLeft, 0, Math.Max(80, (int)BoardSize), 22);
    protected override void OnResize(EventArgs e) { base.OnResize(e); LayoutNotationInput(); }
    private void ShowKeyboardMatches()
    {
        SelectedSquare = keyboardMove?.From;
        CandidateMoves = keyboardMatches.Where(m => m.From == keyboardMove?.From).ToArray();
        notationInput!.BackColor = keyboardMatches.Count == 0 ? UiPalette.FromArgb(100, 45, 45) : UiPalette.FromArgb(43, 49, 55);
        Invalidate();
    }
    private void CycleKeyboardPiece()
    {
        var sources = keyboardMatches.Select(m => m.From).Distinct().OrderBy(VisualBottomLeftOrder).ToArray();
        if (sources.Length == 0) return;
        int next = (Array.IndexOf(sources, keyboardMove?.From ?? -1) + 1) % sources.Length;
        keyboardMove = keyboardMatches.Where(m => m.From == sources[next]).OrderBy(m => VisualBottomLeftOrder(m.To)).First();
        ShowKeyboardMatches();
    }
    private void NavigateKeyboard(Keys direction)
    {
        if (keyboardMove is not { } current) return;
        PointF origin = SquareCenter(current.To, 1);
        int dx = direction == Keys.Left ? -1 : direction == Keys.Right ? 1 : 0;
        int dy = direction == Keys.Up ? -1 : direction == Keys.Down ? 1 : 0;
        var next = keyboardMatches.Where(m => m.From == current.From && m.To != current.To)
            .Select(m => (Move: m, Point: SquareCenter(m.To, 1)))
            .Where(x => (x.Point.X - origin.X) * dx + (x.Point.Y - origin.Y) * dy > 0)
            .OrderBy(x => Math.Abs((x.Point.X - origin.X) * dy - (x.Point.Y - origin.Y) * dx))
            .ThenBy(x => Math.Abs(x.Point.X - origin.X) + Math.Abs(x.Point.Y - origin.Y)).FirstOrDefault();
        if (next.Move is not null) keyboardMove = next.Move;
        ShowKeyboardMatches();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (KeyboardInputOpen)
        {
            Keys key = keyData & Keys.KeyCode;
            if (key == Keys.Escape) { CloseKeyboardInput(); return true; }
            if (key is Keys.Left or Keys.Right or Keys.Up or Keys.Down) { NavigateKeyboard(key); return true; }
            if (key == Keys.Tab) { CycleKeyboardPiece(); return true; }
            if (key == Keys.Enter)
            {
                if (keyboardMove is not { } move || !CanOpenKeyboard) return true;
                submittingKeyboardMove = move;
                CloseKeyboardInput();
                try
                {
                    // Existing feature handlers remain responsible for rules, animations and feedback.
                    if (SquareClicked is not null && !UnrestrictedDrag && PremoveSide is null)
                    { SquareClicked.Invoke(move.From); SquareClicked.Invoke(move.To); }
                    else if (SquareDragged is not null) SquareDragged.Invoke(move.From, move.To);
                    else { SquareClicked?.Invoke(move.From); SquareClicked?.Invoke(move.To); }
                }
                finally { submittingKeyboardMove = null; }
                return true;
            }
            if (key == Keys.Space) return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }
    public void CloseKeyboardInput()
    {
        if (notationInput is not null) notationInput.Visible = false;
        keyboardMatches = []; keyboardMove = null;
        SelectedSquare = null; CandidateMoves = [];
        Invalidate();
    }
    public Move? ChoosePromotion(IWin32Window owner, List<Move> choices, bool white) =>
        submittingKeyboardMove is { Promotion: not '\0' } requested && choices.FirstOrDefault(m => m.Equals(requested)) is { } chosen
            ? chosen : PromotionPicker.Choose(owner, choices, white);

    private void PaintKeyboardSelection(Graphics g)
    {
        if (!KeyboardInputOpen || keyboardMove is not { } move) return;
        float cell = BoardSize / 8;
        var target = SquareCenter(move.To, cell);
        using var border = new Pen(UiPalette.Gainsboro, Math.Max(2, cell * .045f));
        g.DrawRectangle(border, BoardLeft + target.X - cell / 2 + 3, 22 + target.Y - cell / 2 + 3, cell - 6, cell - 6);
    }
}
