namespace ChessOpeningTrainer;
public sealed class SectionEditor : TableLayoutPanel
{
    private readonly ToolTip tips = new()
    {
        AutoPopDelay = 12000
    };
    private readonly List<NumericUpDown[]> columns = [];
    private readonly List<NumericUpDown[]> cachedColumns = [];
    private readonly List<Label> headings = [];
    private readonly List<Label> labels = [];
    private int initializedColumns;
    private bool changingCount;
    private int complexityStartDepth = 5;
    private static readonly string[] names = ["Abschnittstiefengrenze", "Komplexitätsgrenze", "Leichtigkeitsgrenze", "Enginesuchtiefe", "Maximaler gegnerischer Verlust", "Maximaler Kompressionsverlust", "Komplexitätsreserve (KGR): Topzüge", "KGR-Bewertungsgrenze (Weißsicht)", "Maximale Zugvariation"];
    private static readonly string[] descriptions = ["Exklusive Endtiefe dieses Abschnitts in Halbzügen ab Startstellung. Grenzen müssen ansteigen.", "Ab dieser Anzahl plausibler Gegnerzüge endet die Fortsetzung, sobald die Mindesttiefe erreicht ist.", "Gegnerzüge mit dieser oder schlechterer Bewertung aus Gegnersicht werden nicht gelernt (Bauern).", "Stockfish-Suchtiefe pro Stellung. Höher bedeutet mehr Rechenzeit; unabhängig von der Baumtiefe.", "Maximaler Bewertungsverlust einer Gegnerantwort gegenüber deren bestem Zug (Bauern).", "Maximal erlaubter eigener Bewertungsverlust für das Zusammenführen von Ästen (Bauern).", "Trotz Komplexitätsabbruch bis zu X beste Gegnerzüge weiterführen. 0 = aus; Maximal-Tiefe bleibt verbindlich.", "Mindestbewertung der Fortsetzung aus Weißsicht wie in der Bewertungsleiste. 0 = ausgeglichen.", "Zufällige eigene Antwort unter legalen Zügen mit Bewertung strikt über bester Zug minus diesem Verlust (Bauern). 0 = bester Zug. Lernkompression hat Vorrang."];
    private string language = Preferences.Current.Language;
    public event Action? Changed;
    public SectionEditor()
    {
        DoubleBuffered = true;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        MinimumSize = new(520, 0);
        Margin = new(0, 8, 0, 8);
        Width = 520;
        RowCount = names.Length + 1;
        for (int row = 0; row < RowCount; row++) RowStyles.Add(new(SizeType.AutoSize));
        ColumnCount = 6;
        SuspendLayout();
        ColumnStyles.Add(new(SizeType.Absolute, 190));
        for (int col = 0; col < 5; col++) ColumnStyles.Add(new(SizeType.Absolute, 0));
        Controls.Add(new Label { Text = "Parameter", AutoSize = true }, 0, 0);
        for (int row = 0; row < names.Length; row++)
        {
            var label = new Label { Text = names[row], AutoSize = true, MaximumSize = new(185, 0), Margin = new(0, 8, 0, 4) };
            labels.Add(label); Controls.Add(label, 0, row + 1);
            L.SetToolTip(tips, label, descriptions[row]);
        }
        // Create and theme every native input once, before the editor is shown.
        // Hidden columns retain their values and HWNDs instead of adding dozens
        // of new child windows while the user watches a live FlowLayoutPanel.
        for (int col = 0; col < 5; col++)
        {
            var heading = new Label { Text = (col + 1).ToString(), AutoSize = true, Visible = false };
            headings.Add(heading); Controls.Add(heading, col + 1, 0);
            var defaults = new DepthSection { EndDepth = 10 + col * 5 };
            decimal[] data = [defaults.EndDepth, defaults.ComplexThreshold, (decimal)defaults.EasyThreshold, defaults.SearchDepth,
                (decimal)defaults.MaxReplyLoss, (decimal)defaults.CompressionLoss, defaults.RetainBest, (decimal)defaults.RetainMinEval, (decimal)defaults.LearnerVariationLoss];
            decimal[] min = [1, 2, -20, 5, 0, 0, 0, -20, 0], max = [60, 50, 0, 24, 20, 5, 50, 20, 20];
            var fields = new NumericUpDown[names.Length];
            for (int row = 0; row < names.Length; row++)
            {
                int places = row is 2 or 4 or 7 ? 1 : row is 5 or 8 ? 2 : 0;
                var field = new ScrollNumber
                {
                    Minimum = min[row], Maximum = max[row], Value = data[row], Visible = false,
                    EmptyDefaultValue = data[row],
                    DecimalPlaces = places, Increment = row == 8 ? .01m : places == 0 ? 1 : places == 2 ? .05m : .1m,
                    Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new(3, 5, 3, 5), MinimumSize = new(55, 26)
                };
                fields[row] = field; Controls.Add(field, col + 1, row + 1);
                L.SetToolTip(tips, field, descriptions[row]);
                int parameter = row;
                field.TabIndex = col * names.Length + row;
                field.ValueChanged += (_, _) =>
                {
                    if (changingCount) return;
                    if (parameter == 0) UpdateSectionLayout(() => { });
                    else Changed?.Invoke();
                };
            }
            cachedColumns.Add(fields);
        }
        ResumeLayout(false);
        SetCount(1);
        Preferences.Changed += RefreshLanguage;
    }

