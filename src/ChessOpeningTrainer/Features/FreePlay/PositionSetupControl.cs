namespace ChessOpeningTrainer;
public sealed class PositionSetupControl : UserControl
{
    private readonly PlacementBoard board = new()
    {
        Dock = DockStyle.Fill
    };
    private readonly ComboBox turn = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Width = 240
    };
    private readonly Label info = new()
    {
        Width = 245,
        Height = 100
    };
    private char? tool;
    private readonly Label engineWarning = new() { Width = 240, Height = 110, ForeColor = Color.Orange };
    private readonly List<PieceToolButton> pieceTools = [];
    private Button? moveTool;
    public event Action<Position>? Accepted;
    private readonly Dictionary<char, CheckBox> castling = [];
    public Position Result { get; private set; } = Position.FromFen(Position.InitialFen);
    private readonly Dictionary<bool, ComboBox> kingTargets = [];
    private readonly Dictionary<bool, Control> kingRows = [];
    public KingCaptureRules? KingRules => !KingCaptureRules.IsMultiple(Result) ? null : new(
        Result.Squares.Count(c => c == 'K'), Result.Squares.Count(c => c == 'k'),
        kingTargets.GetValueOrDefault(true)?.SelectedIndex != 1, kingTargets.GetValueOrDefault(false)?.SelectedIndex != 1);

    public static string? Validate(Position p, bool allowMultipleKings = false)
    {
        int whiteKings = p.Squares.Count(c => c == 'K'), blackKings = p.Squares.Count(c => c == 'k');
        if (allowMultipleKings ? whiteKings < 1 || blackKings < 1 : whiteKings != 1 || blackKings != 1)
            return allowMultipleKings ? "Mindestens ein König je Seite ist erforderlich." : "Genau ein König je Seite ist erforderlich.";
        if (p.Squares.Take(8).Concat(p.Squares.Skip(56)).Any(c => char.ToLowerInvariant(c) == 'p'))
            return "Bauern dürfen nicht auf der ersten oder achten Reihe stehen.";
        for (int side = 0; side < 2; side++)
        {
            bool white = side == 0;
            var men = p.Squares.Where(c => c != '\0' && char.IsUpper(c) == white).ToList();
            if ((men.Count > 16 && !(allowMultipleKings && KingCaptureRules.IsMultiple(p))) || men.Count(c => char.ToLowerInvariant(c) == 'p') > 8)
                return "Höchstens 16 Figuren und 8 Bauern je Seite.";
        }

        if (!(allowMultipleKings && KingCaptureRules.IsMultiple(p)) && MoveGenerator.IsKingInCheck(p, !p.WhiteToMove))
            return "Die Seite, die zuletzt gezogen hat, darf nicht im Schach stehen.";
        return null;
    }

    public PositionSetupControl(Position? initial = null, bool reconstruction = false, bool flipped = false, bool allowMultipleKings = false)
    {
        Text = "Brett aufstellen · Freies Spiel";
        Size = new(1120, 840);
        MinimumSize = new(900, 680);
        BackColor = UiPalette.FromArgb(31, 35, 39);
        ForeColor = UiPalette.Gainsboro;
        Result = (initial ?? Position.FromFen(Position.InitialFen)).Clone();
        board.Position = Result;
        board.Flip = flipped;
        var tools = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 265,
            Padding = new(10),
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true
        };
        Controls.Add(board);
        Controls.Add(tools);
        tools.Controls.Add(new Label { Text = "Figur wählen und Feld anklicken. Ziehen verschiebt Figuren. Rechtsklick löscht.", Width = 240, Height = 60 });
        foreach (string pieces in new[]
        {
            "KQRBNP",
            "kqrbnp"
        }

        )
        {
            var row = new FlowLayoutPanel
            {
                Width = 245,
                Height = 44
            };
            foreach (char piece in pieces)
            {
                var button = new PieceToolButton
                {
                    Piece = piece,
                    Width = 36,
                    Height = 40,
                    Margin = new Padding(1)
                };
                button.Click += (_, _) =>
                {
                    tool = piece;
                    info.Text = "Setzen: " + piece;
                    RefreshTools();
                };
                pieceTools.Add(button);
                row.Controls.Add(button);
            }

            tools.Controls.Add(row);
        }

        Button Button(string label, Action action)
        {
            Button button = label == "Brett drehen" ? new MenuIconButton("flip-board") { AccessibleName = label } : new Button { Text = label };
            button.Width = label == "Brett drehen" ? 38 : 240;
            button.Height = 34;
            if (reconstruction && label is "Brett drehen" or "Aufbau bestätigen") button.Visible = false;
            button.Click += (_, _) => action();
            tools.Controls.Add(button);
            return button;
        }

        moveTool = Button("Verschieben", () =>
        {
            tool = null;
            info.Text = "Figuren ziehen, um sie zu versetzen.";
            RefreshTools();
        });
        Button("Leeres Brett", () =>
        {
            Array.Clear(Result.Squares);
            Result.Castling = "";
            foreach (var c in castling.Values)
                c.Checked = false;
            board.Invalidate();
        });
        Button("Grundstellung", () =>
        {
            Result = Position.FromFen(Position.InitialFen);
            board.Position = Result;
            foreach (var c in castling.Values)
                c.Checked = true;
            turn.SelectedIndex = 0;
            board.Invalidate();
        });
        Button("Brett drehen", () =>
        {
            board.Flip = !board.Flip;
            board.Invalidate();
        });
        turn.Items.AddRange(["Weiß am Zug", "Schwarz am Zug"]);
        turn.SelectedIndex = Result.WhiteToMove ? 0 : 1;
        tools.Controls.Add(turn);
        turn.Visible = !reconstruction;
        turn.SelectedIndexChanged += (_, _) => Result.WhiteToMove = turn.SelectedIndex == 0;
        if (allowMultipleKings && !reconstruction)
            foreach (bool white in new[] { true, false })
            {
                var row = new FlowLayoutPanel { Width = 245, Height = 63, WrapContents = false, FlowDirection = FlowDirection.TopDown, Visible = false };
                row.Controls.Add(new Label { Text = white ? "Weiß verliert nach Verlust von:" : "Schwarz verliert nach Verlust von:", Width = 240, Height = 25 });
                var choice = new ComboBox { Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };
                choice.Items.AddRange(["Allen Königen", "Einem König"]); choice.SelectedIndex = 0;
                row.Controls.Add(choice); tools.Controls.Add(row);
                kingTargets[white] = choice; kingRows[white] = row;
            }
        foreach (char c in "KQkq")
        {
            var check = new CheckBox
            {
                Text = c switch
                {
                    'K' => "Weiß kurze Rochade",
                    'Q' => "Weiß lange Rochade",
                    'k' => "Schwarz kurze Rochade",
                    _ => "Schwarz lange Rochade"
                },
                Checked = Result.Castling.Contains(c),
                AutoSize = true
            };
            check.Visible = !reconstruction;
            castling[c] = check;
            tools.Controls.Add(check);
        }

        tools.Controls.Add(info);
        tools.Controls.Add(engineWarning);
        engineWarning.Visible = !reconstruction;
        void RefreshRules()
        {
            engineWarning.Text = L.T(SetupMaterial.EngineWarning(Result) ?? "");
            foreach (var pair in kingRows)
                pair.Value.Visible = Result.Squares.Count(c => c == (pair.Key ? 'K' : 'k')) > 1;
            foreach (var pair in castling) pair.Value.Enabled = !KingCaptureRules.IsMultiple(Result);
        }
        board.Invalidated += (_, _) => RefreshRules();
        Button(reconstruction ? "Aufbau bestätigen" : "Mit dieser Stellung spielen", () =>
        {
            Result.EnPassant = null;
            Result.Halfmove = 0;
            Result.Fullmove = 1;
            Result.Captures.Clear();
            Result.Castling = string.Concat(castling.Where(k => k.Value.Checked && Available(k.Key)).Select(k => k.Key));
            if (allowMultipleKings && KingCaptureRules.IsMultiple(Result)) Result.Castling = "";
            string? error = reconstruction ? null : Validate(Result, allowMultipleKings);
            if (error is not null)
            {
                info.Text = error;
                return;
            }

            Accepted?.Invoke(Result.Clone());
        });
        bool Available(char c) => c switch
        {
            'K' => Result.Squares[4] == 'K' && Result.Squares[7] == 'R',
            'Q' => Result.Squares[4] == 'K' && Result.Squares[0] == 'R',
            'k' => Result.Squares[60] == 'k' && Result.Squares[63] == 'r',
            _ => Result.Squares[60] == 'k' && Result.Squares[56] == 'r'
        };
        board.Place += square =>
        {
            if (tool is char piece)
                Result.Squares[square] = piece;
            board.Invalidate();
        };
        board.Relocate += (from, to) =>
        {
            if (Result.Squares[from] == '\0')
                return;
            Result.Squares[to] = Result.Squares[from];
            Result.Squares[from] = '\0';
            board.Invalidate();
        };
        info.Text = "Figuren ziehen, um sie zu versetzen.";
        RefreshTools();
        RefreshRules();
    }

    private void RefreshTools()
    {
        foreach (var button in pieceTools)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = tool == button.Piece ? UiPalette.FromArgb(57, 91, 119) : BackColor;
            button.FlatAppearance.BorderSize = tool == button.Piece ? 3 : 1;
            button.FlatAppearance.BorderColor = tool == button.Piece ? UiPalette.Gainsboro : UiPalette.FromArgb(70, 75, 80);
            button.Invalidate();
        }
        if (moveTool is null) return;
        moveTool.FlatStyle = FlatStyle.Flat;
        moveTool.FlatAppearance.BorderSize = tool is null ? 3 : 1;
        moveTool.BackColor = tool is null ? UiPalette.FromArgb(57, 91, 119) : BackColor;
    }
}
