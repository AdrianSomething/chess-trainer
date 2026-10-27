namespace ChessOpeningTrainer;

public sealed partial class BranchForm
{
    private readonly Button coverageButton = new() { Text = "Opening Coverage Score", Height = 36, Width = 397, Visible = false, FlatStyle = FlatStyle.Flat, BackColor = UiPalette.FromArgb(43,49,55), ForeColor = UiPalette.Gainsboro };
    private CoverageReport? cachedCoverage;
    private CancellationTokenSource? coverageSearch;
    private Panel? coverageArea;
    private const string CoverageDescription = "Abdeckung der beobachteten Gegnerantworten, 0–100. Gewicht = absolute Partienzahl / (1 + Abstand in vollen Zügen ab Startstellung). Eigene Züge und die feste unverzweigte Anfangssequenz zählen nicht. Unbekannte Stellungen werden separat ausgewiesen. Vorschläge zeigen den unmittelbaren Score-Gewinn bei unverändertem restlichem Baum.";
    private void InitializeCoverage(FlowLayoutPanel top)
    {
        int order=Controls.GetChildIndex(top);
        var host=new Panel { Dock=DockStyle.Top, Height=top.Height };
        coverageArea=new Panel { Dock=DockStyle.Right, Width=425, Visible=false };
        Controls.Remove(top); top.Dock=DockStyle.Fill;
        host.Controls.Add(top); host.Controls.Add(coverageArea);
        coverageButton.Location=new(12,8);coverageArea.Controls.Add(coverageButton);
        Controls.Add(host);Controls.SetChildIndex(host,order);
        var tip = new ToolTip { AutoPopDelay = 30000 }; L.SetToolTip(tip, coverageButton, CoverageDescription);
        FormClosed += (_, _) => { tip.Dispose(); coverageSearch?.Cancel(); };
        coverageButton.Click += (_, _) => ShowCoverage();
    }
    private async void RefreshCoverage()
    {
        coverageSearch?.Cancel();
        coverageSearch=null; cachedCoverage=null;
        coverageButton.Visible=branchMode==BranchMode.Analysis;
        if(coverageArea is not null)coverageArea.Visible=branchMode==BranchMode.Analysis;
        if(!coverageButton.Visible)return;
        coverageButton.BringToFront();
        if(datasetIndexPath is not {} path){coverageButton.Text="Opening Coverage Score: —";return;}
        coverageButton.Text="Opening Coverage Score: …";
        using var search = new CancellationTokenSource(); coverageSearch=search;
        var token=search.Token;
        try
        {
            var snapshot=BranchEditSession.Clone(opening);
            var report=await Task.Run(()=>{using var index=new DatasetIndex(path);return OpeningCoverage.Calculate(snapshot,index.Children,token);},token);
            if(IsDisposed || token.IsCancellationRequested)return;
            cachedCoverage=report; coverageButton.Text=report.Score is double score?$"Opening Coverage Score: {score:0.0}/100":"Opening Coverage Score: —";
        }
        catch(OperationCanceledException){}
        catch(Exception ex) when(ex is IOException or InvalidDataException){if(!IsDisposed)coverageButton.Text="Opening Coverage Score: —";System.Diagnostics.Debug.WriteLine(ex);}
        finally{if(coverageSearch==search)coverageSearch=null;}
    }
    private void ShowCoverage()
    {
        if(cachedCoverage is not {} report)return;
        var window=new LocalizedForm {Text="Opening Coverage Score",Size=new(850,620),MinimumSize=new(550,350),BackColor=UiPalette.FromArgb(31,35,39),ForeColor=UiPalette.Gainsboro};
        var heading=new Label{Dock=DockStyle.Top,Height=150,Padding=new(14),ForeColor=UiPalette.Gainsboro,BackColor=UiPalette.FromArgb(43,49,55)};
        heading.Text=(report.Score is double score?$"Opening Coverage Score: {score:0.0}/100":L.T("Keine ausreichenden Datensatzbelege."))+"\n"+L.T($"{report.ObservedPositions} beobachtete Gegnerstellungen · {report.UnknownPositions} ohne Daten")+"\n"+L.T(CoverageDescription);
        var list=new CoverageList{Dock=DockStyle.Fill,Rows=report.Gaps};
        window.Controls.Add(list);window.Controls.Add(heading);
        list.Chosen+=gap=>
        {
            if(IsDisposed)return;
            window.Close();modeSelector.SelectedIndex=(int)BranchMode.Editing;ChangeBranchMode(BranchMode.Editing);
            var p=Position.FromFen(gap.Fen);SelectEditedPosition(p);ResetDraft(p);
            ApplyDraftMove(MoveGenerator.GenerateLegal(p).First(m=>m.Uci==gap.Move),true);
        };
        window.Show();
    }
}
