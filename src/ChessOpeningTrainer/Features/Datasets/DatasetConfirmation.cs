namespace ChessOpeningTrainer;

/// <summary>App-language buttons, independent of Windows' MessageBox language.</summary>
internal static class DatasetConfirmation
{
    internal static bool Ask(Form owner, string text, string title)
    {
        using var dialog = new LocalizedForm { Text = L.T(title), StartPosition = FormStartPosition.CenterParent,
            ClientSize = new(490, 195), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false,
            BackColor = UiPalette.FromArgb(31, 35, 39), ForeColor = UiPalette.Gainsboro };
        var label = new Label { Text = L.T(text), Bounds = new(20, 18, 450, 110) };
        var yes = DatasetControls.Button("Ja", () => { dialog.DialogResult = DialogResult.Yes; dialog.Close(); }, 120);
        var no = DatasetControls.Button("Nein", () => { dialog.DialogResult = DialogResult.No; dialog.Close(); }, 120);
        yes.Location = new(214, 140); no.Location = new(346, 140);
        dialog.Controls.AddRange([label, yes, no]); dialog.CancelButton = no;
        dialog.Shown += (_, _) => no.Focus();
        return dialog.ShowDialog(owner) == DialogResult.Yes;
    }
}
