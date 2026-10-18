using System.Text.Json;
using System.Text.RegularExpressions;

namespace ChessOpeningTrainer;
public class LocalizedForm : Form
{
    private sealed class TextState
    {
        public string Source = "";
        public bool Busy;
    }

    private readonly Dictionary<Control, TextState> texts = [];
    private readonly List<NativeControlTheme> nativeThemes = [];
    private string language = Preferences.Current.Language;
    private UiThemeDefinition appearance = UiPalette.Current;
    public LocalizedForm()
    {
        using (var stream = typeof(LocalizedForm).Assembly.GetManifestResourceStream("ChessOpeningTrainer.app.ico"))
            if (stream is not null) Icon = new Icon(stream);
        Attach(this);
        Preferences.Changed += RefreshLanguage;
        FormClosed += (_, _) => Preferences.Changed -= RefreshLanguage;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        WindowTitleTheme.Apply(this);
    }

    private void Attach(Control control)
    {
        if (texts.ContainsKey(control))
            return;
        var state = new TextState
        {
            Source = control.Text
        };
        texts[control] = state;
        control.BackColor = UiPalette.InitialControlColor(control.BackColor);
        control.ForeColor = UiPalette.InitialControlColor(control.ForeColor);
        if (control is ComboBox or UpDownBase or ListBox || control is ScrollableControl { AutoScroll: true })
            nativeThemes.Add(new NativeControlTheme(control));
        if (control is TextBoxBase or UpDownBase or ComboBox)
        {
            control.BackColor = UiPalette.FromArgb(43, 49, 55);
            control.ForeColor = UiPalette.Gainsboro;
        }
        if (control is Label label) label.UseMnemonic = false;
        if (control is CheckBox check) check.UseMnemonic = false;
        if (control is Button literalButton) literalButton.UseMnemonic = false;
        if (control is Button themedButton)
        {
            bool applyingInk = false;
            void ReadableInk()
            {
                if (applyingInk || themedButton.IsDisposed || !UiPalette.LightBackground || themedButton.ForeColor != Color.White) return;
                applyingInk = true;
                try { themedButton.ForeColor = UiPalette.ButtonText; }
                finally { applyingInk = false; }
            }
            themedButton.ForeColorChanged += (_, _) => ReadableInk();
            ReadableInk();
        }
        if (control.ForeColor == Color.White) control.ForeColor = UiPalette.Gainsboro;
        if (control is Button button && button.ForeColor == SystemColors.ControlText)
        {
            button.ForeColor = UiPalette.Gainsboro;
            button.BackColor = UiPalette.FromArgb(57, 91, 119);
            button.FlatStyle = FlatStyle.Flat;
        }

        if (control is ComboBox combo)
        {
            combo.FlatStyle = FlatStyle.Flat;
            if (combo.DrawMode == DrawMode.Normal)
            {
                combo.DrawMode = DrawMode.OwnerDrawFixed;
                combo.DrawItem += (_, e) =>
                {
                    using var background = new SolidBrush((e.State & DrawItemState.Selected) != 0 ? UiPalette.FromArgb(57, 91, 119) : UiPalette.FromArgb(43, 49, 55));
                    e.Graphics.FillRectangle(background, e.Bounds);
                    string text = (e.Index >= 0 ? combo.GetItemText(combo.Items[e.Index]) : combo.Text) ?? "";
                    TextRenderer.DrawText(e.Graphics, text, combo.Font, e.Bounds, UiPalette.Gainsboro, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
                };
            }
            combo.FormattingEnabled = true;
            combo.Format += (_, e) => e.Value = L.T(e.Value?.ToString() ?? "");
        }
        else if (control is ListBox list)
        {
            list.FormattingEnabled = true;
            list.Format += (_, e) => e.Value = L.T(e.Value?.ToString() ?? "");
        }
        else if (control is not TextBoxBase and not UpDownBase and not DateTimePicker)
        {
            control.TextChanged += (_, _) =>
            {
                if (state.Busy)
                    return;
                state.Source = control.Text;
                Apply(control, state);
            };
            Apply(control, state);
        }

        control.ControlAdded += (_, e) =>
        {
            if (e.Control is not null)
                Attach(e.Control);
        };
        control.Disposed += (_, _) => texts.Remove(control);
        foreach (Control child in control.Controls)
            Attach(child);
    }

    private static void Apply(Control c, TextState s)
    {
        if (c.IsDisposed)
            return;
        s.Busy = true;
        try
        {
            c.Text = L.T(s.Source);
        }
        finally
        {
            s.Busy = false;
        }
    }

    private void RefreshLanguage()
    {
        if (appearance != UiPalette.Current)
        {
            UiPalette.ApplyTree(this, appearance);
            appearance = UiPalette.Current;
            foreach (var theme in nativeThemes) theme.RefreshTheme();
            WindowTitleTheme.Apply(this);
        }

        if (language == Preferences.Current.Language)
            return;
        language = Preferences.Current.Language;
        foreach (var(c, s)in texts.ToArray())
        {
            if (c is ComboBox or ListBox)
            {
                c.Refresh();
                continue;
            }

            if (c is not TextBoxBase and not UpDownBase and not DateTimePicker)
                Apply(c, s);
            c.Invalidate();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Preferences.Changed -= RefreshLanguage;
            foreach (var theme in nativeThemes) theme.Dispose();
            nativeThemes.Clear();
        }
        base.Dispose(disposing);
    }
}
