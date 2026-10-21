using System.Drawing.Drawing2D;

namespace ChessOpeningTrainer;
public sealed partial class BranchForm : LocalizedForm
{
    private OpeningDefinition opening;
    private ProgressStore progress;
    private readonly OpeningDefinition originalOpening;
    private readonly ProgressStore originalProgress;
    internal string OpeningDirectory => Path.GetDirectoryName(originalOpening.FilePath)!;
    private readonly StockfishEngine engine;
    private readonly AnalysisPresenter presenter;
    public event Action<Position>? LearnRequested;
    private BranchCanvas canvas;
    private readonly BoardControl board = new()
    {
        InputEnabled = false
    };
    private readonly EvalScale evaluation = new();
    private readonly Label info = new()
    {
        ForeColor = UiPalette.Gainsboro,
        AutoSize = false
    };
    private CancellationTokenSource analysis = new();
    private Action? collapseTutorialSettings;
    internal void CollapseTutorialSettings() => collapseTutorialSettings?.Invoke();
    private bool clearTutorialWhenLoaded;
    internal void ClearTutorialSelection()
    {
        clearTutorialWhenLoaded = true; canvas.ClearSelection();
    }
    internal void SelectTutorialCurrent()
    {
        clearTutorialWhenLoaded = false; canvas.FocusCurrent();
    }
    internal void SetTutorialPanOnly(bool value) => canvas.TutorialPanOnly = value;
    internal void EnsureTutorialSelection()
    {
        clearTutorialWhenLoaded = false;
        if (!canvas.HasSelection) canvas.FocusCurrent();
    }
    public BranchForm(OpeningDefinition opening, ProgressStore progress, StockfishEngine engine, Position? current = null)
    {
        originalOpening = this.opening = opening; originalProgress = this.progress = progress; this.engine = engine;
        presenter = new AnalysisPresenter(engine, evaluation, new VariationRows());
        FormClosed += (_, _) => presenter.Dispose();
        Text = "Branch Modus · " + opening.Name;
        Size = new(1300, 850);
        MinimumSize = new(1000, 700);
        BackColor = UiPalette.FromArgb(31, 35, 39);
        var right = new Panel
        {
            Dock = DockStyle.Right,
            Width = 425,
            AutoScroll = true
        };
        var top = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 92
        };
        canvas = new(opening, progress, current, deferBuild: true);
        graphArea.Controls.Add(canvas); Controls.Add(graphArea);
        Controls.Add(right);
        Controls.Add(top);
        var fit = new Button
        {
            Text = "Gesamter Graph",
            Width = 140
        };
        fit.Click += (_, _) => canvas.Fit();
        var focus = new Button
        {
            Text = "Aktuelle Stellung",
            Width = 150
        };
        focus.Click += (_, _) => canvas.FocusCurrent();
        foreach (var button in new[]
        {
            fit,
            focus
        }

        )
        {
            button.ForeColor = UiPalette.Gainsboro;
            button.BackColor = UiPalette.FromArgb(57, 91, 119);
            button.FlatStyle = FlatStyle.Flat;
        }

