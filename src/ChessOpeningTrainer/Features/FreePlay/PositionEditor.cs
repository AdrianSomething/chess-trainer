namespace ChessOpeningTrainer;

// Dialog adapter; Freeplay embeds the same editor without opening a second window.
public sealed class PositionEditor : LocalizedForm
{
    private readonly PositionSetupControl editor;
    public Position Result => editor.Result;
    public static string? Validate(Position position) => PositionSetupControl.Validate(position);

    public PositionEditor(Position? initial = null)
    {
        Text = "Brett aufstellen · Freies Spiel";
        Size = new(1120, 840);
        MinimumSize = new(900, 680);
        editor = new(initial) { Dock = DockStyle.Fill };
        Controls.Add(editor);
        editor.Accepted += _ => { DialogResult = DialogResult.OK; Close(); };
    }
}
