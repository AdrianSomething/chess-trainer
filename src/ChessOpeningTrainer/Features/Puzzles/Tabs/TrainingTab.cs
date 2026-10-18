namespace ChessOpeningTrainer;
public sealed partial class PuzzleSessionForm
{
    [PuzzleTab(50)]
    private void BuildTrainingTab(string dataDirectory)
    {
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 48
        };
        toolbar.Controls.AddRange([next, solution, whole, openingBranch]);
        resume.AutoSize = false;
        openingBranch.Click += (_, _) =>
        {
            if (mistakeOpening is null || puzzle?.OpeningAnchorFen is not string fen) return;
            var selectedOpening = mistakeOpening;
            var branch = new BranchForm(selectedOpening, new ProgressStore(dataDirectory), engine, Position.FromFen(fen));
            branch.LearnRequested += p => Application.OpenForms.OfType<MainForm>().FirstOrDefault()?.OpenLearning(selectedOpening, BranchLearning.Plan(selectedOpening, p));
            branch.Show();
        };
        whole.Click += async (_, _) => await RevealWholeSolution();
        play.Controls.AddRange([board, evaluation, variants, info, clock, history, objective, toolbar, resume]);
        board.Controls.Add(generationDisplay);
        BuildSessionResult();
        foreach (var b in new[]
        {
            next,
            solution,
            whole,
            resume
        }

        )
        {
            b.BackColor = UiPalette.FromArgb(57, 91, 119);
            b.ForeColor = Color.White;
            b.FlatStyle = FlatStyle.Flat;
        }

        resume.BackColor = UiPalette.FromArgb(31, 35, 39);
        resume.ForeColor = UiPalette.Gainsboro;

        void LayoutBoard()
        {
            int size = Math.Max(320, Math.Min(play.ClientSize.Height - (restartBelow.Visible ? 140 : 90), play.ClientSize.Width - 450));
            board.SetBounds(55, 60, size, size);
            evaluation.SetBounds(8, 60, 40, size);
            restartBelow.SetBounds(8,board.Bottom+8,size+47,40);
            int x = size + 78, w = Math.Max(300, play.ClientSize.Width - x - 16);
            objective.SetBounds(x, 60, w, 75);
            clock.SetBounds(x, 140, w, 122);
            info.SetBounds(x, 268, w, 90);
            variants.SetBounds(x, 362, w, 145);
            history.SetBounds(x, 515, w, Math.Max(60, play.ClientSize.Height - 533 - (resume.Visible ? 44 : 0)));
            resume.SetBounds(x, play.ClientSize.Height - 48, w, 38);
        }

        play.Resize += (_, _) => LayoutBoard();
        resume.VisibleChanged += (_, _) => LayoutBoard();
        restartBelow.VisibleChanged += (_,_)=>LayoutBoard();
        LayoutBoard();
        board.SquareClicked += sq =>
        {
            if (busy && !waitingReply || loading)
                return;
            if (selected is int from && TryMove(from, sq, true))
                return;
            selected = sq;
            board.SelectedSquare = sq;
            board.CandidateMoves = waitingReply ? Premoves.Candidates(Display, sq, solverWhite) : MoveGenerator.GenerateLegal(Display).Where(m => m.From == sq).ToList();
            board.Invalidate();
        };
        board.SquareDragged += (a, b) =>
        {
            if ((!busy || waitingReply) && !loading)
                TryMove(a, b, false);
        };
        board.BlankClicked += () =>
        {
            premove = null;
            board.QueuedMove = null;
            Clear();
        };
        variants.Navigate += Preview;
        history.NavigationRequested += async (target, kind) => await PreviewHistoryAsync(target, kind);
        resume.Click += (_, _) =>
        {
            historyPlayback.Cancel(); busy = false;
            review = null;
            history.SelectedIndex = snapshots.Count - 1;
            RefreshBoard();
            if (!question)
                Analyze();
        };
        next.Click += async (_, _) =>
        {
            if (!active)
            {
                BeginSession(profile ?? profiles.Last);
                return;
            }

            if (question && profile?.Tactics == true && profile.TotalSeconds > 0) { score--; AppSounds.Play("error"); UpdateScoreboard(); }
            await NextExercise();
        };
        solution.Click += async (_, _) =>
        {
            if (question && !busy)
            {
                if (profile?.Tactics == true && profile.Lives > 0) { await RevealWholeSolution(); return; }
                failed = true;
                await Reveal();
            }
        };
    }
}