        top.Controls.AddRange([fit, focus, new Label { Text = "Ziehen: Kamera · Mausrad: Zoom · Knoten anklicken\nGrün: gelernt · Orange: fällig · Blau: angeschaut · Grau: unbekannt · Gegnerzüge: dunkler", ForeColor = UiPalette.Gainsboro, AutoSize = true }]);
        var grouping = new CheckBox { Text = "Ähnliche Gegenvarianten als gemeinsame Lernkarte", Checked = progress.Data.GroupSimilarReplies, AutoSize = true, ForeColor = UiPalette.Gainsboro };
        var cardCount = new Label { AutoSize = true, ForeColor = UiPalette.Gainsboro };
        void UpdateCount()
        {
            var targets = OpeningStore.LearningTargets(opening);
            cardCount.Text = $"Lernkarten insgesamt: {(grouping.Checked ? targets.Select(LearningGroups.Signature).Distinct().Count() : targets.Count)}";
        }
        grouping.CheckedChanged += (_, _) => { progress.Data.GroupSimilarReplies = grouping.Checked; progress.Save(); UpdateCount(); CheckpointGraphChange(); };
        UpdateCount();
        top.Controls.Add(grouping);
        top.Controls.Add(cardCount);
        right.Controls.AddRange([board, evaluation, info]);
        board.SetBounds(48, 15, 360, 360);
        evaluation.SetBounds(2, 15, 42, 360);
        info.SetBounds(12, 390, 385, 60);
        BranchVertex? selectedVertex = null;
        var branchColor = new Button { Left = 350, Top = 456, Width = 28, Height = 28, Enabled = false, FlatStyle = FlatStyle.Flat, AccessibleName = "Subbranchfarbe" };
        right.Controls.Add(branchColor);
        var actionButtons = new Dictionary<string, Button>();
        var nodeMenu = new ContextMenuStrip();
        FormClosed += (_, _) => nodeMenu.Dispose();
        bool cycleVariants = true;
        var collapsed = new HashSet<string>();
        var compact = new HashSet<string>();
        var learn = new Button
        {
            Text = "Jetzt diesen Branch lernen",
            Left = 15,
            Top = 662,
            Width = 370,
            Height = 36,
            Enabled = false,
            ForeColor = Color.White,
            BackColor = UiPalette.FromArgb(57, 91, 119),
            FlatStyle = FlatStyle.Flat
        };
        right.Controls.Add(learn);
        learn.Click += (_, _) =>
        {
            if (board.Position is not null)
                LearnRequested?.Invoke(board.Position.Clone());
        };
        var alternatives = new ComboBox
        {
            Left = 15,
            Top = 550,
            Width = 370,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        right.Controls.Add(alternatives);
        var priority = new ComboBox
        {
            Left = 15,
            Top = 510,
            Width = 370,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        priority.Items.AddRange(["Aktiv", "Später", "Ausgeschlossen"]);
        priority.SelectedIndex = 0;
        right.Controls.Add(priority);
        var priorityInfo = new Label
        {
            Left = 15,
            Top = 480,
            Width = 380,
            Height = 28,
            ForeColor = UiPalette.Gainsboro
        };
        right.Controls.Add(priorityInfo);
        HashSet<string> Keys(BranchVertex root)
        {
            var result = new HashSet<string>();
            var seen = new HashSet<BranchVertex>();
            var todo = new Stack<BranchVertex>();
            todo.Push(root);
            while (todo.TryPop(out var v))
            {
                if (!seen.Add(v))
                    continue;
                result.UnionWith(v.ProgressKeys);
                foreach (var child in v.Children)
                    todo.Push(child);
            }

            return result;
        }

        void SelectVertex(BranchVertex v)
        {
            int previousVariation = selectedVertex == v ? alternatives.SelectedIndex : 0;
            selectedVertex = v;
            canvas.MarkSelected(v);
            branchColor.Enabled = v.SubbranchRoot is not null;
            branchColor.BackColor = v.SubbranchColor ?? UiPalette.FromArgb(43, 49, 55);
            cycleVariants = true;
            learn.Enabled = true;
            if (actionButtons.TryGetValue("Tiefer erforschen", out var exploreButton)) exploreButton.Enabled = true;
            alternatives.Items.Clear();
            alternatives.Items.AddRange(v.AlternativeNames.Cast<object>().ToArray());
            if (alternatives.Items.Count > 0)
                alternatives.SelectedIndex = Math.Clamp(previousVariation, 0, alternatives.Items.Count - 1);
            var keys = v.ProgressKeys.Count > 0 ? v.ProgressKeys : Keys(v);
            var values = keys.Select(k => progress.Data.BranchPriority.GetValueOrDefault(k) ?? "Aktiv").Distinct().ToList();
            priority.SelectedIndex = values.Count > 0 ? Math.Max(0, priority.Items.IndexOf(values[0])) : 0;
            priority.Enabled = keys.Count > 0;
            priorityInfo.Text = values.Count > 1 ? "Der Folgeast enthält unterschiedliche Prioritäten." : "";
            if (actionButtons.TryGetValue("Subbranch-Status umschalten", out var subButton))
            {
                subButton.Text = v.Nodes.All(n => n.IsSubbranch) ? "Subbranch entfernen" : "Subbranch erstellen";
                subButton.Enabled = v.IsLearnerMove;
            }
            UpdateModeSelection();
        }

        canvas.VertexSelected += SelectVertex;
        priority.SelectionChangeCommitted += (_, _) =>
        {
            if (selectedVertex is null || priority.SelectedIndex < 0)
                return;
            foreach (var key in Keys(selectedVertex))
                progress.Data.BranchPriority[key] = (string)priority.SelectedItem!;
            progress.Save();
            CheckpointGraphChange();
            canvas.Invalidate();
            info.Text = "Priorität gespeichert (inklusive gemeinsamer Folgekarten). Wirksam ab nächster Lernkarte. Wieder aktivierbar.";
        };
        var hideHuman = new CheckBox
        {
            Left = 15,
            Top = 592,
            Width = 380,
            Text = "Engine-untypische Züge ausblenden",
            ForeColor = UiPalette.Gainsboro
        };
        var groups = new CheckBox
        {
            Left = 15,
            Top = 624,
            Width = 380,
            Text = "Gleiche Antworten als Varianten bündeln",
            Checked = true,
            ForeColor = UiPalette.Gainsboro
        };
        hideHuman.AutoSize = groups.AutoSize = true;
        top.Controls.AddRange([hideHuman, groups]);
        var hideSubs = new CheckBox
        {
            Text = "Alle Subbranches einklappen",
            Appearance = Appearance.Button,
            TextAlign = ContentAlignment.MiddleCenter,
            Left = 15,
            Top = 856,
            Width = 370,
            ForeColor = UiPalette.Gainsboro
        };
        right.Controls.Add(hideSubs);
        bool initialBuildPending = true, rebuildAfterLoad = false;
        void Rebuild()
        {
            if (initialBuildPending) { rebuildAfterLoad = true; return; }
            var camera = canvas.Camera;
            var rootBefore = canvas.Vertices.FirstOrDefault()?.Location ?? PointF.Empty;
            string? selectedKey = selectedVertex is null ? null : OpeningStore.PositionKey(selectedVertex.Position);
            var replacement = new BranchCanvas(opening, progress, current, groups.Checked, hideHuman.Checked, hideSubs.Checked, collapsed, compact,
                fastLayout: IsEditingMode);
            var previous = canvas;
            SuspendLayout();
            canvas = replacement;
            graphArea.Controls.Add(canvas);
            canvas.BringToFront();
            graphArea.Controls.Remove(previous);
            previous.Dispose();
            ResumeLayout(true);
            canvas.Selected += ShowPosition;
            canvas.VertexSelected += SelectVertex;
            canvas.NodeMenuRequested += ShowNodeMenu;
            WireHandles();
            selectedVertex = null;
            alternatives.Items.Clear();
            priority.SelectedIndex = 0;
            learn.Enabled = false;
            var rootAfter = canvas.Vertices.FirstOrDefault()?.Location ?? PointF.Empty;
            canvas.Camera = (camera.Zoom, new PointF(camera.Pan.X + (rootBefore.X - rootAfter.X) * camera.Zoom,
                camera.Pan.Y + (rootBefore.Y - rootAfter.Y) * camera.Zoom));
            if (!canvas.HasVisibleNodes)
                canvas.FocusCurrent();
            var restored = canvas.Vertices.FirstOrDefault(v => v.Alternatives.Any(p => OpeningStore.PositionKey(p) == selectedKey));
            if (restored is not null)
                SelectVertex(restored);
            UpdateCount(); ApplyModeCanvas(); UpdateModeSelection(); CheckpointGraphChange();
        }

        hideSubs.CheckedChanged += (_, _) => Rebuild();
        branchColor.Click += (_, _) =>
        {
            var root = selectedVertex?.SubbranchRoot;
            if (root is null) return;
            var original = branchColor.BackColor;
            using var picker = new ColorWheelDialog(original);
            picker.ColorChanged += color => { branchColor.BackColor = color; canvas.PreviewSubbranchColor(root, color); };
            if (picker.ShowDialog(this) != DialogResult.OK)
            {
                branchColor.BackColor = original; canvas.PreviewSubbranchColor(root, original); return;
            }
            foreach (var node in root.Nodes.Where(n => n.IsSubbranch)) node.SubbranchColor = ColorTranslator.ToHtml(picker.SelectedColor);
            branchColor.BackColor = picker.SelectedColor; canvas.PreviewSubbranchColor(root, picker.SelectedColor);
            try { OpeningStore.Save(opening, opening.FilePath); CheckpointGraphChange(); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { MessageBox.Show(this, ex.Message); }
        };
        hideHuman.CheckedChanged += (_, _) => Rebuild();
        groups.CheckedChanged += (_, _) => Rebuild();
        void ShowPosition(Position p)
        {
            BoardPresentation.Preview(board, p, !opening.LearnsWhite);
            board.HintMove = LastMoveFor(p);
            evaluation.WhiteAtBottom = opening.LearnsWhite;
            presenter.Show(p, true, false, true, MoveGenerator.GameState(p, new Dictionary<string, int>()));
            info.Text = "Knotenstellung · Hintergrundanalyse\n" + p.ToFen();
            info.Visible = Preferences.Current.DebugInformation;
            ModePositionChanged(p);
        }

        canvas.Selected += ShowPosition;
        canvas.NodeMenuRequested += ShowNodeMenu;
        WireHandles();
        void MaterializeGlobalCollapse()
        {
            if (!hideSubs.Checked) return;
            using var full = new BranchCanvas(opening, progress, current, groups.Checked, hideHuman.Checked);
            foreach (var root in full.Vertices.Where(v => v.Nodes.Any(n => n.IsSubbranch)))
                foreach (var p in root.Alternatives) collapsed.Add(OpeningStore.PositionKey(p));
        }
        Move? LastMoveFor(Position p)
        {
            string key = OpeningStore.PositionKey(p);
            foreach (var parent in canvas.Vertices.Where(v => v.Children.Any(c => c.Alternatives.Any(a => OpeningStore.PositionKey(a) == key))))
                foreach (var before in parent.Alternatives)
                    foreach (var move in MoveGenerator.GenerateLegal(before))
                    {
                        var after = before.Clone(); after.Apply(move);
                        if (OpeningStore.PositionKey(after) == key) return move;
                    }
            return null;
        }
        void WireHandles()
        {
            canvas.SelectionCleared += () =>
            {
                selectedVertex = null; cycleVariants = false;
                branchColor.Enabled = false;
                alternatives.Items.Clear(); priority.Enabled = false; learn.Enabled = false;
                foreach (string key in new[] { "Subbranch-Status umschalten", "Tiefer erforschen" })
                    if (actionButtons.TryGetValue(key, out var button)) button.Enabled = false;
                UpdateModeSelection();
            };
            canvas.ToggleVariants += vertex =>
            {
                SelectVertex(vertex);
                foreach (var p in vertex.Alternatives)
                    if (vertex.Compact) compact.Remove(OpeningStore.PositionKey(p)); else compact.Add(OpeningStore.PositionKey(p));
                Rebuild();
            };
            canvas.ToggleSubbranch += vertex =>
            {
                SelectVertex(vertex);
                MaterializeGlobalCollapse();
                foreach (var p in vertex.Alternatives)
                    if (vertex.Collapsed) collapsed.Remove(OpeningStore.PositionKey(p)); else collapsed.Add(OpeningStore.PositionKey(p));
                if (hideSubs.Checked) hideSubs.Checked = false; else Rebuild();
            };
        }
        void ShowNodeMenu(BranchVertex vertex, Point point)
        {
            foreach (ToolStripItem old in nodeMenu.Items.Cast<ToolStripItem>().ToArray()) old.Dispose();
            nodeMenu.Items.Clear();
            void Action(string title, string source, bool enabled = true)
            {
                var item = nodeMenu.Items.Add(L.T(title));
                item.Enabled = enabled;
                item.Click += (_, _) => { if (actionButtons.TryGetValue(source, out var button)) button.PerformClick(); };
            }
            bool sub = vertex.Nodes.Any(n => n.IsSubbranch);
            Action(sub ? "Subbranch löschen" : "Subbranch erstellen", "Subbranch-Status umschalten", vertex.IsLearnerMove);
            Action("Tiefer erforschen", "Tiefer erforschen");
            if (sub)
            {
                bool closed = vertex.Collapsed;
                var toggle = nodeMenu.Items.Add(L.T(closed ? "Aufklappen" : "Zuklappen"));
                toggle.Click += (_, _) =>
                {
                    MaterializeGlobalCollapse();
                    foreach (var p in vertex.Alternatives)
                        if (closed) collapsed.Remove(OpeningStore.PositionKey(p)); else collapsed.Add(OpeningStore.PositionKey(p));
                    if (hideSubs.Checked) hideSubs.Checked = false; else Rebuild();
                };
            }
            var keys = Keys(vertex);
            var reset = nodeMenu.Items.Add(L.T("Lernstatus dieses Branches zurücksetzen"));
            reset.Enabled = keys.Any(k => progress.Status(k) != "Unbekannt");
            reset.Click += (_, _) =>
            {
                if (L.Message(this, "Lernstatus dieses Branches einschließlich gemeinsamer Folgekarten zurücksetzen?", "Bestätigen", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
                foreach (var key in keys) progress.Data.Moves.Remove(key);
                progress.Save();
                CheckpointGraphChange();
                canvas.Invalidate();
            };
            var priorityMenu = new ToolStripMenuItem(L.T("Lernpriorität"));
            foreach (string state in new[] { "Aktiv", "Später", "Ausgeschlossen" })
            {
                var item = new ToolStripMenuItem(L.T(state)) { Checked = keys.Count > 0 && keys.All(k => (progress.Data.BranchPriority.GetValueOrDefault(k) ?? "Aktiv") == state) };
                item.Click += (_, _) =>
                {
                    foreach (var key in keys) progress.Data.BranchPriority[key] = state;
                    progress.Save(); CheckpointGraphChange(); SelectVertex(vertex); canvas.Invalidate();
                };
                priorityMenu.DropDownItems.Add(item);
            }
            nodeMenu.Items.Add(priorityMenu);
            var view = new ToolStripMenuItem(L.T("Zug anschauen"));
            for (int i = 0; i < vertex.Alternatives.Count; i++)
            {
                int index = i;
                var item = view.DropDownItems.Add(vertex.AlternativeNames[i]);
                item.Click += (_, _) => { cycleVariants = false; alternatives.SelectedIndex = index; };
            }
            nodeMenu.Items.Add(view);
            AppendModeMenu(nodeMenu);
            nodeMenu.Show(canvas, point);
        }
        Shown += async (_, _) =>
        {
            using var loading = new BranchLoadingOverlay { Dock = DockStyle.Fill };
            graphArea.Controls.Add(loading); loading.BringToFront();
            top.Enabled = right.Enabled = false;
            try
            {
                var vertices = await Task.Run(() => BranchGraphBuilder.Build(opening));
                if (IsDisposed) return;
                canvas.Vertices.AddRange(vertices);
                canvas.FocusCurrent();
                ShowPosition(current ?? Position.FromFen(opening.StartFen ?? Position.InitialFen));
                ApplyModeCanvas(); UpdateModeSelection();
                if (clearTutorialWhenLoaded) canvas.ClearSelection();
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            { if (!IsDisposed) L.Message(this, ex.Message, "Branch"); }
            finally
            {
                initialBuildPending = false;
                if (!IsDisposed)
                {
                    graphArea.Controls.Remove(loading); top.Enabled = right.Enabled = true;
                    if (rebuildAfterLoad) Rebuild();
                }
            }
        };
        FormClosed += (_, _) => analysis.Cancel();
        alternatives.SelectedIndexChanged += (_, _) =>
        {
            if (selectedVertex is not null && alternatives.SelectedIndex >= 0)
                ShowPosition(selectedVertex.Alternatives[alternatives.SelectedIndex]);
        };
        alternatives.SelectionChangeCommitted += (_, _) => cycleVariants = false;
        var cycle = new System.Windows.Forms.Timer
        {
            Interval = 2500
        };
        cycle.Tick += (_, _) =>
        {
            if (!Visible || WindowState == FormWindowState.Minimized)
                return;
            if (!IsEditingMode && cycleVariants && !alternatives.DroppedDown && alternatives.Items.Count > 1)
                alternatives.SelectedIndex = (alternatives.SelectedIndex + 1) % alternatives.Items.Count;
        };
        cycle.Start();
        FormClosed += (_, _) => cycle.Dispose();
        Button ActionButton(string text, int y)
        {
            var button = new Button
            {
                Text = text,
                Left = 15,
                Top = y,
                Width = 370,
                Height = 32,
                ForeColor = Color.White,
                BackColor = UiPalette.FromArgb(57, 91, 119)
            };
            right.Controls.Add(button);
            actionButtons[text] = button;
            return button;
        }

        BranchFeatureRegistry.Install(new(this, opening, engine, board, info, analysis, () => selectedVertex, Rebuild, ActionButton, collapsed, compact, hideSubs, () => this.opening, () => featureCancellation ?? analysis));
        actionButtons["Subbranch-Status umschalten"].Text = "Subbranch erstellen";
        actionButtons["Subbranch-Status umschalten"].Enabled = false;
        var variantLabel = new Label { Text = "Stellungsvarianten", ForeColor = UiPalette.Gainsboro };
        variantLabel.SetBounds(15, 490, 120, 24);
        right.Controls.Add(variantLabel);
        // Compact, stable sidebar order; context actions remain available but no
        // duplicate collapse/expand buttons beside the graph handles.
        alternatives.SetBounds(140, 487, 245, 28);
        priorityInfo.SetBounds(15, 555, 370, 24);
        var priorityLabel = new Label { Text = "Lernpriorität", ForeColor = UiPalette.Gainsboro };
        priorityLabel.SetBounds(15, 528, 120, 26);
        right.Controls.Add(priorityLabel);
        priority.SetBounds(140, 525, 245, 28);
        int actionY = 582;
        foreach (string name in new[] { "Subbranch-Status umschalten", "Jetzt diesen Branch lernen", "Tiefer erforschen", "Variante als eigenes Opening speichern", "Alle Subbranches einklappen", "Human Moves neu generieren", "Mit anderem Opening kombinieren" })
        {
            Control? control = name == "Jetzt diesen Branch lernen" ? learn : name == "Alle Subbranches einklappen" ? hideSubs : actionButtons.GetValueOrDefault(name);
            if (control is null && name.StartsWith("Mit anderem")) control = actionButtons.FirstOrDefault(x => x.Key.StartsWith("Mit andere")).Value;
            if (control is null) continue;
            control.SetBounds(15, actionY, 370, 34); actionY += 40;
        }
        foreach (string name in new[] { "Diesen Subbranch auf-/zuklappen", "Stellungsvarianten vergrößern/verkleinern" })
            if (actionButtons.TryGetValue(name, out var duplicate)) duplicate.Visible = false;
        // Global graph controls remain reachable when the settings column is collapsed.
        top.Height = 70;
        top.Controls.Clear();
        hideSubs.AutoSize = true;
        var toggleSettings = new Button { Text = "◀", Dock = DockStyle.Right, Width = 18, FlatStyle = FlatStyle.Flat, AccessibleName = "Einstellungen ein-/ausklappen" };
        top.Controls.AddRange([fit, focus, hideSubs, cardCount]);
        var settingsHost = new Panel { Dock = DockStyle.Left, Width = 273 };
        var settings = new FlowLayoutPanel
        {
            Dock = DockStyle.Left, Width = 255, FlowDirection = FlowDirection.TopDown,
            WrapContents = false, AutoScroll = true, Padding = new Padding(8)
        };
        settingsHost.Controls.Add(settings); settingsHost.Controls.Add(toggleSettings);
        Controls.Add(settingsHost); settingsHost.BringToFront(); top.BringToFront();
        foreach (var check in new[] { grouping, groups, hideHuman })
        {
            check.AutoSize = false; check.Size = new Size(230, 52);
            settings.Controls.Add(check);
        }
        bool updatingLayoutSettings = false;
        var legacyLayout = new CheckBox { Text = "Bisherige Branchpositionierung verwenden", Checked = Preferences.Current.BranchLegacyPositioning, Size = new(230, 48), ForeColor = UiPalette.Gainsboro };
        settings.Controls.Add(legacyLayout);
        var refinement = new CheckBox { Text = "Stärkere Kreuzungsreduktion (Beta)", Checked = Preferences.Current.BranchGlobalRefinementBeta, Size = new(230, 48), ForeColor = UiPalette.Gainsboro };
        settings.Controls.Add(refinement);
        refinement.CheckedChanged += async (_, _) =>
        {
            Preferences.Current.BranchGlobalRefinementBeta = refinement.Checked; Preferences.Save();
            if (!refinement.Checked || IsEditingMode) { Rebuild(); return; }
            bool group = groups.Checked, hide = hideHuman.Checked, hideAll = hideSubs.Checked;
            var closedNodes = collapsed.ToHashSet(); var compactNodes = compact.ToHashSet(); var source = opening;
            using var loading = new BranchLoadingOverlay { Dock = DockStyle.Fill };
            graphArea.Controls.Add(loading); loading.BringToFront(); top.Enabled = settings.Enabled = right.Enabled = false;
            try
            {
                await Task.Run(() => BranchGraphBuilder.Build(source, group, hide, hideAll, closedNodes, compactNodes));
                if (!IsDisposed && ReferenceEquals(source, opening)) Rebuild();
            }
            finally
            {
                if (!IsDisposed) { graphArea.Controls.Remove(loading); top.Enabled = settings.Enabled = right.Enabled = true; }
            }
        };
        var spacingRow = new FlowLayoutPanel { Size = new(230, 64), Margin = new(3, 3, 3, 8), WrapContents = true };
        var spacingLabel = new Label { Text = "Horizontaler Spaltenabstand", Width = 224, Height = 24, ForeColor = UiPalette.Gainsboro };
        var spacing = new ScrollNumber
        {
            Name = "BranchColumnSpacing", Minimum = (decimal)BranchLayoutOptions.MinimumSpacing, Maximum = (decimal)BranchLayoutOptions.MaximumSpacing, DecimalPlaces = 2, Increment = .25m,
            Value = (decimal)BranchLayoutOptions.ClampSpacing(Preferences.Current.BranchColumnSpacing), Width = 100
        };
        spacingRow.Controls.AddRange([spacingLabel, spacing, new Label { Text = "×", AutoSize = true, Margin = new(4, 5, 0, 0) }]);
        settings.Controls.Add(spacingRow);
        var layoutTips = new ToolTip { AutoPopDelay = 12000 };
        L.SetToolTip(layoutTips, refinement, "Zusätzliche, zeitlich begrenzte Suche nach weniger Verbindungskreuzungen. Nur messbar bessere Anordnungen werden übernommen. Bei großen Branches dauert das erste Laden etwas länger; das Ergebnis wird zwischengespeichert.");
        L.SetToolTip(layoutTips, spacing, "Ändert nur den horizontalen Abstand der Graphspalten auf das 0,25- bis 5-Fache. Knotengröße, Zoom und Kameraposition am Startknoten bleiben erhalten.");
        legacyLayout.CheckedChanged += (_, _) =>
        {
            if (updatingLayoutSettings) return;
            if (Preferences.Current.BranchLegacyPositioning != legacyLayout.Checked)
            {
                Preferences.Current.BranchLegacyPositioning = legacyLayout.Checked;
                Preferences.Save();
            }
            Rebuild();
        };
        spacing.ValueChanged += (_, _) =>
        {
            if (updatingLayoutSettings) return;
            Preferences.Current.BranchColumnSpacing = (double)spacing.Value;
            Preferences.Save();
            Rebuild();
        };
        void UpdateLayoutSettings()
        {
            if (IsDisposed) return;
            decimal nextSpacing = (decimal)BranchLayoutOptions.ClampSpacing(Preferences.Current.BranchColumnSpacing);
            bool nextLegacy = Preferences.Current.BranchLegacyPositioning;
            if (legacyLayout.Checked == nextLegacy && spacing.Value == nextSpacing) return;
            updatingLayoutSettings = true;
            try { legacyLayout.Checked = nextLegacy; spacing.Value = nextSpacing; }
            finally { updatingLayoutSettings = false; }
            Rebuild();
        }
        Preferences.Changed += UpdateLayoutSettings;
        FormClosed += (_, _) => { Preferences.Changed -= UpdateLayoutSettings; layoutTips.Dispose(); };
        foreach (string key in new[] { "Human Moves neu generieren", "Mit anderem Opening kombinieren" })
        {
            var button = actionButtons.GetValueOrDefault(key)
                ?? actionButtons.FirstOrDefault(x => key.StartsWith("Mit anderem") && x.Key.StartsWith("Mit andere")).Value;
            if (button is null) continue;
            button.Size = new Size(230, 52);
            if (key.StartsWith("Human")) button.Text = "Engine-untypische Züge neu generieren";
            settings.Controls.Add(button);
        }
        toggleSettings.Click += (_, _) =>
        {
            settings.Visible = !settings.Visible;
            settingsHost.Width = settings.Visible ? 273 : 18;
            toggleSettings.Text = settings.Visible ? "◀" : "▶";
        };
        collapseTutorialSettings = () =>
        {
            settings.Visible = false; settingsHost.Width = 18; toggleSettings.Text = "▶";
        };
        Controls.SetChildIndex(top, Controls.Count - 1);
        Controls.SetChildIndex(right, Controls.Count - 2);
        // Dock the fill host after the surrounding toolbars/sidebar. Bringing
        // only its nested canvas forward does not order the host in this form.
        graphArea.BringToFront();
        canvas.BringToFront();
        incomingMoveFor = LastMoveFor;
        InitializeModes(top, right, settings, alternatives, () => selectedVertex, Rebuild,
            (definition, store) => { opening = this.opening = definition; progress = this.progress = store; });
        var refresh = new System.Windows.Forms.Timer
        {
            Interval = 750
        };
        refresh.Tick += (_, _) =>
        {
            if (Visible && WindowState != FormWindowState.Minimized)
                canvas.Invalidate();
        };
        refresh.Start();
        FormClosed += (_, _) => refresh.Dispose();
    }
}
