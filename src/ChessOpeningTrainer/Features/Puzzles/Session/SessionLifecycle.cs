namespace ChessOpeningTrainer;
public sealed partial class PuzzleSessionForm
{
    private void StartSession(PuzzleProfile p)
    {
        quickStartSession = false;
        BeginSession(p);
    }

    private async void BeginSession(PuzzleProfile p)
    {
        // Keep the serialized legacy flag, but no longer offer or schedule draw tactics.
        p.Draw = false;
        p.Lives = Math.Clamp(p.Lives, 0, 7);
        openingMistakesMode = p.Name == "Gegnerfehler ausnutzen" && openingMistakes is not null;
        mistakesMode = p.Name == "Eigene Fehler" && mistakes.Count > 0;
        if (p.MinGoodMoves > p.MaxGoodMoves)
        {
            L.Message(this, "Mindestanzahl guter Züge darf die Höchstanzahl nicht überschreiten.", "Mittelspiel");
            return;
        }

        if (!p.Tactics && p.Endgames.Count == 0 || p.Tactics && !p.Mate && !p.Material && !p.Draw && !p.Positional && !p.PositionalSubtle && p.Endgames.Count == 0)
        {
            L.Message(this, "Bitte mindestens einen Aufgabentyp oder ein Endspiel wählen.", "Rätsel");
            return;
        }

        revision++;
        work.Cancel();
        profile = p;
        roundStartHighScore = p.CanLose ? Math.Max(1, p.HighScore) : p.HighScore;
        if (p.CanLose) p.HighScore = roundStartHighScore;
        if (p.Tactics && !mistakesMode && !openingMistakesMode)
        {
            profiles.LastId = p.ExplicitlySaved ? p.Id : null;
            profiles.LastSettings = p.Copy();
            profiles.Save();
        }

        active = true;
        clock.ResetLossAnimation();
        resultVisible = false; sessionResult.Visible = false;
        resultDismissed=false; restartBelow.Visible=false;
        score = 0;
        remainingLives = p.Lives;
        totalLeft = p.TotalSeconds;
        moveLeft = p.MoveSeconds;
        previousTick = tick.ElapsedMilliseconds;
        showTrainingPage?.Invoke();
        tabs.SelectedTab = play;
        await NextExercise();
    }

