using System.Text.Json;
using ChessOpeningTrainer;

internal static class Version270Checks
{
    private sealed class KeyboardBoard : BoardControl
    {
        public void Key(Keys key) { var message = new Message(); ProcessCmdKey(ref message, key); }
    }
    public static void Run(string root)
    {
        var initial = Position.FromFen(Position.InitialFen);
        var legal = MoveGenerator.GenerateLegal(initial);
        foreach (string text in new[] { "e4", "Pe4", "e2e4", "ee24", "2e4e", "Pee24#", "Pe4+" })
            Program.Check(SimplifiedMoveNotation.Match(initial, text, legal).Single().Uci == "e2e4", "Simplified notation: " + text);
        var capture = Position.FromFen("4k3/8/8/8/3p4/4P3/8/4K3 w - - 0 1");
        foreach (string text in new[] { "Ped", "exd", "exd4", "Pexd4#" })
            Program.Check(SimplifiedMoveNotation.Match(capture, text, MoveGenerator.GenerateLegal(capture)).Single().Uci == "e3d4", "Pawn capture shorthand: " + text);
        var bishops = Position.FromFen("4k3/8/8/4B3/8/8/8/2B1K3 w - - 0 1");
        Program.Check(SimplifiedMoveNotation.Match(bishops, "Bea", MoveGenerator.GenerateLegal(bishops)).Single().Uci == "e5a1", "Optional source-file disambiguation");
        Program.Check(SimplifiedMoveNotation.Match(initial, "xyz", legal).Count == 0, "Invalid input is not silently played");
        var promotion = Position.FromFen("4k3/P7/8/8/8/8/8/4K3 w - - 0 1");
        Program.Check(SimplifiedMoveNotation.Match(promotion, "a8=N", MoveGenerator.GenerateLegal(promotion)).Single().Promotion == 'n', "Underpromotion notation");
        var castle = Position.FromFen("r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1");
        Program.Check(SimplifiedMoveNotation.Match(castle, "0-0", MoveGenerator.GenerateLegal(castle)).Single().Uci == "e1g1", "Castling notation");
        using (var form = new LocalizedForm())
        using (var board = new KeyboardBoard { Position = initial, Size = new(500, 500) })
        {
            form.Controls.Add(board); form.Show();
            string? played = null; board.SquareDragged += (from, to) => played = Square.Name(from) + Square.Name(to);
            board.OpenKeyboardInput();
            var input = board.Controls.OfType<TextBox>().Single(); input.Text = "N";
            Program.Check(board.SelectedSquare == 1, "Bottom-left default source for knight options");
            board.Key(Keys.Enter); Program.Check(played == "b1a3", "Keyboard dispatches through existing drag handler");
            board.OpenKeyboardInput(); input.Text = "N"; board.Key(Keys.Right); board.Key(Keys.Enter);
            Program.Check(played == "b1c3", "Arrow navigation retains the selected source");
            board.Flip = true; board.OpenKeyboardInput(); input.Text = "N"; board.Key(Keys.Enter);
            Program.Check(played == "g1h3", "Bottom-left keyboard choice respects board rotation");
            board.Flip = false; board.Position = capture; board.PremoveSide = false;
            board.OpenKeyboardInput(); input.Text = "Ke7"; board.Key(Keys.Enter);
            Program.Check(played == "e8e7", "Keyboard premove uses the pending side rather than current turn");
            board.PremoveSide = null;
            board.OpenKeyboardInput(); input.Text = "e4"; board.Position = initial;
            Program.Check(!board.KeyboardInputOpen && board.CandidateMoves.Count == 0, "Position changes clear keyboard candidates");
            form.Close();
        }
        var plan = new List<PreferredStep> { new(12, 20, 'P'), new(5, 26, 'B') };
        Program.Check(!PreferredMovePlan.CanAppend(initial, true, 5, 26), "Own pieces block unprepared preferences");
        var preview = PreferredMovePlan.Preview(initial, plan.Take(1));
        Program.Check(PreferredMovePlan.CanAppend(preview, true, 5, 26), "Earlier planned move frees own path");
        Program.Check(initial.Squares[12] == 'P', "Preference planning preserves actual position");
        var options = new GeneratorOptions { LearnWhite = true, PreferredMoves = [new(12, 28, 'P')] };
        var lines = new List<EngineLine> { new() { BestMove = "d2d4", ScoreCentipawns = 30 }, new() { BestMove = "e2e4", ScoreCentipawns = 25 } };
        Program.Check(OpeningGenerator.ChooseLearner(initial, lines, options, _ => false).BestMove == "e2e4", "Acceptable preferred move beats fallback");
        Program.Check(OpeningGenerator.ChooseLearner(initial, lines, options, _ => false, new(0, 3)).BestMove == "d2d4", "Preference deviation cutoff");
        options.PreferenceMaximumLoss = .01;
        Program.Check(OpeningGenerator.ChooseLearner(initial, lines, options, _ => false).BestMove == "d2d4", "Preference loss guard");
        options.PreferenceMaximumLoss = .1;
        Program.Check(OpeningGenerator.ChooseLearner(initial, lines, options, _ => true).BestMove == "d2d4", "Compression has priority");
        var opening = new OpeningDefinition { Perspective = "black", Tree = new() };
        var next = new OpeningNode(); opening.Tree.Moves.Add(new() { Uci = "e2e4", Next = next });
        opening.Tree.Moves.Add(new() { Uci = "g1f3", Next = new() });
        var report = OpeningCoverage.Calculate(opening, p => p.ToFen() == initial.ToFen() ? [("e2e4", new(60,0,0,0)), ("d2d4", new(40,0,0,0))] : [], default);
        Program.Check(report.Score == 60 && report.Gaps.Single().Gain == 40, "Coverage 60/100 and exact immediate gap contribution");
        opening.GenerationStartFen = Position.InitialFen;
        Program.Check(OpeningCoverage.Calculate(opening, _ => [], default).Score is null, "No observations is not misleading zero coverage");
        var game = new TrainingGame();
        Program.Check(!game.Apply("e2e5") && game.Plies == 0, "Training host rejects illegal moves without changing state");
        foreach (string uci in new[] { "f2f3", "e7e5", "g2g4", "d8h4" }) Program.Check(game.Apply(uci), "Training legal move " + uci);
        Program.Check(game.Status == "checkmate" && game.Result == "0-1", "Training result uses shared chess rules");
        var indices = MoveGenerator.GenerateLegal(promotion).Select(TrainingGame.ActionIndex).ToArray();
        Program.Check(indices.Distinct().Count() == indices.Length && indices.All(i => i is >= 0 and < 20480), "All promotions have distinct policy actions");
        var limited = new TrainingGame(maximumFullMoves:1); limited.Apply("e2e4"); limited.Apply("e7e5");
        Program.Check(limited.Status == "move_limit", "Full-move limit counts both sides");
        var repeat = new TrainingGame();
        foreach (var uci in Enumerable.Repeat(new[] { "g1f3", "g8f6", "f3g1", "f6g8" }, 2).SelectMany(x => x)) repeat.Apply(uci);
        Program.Check(repeat.Status == "draw", "Training preserves repetition history");
        using (var cts = new CancellationTokenSource())
        {
            cts.Cancel();
            try { OpeningCoverage.Calculate(opening, _ => [], cts.Token); Program.Check(false, "Coverage cancellation"); }
            catch (OperationCanceledException) { Program.Check(true, "Coverage cancels without producing a partial score"); }
        }
        var store = new DatasetStore(Path.Combine(root, "generator-270"));
        var games = Enumerable.Range(0, 12).Select(i => new DatasetGame(Position.InitialFen, [i < 9 ? "e2e4" : "d2d4", "d7d5"], "1-0", 1500, 1500, 180, 0, "Normal"));
        store.Register(store.Write(games, "Synthetic frequency preferences", 0, 100, null, default));
        string index = DatasetIndex.Build(store, new(), null, default);
        var filtered = new GeneratorOptions { LearnWhite = false, DatasetIndexFile = index, MinimumCommonReplies = 1, StopBelowGames = 20 };
        using (var policy = new GeneratorDatasetPolicy(filtered))
        {
            var decision = policy.Decide(initial, filtered);
            Program.Check(decision.Stop && decision.Continue.SetEquals(["e2e4"]), "Common opponent move ranking and low-frequency stop");
            filtered.MinimumDatasetGames = 13;
            var ignored = policy.Decide(initial, filtered);
            Program.Check(!ignored.Stop && ignored.Continue.Count == 0, "Too few observations restore engine-only behavior");
            filtered.MinimumDatasetGames = 0; filtered.LearnWhite = true;
            var own = policy.Decide(initial, filtered);
            Program.Check(!own.Stop && own.Continue.Count == 0, "Dataset rules never constrain learner moves");
        }
        using (var editor = new SectionEditor())
        {
            editor.SetValues([new() { MinimumCommonReplies = 2, StopBelowGames = 100 }]);
            Program.Check(editor.Values[0].MinimumCommonReplies == 2 && editor.Values[0].StopBelowGames == 100, "Dataset section parameters round-trip through UI");
        }
    }
}