    private void RefreshLanguage()
    {
        if (language == Preferences.Current.Language)
            return;
        language = Preferences.Current.Language;
        SuspendLayout();
        for (int row = 0; row < labels.Count; row++)
        {
            labels[row].Text = L.T(names[row]);
            L.SetToolTip(tips, labels[row], descriptions[row]);
            foreach (var column in cachedColumns) L.SetToolTip(tips, column[row], descriptions[row]);
        }
        SetParameterRowHeights();
        ResumeLayout(true);
    }

    public List<DepthSection> Values => columns.Select(c => new DepthSection { EndDepth = (int)c[0].Value, ComplexThreshold = (int)c[1].Value, EasyThreshold = (double)c[2].Value, SearchDepth = (int)c[3].Value, MaxReplyLoss = (double)c[4].Value, CompressionLoss = (double)c[5].Value, RetainBest = (int)c[6].Value, RetainMinEval = (double)c[7].Value, LearnerVariationLoss = (double)c[8].Value }).ToList();

    public int ComplexityStartDepth
    {
        get => complexityStartDepth;
        set
        {
            value = Math.Clamp(value, 0, 60);
            if (value == complexityStartDepth) return;
            complexityStartDepth = value;
            UpdateSectionLayout(() => { });
        }
    }
    public void SetValues(IReadOnlyList<DepthSection> values)
    {
        if (values.Count == 0) return;
        SetCount(values.Count);
        UpdateSectionLayout(() =>
        {
            for (int col = 0; col < values.Count; col++)
            {
                var s = values[col];
                decimal[] data = [s.EndDepth, s.ComplexThreshold, (decimal)s.EasyThreshold, s.SearchDepth,
                    (decimal)s.MaxReplyLoss, (decimal)s.CompressionLoss, s.RetainBest, (decimal)s.RetainMinEval, (decimal)s.LearnerVariationLoss];
                // Remove bounds inherited from the previously selected job first.
                columns[col][0].Minimum = 1; columns[col][0].Maximum = 60;
                for (int row = 0; row < data.Length; row++)
                    columns[col][row].Value = Math.Clamp(data[row], columns[col][row].Minimum, columns[col][row].Maximum);
            }
        });
    }
    internal IEnumerable<ScrollNumber> AllInputs => cachedColumns.SelectMany(c => c).Cast<ScrollNumber>();
    internal IEnumerable<ScrollNumber> FocusInputs => columns.SelectMany(c => c).OfType<ScrollNumber>().Where(n => n.Visible && n.Enabled);

