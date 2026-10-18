namespace ChessOpeningTrainer;

internal sealed class VisionRunTimer : Label
{
    private readonly System.Windows.Forms.Timer refresh = new() { Interval = 100 };
    public Func<string>? ReadTime { get; set; }
    public VisionRunTimer()
    {
        AutoSize = false; Size = new(180, 38); TextAlign = ContentAlignment.MiddleRight;
        Font = new Font("Segoe UI", 18, FontStyle.Bold); UseMnemonic = false;
        refresh.Tick += (_, _) => UpdateTime();
        VisibleChanged += (_, _) => { refresh.Enabled = Visible; UpdateTime(); };
    }
    public static string Format(double seconds) => TimeSpan.FromSeconds(Math.Max(0, seconds)).ToString(@"mm\:ss\.f");
    public void UpdateTime() { string value = ReadTime?.Invoke() ?? "00:00.0"; if (Text != value) Text = value; }
    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); refresh.Enabled = Visible; UpdateTime(); }
    protected override void Dispose(bool disposing) { if (disposing) { refresh.Dispose(); Font.Dispose(); } base.Dispose(disposing); }
}
