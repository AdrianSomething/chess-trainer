using System.Globalization;

namespace ChessOpeningTrainer;

/// <summary>Date value is independent of localized button text; calendar uses the app palette.</summary>
public sealed class ThemedDatePicker : Button
{
    private DateTime value = DateTime.Today;
    private CalendarPopup? popup;
    public event EventHandler? ValueChanged;
    public DateTime Value
    {
        get => value;
        set { if (this.value == value.Date) return; this.value = value.Date; UpdateText(); ValueChanged?.Invoke(this, EventArgs.Empty); }
    }
    public ThemedDatePicker()
    {
        FlatStyle = FlatStyle.Flat; Height = 28; Width = 120;
        BackColor = UiPalette.FromArgb(43, 49, 55); ForeColor = UiPalette.Gainsboro;
        AccessibleName = "Datum auswählen"; UpdateText();
    }
    private void UpdateText() => Text = value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + " ▾";
    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        if (popup is { IsDisposed: false }) { popup.Activate(); return; }
        popup = new CalendarPopup(value, date => Value = date);
        var point = PointToScreen(new Point(0, Height));
        var area = Screen.FromControl(this).WorkingArea;
        popup.Location = new(Math.Clamp(point.X, area.Left, Math.Max(area.Left, area.Right - popup.Width)),
            Math.Clamp(point.Y, area.Top, Math.Max(area.Top, area.Bottom - popup.Height)));
        if (FindForm() is Form owner) popup.Show(owner); else popup.Show();
    }
    protected override void Dispose(bool disposing) { if (disposing) popup?.Dispose(); base.Dispose(disposing); }

    private sealed class CalendarPopup : LocalizedForm
    {
        public CalendarPopup(DateTime initial, Action<DateTime> choose)
        {
            Text = "Datum auswählen"; ClientSize = new(286, 262); StartPosition = FormStartPosition.Manual;
            FormBorderStyle = FormBorderStyle.FixedToolWindow; ShowInTaskbar = false; MaximizeBox = MinimizeBox = false;
            BackColor = UiPalette.FromArgb(43, 49, 55); ForeColor = UiPalette.Gainsboro;
            var grid = new CalendarGrid(initial) { Dock = DockStyle.Fill };
            grid.Chosen += date => { choose(date); Close(); };
            Controls.Add(grid); Deactivate += (_, _) => Close();
        }
    }
    private sealed class CalendarGrid : Control
    {
        private readonly DateTime selected;
        private DateTime month;
        private int hover = -1;
        public event Action<DateTime>? Chosen;
        private const int TopHeight = 62, CellHeight = 31;
        public CalendarGrid(DateTime initial) { selected = initial.Date; month = new(initial.Year, initial.Month, 1); DoubleBuffered = true; }
        private DateTime First => month.AddDays(-((int)month.DayOfWeek + 6) % 7);
        private DateTime? DayAt(int cell) => First <= DateTime.MaxValue.AddDays(-cell) ? First.AddDays(cell) : null;
        private int Cell(Point point) => point.Y < TopHeight ? -1 : Math.Clamp(point.X * 7 / Math.Max(1, Width), 0, 6) + (point.Y - TopHeight) / CellHeight * 7;
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(UiPalette.FromArgb(43, 49, 55));
            var culture = CultureInfo.GetCultureInfo(Preferences.Current.Language == "de" ? "de-DE" : "en-US");
            TextRenderer.DrawText(e.Graphics, month.ToString("MMMM yyyy", culture), Font, new Rectangle(34, 3, Width - 68, 30), UiPalette.Gainsboro, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(e.Graphics, "‹", Font, new Rectangle(4, 3, 28, 30), UiPalette.Gainsboro, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(e.Graphics, "›", Font, new Rectangle(Width - 32, 3, 28, 30), UiPalette.Gainsboro, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            for (int i = 0; i < 7; i++)
                TextRenderer.DrawText(e.Graphics, culture.DateTimeFormat.AbbreviatedDayNames[(i + 1) % 7], Font, new Rectangle(i * Width / 7, 34, Width / 7, 25), UiPalette.FromArgb(155, 163, 171), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            for (int i = 0; i < 42; i++)
            {
                if (DayAt(i) is not DateTime day) continue;
                var bounds = new Rectangle(i % 7 * Width / 7, TopHeight + i / 7 * CellHeight, Width / 7, CellHeight);
                using var fill = new SolidBrush(day == selected ? UiPalette.FromArgb(57, 91, 119) : i == hover ? UiPalette.FromArgb(58, 66, 74) : UiPalette.FromArgb(43, 49, 55));
                e.Graphics.FillRectangle(fill, Rectangle.Inflate(bounds, -2, -2));
                TextRenderer.DrawText(e.Graphics, day.Day.ToString(culture), Font, bounds, day.Month == month.Month ? UiPalette.Gainsboro : UiPalette.FromArgb(155, 163, 171), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }
        protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); int cell = Cell(e.Location); if (cell != hover) { hover = cell; Invalidate(); } }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = -1; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e); if (e.Button != MouseButtons.Left) return;
            if (e.Y < 34)
            {
                if (e.X < 34 && (month.Year > 1 || month.Month > 1)) month = month.AddMonths(-1);
                else if (e.X > Width - 34 && (month.Year < 9999 || month.Month < 12)) month = month.AddMonths(1);
                Invalidate(); return;
            }
            int cell = Cell(e.Location); if (cell is >= 0 and < 42 && DayAt(cell) is DateTime date) Chosen?.Invoke(date);
        }
    }
}
