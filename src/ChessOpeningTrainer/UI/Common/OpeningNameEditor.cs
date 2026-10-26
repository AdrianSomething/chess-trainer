namespace ChessOpeningTrainer;

public static class OpeningNameEditor
{
    public static void Attach(Label label, OpeningDefinition opening)
    {
        label.DoubleClick += (_, _) =>
        {
            if (label.Parent is not { } parent || !label.Visible) return;
            var input = new TextBox { Text = opening.Name, Bounds = label.Bounds, Width = Math.Max(80, label.Width - 44) };
            var accept = new Button { Bounds = new(input.Right + 4, input.Top, input.Height, input.Height) };
            RenameConfirmation.Style(accept);
            label.Visible = false;
            parent.Controls.AddRange([input, accept]); input.BringToFront(); accept.BringToFront();
            OutsideClickCancel? guard = null;
            bool closed = false;
            void Close()
            {
                if (closed) return;
                closed = true; guard?.Dispose(); input.Dispose(); accept.Dispose();
                if (!label.IsDisposed) label.Visible = true;
            }
            guard = new(input, accept, Close);
            Application.AddMessageFilter(guard);
            input.Disposed += (_, _) => guard.Dispose();
            void Save()
            {
                string name = input.Text.Trim();
                if (name.Length == 0) return;
                string previous = opening.Name;
                try { opening.Name = name; OpeningStore.Save(opening, opening.FilePath); label.Text = name; Close(); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
                { opening.Name = previous; MessageBox.Show(parent, ex.Message, L.T("Opening umbenennen")); }
            }
            accept.Click += (_, _) => Save();
            input.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Save(); }
                else if (e.KeyCode == Keys.Escape) { e.SuppressKeyPress = true; Close(); }
            };
            input.Focus(); input.SelectAll();
        };
    }
}