    private async Task NextExercise()
    {
        if (profile is null || !active)
            return;
        historyPlayback.Cancel();
        autoNextAt = 0;
        showingSolution = false;
        waitingReply = false;
        premove = null;
        board.QueuedMove = null;
        int version = ++revision;
        work.Cancel();
        work.Dispose();
        work = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        var token = work.Token;
        analysis.Cancel();
        loading = true;
        busy = false;
        question = false;
        review = null;
        evaluation.Visible = variants.Visible = false;
        board.StopAnimation();
        Clear();
        board.HintMove = null;
        board.Arrows.Clear();
        board.MarkedSquares.Clear();
        variants.MaximumLines = 3; variants.AcceptedMoves.Clear(); variants.Height = 145;
        history.Top = 515; history.Height = Math.Max(60, play.ClientSize.Height - 533);
        RefreshBoard();
        info.Text = "Aufgabe wird vorbereitet und geprüft …";
        try
        {
            var choices = profile.Endgames.ToList();
            if (profile.Tactics && (profile.Mate || profile.Material))
                choices.Add("Taktik");
            if (profile.Tactics && profile.Positional)
                choices.Add("Stellungsgefühl");
            if (profile.Tactics && profile.PositionalSubtle)
                choices.Add("Feines Stellungsgefühl");
            if (choices.Count == 0)
                throw new InvalidOperationException("Keine Aufgabentypen gewählt.");
            kind = choices[Random.Shared.Next(choices.Count)];
            if (openingMistakesMode)
            {
                kind = "Gegnerfehler ausnutzen";
                puzzle = await openingMistakes!.Next(token);
            }
            else if (mistakesMode)
            {
                if (mistakeIndex > 0 && mistakeIndex % mistakes.Count == 0)
                    mistakes = mistakes.OrderBy(_ => Random.Shared.Next()).ToList();
                var entry = mistakes[mistakeIndex++ % mistakes.Count];
                var original = entry.Game.Positions()[entry.Moment.Ply];
                kind = "Eigene Fehler";
                puzzle = new Puzzle
                {
                    Id = entry.Game.Id + "_" + entry.Moment.Ply,
                    Fen = entry.Game.Positions()[entry.Moment.Ply].ToFen(),
                    AcceptedMoves = entry.Moment.BestMoves,
                    Moves = entry.Moment.Pv,
                    Theme = entry.Moment.Category + " verbessern",
                    Origin = entry.Game.ToString(),
                    GoalKind = "PersonalMistake",
                    PastMove = SanNotation.Format(original, MoveGenerator.GenerateLegal(original).First(m => m.Uci == entry.Game.Moves[entry.Moment.Ply]))
                };
            }
            else if (kind is "Stellungsgefühl" or "Feines Stellungsgefühl")
            {
                puzzle = await positionalTrainer.Next(profile.MinGoodMoves, profile.MaxGoodMoves, token, kind == "Feines Stellungsgefühl");
            }
            else if (kind == "Taktik")
            {
                puzzle = bank.Next(profile.Difficulty, profile.Mate, profile.Material);
                while (puzzle is null)
                {
                    token.ThrowIfCancellationRequested();
                    _ = bank.FillAsync(lifetime.Token);
                    await Task.Delay(400, token);
                    puzzle = bank.Next(profile.Difficulty, profile.Mate, profile.Material);
                }
            }
            else if (profile.FullEndgames && MatingPractice.Supports(kind))
            {
                puzzle = new()
                {
                    Fen = MatingPractice.Generate(kind).ToFen(),
                    Theme = "Matt setzen · freie Technikübung",
                    Origin = "Zufällige legale Materialstellung · Stockfish-Verteidigung"
                };
            }
            else if (kind == "Remis erzwingen")
            {
                puzzle = bank.NextDraw();
                if (puzzle is null)
                {
                    for (int attempt = 0; attempt < 8 && puzzle is null; attempt++)
                    {
                        bank.AddDraw(await DrawPreparation.Compose(engine.ForGeneration(supply.Budget), token));
                        puzzle = bank.NextDraw();
                    }
                    if (puzzle is null)
                        throw new InvalidOperationException("Noch keine neue Remisaufgabe vorbereitet. Nachschub wird im Hintergrund erzeugt.");
                }
            }
            else
            {
                var exercise = await endgames.Next(kind, token);
                kind = exercise.Kind;
                puzzle = exercise.Puzzle;
            }

            if (version != revision || token.IsCancellationRequested)
                return;
            position = Position.FromFen(puzzle.Fen);
            if (!profile.Tactics && (sideChoice.SelectedIndex == 0 ? Random.Shared.Next(2) == 0 : position.WhiteToMove != (sideChoice.SelectedIndex == 1)))
            {
                position = PawnTablebase.SwapColors(position);
                puzzle = PuzzleTransforms.Swap(puzzle);
            }

            solverWhite = position.WhiteToMove;
            ply = 0;
            onSolutionLine = true;
            supportingMoves = 0;
            trainedPieces.Clear();
            pieceIds = Enumerable.Range(0, 64).ToArray();
            quietDefense = 0;
            failed = false;
            question = true;
            selected = null;
            repetitions.Clear();
            snapshots.Clear();
            history.Items.Clear();
            AddPuzzleHistory();
            openingBranch.Visible = openingMistakesMode && mistakeOpening is not null && puzzle.OpeningAnchorFen is not null;
            moveLeft = profile.MoveSeconds;
            info.Text = kind + " · " + puzzle.Theme + (Preferences.Current.DebugInformation ? "\n" + puzzle.Origin : "") + (puzzle.PastMove is not null ? "\nDamals in deiner Partie gespielt: " + puzzle.PastMove : "") + (kind == "Bauernverteidigung" ? $"\nHalte das exakte Remis: {profile.DefenseMoves} eigene Züge ohne Bauernbewegung, oder gewinne den Bauern." : "");
            if (openingMistakesMode)
                _ = openingMistakes!.Fill(lifetime.Token);
            if (IsAdvanced)
                _ = decisions.Prefetch(position.Clone(), puzzle.GoalKind == "AdvancedDraw", profile.StrictOptimal);
            _ = endgames.Fill(profile.Endgames, lifetime.Token);
            _ = bank.FillAsync(lifetime.Token);
            if (profile.Positional || profile.PositionalSubtle)
                _ = positionalTrainer.Fill(profile.MinGoodMoves, profile.MaxGoodMoves, lifetime.Token, profile.PositionalSubtle && (!profile.Positional || kind == "Feines Stellungsgefühl"));
        }
        catch (OperationCanceledException)
        {
            if (!token.IsCancellationRequested)
                info.Text = "Vorbereitung hat ihr Rechenzeitbudget erreicht. Erneut starten; keine triviale Ersatzaufgabe.";
        }
        catch (Exception ex)
        {
            if (version == revision)
            {
                info.Text = ex.Message;
                question = false;
                _ = bank.FillAsync(lifetime.Token);
            }
        }
        finally
        {
            if (version == revision)
            {
                loading = false;
                previousTick = tick.ElapsedMilliseconds;
                RefreshBoard();
            }
        }
    }

