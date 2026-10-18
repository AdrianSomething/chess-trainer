using System.Runtime.InteropServices;

namespace ChessOpeningTrainer;

internal static class WindowTitleTheme
{
    public static bool Apply(Form form)
    {
        if (!form.IsHandleCreated || form.IsDisposed || !form.TopLevel) return false;
        var background = UiPalette.FromArgb(31, 35, 39);
        int dark = background.GetBrightness() < .55f ? 1 : 0;
        int caption = background.R | background.G << 8 | background.B << 16;
        var text = UiPalette.Gainsboro;
        int foreground = text.R | text.G << 8 | text.B << 16;
        DwmSetWindowAttribute(form.Handle, 20, ref dark, sizeof(int));
        int captionResult = DwmSetWindowAttribute(form.Handle, 35, ref caption, sizeof(int));
        int textResult = DwmSetWindowAttribute(form.Handle, 36, ref foreground, sizeof(int));
        return captionResult >= 0 && textResult >= 0;
    }
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
}
