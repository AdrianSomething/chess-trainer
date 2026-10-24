using ChessOpeningTrainer;

internal static class Program
{
    private static int checks;
    internal static void Check(bool condition, string description)
    {
        if (!condition) throw new Exception(description);
        checks++;
    }
    [STAThread]
    private static int Main(string[] args)
    {
        string root = Path.Combine(Environment.CurrentDirectory, "work", "public-checks-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Environment.SetEnvironmentVariable("CHESS_TRAINER_DATA", root);
        try
        {
            Preferences.Initialize(root);
            if (args.Length == 0 || args[0] == "rules") { RulesChecks.Run(); BoardColorChecks.Run(); }
            if (args.Length == 0 || args[0] == "datasets") StorageChecks.Run(root);
            Console.WriteLine($"PASS: {checks} synthetic checks.");
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
        finally { Directory.Delete(root, true); }
    }
}
