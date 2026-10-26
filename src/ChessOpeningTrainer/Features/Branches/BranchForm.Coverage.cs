namespace ChessOpeningTrainer;

public sealed partial class BranchForm
{
    private sealed record CoverageRow(CoverageGap Gap)
    { public override string ToString() => $"+{Gap.Gain:0.00} · {Gap.Games:N0} " + L.T("Partien") + " · " + Gap.Sequence; }
    private readonly Button coverageButton = new() { Text = "Opening Coverage Score", AutoSize = true, Height = 34, Visible = false };
    private const string CoverageDescription = "Abdeckung der beobachteten Gegnerantworten, 0–100. Gewicht = absolute Partienzahl / (1 + Abstand in vollen Zügen ab Startstellung). Eigene Züge und die manuelle Startsequenz zählen nicht. Unbekannte Stellungen werden separat ausgewiesen. Vorschläge zeigen den unmittelbaren Score-Gewinn bei unverändertem restlichem Baum.";
    private void InitializeCoverage(FlowLayoutPanel top)
    {
        top.Controls.Add(coverageButton);
        var tip = new ToolTip { AutoPopDelay = 30000 }; L.SetToolTip(tip, coverageButton, CoverageDescription);
        FormClosed += (_, _) => tip.Dispose();
        coverageButton.Click += async (_, _) =>
        {
            if (datasetIndexPath is not { } path) { L.Message(this, "Zuerst einen Datensatzbaum erstellen.", "Opening Coverage Score"); return; }
            var cancellation = CancellationTokenSource.CreateLinkedTokenSource(analysis.Token);
            var token = cancellation.Token;
            var window = new LocalizedForm { Text = "Opening Coverage Score", Size = new(850, 620), BackColor = BackColor, ForeColor = ForeColor };
            var heading = new Label { Dock = DockStyle.Top, Height = 130, Padding = new(14), Text = L.T("Abdeckung wird berechnet …") };
            var list = new BranchGameList { Dock = DockStyle.Fill, ItemHeight = 34 };
            window.Controls.Add(list); window.Controls.Add(heading);
            window.FormClosed += (_, _) => { cancellation.Cancel(); cancellation.Dispose(); }; window.Show();
            coverageButton.Enabled = false;
            try
            {
                var snapshot = BranchEditSession.Clone(opening);
                var report = await Task.Run(() => { using var index = new DatasetIndex(path); return OpeningCoverage.Calculate(snapshot, index.Children, token); }, token);
                if (window.IsDisposed) return;
                heading.Text = (report.Score is double score ? $"Opening Coverage Score: {score:0.0}/100" : L.T("Keine ausreichenden Datensatzbelege.")) + "\n" + L.T($"{report.ObservedPositions} beobachtete Gegnerstellungen · {report.UnknownPositions} ohne Daten") + "\n" + L.T(CoverageDescription);
                list.Items.AddRange(report.Gaps.Select(g => (object)new CoverageRow(g)).ToArray());
                void OpenGap()
                {
                    if (list.SelectedItem is not CoverageRow row || IsDisposed) return;
                    var gap = row.Gap;
                    window.Close(); modeSelector.SelectedIndex = (int)BranchMode.Editing; ChangeBranchMode(BranchMode.Editing);
                    var position = Position.FromFen(gap.Fen); SelectEditedPosition(position); ResetDraft(position);
                    var move = MoveGenerator.GenerateLegal(position).First(m => m.Uci == gap.Move); ApplyDraftMove(move, true);
                }
                list.MouseClick += (_, e) => { if (list.IndexFromPoint(e.Location) >= 0) OpenGap(); };
                list.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; OpenGap(); } };

            }
            catch (OperationCanceledException) { }
            catch (Exception ex) when (ex is IOException or InvalidDataException) { if (!window.IsDisposed) heading.Text = ex.Message; }
            finally { if (!IsDisposed) coverageButton.Enabled = true; }
        };
    }
}
