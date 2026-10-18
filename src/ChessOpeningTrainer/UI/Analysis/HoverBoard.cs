namespace ChessOpeningTrainer;
public sealed class HoverBoard : Form
{
    public BoardControl Board { get; } = new()
    {
        MinimumSize = Size.Empty,
        Dock = DockStyle.Fill,
        InputEnabled = false
    };

    public HoverBoard()
    {
        StartPosition = FormStartPosition.Manual;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        ClientSize = new(240, 240);
        Controls.Add(Board);
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x08000080;
            return cp;
        }
    }
}
