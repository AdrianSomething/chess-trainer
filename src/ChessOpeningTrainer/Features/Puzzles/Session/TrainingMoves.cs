namespace ChessOpeningTrainer;
public sealed partial class PuzzleSessionForm
{
    private bool TryMove(int from, int to, bool animate)
    {
        if (waitingReply && review is null)
        {
            premove = Premoves.Candidates(Display, from, solverWhite).FirstOrDefault(m => m.To == to);
            board.QueuedMove = premove;
            Clear();
            return premove is not null;
        }

        var choices = MoveGenerator.GenerateLegal(Display).Where(m => m.From == from && m.To == to).ToList();
        if (choices.Count == 0)
            return false;
        var move = choices.Count == 1 ? choices[0] : board.ChoosePromotion(this, choices, Display.WhiteToMove);
        if (move is not null)
            _ = Move(move, animate, false);
        return true;
    }

    private new async Task Move(Move move, bool animate, bool revealed)
    {
        if (busy || loading)
            return;
        int version = revision;
        if (review is not null || !question)
        {
            var before = Display.Clone();
            var after = before.Clone();
            after.Apply(move);
            AppSounds.PlayMove(before, move, after);
            review = after;
            busy = true;
            RefreshBoard();
            if (animate)
                await board.AnimateAsync(before, move, after);
            if (version == revision)
            {
                busy = false;
                RefreshBoard();
                Analyze();
            }

            return;
        }

        if (!active || puzzle is null || profile is null)
            return;
        busy = true;
        RefreshBoard();
        try
        {
            string? defenseMove = null;
            bool accepted;
            bool fullMate = profile.FullEndgames && MatingPractice.Supports(kind);
            if (puzzle.GoalKind is "Positional" or "PersonalMistake" or "OpeningChoice")
                accepted = puzzle.AcceptedMoves.Contains(move.Uci);
            else if (IsPawnMode)
            {
                var table = await PawnTablebase.Shared.WaitAsync(work.Token);
                accepted = table.PreservingMoves(position, kind == "Bauernumwandlung").Any(m => m.Uci == move.Uci);
                if (accepted && kind == "Bauernumwandlung")
                {
                    var after = position.Clone();
                    after.Apply(move);
                    var counts = new Dictionary<string, int>(repetitions);
                    counts[after.RepetitionKey()] = counts.GetValueOrDefault(after.RepetitionKey()) + 1;
                    accepted = MoveGenerator.GameState(after, counts)is "" or "Schach" or "Schachmatt";
                }
            }
            else if (IsAdvanced)
            {
                var decision = await decisions.Get(position, puzzle.GoalKind == "AdvancedDraw", profile.StrictOptimal).WaitAsync(work.Token);
                accepted = decision.Accepted.Contains(move.Uci);
                defenseMove = decision.Reply(move.Uci);
            }
            else if (fullMate)
                accepted = MatingPractice.SafeMoves(position, repetitions).Any(m => m.Uci == move.Uci) && (!profile.StrictOptimal || await EndgameJudge.Accept(engine, position, move, false, true, work.Token));
            else if (puzzle.PreparedMateRoutes is not null)
            {
                int remaining = (puzzle.MateGoal ?? puzzle.GoalMoves) - ply / 2;
                var route = puzzle.PreparedMateRoutes.GetValueOrDefault(MatePreparation.Key(position, remaining))?.FirstOrDefault(r => r.Move == move.Uci);
                accepted = route is not null;
                defenseMove = route?.Defense;
            }
            else
            {
                accepted = puzzle.Moves.ElementAtOrDefault(ply) == move.Uci;
                defenseMove = puzzle.Moves.ElementAtOrDefault(ply + 1);
            }

            if (accepted && IsPawnMode && profile.StrictOptimal && kind == "Bauernumwandlung")
            {
                var table = await PawnTablebase.Shared;
                var options = table.PreservingMoves(position, true);
                int Cost(Move m)
                {
                    if (m.Promotion != '\0')
                        return 0;
                    var after = position.Clone();
                    after.Apply(m);
                    return table.WinDistance(after);
                }

                accepted = Cost(move) == options.Min(Cost);
            }

            if (!accepted)
            {
                busy = false;
                if (Mistake("✕ Dieser Zug erhält das Aufgabenziel nicht.") && profile.RevealOnError)
                    await Reveal();
                return;
            }

            if (revealed)
                failed = true;
            else if (!fullMate && !IsAdvanced)
                AppSounds.Play("success");
            if (IsAdvanced && puzzle.Moves.ElementAtOrDefault(ply) != move.Uci)
                onSolutionLine = false;
            var before = position.Clone();
            await Apply(move, animate);
            if (version != revision)
                return;
            ply++;
            if (puzzle.GoalKind is "Positional" or "PersonalMistake" or "OpeningChoice")
            {
                Complete();
                return;
            }

            waitingReply = true;
            RefreshBoard();
            if (fullMate)
            {
                if (MoveGenerator.GenerateLegal(position).Count == 0 && MoveGenerator.IsKingInCheck(position, position.WhiteToMove))
                {
                    Complete();
                    return;
                }

                var r = await engine.AnalyzeAsync(position.ToFen(), 24, 1, 2, 128, work.Token, 1400);
                if (version != revision)
                    return;
                defenseMove = r.BestMove ?? MoveGenerator.GenerateLegal(position).FirstOrDefault()?.Uci;
            }

            if (IsAdvanced)
            {
                string state = MoveGenerator.GameState(position, repetitions);
                bool win = state == "Schachmatt" && position.WhiteToMove != solverWhite, drawGoal = puzzle.GoalKind == "AdvancedDraw";
                if (win && !drawGoal || drawGoal && state.StartsWith("Remis"))
                {
                    Complete();
                    return;
                }

                if (kind is "Bauern + Läufer" or "Bauern + Springer" && move.Promotion != '\0' && supportingMoves >= 2)
                {
                    var check = await engine.AnalyzeAsync(position.ToFen(), 22, 1, 2, 128, work.Token, 700);
                    if (version != revision)
                        return;
                    if (check.Mate < 0 || check.ScoreCentipawns <= -150)
                    {
                        Complete();
                        return;
                    }
                }

                if (state is not ("" or "Schach"))
                {
                    Mistake("Trainingsziel verfehlt. Neue Stellung oder Lösung ansehen.");
                    question = false;
                    return;
                }

                if (drawGoal && kind != "Bauernverteidigung: mehrere Bauern")
                {
                    var table = await PawnTablebase.Shared;
                    if (DrawPreparation.Secured(position, solverWhite, table))
                    {
                        Complete();
                        return;
                    }
                }

                if (defenseMove is not null && defenseMove != puzzle.Moves.ElementAtOrDefault(ply))
                    onSolutionLine = false;
                defenseMove ??= onSolutionLine ? puzzle.Moves.ElementAtOrDefault(ply) : null;
                if (defenseMove is null)
                {
                    var r = await engine.AnalyzeAsync(position.ToFen(), 22, 1, 2, 128, work.Token, 900);
                    if (version != revision)
                        return;
                    defenseMove = r.BestMove;
                    if (defenseMove is null)
                        throw new IOException(r.Error ?? "Keine Engineantwort");
                }
            }

            bool pawnMode = IsPawnMode;
            if (pawnMode)
            {
                bool done = kind == "Bauernumwandlung" ? move.Promotion != '\0' : !position.Squares.Any(c => char.ToLowerInvariant(c) == 'p') || MoveGenerator.GameState(position, repetitions)is not ("" or "Schach");
                if (done)
                {
                    Complete();
                    return;
                }

                var table = await PawnTablebase.Shared;
                var legal = MoveGenerator.GenerateLegal(position);
                var preserving = table.PreservingMoves(position, kind != "Bauernumwandlung");
                var candidates = preserving.Count > 0 ? preserving : legal;
                defenseMove = table.TrainingReply(position, kind == "Bauernumwandlung")?.Uci;
            }
            else if (defenseMove is null)
            {
                Complete();
                return;
            }

            if (defenseMove is not null)
            {
                var reply = MoveGenerator.GenerateLegal(position).FirstOrDefault(m => m.Uci == defenseMove);
                if (reply is null)
                    throw new InvalidOperationException("Ungültige vorbereitete Antwort.");
                bool pawnMoved = char.ToLowerInvariant(position.Squares[reply.From]) == 'p';
                await Apply(reply, true);
                if (version != revision)
                    return;
                ply++;
                if (pawnMode)
                {
                    quietDefense = pawnMoved ? 0 : quietDefense + 1;
                    if (kind != "Bauernumwandlung" && (quietDefense >= profile.DefenseMoves || MoveGenerator.GameState(position, repetitions)is not ("" or "Schach")))
                    {
                        Complete();
                        return;
                    }
                }
            }

            if (puzzle.GoalKind == "Draw" && DrawPreparation.Secured(position, solverWhite, await PawnTablebase.Shared.WaitAsync(work.Token)))
            {
                Complete();
                return;
            }

            if (!pawnMode && !fullMate && !IsAdvanced && puzzle.PreparedMateRoutes is null && ply >= puzzle.Moves.Count)
            {
                Complete();
                return;
            }

            if (IsAdvanced)
            {
                var state = MoveGenerator.GameState(position, repetitions);
                if (puzzle.GoalKind == "AdvancedDraw" && (state.StartsWith("Remis") || kind != "Bauernverteidigung: mehrere Bauern" && DrawPreparation.Secured(position, solverWhite, await PawnTablebase.Shared)))
                {
                    Complete();
                    return;
                }

                if (state is not ("" or "Schach"))
                {
                    Mistake("Trainingsziel verfehlt. Neue Stellung oder Lösung ansehen.");
                    question = false;
                    return;
                }
            }

            if (fullMate && MoveGenerator.GameState(position, repetitions)is not ("" or "Schach"))
            {
                Mistake("Remis statt Matt. Wähle eine neue Aufgabe.");
                question = false;
                RefreshBoard();
                return;
            }

            if (IsAdvanced)
                _ = decisions.Prefetch(position.Clone(), puzzle.GoalKind == "AdvancedDraw", profile.StrictOptimal);
            moveLeft = profile.MoveSeconds;
            info.BackColor = UiPalette.FromArgb(35, 100, 65);
            info.Text = (IsAdvanced && puzzle.GoalKind == "AdvancedDraw" ? "Remis halten · 50-Züge-Regel beachten" : fullMate || IsAdvanced ? "Endspiel weiter ausspielen · 50-Züge-Regel beachten" : "✓ Richtig · nächster eigener Zug") + (kind == "Bauernverteidigung" ? $" · {quietDefense}/{profile.DefenseMoves} ohne Bauernbewegung" : "");
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            if (version == revision)
            {
                info.Text = ex.Message;
                question = false;
            }
        }
        finally
        {
            if (version == revision)
            {
                waitingReply = false;
                busy = false;
                var queued = premove;
                premove = null;
                board.QueuedMove = null;
                RefreshBoard();
                if (queued is not null && question)
                {
                    var legal = MoveGenerator.GenerateLegal(position).FirstOrDefault(m => m.Uci == queued.Uci);
                    if (legal is not null)
                        _ = Move(legal, false, false);
                }
            }
        }
    }

