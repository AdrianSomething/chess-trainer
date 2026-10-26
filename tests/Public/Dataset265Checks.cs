using ChessOpeningTrainer;
using System.Reflection;
using static Program;

internal static class Dataset265Checks
{
    internal static void Run(string root)
    {
        var assembly = typeof(DatasetForm).Assembly;
        var listType = assembly.GetType("ChessOpeningTrainer.DatasetDownloadList")!;
        using var list = (Control)Activator.CreateInstance(listType)!;
        list.Size = new(600, 300); list.CreateControl();
        void Call(string name, params object[] args) => listType.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.Invoke(list, args);
        var entries = Enumerable.Range(0, 200).Select(i => new DatasetDownloadOption($"Month {i}", new Uri($"https://example.invalid/{i}.pgn.zst"), 100000, 500)).ToArray();
        Call("SetOptions", (object)entries);
        Call("SelectRow", 0, Keys.None);
        using (var image = new Bitmap(600, 300))
        {
            list.DrawToBitmap(image, list.ClientRectangle);
            var color = image.GetPixel(5, 64);
            Check(color.ToArgb() == UiPalette.FromArgb(75, 85, 94).ToArgb(), "Selected dataset row uses the selection fill");
            Check(new[] { 195, 201, 390, 400, 580 }.All(x => image.GetPixel(x, 64) == color), "Selection spans column gaps without darker stripes");
        }
        Check(list.Controls.Count == 0, "Catalog virtualizes rows instead of moving hundreds of native child windows");
        var thumb = listType.GetProperty("Thumb", BindingFlags.Instance | BindingFlags.NonPublic)!;
        float before = ((RectangleF)thumb.GetValue(list)!).Y;
        Call("OnMouseWheel", new MouseEventArgs(MouseButtons.None, 0, 80, 80, -120));
        Check(((RectangleF)thumb.GetValue(list)!).Y > before, "Wheel updates thumb before the event returns");
        Call("SelectRow", 3, Keys.Control); Call("SelectRow", 6, Keys.Shift | Keys.Control);
        var selected = (IReadOnlyList<DatasetDownloadOption>)listType.GetProperty("Selection")!.GetValue(list)!;
        Check(selected.SequenceEqual(new[] { entries[0], entries[3], entries[4], entries[5], entries[6] }), "Ctrl and Shift preserve expected multiple selection");

        var timingType = assembly.GetType("ChessOpeningTrainer.DatasetTiming")!;
        double seconds = 100;
        object Clock(double origin) => Activator.CreateInstance(timingType, (Func<double>)(() => seconds), origin)!;
        var first = Clock(seconds); var total = Clock(0);
        string Time(object timer, long done, long count) => (string)timingType.GetMethod("Describe")!.Invoke(timer, [done, count])!;
        seconds = 120;
        Check(Time(first, 10, 100).Contains("00:00:20"), "First item measures only its own active time");
        var second = Clock(seconds);
        Check(Time(second, 0, 100).Contains("00:00:00"), "Next item starts from zero even with identical work totals");
        Check(Time(total, 1, 10).Contains("00:02:00"), "Queue total is not reset by item transitions");
        seconds = 130;
        Check(Time(first, 20, 100).Contains("00:00:30") && Time(second, 1, 100).Contains("00:00:10"), "Concurrent download/import clocks remain independent");

        using var graph = new BranchCanvas(new OpeningDefinition(), new ProgressStore(root), null, deferBuild: true);
        graph.Size = new(300, 200); graph.Camera = (1, PointF.Empty);
        var position = Position.FromFen(Position.InitialFen);
        var neighborPosition = position.Clone(); neighborPosition.Apply(MoveGenerator.GenerateLegal(neighborPosition).Single(m => m.Uci == "e2e4"));
        var chosen = new BranchVertex { Position = position, Location = new(100, 100) };
        graph.Vertices.Add(chosen); graph.Vertices.Add(new() { Position = neighborPosition, Location = new(148, 100) });
        graph.MarkSelected(chosen); graph.CreateControl();
        using var bitmap = new Bitmap(300, 200); graph.DrawToBitmap(bitmap, graph.ClientRectangle);
        Check(bitmap.GetPixel(128, 100).ToArgb() == UiPalette.Orange.ToArgb(), "Selected node outline paints above a later overlapping neighbor");
    }
}
