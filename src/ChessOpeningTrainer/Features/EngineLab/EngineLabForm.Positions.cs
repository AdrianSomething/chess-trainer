using System.Text.Json;

namespace ChessOpeningTrainer;

public sealed partial class EngineLabForm
{
    private TrainingProtocol? lab;
    private int? labSelected;
    private bool labBusy;
    private async Task<JsonElement> LabCommand(object command)
    {
        using var request = JsonDocument.Parse(JsonSerializer.Serialize(command));
        var response = await lab!.Execute(request.RootElement);
        return JsonSerializer.SerializeToElement(response);
    }
    private async Task StartLab()
    {
        if (process is not null || labBusy) return;
        labBusy = true; options.Enabled = false; board.InputEnabled = false;
        try
        {
            if (lab is not null) await lab.DisposeAsync();
            lab = new(AppContext.BaseDirectory);
            int count = (int)instances.Value, limit = (int)maxMoves.Value, selectedSource = source.SelectedIndex;
            string startingFen = fen.Text; var host = lab;
            var requests = Enumerable.Range(1, count).Select(id =>
            {
                var request = new Dictionary<string, object?> { ["op"] = "reset", ["id"] = id.ToString(), ["maxMoves"] = limit,
                    ["seed"] = Random.Shared.Next(), ["randomPlies"] = selectedSource == 1 ? Random.Shared.Next(8, 50) : 0 };
                if (selectedSource == 2) request["fen"] = startingFen;
                return request;
            }).ToArray();
            await Task.Run(async () =>
            {
                foreach (var request in requests)
                { using var json = JsonDocument.Parse(JsonSerializer.Serialize(request)); await host.Execute(json.RootElement); }
            });
            if (IsDisposed) return;
            instance.Maximum = instances.Value; instance.Value = 1; await ShowLabState(); board.InputEnabled = true;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) when (ex is ArgumentException or InvalidDataException or FormatException) { if (!IsDisposed) status.Text = ex.Message; }
        finally { labBusy = false; if (!IsDisposed) options.Enabled = true; }
    }
    private async Task ShowLabState()
    {
        if (lab is null) return;
        var state = await LabCommand(new { op = "state", id = instance.Value.ToString() });
        // Protocol commands expose complete FEN and legal policy-action indices to external clients.
        board.Position = Position.FromFen(state.GetProperty("fen").GetString()!); board.Invalidate(); labSelected = null;
        report.Text = state.ToString(); status.Text = L.T("Stellungslabor") + $" · {instance.Value}/{instances.Value}";
    }
    private async Task<bool> PlayLabMove(int from, int to)
    {
        if (lab is null || labBusy || board.Position is not { } before) return false;
        var choices = MoveGenerator.GenerateLegal(before).Where(m => m.From == from && m.To == to).ToList();
        if (choices.Count == 0) return false;
        var move = choices.Count == 1 ? choices[0] : board.ChoosePromotion(this, choices, before.WhiteToMove);
        if (move is null) return false;
        labBusy = true;
        try
        {
            await LabCommand(new { op = "step", id = instance.Value.ToString(), move = move.Uci }); await ShowLabState();
            if (teacherDepth.Value > 0)
            { var evaluation = await LabCommand(new { op = "analyze", id = instance.Value.ToString(), depth = (int)teacherDepth.Value }); if (!IsDisposed) report.Text = evaluation.ToString(); }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) when (ex is IOException or InvalidOperationException) { if (!IsDisposed) status.Text = ex.Message; }
        finally { labBusy = false; }
        return true;
    }
}
