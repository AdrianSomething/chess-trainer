using System.Drawing.Drawing2D;
using ChessMove = ChessOpeningTrainer.Move;

namespace ChessOpeningTrainer;
internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Application.ApplicationExit += (_, _) => AnalysisSearch.Shutdown();
        try
        {
            if (args.Contains("--startup-test"))
            {
                using var form = new MainForm();
                form.CreateControl();
                using var bitmap = new Bitmap(form.Width, form.Height);
                form.DrawToBitmap(bitmap, new Rectangle(0, 0, form.Width, form.Height));
                return;
            }

            Application.Run(new MainForm());
        }
        finally
        {
            AnalysisSearch.Shutdown();
        }
    }
}
