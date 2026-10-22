# Architecture and change map

The application is a **modular monolith**: one WinForms assembly, with responsibility boundaries expressed as folders and focused files. The existing ChessOpeningTrainer namespace and serialized contracts remain stable. Publication preparation does not introduce a framework, service layer or UI rewrite.

## Layout

| Area | Responsibility |
| --- | --- |
| App | Entry point, main window, home menu |
| Domain/Chess | Positions, legal moves, notation, material, variant rules |
| Domain/Openings and Domain/Learning | Repertoire and progress models |
| Infrastructure/Engine | UCI processes, pools, staged evaluation, cancellation |
| Infrastructure/Storage | Opening, game, progress, preset and statistics persistence |
| Infrastructure/Customization | Preferences, languages, skins, sounds |
| UI/Board | Shared rendering, interaction, history, animation, promotion |
| UI/Analysis | Evaluation and variation presentation |
| UI/Common and UI/Theming | Reusable controls, settings, palettes, native styling |
| Features/Learning | Card selection, recall and continuation |
| Features/Puzzles/{Models,Generation,Session,Tabs,Legacy} | Training contracts, generation, lifecycle, registered views and older integration |
| Features/Branches/Actions | Individual branch actions |
| Features/Generator | Repertoire generation, parameters and progress preview |
| Features/Games | PGN library, analysis, highlights |
| Features/FreePlay and Features/LocalMatch | Setup, custom games, two-player games and clocks |
| Features/Vision | Chess vision exercises, buffers and statistics |
| Features/Datasets | Dataset catalog, import/download/trim views and lazy frequency-tree browsing |
| Domain/Datasets and Infrastructure/Storage/Datasets | Filter contracts, streaming PGN, compact move/index codecs, local persistence and resumable HTTP |
| Features/Tutorial | Window-local guided tour, input policy and first-run card |

All paths above are relative to src/ChessOpeningTrainer.

## Focused entry points

