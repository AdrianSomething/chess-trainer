namespace ChessOpeningTrainer;

/// <summary>A single-parameter reset, using the same change event as editing its value.</summary>
internal static class SettingsReset
{
    public static Control Wrap(Control input, object defaultValue)
    {
        object? Read() => input switch
        {
            NumericUpDown n => n.Value, TrackBar t => t.Value, CheckBox c => c.Checked,
            ComboBox c => defaultValue is int ? c.SelectedIndex : c.SelectedItem, _ => null
        };
        void Reset()
        {
            switch (input)
            {
                case NumericUpDown n: n.Value = Math.Clamp(Convert.ToDecimal(defaultValue), n.Minimum, n.Maximum); break;
                case TrackBar t: t.Value = Math.Clamp(Convert.ToInt32(defaultValue), t.Minimum, t.Maximum); break;
                case CheckBox c: c.Checked = (bool)defaultValue; break;
                case ComboBox c when defaultValue is int i: c.SelectedIndex = i; break;
                case ComboBox c: c.SelectedItem = defaultValue; break;
            }
        }
        object normalized = input is NumericUpDown ? Convert.ToDecimal(defaultValue) : defaultValue;
        return Wrap(input, () => !Equals(Read(), normalized), Reset);
    }
    public static Control Wrap(Control input, Func<bool> changed, Action reset)
    {
        var button = new ResetButton { AccessibleName = "Auf Standard zurücksetzen" };
        var row = new ResetRow(input, button);
        var tip = new ToolTip(); L.SetToolTip(tip, button, "Diesen Parameter auf den Standard zurücksetzen");
        void Update()
        {
            if (row.IsDisposed || button.IsDisposed || input.IsDisposed) return;
            if (row.InvokeRequired)
            {
                try { row.BeginInvoke((Action)Update); }
                catch (InvalidOperationException) { }
                return;
            }
            bool different = changed();
            button.Visible = different;
            button.Enabled = different;
        }
        button.Click += (_, _) => { reset(); Update(); };
        Preferences.Changed += Update;
        input.TextChanged += (_, _) => Update();
        if (input is CheckBox check) check.CheckedChanged += (_, _) => Update();
        if (input is NumericUpDown number) number.ValueChanged += (_, _) => Update();
        if (input is TrackBar track) track.ValueChanged += (_, _) => Update();
        if (input is ComboBox combo) combo.SelectedIndexChanged += (_, _) => Update();
        row.Disposed += (_, _) => { Preferences.Changed -= Update; tip.Dispose(); };
        Update(); return row;
    }

    /// <summary>The same small, unboxed ↶ glyph used by the engine PV rows.</summary>
    private sealed class ResetButton : MenuIconButton
    {
        public ResetButton() : base("reload", "↶")
        {
            Font = Control.DefaultFont;
            Padding = Padding.Empty;
            Margin = new(2, 2, 2, 0);
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            Size = GetPreferredSize(Size.Empty);
        }
        public override Size GetPreferredSize(Size proposedSize) => TextRenderer.MeasureText("↶", Font);
        protected override void OnPaint(PaintEventArgs e)
        {
            // Do not paint Button chrome, focus boxes, or the larger menu icon.
            // Use the parent background so the glyph has no visible rectangle.
            e.Graphics.Clear(Parent?.BackColor ?? BackColor);
            TextRenderer.DrawText(e.Graphics, "↶", Font, ClientRectangle, UiPalette.Gainsboro,
                TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.NoPrefix);
        }
    }

    /// <summary>
    /// Bounds inputs after font/DPI/parent sizing, instead of treating a
    /// TrackBar's AutoSize height as an unbounded automatic row width.
    /// </summary>
    private sealed class ResetRow : Panel
    {
        private readonly Control input;
        private readonly ResetButton button;
        private readonly bool textSized;
        private readonly Size originalMaximum;
        private int requestedWidth;
        private bool layingOut;
        private Control? watchedParent;

        public ResetRow(Control input, ResetButton button)
        {
            this.input = input; this.button = button;
            textSized = input.AutoSize && input is ButtonBase;
            originalMaximum = input.MaximumSize;
            requestedWidth = Math.Max(100, input.Width);
            DoubleBuffered = true; Margin = input.Margin;
            Width = requestedWidth; Height = Math.Max(32, input.Height + 4);
            input.Margin = Padding.Empty;
            Controls.AddRange([button, input]);
            input.SizeChanged += InputChanged;
            input.FontChanged += InputChanged;
            input.TextChanged += InputChanged;
        }

        private void InputChanged(object? sender, EventArgs e)
        {
            if (!layingOut && !IsDisposed) PerformLayout();
        }

        protected override void OnParentChanged(EventArgs e)
        {
            if (watchedParent is not null) watchedParent.ClientSizeChanged -= InputChanged;
            watchedParent = Parent;
            if (watchedParent is not null) watchedParent.ClientSizeChanged += InputChanged;
            base.OnParentChanged(e);
            PerformLayout();
        }

        private int? AvailableWidth(Control child)
        {
            var parent = child.Parent;
            if (parent is null) return null;
            int? total = parent.AutoSize ? AvailableWidth(parent) : parent.ClientSize.Width;
            if (total is null) return null;
            int available = total.Value - parent.Padding.Horizontal - child.Margin.Horizontal;
            if (parent is FlowLayoutPanel { FlowDirection: FlowDirection.LeftToRight, WrapContents: false })
                foreach (Control sibling in parent.Controls)
                {
                    if (sibling == child) break;
                    if (sibling.Visible) available -= sibling.Width + sibling.Margin.Horizontal;
                }
            return available;
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            if (layingOut || input is null || button is null) return;
            layingOut = true;
            try
            {
                Size icon = button.GetPreferredSize(Size.Empty);
                int inset = icon.Width + 4;
                if (textSized) input.MaximumSize = originalMaximum;
                Size preferred = input.GetPreferredSize(Size.Empty);
                int desired = textSized ? Math.Max(100, preferred.Width + inset) : requestedWidth;
                int width = Math.Max(inset + Math.Max(1, input.MinimumSize.Width), Math.Min(desired, AvailableWidth(this) ?? desired));
                int inputWidth = Math.Max(1, width - inset);
                if (textSized)
                {
                    input.MaximumSize = new(originalMaximum.Width == 0 ? inputWidth : Math.Min(inputWidth, originalMaximum.Width), originalMaximum.Height);
                    preferred = input.GetPreferredSize(new(inputWidth, 0));
                }
                int inputHeight = input.AutoSize || input is NumericUpDown or ComboBox ? preferred.Height : input.Height;
                int height = Math.Max(32, inputHeight + 4);
                Size = new(width, height);
                button.SetBounds(2, (height - icon.Height) / 2, icon.Width, icon.Height);
                input.SetBounds(inset, 2, inputWidth, inputHeight);
                base.OnLayout(e);
            }
            finally { layingOut = false; }
        }

        protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
        {
            if ((specified & BoundsSpecified.Width) != 0) requestedWidth = (int)Math.Round(requestedWidth * factor.Width);
            base.ScaleControl(factor, specified);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (watchedParent is not null) watchedParent.ClientSizeChanged -= InputChanged;
                input.SizeChanged -= InputChanged; input.FontChanged -= InputChanged; input.TextChanged -= InputChanged;
            }
            base.Dispose(disposing);
        }
    }
}
