namespace ChessOpeningTrainer;

/// <summary>Theme-native catalog with standard click/Ctrl/Shift selection.</summary>
internal sealed class DatasetDownloadList : UserControl
{
    private readonly DarkScrollPanel rows = new() { Dock = DockStyle.Fill };
    private readonly HashSet<int> selected = [];
    private IReadOnlyList<DatasetDownloadOption> options = [];
    private int anchor;
    public event Action? SelectionChanged;
    public IReadOnlyList<DatasetDownloadOption> Selection => selected.Order().Select(i => options[i]).ToArray();
    public DatasetDownloadList()
    {
        Dock = DockStyle.Fill;
        var header = new Panel { Dock = DockStyle.Top, Height = 35, BackColor = UiPalette.FromArgb(43, 49, 55) };
        AddCells(header, [L.T("Zeitraum"), L.T("Downloadgröße (ca.)"), L.T("Partien")]);
        Controls.AddRange([rows, header]);
        rows.Resize += (_, _) => { foreach (Control row in rows.Controls) row.Width = rows.Width - 15; };
    }
    private static void AddCells(Control row, string[] values)
    {
        for (int i = 0; i < 3; i++)
            row.Controls.Add(new Label { Text = values[i], TextAlign = ContentAlignment.MiddleLeft, ForeColor = UiPalette.Gainsboro, AutoSize = false, Tag = i });
        void Layout()
        {
            foreach (Control c in row.Controls)
                c.SetBounds((int)c.Tag! * row.Width / 3 + 10, 0, Math.Max(0, row.Width / 3 - 10), row.Height);
        }
        row.Resize += (_, _) => Layout();
        Layout();
    }
    public void SetOptions(IReadOnlyList<DatasetDownloadOption> values)
    {
        options = values;
        selected.Clear();
        foreach (Control c in rows.Controls.Cast<Control>().ToArray())
            c.Dispose();
        for (int i = 0; i < options.Count; i++)
        {
            int index = i;
            var item = options[i];
            var row = new Panel { Left = 0, Top = i * 32, Width = rows.Width - 15, Height = 32 };
            AddCells(row, [item.Name, $"{item.Bytes / 1e9:0.000} GB", item.Games.ToString("N0")]);
            void Click(object? _, MouseEventArgs e)
            {
                if (e.Button != MouseButtons.Left || e.Clicks > 1)
                    return;
                if ((ModifierKeys & Keys.Shift) != 0)
                {
                    if ((ModifierKeys & Keys.Control) == 0)
                        selected.Clear();
                    for (int n = Math.Min(anchor, index); n <= Math.Max(anchor, index); n++)
                        selected.Add(n);
                }
                else
                {
                    if ((ModifierKeys & Keys.Control) == 0)
                        selected.Clear();
                    if (!selected.Add(index))
                        selected.Remove(index);
                    anchor = index;
                }
                PaintSelection();
                SelectionChanged?.Invoke();
            }
            row.MouseDown += Click;
            foreach (Control c in row.Controls)
                c.MouseDown += Click;
            rows.Controls.Add(row);
        }
        PaintSelection();
        SelectionChanged?.Invoke();
    }
    private void PaintSelection()
    {
        for (int i = 0; i < rows.Controls.Count; i++)
            rows.Controls[i].BackColor = selected.Contains(i) ? UiPalette.FromArgb(75, 85, 94) : UiPalette.FromArgb(31, 35, 39);
    }
}
