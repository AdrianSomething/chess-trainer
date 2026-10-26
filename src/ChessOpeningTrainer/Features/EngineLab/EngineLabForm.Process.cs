using System.Diagnostics;
using System.Text.Json;

namespace ChessOpeningTrainer;

public sealed partial class EngineLabForm
{
    private async Task StartExperiment(bool continuing)
    {
        if (process is not null) return;
        string script = Path.Combine(AppContext.BaseDirectory, "Addons", "EngineLab", "train.py");
        if (!File.Exists(script)) { status.Text = L.T("Das optionale Engine-Labor-Add-on fehlt."); return; }
        if ((continuing || mode.SelectedIndex == 2) && (runDirectory is null || !File.Exists(Path.Combine(runDirectory, "best.pt"))))
        { status.Text = L.T("Zuerst ein Modell trainieren oder gespeicherte Gewichte im Experimentordner bereitstellen."); return; }
        if (lab is not null) { await lab.DisposeAsync(); lab = null; }
        board.InputEnabled = false;
        if (!continuing && mode.SelectedIndex != 2) runDirectory = Path.Combine(storage, DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(runDirectory!);
        // This is our per-experiment cancellation marker, never a user dataset.
        string marker = Path.Combine(runDirectory!, "stop.request"); if (File.Exists(marker)) File.Delete(marker);
        var config = new { mode = mode.SelectedIndex switch { 1 => "distillation", 2 => "benchmark", _ => "evolution" }, rounds = (int)rounds.Value,
            population = (int)population.Value, max_moves = (int)maxMoves.Value, stockfish_depth = (int)depth.Value, teacher_depth = (int)teacherDepth.Value,
            position_source = source.SelectedIndex switch { 1 => "generated", 2 => "fen", _ => "initial" }, fen = fen.Text,
            visualize = visual.Checked, record_thinking = timing.Checked };
        string configFile = Path.Combine(runDirectory!, "launch-config.json");
        File.WriteAllText(configFile, JsonSerializer.Serialize(config)); File.WriteAllText(Path.Combine(storage, "latest-run.txt"), runDirectory);
        var launch = new ProcessStartInfo(python.Text.Trim()) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true, WorkingDirectory = Path.GetDirectoryName(script)! };
        foreach (string argument in new[] { script, "--host", Environment.ProcessPath!, "--output", runDirectory!, "--config", configFile }) launch.ArgumentList.Add(argument);
        if (continuing) launch.ArgumentList.Add("--resume");
        var running = new Process { StartInfo = launch };
        try
        {
            running.Start(); process = running; SetRunning(true);
            async Task ReadOutput()
            {
                while (await running.StandardOutput.ReadLineAsync() is { } line)
                { lock (messageLock) pendingMessage = line; }
            }
            Task output = ReadOutput(); Task<string> errors = running.StandardError.ReadToEndAsync();
            await Task.WhenAll(output, running.WaitForExitAsync());
            string error = await errors;
            if (!IsDisposed)
            {
                PaintLatest();
                if (running.ExitCode != 0) report.Text = error.Length > 0 ? error[^Math.Min(4000, error.Length)..] : report.Text;
            }
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or IOException or InvalidOperationException)
        { if (!IsDisposed) status.Text = ex.Message; }
        finally { process = null; running.Dispose(); if (!IsDisposed) SetRunning(false); }
    }
    private void SetRunning(bool running)
    {
        foreach (Control control in options.Controls) control.Enabled = !running;
        stop.Enabled = running; visual.Enabled = true;
        start.Enabled = resume.Enabled = !running;
    }
    private void RequestStop()
    {
        if (runDirectory is not null && process is not null)
        { File.WriteAllText(Path.Combine(runDirectory, "stop.request"), "stop"); status.Text = L.T("Anhalten nach der aktuellen Berechnung …"); stop.Enabled = false; }
    }
    private void PaintLatest()
    {
        string? line; lock (messageLock) { line = pendingMessage; pendingMessage = null; }
        if (line is null) return;
        try
        {
            using var document = JsonDocument.Parse(line); var value = document.RootElement;
            string kind = value.GetProperty("event").GetString() ?? "";
            if (visual.Checked && value.TryGetProperty("fen", out var position) && position.ValueKind == JsonValueKind.String)
            { board.Position = Position.FromFen(position.GetString()!); board.Invalidate(); }
            if (value.TryGetProperty("completed", out var completed))
                status.Text = L.T("Berechnung läuft") + $" · {completed}/{value.GetProperty("total")}";
            else if (kind == "round_start") status.Text = L.T("Runde") + $" {value.GetProperty("round")}/{value.GetProperty("total")}";
            else if (kind == "round_complete") status.Text = L.T("Runde abgeschlossen") + $" · {value.GetProperty("round")}";
            else if (kind == "complete") { status.Text = L.T("Experiment abgeschlossen. Bericht im Experimentordner."); report.Text = line; }
            else if (kind is "error" or "stopped") { status.Text = L.T(kind == "error" ? "Experiment fehlgeschlagen" : "Experiment angehalten"); report.Text = value.GetProperty("message").GetString(); }
            else report.Text = line;
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException or KeyNotFoundException) { report.Text = line; }
    }
}
