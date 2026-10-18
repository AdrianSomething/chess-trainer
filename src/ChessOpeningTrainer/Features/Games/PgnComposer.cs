using System.Text;
using System.Text.RegularExpressions;

namespace ChessOpeningTrainer;
public sealed class PgnComposer : LocalizedForm
{
    private readonly TextBox text = new()
    {
        Multiline = true,
        AcceptsReturn = true,
        AcceptsTab = true,
        ScrollBars = ScrollBars.Both,
        Dock = DockStyle.Fill,
        Font = new Font("Consolas", 11)
    };
    private readonly BoardControl board = new();
    private readonly Label status = new()
    {
        Dock = DockStyle.Bottom,
        Height = 54
    };
    private readonly ListBox suggestions = new()
    {
        Visible = false,
        Height = 140,
        BackColor = UiPalette.FromArgb(43, 49, 55),
        ForeColor = UiPalette.Gainsboro,
        BorderStyle = BorderStyle.FixedSingle
    };
    private readonly Button save = new()
    {
        Text = "Partie übernehmen",
        Dock = DockStyle.Bottom,
        Height = 36
    };
    private readonly System.Windows.Forms.Timer debounce = new()
    {
        Interval = 180
    };
    private readonly List<PersonalGame> existing;
    private PersonalGame draft = new();
    private bool updating, valid;
    private int? selected;
    private string? suggestionTag;
    public string Pgn => PgnText.Write(draft);

    public PgnComposer(IEnumerable<PersonalGame> games)
    {
        existing = games.ToList();
        Text = "PGN schreiben";
        Size = new(1250, 850);
        MinimumSize = new(1000, 650);
        BackColor = UiPalette.FromArgb(31, 35, 39);
        ForeColor = UiPalette.Gainsboro;
        var left = new Panel
        {
            Dock = DockStyle.Left,
            Width = 490
        };
        var right = new Panel
        {
            Dock = DockStyle.Fill
        };
        Controls.Add(right);
        Controls.Add(left);
        left.Controls.AddRange([text, suggestions, status, save]);
        right.Controls.Add(board);
        board.Dock = DockStyle.Fill;
        Resize += (_, _) => left.Width = (int)(ClientSize.Width * .4);
        draft.Tags = new()
        {
            {
                "Event",
                ""
            },
            {
                "Site",
                ""
            },
            {
                "Date",
                DateTime.Today.ToString("yyyy.MM.dd")
            },
            {
                "Round",
                ""
            },
            {
                "White",
                ""
            },
            {
                "Black",
                ""
            },
            {
                "Result",
                "*"
            }
        };
        text.Text = PgnText.Write(draft, true);
        save.Click += (_, _) =>
        {
            Parse();
            if (valid && draft.Moves.Count > 0)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        };
        text.TextChanged += (_, _) =>
        {
            if (updating)
                return;
            debounce.Stop();
            debounce.Start();
            Suggest();
        };
        debounce.Tick += (_, _) =>
        {
            debounce.Stop();
            Parse();
        };
        text.KeyDown += OnKey;
        text.MouseUp += (_, _) => Suggest();
        text.Enter += (_, _) => Suggest();
        Shown += (_, _) =>
        {
            text.Focus();
            text.SelectionStart = text.Text.IndexOf('"') + 1;
            text.SelectionLength = 0;
            Suggest();
        };
        text.KeyUp += (_, e) =>
        {
            if (e.KeyCode is Keys.Left or Keys.Right or Keys.Home or Keys.End)
                Suggest();
            if (e.KeyCode is Keys.Space or Keys.Enter && !suggestions.Visible)
            {
                Parse();
                int body = text.Text.IndexOf("\n\n", StringComparison.Ordinal);
                if (body < 0)
                    body = text.Text.IndexOf("\r\n\r\n", StringComparison.Ordinal);
                string rest = text.Text[text.SelectionStart..].Trim();
                if (valid && body >= 0 && text.SelectionStart > body && rest is "" or "*" or "1-0" or "0-1" or "1/2-1/2")
                    Normalize();
            }
        };
        board.SquareClicked += square =>
        {
            if (!valid)
                return;
            if (selected is int from && Play(from, square))
                return;
            selected = square;
            board.SelectedSquare = square;
            board.CandidateMoves = MoveGenerator.GenerateLegal(board.Position!).Where(m => m.From == square).ToList();
            board.Invalidate();
        };
        board.SquareDragged += (from, to) =>
        {
            if (valid)
                Play(from, to);
        };
        board.BlankClicked += () =>
        {
            selected = null;
            board.SelectedSquare = null;
            board.CandidateMoves = [];
            board.Invalidate();
        };
        FormClosed += (_, _) => debounce.Dispose();
        Parse();
    }

