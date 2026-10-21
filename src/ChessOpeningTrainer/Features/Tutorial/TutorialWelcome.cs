namespace ChessOpeningTrainer;

internal sealed class TutorialWelcome : Panel, IMessageFilter
{
    private readonly Panel card = new() { BackColor = Color.Black };
    public TutorialWelcome(Action start)
    {
        Dock = DockStyle.Fill; BackColor = Color.FromArgb(35, 38, 42);
        var language = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        language.Items.AddRange(L.Languages.Cast<object>().ToArray());
        language.SelectedItem = L.Languages.FirstOrDefault(l => l.Id == Preferences.Current.Language);
        var label = new Label { ForeColor = Color.White, Font = new("Segoe UI", 22, FontStyle.Bold) };
        Button yes = TutorialOverlay.MakeButton(""), no = TutorialOverlay.MakeButton(""), close = TutorialOverlay.MakeButton("×");
        void Translate() { label.Text = L.T("Neu hier? Brauchst du einen Durchgang?"); yes.Text = L.T("Gib mir einen Überblick!"); no.Text = L.T("Nein."); }
        language.SelectedIndexChanged += (_, _) =>
        {
            if (language.SelectedItem is not L.Language selected) return;
            Preferences.Current.Language = selected.Id; Preferences.Save(); Translate();
        };
        card.Controls.AddRange([language, label, yes, no, close]); Controls.Add(card);
        void Arrange()
        {
            int width = Math.Min(580, Math.Max(280, Width - 36));
            card.SetBounds((Width - width) / 2, Math.Max(12, (Height - 270) / 2), width, 270);
            label.SetBounds(24, 28, width - 70, 105); language.SetBounds(24, 145, width - 48, 30);
            yes.SetBounds(24, 207, width - 164, 40); no.SetBounds(width - 128, 207, 104, 40);
            close.SetBounds(width - 38, 6, 30, 28);
        }
        Resize += (_, _) => Arrange();
        yes.Click += (_, _) => { Dismiss(); start(); }; no.Click += (_, _) => Dismiss(); close.Click += (_, _) => Dismiss();
        Translate(); Arrange(); Application.AddMessageFilter(this);
    }
    private void Dismiss()
    {
        Preferences.Current.TutorialOffered = true; Preferences.Save(); Dispose();
    }
    public bool PreFilterMessage(ref Message m) =>
        Visible && (m.Msg is 0x0100 or 0x0101 or 0x0102) && (Keys)(int)m.WParam == Keys.Space;
    protected override void Dispose(bool disposing)
    {
        if (disposing) Application.RemoveMessageFilter(this);
        base.Dispose(disposing);
    }
}
