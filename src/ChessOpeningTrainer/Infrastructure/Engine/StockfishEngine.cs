using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChessOpeningTrainer;
public sealed class StockfishEngine
{
    public string? Path { get; }
    public SavedEngine? NeuralOverride { get; private set; }
    public StockfishEngine WithNeural(SavedEngine model) { var copy=new StockfishEngine(AppContext.BaseDirectory);copy.NeuralOverride=model;return copy; }
    internal GenerationBudget? Budget { get; }
    private StockfishEngine(string? path, GenerationBudget budget) { Path = path; Budget = budget; }
    public StockfishEngine ForGeneration(GenerationBudget budget) => new(Path, budget);

    public StockfishEngine(string appDirectory)
    {
        var candidates = new[]
        {
            Environment.GetEnvironmentVariable("CHESS_TRAINER_STOCKFISH"),
            System.IO.Path.Combine(appDirectory, "engine", "stockfish.exe"),
            System.IO.Path.Combine(appDirectory, "engine", "stockfish-windows-x86-64-universal.exe"),
            System.IO.Path.Combine(appDirectory, "engine", "stockfish_release", "stockfish", "stockfish-windows-x86-64-universal.exe"),
        };
        Path = candidates.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x) && File.Exists(x));
    }

    public Task<EngineResult> AnalyzeAsync(string fen, int depth = 19, int multiPv = 1, int threads = 8, int hashMb = 768, CancellationToken cancellationToken = default, int milliseconds = 4000, bool unlimited = false, string? searchMoves = null) => Task.Run(() => AnalyzeCore(fen, depth, multiPv, threads, hashMb, cancellationToken, milliseconds, unlimited, searchMoves));
    private async Task<EngineResult> AnalyzeCore(string fen, int depth, int multiPv, int threads, int hashMb, CancellationToken cancellationToken, int milliseconds, bool unlimited, string? searchMoves)
    {
        using var slot = Budget is null ? null : await Budget.Acquire(cancellationToken);
        if (Budget is not null) threads = Budget.Threads;
        if (Path is null)
            return new EngineResult
            {
                Error = "Stockfish fehlt"
            };
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (!unlimited)
            timeout.CancelAfter(milliseconds + 10000);
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo(Path)
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };
        try
        {
            process.Start();
            using var registration = timeout.Token.Register(() =>
            {
                _ = Task.Run(() =>
                {
                    try
                    {
                        if (!process.HasExited)
                            process.Kill(true);
                    }
                    catch
                    {
                    }
                });
            });
            var input = process.StandardInput;
            async Task Send(string command)
            {
                await input.WriteLineAsync(command);
                await input.FlushAsync();
            }

            async Task Wait(string expected)
            {
                while (await process.StandardOutput.ReadLineAsync(timeout.Token)is { } line)
                    if (line == expected)
                        return;
                throw new IOException("Engine beendet.");
            }

            await Send("uci");
            await Wait("uciok");
            await Send("setoption name Threads value " + Math.Clamp(threads, 1, Environment.ProcessorCount));
            await Send("setoption name Hash value " + Math.Clamp(hashMb, 64, 2048));
            await Send("setoption name MultiPV value " + multiPv);
            await Send("isready");
            await Wait("readyok");
            await Send("position fen " + fen);
            await Send($"go depth {depth}" + (unlimited ? "" : $" movetime {milliseconds}") + (searchMoves is null ? "" : " searchmoves " + searchMoves));
            var lines = new Dictionary<int, EngineLine>();
            string? best = null;
            while (await process.StandardOutput.ReadLineAsync(timeout.Token)is { } line)
            {
                if (line.StartsWith("bestmove "))
                {
                    best = line.Split(' ')[1];
                    break;
                }

                if (!line.StartsWith("info ") || !line.Contains(" pv "))
                    continue;
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                int Value(string key, int fallback = 0)
                {
                    var i = Array.IndexOf(parts, key);
                    return i >= 0 && i + 1 < parts.Length && int.TryParse(parts[i + 1], out var v) ? v : fallback;
                }

                var pi = Array.IndexOf(parts, "pv");
                var si = Array.IndexOf(parts, "score");
                if (si < 0 || si + 2 >= parts.Length || pi < 0)
                    continue;
                if (!int.TryParse(parts[si + 2], out var score))
                    continue;
                var rank = Value("multipv", 1);
                lines[rank] = new EngineLine
                {
                    Rank = rank,
                    Depth = Value("depth"),
                    BestMove = parts[pi + 1],
                    Pv = parts[(pi + 1)..].ToList(),
                    ScoreCentipawns = parts[si + 1] == "cp" ? score : null,
                    Mate = parts[si + 1] == "mate" ? score : null
                };
            }

            await Send("quit");
            var first = lines.GetValueOrDefault(1);
            return new EngineResult
            {
                BestMove = best,
                ScoreCentipawns = first?.ScoreCentipawns,
                Mate = first?.Mate,
                Lines = lines.Values.OrderBy(x => x.Rank).ToList(),
                Error = best is null ? "Keine Engineantwort" : null
            };
        }
        catch (OperationCanceledException)
        {
            return new EngineResult
            {
                Error = cancellationToken.IsCancellationRequested ? "Analyse abgebrochen" : "Zeitlimit der Enginekommunikation erreicht"
            };
        }
        catch (Exception ex)
        {
            return new EngineResult
            {
                Error = ex.Message
            };
        }
        finally
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(true);
            }
            catch
            {
            }
        }
    }
}
