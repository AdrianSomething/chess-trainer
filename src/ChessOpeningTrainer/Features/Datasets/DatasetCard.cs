namespace ChessOpeningTrainer;

internal sealed class DatasetCard : Panel
{
    public DatasetCard(DatasetEntry entry, long bytes, Action save, Action activeChanged, Action orderChanged, Action trim, Action delete)
    {
        Height = 125;
        Width = 600;
        BackColor = UiPalette.FromArgb(43, 49, 55);
        var title = new Label { Text = DatasetNames.FromFile(entry.Name), Font = new Font(SystemFonts.MessageBoxFont!.FontFamily, 13, FontStyle.Bold), Left = 12, Top = 10, Height = 30, AutoEllipsis = true, UseMnemonic = false };
        var edit = new TextBox { Visible = false, Left = 12, Top = 10, Font = title.Font, BorderStyle = BorderStyle.FixedSingle, BackColor = BackColor, ForeColor = UiPalette.Gainsboro };
        var confirm = DatasetControls.Button("✓", () => { if (edit.Text.Trim().Length > 0) { entry.Name = edit.Text.Trim(); title.Text = entry.Name; save(); } edit.Visible = confirmPlaceholder!.Visible = false; title.Visible = true; }, 36);
        confirmPlaceholder = confirm;
        RenameConfirmation.Style(confirm);
        confirm.Visible = false;
        void Cancel()
        {
            edit.Visible = confirm.Visible = false;
            title.Visible = true;
        }
        title.DoubleClick += (_, _) => { title.Visible = false; edit.Text = title.Text; edit.Visible = confirm.Visible = true; edit.Focus(); edit.SelectAll(); };
        edit.Leave += (_, _) => BeginInvoke((Action)(() => { if (!IsDisposed && !confirm.Focused) Cancel(); }));
        edit.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { confirm.PerformClick(); e.SuppressKeyPress = true; } else if (e.KeyCode == Keys.Escape) { Cancel(); e.SuppressKeyPress = true; } };
        var info = new Label { Text = $"{entry.Games:N0} " + L.T("Partien") + $" · {bytes / 1e6:0.0} MB" + (entry.Skipped > 0 ? $" · {entry.Skipped:N0} " + L.T("übersprungen") : ""), Left = 12, Top = 50, Height = 25 };
        var active = new CheckBox { Text = L.T("Aktiv"), Checked = entry.Active, Left = 12, Top = 84, Width = 100 };
        active.CheckedChanged += (_, _) => { entry.Active = active.Checked; save(); activeChanged(); };
        var bookmark = new MenuIconButton(entry.Bookmarked ? "bookmark-filled" : "bookmark") { Width = 32, Height = 32, Left = 120, Top = 80, AccessibleName = L.T("Lesezeichen") };
        bookmark.FlatAppearance.BorderSize = 0;
        bookmark.Click += (_, _) => { entry.Bookmarked = !entry.Bookmarked; save(); orderChanged(); };
        var trimButton = DatasetControls.Button("Zuschneiden", trim, 120);
        var deleteButton = DatasetControls.Button("Löschen", delete, 120);
        foreach (var b in new[] { trimButton, deleteButton })
        {
            b.BackColor = UiPalette.FromArgb(57, 91, 119);
            b.ForeColor = UiPalette.ButtonText;
        }
        Controls.AddRange([title, edit, confirm, info, active, bookmark, trimButton, deleteButton]);
        void Arrange()
        {
            title.Width = Width - 200;
            edit.Width = Width - 240;
            confirm.SetBounds(Width - 222, 8, 32, 32);
            bookmark.SetBounds(Width - 180, 10, 34, 35);
            info.Width = Width - 155;
            trimButton.SetBounds(Width - 135, 10, 120, 40);
            deleteButton.SetBounds(Width - 135, 62, 120, 40);
        }
        Resize += (_, _) => Arrange();
        Arrange();
        if (entry.ImportIssues.Count > 0)
        {
            var tip = new ToolTip();
            tip.SetToolTip(info, string.Join("\n", entry.ImportIssues.Select(p => $"{p.Key}: {p.Value:N0}")));
            Disposed += (_, _) => tip.Dispose();
        }
    }
    private readonly Button? confirmPlaceholder;
}
