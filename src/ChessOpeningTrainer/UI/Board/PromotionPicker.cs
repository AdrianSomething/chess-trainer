namespace ChessOpeningTrainer;
public static class PromotionPicker
{
    public static Move? Choose(IWin32Window owner, IReadOnlyList<Move> moves, bool white)
    {
        using var dialog = new LocalizedForm
        {
            Text = "Bauer umwandeln",
            ClientSize = new(344, 94),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            StartPosition = FormStartPosition.CenterParent,
            BackColor = UiPalette.FromArgb(31, 35, 39)
        };
        Move? chosen = null;
        int x = 8;
        foreach (var m in moves)
        {
            var b = new Button
            {
                Left = x,
                Top = 8,
                Width = 76,
                Height = 76,
                FlatStyle = FlatStyle.Flat,
                BackColor = UiPalette.FromArgb(91, 111, 76),
                AccessibleName = m.Promotion.ToString()
            };
            x += 84;
            b.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                BoardControl.DrawPiece(e.Graphics, white ? char.ToUpperInvariant(m.Promotion) : m.Promotion, new(38, 38), 72);
            };
            b.Click += (_, _) =>
            {
                chosen = m;
                dialog.Close();
            };
            dialog.Controls.Add(b);
        }

        dialog.ShowDialog(owner);
        return chosen;
    }
}
