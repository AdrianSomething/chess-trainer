namespace ChessOpeningTrainer;

public sealed class ColorWheelDialog : LocalizedForm
{
    private readonly Wheel wheel;
    private readonly TrackBar brightness = new() { Dock = DockStyle.Bottom, Minimum = 0, Maximum = 100, TickStyle = TickStyle.None, Height = 36 };
    public Color SelectedColor => wheel.SelectedColor;
    public event Action<Color>? ColorChanged;
    public ColorWheelDialog(Color initial)
    {
        Text = "Farbe auswählen"; ClientSize = new(330, 395); FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false; StartPosition = FormStartPosition.CenterParent;
        BackColor = UiPalette.FromArgb(31, 35, 39); ForeColor = UiPalette.Gainsboro;
        wheel = new(initial) { Dock = DockStyle.Fill };
        wheel.ColorChanged += () => ColorChanged?.Invoke(SelectedColor);
        brightness.Value = (int)(Math.Max(initial.R, Math.Max(initial.G, initial.B)) / 255d * 100);
        brightness.ValueChanged += (_, _) => { wheel.Value = brightness.Value / 100d; wheel.Rebuild(); ColorChanged?.Invoke(SelectedColor); };
        var accept = new Button { Text = "Übernehmen", DialogResult = DialogResult.OK, Dock = DockStyle.Bottom, Height = 38 };
        Controls.AddRange([wheel, brightness, accept]); AcceptButton = accept;
    }
    private sealed class Wheel : Control
    {
        private double hue, saturation;
        public double Value;
        private Bitmap? cache;
        private CancellationTokenSource? rendering;
        public event Action? ColorChanged;
        public Color SelectedColor => Hsv(hue, saturation, Value);
        public Wheel(Color color)
        {
            DoubleBuffered = true; hue = color.GetHue();
            double max = Math.Max(color.R, Math.Max(color.G, color.B)), min = Math.Min(color.R, Math.Min(color.G, color.B));
            Value = max / 255; saturation = max == 0 ? 0 : 1 - min / max;
            Resize += (_, _) => Rebuild();
        }
        protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); Rebuild(); }
        private static Color Hsv(double h, double s, double v)
        {
            double c = v * s, x = c * (1 - Math.Abs(h / 60 % 2 - 1)), m = v - c;
            (double r, double g, double b) = h switch { < 60 => (c, x, 0d), < 120 => (x, c, 0d), < 180 => (0d, c, x), < 240 => (0d, x, c), < 300 => (x, 0d, c), _ => (c, 0d, x) };
            return Color.FromArgb((int)Math.Round((r + m) * 255), (int)Math.Round((g + m) * 255), (int)Math.Round((b + m) * 255));
        }
        public async void Rebuild()
        {
            if (!IsHandleCreated || IsDisposed) return;
            rendering?.Cancel();
            var request = new CancellationTokenSource();
            rendering = request;
            int size = Math.Max(2, Math.Min(Width, Height) - 20);
            double value = Value;
            Bitmap? next = null;
            try
            {
                next = await Task.Run(() => RenderWheel(size, value, request.Token), request.Token);
                if (IsDisposed || request.IsCancellationRequested || rendering != request) return;
                var previous = cache; cache = next; next = null; previous?.Dispose(); Invalidate();
            }
            catch (OperationCanceledException) { }
            finally { next?.Dispose(); if (rendering == request) rendering = null; request.Dispose(); }
        }
        private static Bitmap RenderWheel(int size, double value, CancellationToken token)
        {
            var bitmap = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            try
            {
                var pixels = new int[size * size];
                double radius = (size - 1) / 2d;
                for (int y = 0; y < size; y++)
                {
                    token.ThrowIfCancellationRequested();
                    for (int x = 0; x < size; x++)
                    {
                        double dx = x - radius, dy = y - radius, s = Math.Sqrt(dx * dx + dy * dy) / radius;
                        if (s <= 1) pixels[y * size + x] = Hsv((Math.Atan2(dy, dx) * 180 / Math.PI + 360) % 360, s, value).ToArgb();
                    }
                }
                var bits = bitmap.LockBits(new(0, 0, size, size), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                try { System.Runtime.InteropServices.Marshal.Copy(pixels, 0, bits.Scan0, pixels.Length); }
                finally { bitmap.UnlockBits(bits); }
                return bitmap;
            }
            catch { bitmap.Dispose(); throw; }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            if (cache is not null) e.Graphics.DrawImageUnscaled(cache, (Width - cache.Width) / 2, (Height - cache.Height) / 2);
            float radius = (Math.Max(2, Math.Min(Width, Height) - 20) - 1) / 2f, x = Width / 2f + (float)(Math.Cos(hue * Math.PI / 180) * saturation * radius), y = Height / 2f + (float)(Math.Sin(hue * Math.PI / 180) * saturation * radius);
            using var outer = new Pen(Color.Black, 3); e.Graphics.DrawEllipse(outer, x - 5, y - 5, 10, 10);
            e.Graphics.DrawEllipse(Pens.White, x - 5, y - 5, 10, 10);
        }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { Capture = true; Choose(e); } base.OnMouseDown(e); }
        protected override void OnMouseMove(MouseEventArgs e) { if (Capture) Choose(e); base.OnMouseMove(e); }
        protected override void OnMouseUp(MouseEventArgs e) { Capture = false; base.OnMouseUp(e); }
        private void Choose(MouseEventArgs e)
        {
            double dx = e.X - Width / 2d, dy = e.Y - Height / 2d;
            hue = (Math.Atan2(dy, dx) * 180 / Math.PI + 360) % 360;
            saturation = Math.Clamp(Math.Sqrt(dx * dx + dy * dy) / Math.Max(1, (Math.Min(Width, Height) - 21) / 2d), 0, 1);
            Invalidate(); ColorChanged?.Invoke();
        }
        protected override void Dispose(bool disposing) { if (disposing) { rendering?.Cancel(); cache?.Dispose(); } base.Dispose(disposing); }
    }
}