    private void Parse()
    {
        try
        {
            var parsed = PgnImport.Parse(text.Text);
            if (parsed.Count > 1)
                throw new InvalidDataException("Hier eine Partie schreiben; Sammeldateien über PGN importieren laden.");
            if (parsed.Count == 1)
                draft = parsed[0];
            else
            {
                var tags = new Dictionary<string, string>();
                foreach (Match match in Regex.Matches(text.Text, "(?m)^\\s*\\[(\\w+)\\s+\"([^\"]*)\"\\]"))
                    tags[match.Groups[1].Value] = match.Groups[2].Value;
                draft = new()
                {
                    Tags = tags,
                    StartFen = tags.GetValueOrDefault("FEN") ?? Position.InitialFen
                };
            }

            var p = draft.Positions()[^1];
            board.Position = p;
            board.Invalidate();
            valid = true;
            board.InputEnabled = true;
            save.Enabled = draft.Moves.Count > 0;
            status.Text = $"{draft.Moves.Count} legale Halbzüge · Beide Seiten am Brett spielbar.\nTag-Vorschlag: ↑/↓ auswählen, Tab übernehmen.";
        }
        catch (Exception ex)
        {
            valid = false;
            board.InputEnabled = false;
            save.Enabled = false;
            status.Text = ex.Message;
        }
    }

    private bool Play(int from, int to)
    {
        Parse();
        if (!valid)
            return false;
        var p = board.Position!;
        var options = MoveGenerator.GenerateLegal(p).Where(m => m.From == from && m.To == to).ToList();
        if (options.Count == 0)
            return false;
        var move = options.Count == 1 ? options[0] : PromotionPicker.Choose(this, options, p.WhiteToMove);
        if (move is null)
            return false;
        draft.Moves.Add(move.Uci);
        Normalize();
        return true;
    }

    private void Normalize()
    {
        updating = true;
        text.Text = PgnText.Write(draft, true);
        string? result = draft.Tags.GetValueOrDefault("Result");
        if (string.IsNullOrWhiteSpace(result))
            result = "*";
        text.SelectionStart = Math.Max(0, text.Text.Length - result.Length);
        updating = false;
        suggestions.Visible = false;
        Parse();
    }

    private void Suggest()
    {
        int line = text.GetLineFromCharIndex(text.SelectionStart);
        if (line < 0 || line >= text.Lines.Length)
            return;
        var match = Regex.Match(text.Lines[line], "^\\[(\\w+)\\s+\"([^\"]*)");
        int column = text.SelectionStart - text.GetFirstCharIndexFromLine(line);
        if (!match.Success || column < match.Groups[2].Index || column > match.Groups[2].Index + match.Groups[2].Length)
        {
            suggestions.Visible = false;
            return;
        }

        suggestionTag = match.Groups[1].Value;
        string prefix = match.Groups[2].Value;
        var values = existing.Select(g => g.Tags.GetValueOrDefault(suggestionTag)).Where(v => !string.IsNullOrWhiteSpace(v) && v.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && v != prefix).Distinct().Take(30).ToArray();
        suggestions.Items.Clear();
        suggestions.Items.AddRange(values!);
        if (values.Length == 0)
        {
            suggestions.Visible = false;
            return;
        }

        suggestions.SelectedIndex = 0;
        var point = text.GetPositionFromCharIndex(text.SelectionStart);
        suggestions.SetBounds(Math.Min(point.X, Math.Max(0, text.Width - 300)), Math.Min(point.Y + 24, Math.Max(0, text.Height - 140)), Math.Min(300, text.Width), 140);
        suggestions.Visible = true;
        suggestions.BringToFront();
    }

    private void OnKey(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Tab)
        {
            int currentLine = text.GetLineFromCharIndex(text.SelectionStart);
            if (suggestions.Visible && suggestions.SelectedItem is string completion)
            {
                int start = text.GetFirstCharIndexFromLine(currentLine);
                text.Select(start, text.Lines[currentLine].Length);
                text.SelectedText = $"[{suggestionTag} \"{completion.Replace("\"", "\\\"")}\"]";
            }
            suggestions.Visible = false;
            for (int next = currentLine + 1; next < text.Lines.Length; next++)
            {
                int empty = text.Lines[next].IndexOf("\"\"", StringComparison.Ordinal);
                if (empty < 0 || !text.Lines[next].StartsWith('[')) continue;
                text.Select(text.GetFirstCharIndexFromLine(next) + empty + 1, 0);
                Suggest();
                break;
            }
            e.SuppressKeyPress = true;
            return;
        }
        if (!suggestions.Visible)
            return;
        if (e.KeyCode is Keys.Up or Keys.Down)
        {
            int count = suggestions.Items.Count;
            suggestions.SelectedIndex = (suggestions.SelectedIndex + (e.KeyCode == Keys.Down ? 1 : -1) + count) % count;
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            suggestions.Visible = false;
            e.SuppressKeyPress = true;
        }
    }
}
