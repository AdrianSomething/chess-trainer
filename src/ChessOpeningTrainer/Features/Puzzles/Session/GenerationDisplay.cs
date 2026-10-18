namespace ChessOpeningTrainer;

public sealed partial class PuzzleSessionForm
{
    private long generationFrame = -1;
    private readonly Label generationDisplay = new()
    {
        Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter,
        BackColor = Color.FromArgb(31, 35, 39), ForeColor = Color.White,
        Font = new Font("Segoe UI", 13), Padding = new Padding(18), Visible = false
    };
    private void UpdateGenerationDisplay()
    {
        generationDisplay.Visible = loading;
        if (!loading) return;
        generationDisplay.BringToFront();
        if (generationFrame == tick.ElapsedMilliseconds / 400) return;
        generationFrame = tick.ElapsedMilliseconds / 400;
        generationDisplay.Text = "Neue Aufgabe wird generiert und geprüft" + new string('.', 1 + (int)(tick.ElapsedMilliseconds / 400 % 3))
            + "\n\nDu kannst mit Zurück die Einstellungen wechseln."
            + (Preferences.Current.DebugInformation ? "\n\n" + bank.Diagnostics
                + $"\nAktive Stockfish-Suchen (alle Rätselquellen): {supply.Budget.Active} · Threads je Suche: {supply.Budget.Threads}"
                + "\nRegel-/Mattprüfungen laufen direkt in der App, ohne Stockfish-Prozess."
                : "");
    }
}
