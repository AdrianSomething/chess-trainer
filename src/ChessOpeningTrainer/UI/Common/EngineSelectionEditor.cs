namespace ChessOpeningTrainer;

internal static class EngineSelectionEditor
{
    public static Control Create()
    {
        var panel=new FlowLayoutPanel{Width=480,Height=100,WrapContents=true};
        var title=new Label{Text="Bewertungsengine",Width=160,Height=30,TextAlign=ContentAlignment.MiddleLeft};
        var choices=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=220};
        var remove=new Button{Text="Engine löschen",Width=140,Height=30};
        var note=new Label{Text="Neuronale Modelle liefern Policy-/Wertvorhersagen ohne Stockfish-Tiefensuche. Stockfish bleibt für Generatoren und überprüfte Aufgaben verfügbar.",Width=465,Height=48,ForeColor=UiPalette.Gainsboro};
        var models=EngineRegistry.List();choices.Items.Add("Stockfish");foreach(var model in models)choices.Items.Add(model);
        choices.SelectedIndex=Math.Max(0,models.ToList().FindIndex(m=>m.Id==Preferences.Current.EvaluationEngine)+1);
        void Changed(){Preferences.Current.EvaluationEngine=choices.SelectedItem is SavedEngine model?model.Id:"stockfish";remove.Enabled=choices.SelectedItem is SavedEngine;Preferences.Save();}
        choices.DropDown+=(_,_)=>
        {
            models=EngineRegistry.List();choices.Items.Clear();choices.Items.Add("Stockfish");foreach(var item in models)choices.Items.Add(item);
            choices.SelectedIndex=Math.Max(0,models.ToList().FindIndex(m=>m.Id==Preferences.Current.EvaluationEngine)+1);remove.Enabled=choices.SelectedItem is SavedEngine;
        };
        choices.SelectionChangeCommitted+=(_,_)=>Changed();remove.Enabled=choices.SelectedItem is SavedEngine;
        remove.Click+=(_,_)=>
        {
            if(choices.SelectedItem is not SavedEngine model)return;
            if(L.Message(panel,"Diese gespeicherte Engine löschen? Stockfish bleibt erhalten.","Engine löschen",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
            AnalysisSearch.Shutdown();EngineRegistry.Delete(model);choices.Items.Remove(model);choices.SelectedIndex=0;Changed();
        };
        var tips=new ToolTip{AutoPopDelay=20000};L.SetToolTip(tips,choices,"Stockfish oder ein im Engine-Labor gespeichertes Modell. Die Auswahl betrifft Stellungsbewertung und automatische Gegnerzüge, nicht die Stockfish-Prüfung von Lernkarten und Rätseln.");panel.Disposed+=(_,_)=>tips.Dispose();
        panel.Controls.AddRange([title,choices,remove,note]);panel.Height=140;return panel;
    }
}