| Change | Start here |
| --- | --- |
| Rules / castling / en passant | Domain/Chess/MoveGenerator.cs and Position.cs |
| Board input, silhouettes, drawing | UI/Board/BoardControl.Input.cs, .Pieces.cs, .Rendering.cs |
| Shared history playback / reverse animation | UI/Board/HistoryPlayback.cs, BoardControl.Animation.cs, .ReverseAnimation.cs |
| Selection cleanup / preview | UI/Board/BoardPresentation.cs |
| Evaluation cache / staged searches | UI/Analysis/AnalysisPresenter.cs, Infrastructure/Engine/AnalysisSearch.cs |
| Persistent engine workers / limits | Infrastructure/Engine/EnginePool.cs, GenerationBudget.cs |
| Puzzle source / refill | Features/Puzzles/Generation, PuzzleSupply.cs, PuzzleBank.Refill.cs |
| Puzzle view registration | Features/Puzzles/Tabs; PuzzleTab attribute |
| Branch feature registration | Features/Branches/Actions; IBranchFeature and BranchFeature attribute |
| Branch layout / crossing reduction | BranchGraphBuilder.cs, BranchLayoutCache.cs, BranchBetaLayout.cs, BranchCrossingReduction.cs, optional BranchGlobalRefinement.cs; BranchCanvas.cs renders the result |
| Extract an opening variation | Features/Branches/BranchExtraction.cs, Actions/ExtractOpeningFeature.cs |
| Staged branch edits / undo / persistence | BranchGraphEdit.cs, BranchForm.Editing.cs, BranchEditSession.cs |
| Match-to-branch mapping | BranchGameAnalysis.cs, BranchGameMatchIndex.cs, BranchAnalysisCaptions.cs |
| Learning selection / navigation | Features/Learning/MainForm.Lessons.cs, MainForm.LessonNavigation.cs |
| PGN and analysis | Features/Games/PgnImport.cs, PgnComposer.cs, GameAnalysisQueue.cs, GameHighlights.cs |
| Setup / multiple kings | Features/FreePlay/PositionSetupControl.cs, SetupMaterial.cs; Domain/Chess/KingCaptureRules.cs |
| Local game clocks and analysis | Features/LocalMatch/LocalMatchForm.Pause.cs, .Analysis.cs |
| Vision buffer / comparisons | Features/Vision/VisionBuffer.cs, VisionComparisonStock.cs, VisionComparisonGenerator.cs |
| Statistics / demo switch | VisionTimelineChart.cs, VisionTimelineData.cs, VisionSettingsEditor.cs |
| Generator sections / forecasts | Features/Generator/DepthSection.cs, GeneratorForecast.cs, GeneratorPreviewGraph.cs |
| Generator job scheduling / approval / cooling | Features/Generator/GeneratorWorkQueue.cs; OpeningGeneratorForm.Queue.cs owns its UI lifetime |
| Guided overview / first-run language | Features/Tutorial; App/MainForm.Tutorial.cs defines the route |
| Reversible tutorial checkpoints | App/MainForm.TutorialLesson.cs restores lesson state; the route prepares/closes section windows; TutorialSession owns input and confirmation |
| Pre-analysed tutorial game | Features/Tutorial/TutorialSampleGame.cs; embedded Samples/tutorial-game.json, isolated tutorial storage |
| Opening menu bookmarks / order | App/OpeningMenuOrder.cs; UserPreferences.BookmarkedOpenings stores IDs only |
| Audio recovery | Infrastructure/Customization/SoundVoice.cs |
| Dataset index / pruning / size forecast | DatasetIndexBuilder, DatasetIndexFormat, DatasetIndex, DatasetForecast; PackedMove is the shared 15-bit translation layer |
| Dataset navigation / position frequencies | DatasetGraphModel reuses BranchCanvas; DatasetResultsStrip is shared with BranchForm.Datasets |
| Languages and tooltips | Infrastructure/Customization/L.cs, Customization/languages.json, UI/Common/LocalizedToolTips.cs |
| Theme and native controls | UI/Theming/UiPalette.cs, ComboArrowSurface.cs, WindowTitleTheme.cs |
| Fresh public demonstration game | Infrastructure/Storage/BundledSampleGame.cs |

## Extension rules

Keep one responsibility per file. A puzzle view uses the existing PuzzleTab attribute registration; a branch action implements IBranchFeature and uses BranchFeature. Registration inspects known application code, not downloaded plugin assemblies. A new view alone does not implement new chess rules: generation and validation still belong to the corresponding services.

Domain stays UI- and process-free. Generators do not know Controls. Engine communication belongs in Infrastructure/Engine and persistence in Infrastructure/Storage. Do not add a global singleton to share new feature state.

BoardControl remains the common board implementation. BoardPresentation handles presentation operations; StudyBoard combines board/history/analysis where appropriate. AnalysisPresenter manages UI/cache state; AnalysisSearch performs engine work without WinForms controls.

Partial form files intentionally share window state. This is not a complete MVVM/MVP conversion; future UI changes should replace views/adapters without duplicating chess rules or UCI services.

## Background work and compatibility

Searches and generation have cancellation/lifetime ownership. Preserve puzzle preparation and analysis caches: removing them to reduce idle CPU changes the user experience. Avoid stale UI updates after a position/window changes. Native audio playback is STA-bound.

Branch layout memoization stores only bounded, immutable coordinate arrays keyed by graph structure and layout options, never selections or progress. Editing the structure invalidates the key; each window receives independent vertices. Tutorial dimming is a child control of its host; live board frames update behind the dimming layer, and hover previews are not treated as separate tutorial destinations.

Keep serialized names, defaults and data locations compatible. New optional settings need safe defaults. AppData, outputs and work are private; tests must use isolated directories. Publication preparation adds an allowlisted export, not a second maintained copy of application source.