    private void ClockTick()
    {
        UpdateGenerationDisplay();
        long now = tick.ElapsedMilliseconds;
        if (autoNextAt > 0 && now >= autoNextAt && !loading && !busy)
        {
            autoNextAt = 0;
            _ = NextExercise();
        }

        double elapsed = (now - previousTick) / 1000d;
        previousTick = now;
        if (active && !loading && !busy && question && profile is not null)
        {
            if (profile.TotalSeconds > 0)
            {
                totalLeft -= elapsed;
                if (totalLeft <= 0)
                {
                    EndSession("Gesamtzeit abgelaufen.");
                    return;
                }
            }

            if (profile.MoveSeconds > 0)
            {
                moveLeft -= elapsed;
                if (moveLeft <= 0)
                {
                    if (profile.TotalSeconds > 0 && !profile.ContinueAfterMoveTimeout)
                    {
                        Mistake("Zugzeit abgelaufen.");
                    }
                    else if (Mistake("Zugzeit abgelaufen.")) _ = NextExercise();
                }
            }
        }

        UpdateScoreboard();
    }

    private void UpdateScoreboard()
    {
        clock.Score = score; clock.Lives = remainingLives; clock.MaximumLives = profile?.Lives ?? 0;
        clock.MoveTime = profile?.MoveSeconds > 0 ? Math.Max(0, moveLeft) : null;
        clock.TotalTime = profile?.TotalSeconds > 0 ? Math.Max(0, totalLeft) : null;
        clock.Invalidate();
    }

    private bool Mistake(string message)
    {
        failed = true;
        if (profile?.Tactics == true && (profile.MoveSeconds > 0 || profile.TotalSeconds > 0)
            && !(message == "Zugzeit abgelaufen." && profile.TotalSeconds == 0)) score--;
        AppSounds.Play("error");
        info.BackColor = UiPalette.FromArgb(145, 48, 48);
        info.Text = message;
        if (profile?.Lives > 0) { remainingLives--; clock.AnimateLoss(remainingLives, Preferences.Current.HeartLossAnimation); }
        UpdateScoreboard();
        if (profile?.Lives > 0 && remainingLives <= 0)
        {
            EndSession("Keine Leben mehr.");
            return false;
        }

        if (profile?.MoveSeconds > 0 && message == "Zugzeit abgelaufen." &&
            (profile.TotalSeconds > 0 ? !profile.ContinueAfterMoveTimeout : profile.Lives == 0))
        {
            EndSession(message);
            return false;
        }

        return active;
    }

    private void EndSession(string message)
    {
        historyPlayback.Cancel();
        active = false;
        autoNextAt = 0;
        question = false;
        revision++;
        work.Cancel();
        busy = loading = false;
        board.StopAnimation();
        bool record = profile?.CanLose == true && Math.Max(profile.HighScore, score) > roundStartHighScore;
        SaveScore();
        string heading = record ? "Neuer Highscore!" : message;
        info.Text = heading + $"\nSitzung beendet · {score} Punkte";
        ShowSessionResult(heading);
        RefreshBoard();
        Analyze();
    }

    private void SaveScore()
    {
        if (profile is null)
            return;
        if (profile.CanLose)
            profile.HighScore = Math.Max(1, Math.Max(profile.HighScore, score));
        if (!profile.ExplicitlySaved && profile.Tactics && !mistakesMode && !openingMistakesMode)
            profiles.LastSettings = profile.Copy();
        profiles.Save();
        UpdateStats(profile);
        RefreshPresetCards();
    }

    private void Complete()
    {
        question = false;
        busy = false;
        if (profile is not null)
        {
            profile.Completed++;
            if (!failed)
                score++;
            SaveScore();
        }

        if (puzzle is not null && (kind == "Taktik" || puzzle.GoalKind == "Draw"))
            bank.Complete(puzzle.Id);
        info.BackColor = UiPalette.FromArgb(35, 100, 65);
        info.Text = failed ? "✓ Abgeschlossen mit Hilfe · kein Punkt" : "✓ Gelöst · +1 Punkt";
        RefreshBoard();
        Analyze();
        if (profile?.Tactics == true && (profile.MoveSeconds > 0 || profile.TotalSeconds > 0))
        {
            _ = ScheduleNextAfterSound(revision);
        }
    }

    private async Task ScheduleNextAfterSound(int version)
    {
        await AppSounds.PlayAndWait("success");
        if (!IsDisposed && version == revision && active && !question)
            autoNextAt = tick.ElapsedMilliseconds + 150;
    }
}