    private async Task Apply(Move move, bool animate)
    {
        var before = position.Clone();
        string san = SanNotation.Format(before, move);
        if (before.WhiteToMove == solverWhite)
        {
            trainedPieces.Add(pieceIds[move.From]);
            if (char.ToLowerInvariant(before.Squares[move.From])is 'b' or 'n')
                supportingMoves++;
        }

        pieceIds[move.To] = pieceIds[move.From];
        pieceIds[move.From] = -1;
        position.Apply(move);
        AppSounds.PlayMove(before, move, position);
        AddSnapshot($"{before.Fullmove}{(before.WhiteToMove ? "." : "…")} {san}");
        Clear();
        RefreshBoard();
        if (animate)
            await board.AnimateAsync(before, move, position.Clone());
    }

    private async Task Reveal()
    {
        if (!question || busy || puzzle is null)
            return;
        review = null;
        string? u;
        if (IsPawnMode)
        {
            var table = await PawnTablebase.Shared;
            u = table.Suggest(position, kind == "Bauernumwandlung")?.Uci;
        }
        else if (IsAdvanced)
        {
            int version = revision;
            busy = true;
            RefreshBoard();
            try
            {
                u = (await decisions.Get(position, puzzle.GoalKind == "AdvancedDraw", profile?.StrictOptimal == true).WaitAsync(work.Token)).Best;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                info.Text = ex.Message;
                return;
            }
            finally
            {
                if (version == revision)
                {
                    busy = false;
                    RefreshBoard();
                }
            }

            if (version != revision)
                return;
            if (u is null)
            {
                info.Text = "Keine zielerhaltende Antwort in der vorbereiteten Suche. Neue Aufgabe wählen.";
                return;
            }
        }
        else if (profile?.FullEndgames == true && MatingPractice.Supports(kind))
        {
            int version = revision;
            busy = true;
            RefreshBoard();
            var r = await engine.AnalyzeAsync(position.ToFen(), 26, 3, 4, 256, work.Token, 3000);
            if (version != revision)
                return;
            busy = false;
            var legal = MatingPractice.SafeMoves(position, repetitions);
            u = r.Lines.Select(l => l.BestMove).FirstOrDefault(u => legal.Any(m => m.Uci == u)) ?? legal.FirstOrDefault()?.Uci;
            if (u is null)
            {
                info.Text = "Kein materialerhaltender Zug ohne Remis mehr. Neue Aufgabe wählen.";
                question = false;
                RefreshBoard();
                return;
            }
        }
        else if (puzzle.PreparedMateRoutes is not null)
            u = puzzle.PreparedMateRoutes.GetValueOrDefault(MatePreparation.Key(position, (puzzle.MateGoal ?? puzzle.GoalMoves) - ply / 2))?.FirstOrDefault()?.Move;
        else
            u = puzzle.Moves.ElementAtOrDefault(ply);
        var m = MoveGenerator.GenerateLegal(position).FirstOrDefault(m => m.Uci == u);
        if (m is not null)
            await Move(m, true, true);
    }
}