    public void SetCount(int count)
    {
        if (count is < 1 or > 5) throw new ArgumentOutOfRangeException(nameof(count));
        if (count == columns.Count) return;
        UpdateSectionLayout(() =>
        {
            while (initializedColumns < count)
            {
                int col = initializedColumns++;
                if (col > 0)
                {
                    var field = (ScrollNumber)cachedColumns[col][0];
                    field.Value = Math.Min(field.Maximum, cachedColumns[col - 1][0].Value + 5);
                    field.EmptyDefaultValue = field.Value;
                }
            }
            columns.Clear();
            for (int col = 0; col < cachedColumns.Count; col++)
            {
                var style = ColumnStyles[col + 1];
                style.SizeType = col < count ? SizeType.Percent : SizeType.Absolute;
                style.Width = col < count ? 100f / count : 0;
                headings[col].Visible = col < count;
                if (col < count) columns.Add(cachedColumns[col]);
            }
        });
    }

    private void UpdateSectionLayout(Action edit)
    {
        Rectangle before = Bounds;
        using (var redraw = new GeneratorLayoutBatch(this))
        {
            var ancestors = new List<Control>();
            for (Control? parent = Parent; parent is not null; parent = parent.Parent) { parent.SuspendLayout(); ancestors.Add(parent); }
            SuspendLayout();
            changingCount = true;
            try
            {
                edit();
                // Leave room for each following boundary. Raising a previous
                // boundary adjusts later minima/values, never creates duplicate
                // section depths or defers a validation failure until generation.
                decimal previous = 0;
                for (int col = 0; col < columns.Count; col++)
                {
                    var boundary = columns[col][0];
                    decimal nextMinimum = previous + 1, nextMaximum = 60 - (columns.Count - col - 1);
                    if (boundary.Minimum > nextMaximum) boundary.Minimum = nextMinimum;
                    if (boundary.Maximum != nextMaximum) boundary.Maximum = nextMaximum;
                    if (boundary.Minimum != nextMinimum) boundary.Minimum = nextMinimum;
                    previous = boundary.Value;
                }
                for (int col = 0; col < cachedColumns.Count; col++)
                {
                    for (int row = 0; row < cachedColumns[col].Length; row++)
                        cachedColumns[col][row].Visible = col < columns.Count &&
                            (row is not (1 or 6 or 7) || cachedColumns[col][0].Value > complexityStartDepth);
                }
                SetParameterRowHeights();
                for (int row = 0; row < labels.Count; row++)
                {
                    bool visible = row is not (1 or 6 or 7) || columns.Any(c => c[0].Value > complexityStartDepth);
                    labels[row].Visible = visible;
                    RowStyles[row + 1].SizeType = SizeType.Absolute;
                    if (!visible) RowStyles[row + 1].Height = 0;
                }
            }
            finally
            {
                changingCount = false;
                ResumeLayout(false);
                foreach (var ancestor in ancestors) ancestor.ResumeLayout(false);
                PerformLayout();
                Parent?.PerformLayout();
                Invalidate(true);
                Parent?.Invalidate(Rectangle.Union(before, Bounds), true);
            }
        }
        Changed?.Invoke();
    }

    private void SetParameterRowHeights()
    {
        // Wrapped descriptions must not stretch just the KGR rows and leave
        // irregular gaps between their fixed-height native input boxes.
        int height = Math.Max(
            labels.Max(label => label.GetPreferredSize(new(label.MaximumSize.Width, 0)).Height + label.Margin.Vertical),
            cachedColumns.SelectMany(c => c).Max(input => input.PreferredSize.Height + input.Margin.Vertical));
        for (int row = 0; row < labels.Count; row++)
        {
            bool visible = row is not (1 or 6 or 7) || columns.Any(c => c[0].Value > complexityStartDepth);
            RowStyles[row + 1].SizeType = SizeType.Absolute;
            RowStyles[row + 1].Height = visible ? height : 0;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        // Create the hidden native arrows/textboxes too. Their first appearance
        // must not pay handle creation or per-HWND theme initialization costs.
        foreach (var field in cachedColumns.SelectMany(c => c))
        {
            _ = field.Handle;
            foreach (Control child in field.Controls) _ = child.Handle;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Preferences.Changed -= RefreshLanguage;
            tips.Dispose();
        }

        base.Dispose(disposing);
    }
}
