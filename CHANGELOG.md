# Changelog

Earlier private iterations are not presented as independently published releases.

## 0.26.5-beta — Development

- Paint the Lichess download catalog as one virtualized, double-buffered surface. Wheel and thumb dragging update immediately without moving hundreds of child controls; selected rows have one continuous highlight across all columns.
- Label the selected dataset count explicitly in both languages, retaining click/Ctrl/Shift selection.
- Give each download and import an independent pause-aware elapsed/remaining clock. Keep the queue-wide total separate, including when downloads and imports overlap.
- Paint branch selection outlines after all nodes, captions and other borders so neighboring nodes cannot hide the selection.
- Add synthetic rendering, selection, scrolling and independent-clock regressions. No index format or stable-release change.

## 0.26.4-beta — Development

- Make the streaming compact dataset builder the only/default build path; remove the old SQL aggregation builder and its beta switch. Keep readers for existing index formats and preserve the compact format, exact counters and merge/pruning rules.
- Show current/total merge passes and block groups, consumed entries and a merge-specific remaining-time estimate that excludes pauses.
- Forecast pruned tree size from growth between two nested samples at the selected cutoff. Prefer exact measurements when a complete suitable index exists. Budget simultaneous merge inputs/output separately from final tree size.
- Keep Minimum games per node at 0: 1 is not equivalent, because it stops continuations at positions occurring in only one game.
- Replace the branch-mode screenshot with the supplied updated image.

## 0.26.3-beta — Development

- Add an opt-in streaming compact dataset index: bounded aggregation, compressed sorted runs, multi-pass merge/reduce, adaptive row/column blocks, and disk-bitset reachability pruning. Preserve exact results, repetitions, transpositions and custom starting positions; keep formats 2/3 readable.
- Stop the dataset activity ring while paused, highlight Resume, and use app-language confirmation buttons rather than Windows-language Yes/No buttons. Translate the black-win trim filter.
- Collapse forced lines leading to later forks into an expandable first-move/ellipsis node; keep terminal-line previews and reversible expansion.
- Make Sugiyama the default branch layout. Move the three mutually exclusive choices (Sugiyama, Legacy, stronger-crossing alternative) into Settings → Branches.
- Reflow opening Freeplay history immediately when Top 3 changes; add the shared optional Game statistics strip beside that checkbox.
- Extend synthetic CI checks for compact/legacy equivalence, multi-pass merging, transpositions, pruning and cancellation. Stable releases and opening file formats are unchanged.

## 0.26.2-beta — Development

- Correct reversed board-square parity in the shared renderer: a1/d8 are dark, d1/h1 are light. Add off-screen pixel checks for every square, both orientations and all bundled skins.

- Enable a read-only, bounded Windows CI build with public synthetic rules, codec, storage and graph checks; no live archive downloads or automatic releases.
- Align dataset cards, bookmark/rename controls and English naming with the home menu; use operation-specific close/resume messages.
- Show actual tree size and recorded peak workspace; measure stricter cutoffs against complete position frequencies instead of presenting misleading sample-scaled rare-tail intervals.
- Store temporary edge counters inside position rows. The 1,000-game comparison reduces peak aggregation workspace from 6,520,832 to 3,584,000 bytes without changing the final index or any counts.
- Add an activity ring during indexing and use recent throughput for remaining-time estimates.
- Collapse only terminal forced lines into a single wide node, restore their collapse handle after expansion, fit visible node bounds and show absolute game totals explicitly.
- Improve order-preserving centering after stronger crossing reduction; add a separate opt-in, bounded Sugiyama-family layout with a crossing-count guard.
- Keep stable downloads, opening formats and existing dataset indexes unchanged.

## 0.26.1-beta — Development

- Align dataset cards, download selection, settings, scrollbars and compact filter sections with the other modes; add bookmarks and explicit rename confirmation.
- Add operation pause/resume, cancellation and stop-and-save, durable import checkpoints, elapsed/remaining estimates and download/import overlap with queue totals.
- Accept valid zero-move games and retain rejection-reason counts for newly imported unsupported records.
- Reduce temporary storage with order-preserving empty-square run coding and by reconstructing destination positions instead of storing them per edge. Use compact pure-result counters and share identical node/edge counters in the final index; read existing 0.26.0 indexes.
- Base temporary-space forecasts on a measured sample workspace, show live working-file usage, and avoid identical lower/upper size ranges.
- Add compact, expandable forced lines, stronger contrasting outcome borders, column spacing and selection retention to the dataset browser.
- Add shared asynchronous game-frequency bars to learning, Custom Game, game analysis and the Opening Branch Generator.
- Refine order-preserving branch centering without accepting additional measured crossings.
- Keep GitHub Actions disabled by explicit request; update the CI recommendation and document remaining 0.26 issue scope.

## 0.26.0-beta — Development

- Add Data sets: stream PGN, ZIP, gzip and Zstandard archives, download selected Lichess months with checksum verification and resumable progress, and trim local datasets using rating, result, termination, clock and opening-prefix filters.
- Build a disk-backed frequency DAG with exact outcome counts and transpositions. The final index uses binary moves, variable-length counters, prefix-coded positions and independently compressed blocks, not PGN or JSON trees.
- Add an optional continuation cutoff (default 0: unlimited depth). A position reached in at most N games becomes a counted leaf. Active-selection/cutoff changes leave the last complete index intact and display an outdated-index warning.
- Show a sample-based size range, lazy frequency-ordered branches, board previews, outcome bars and links to Custom Game and the Opening Branch Generator. Show dataset position counts in learning branches.
- Add bounded order-preserving alignment to stronger crossing reduction; reject shifts that increase the measured crossing count.
- Extend the bilingual tour with two dataset explanations (48 cards). Document a staged CI proposal; do not enable network download tests or publish private test data.

This is a development build, not a stable release. See [dataset design and limits](docs/DATASETS.md).

## 0.25.3 — 2026-10-21

Stable release of the 0.25 development series. Highlights since 0.24.0:

- Queue opening-generation jobs with priorities, resumable approval blocks, optional work/rest intervals and one shared progress window.
- Keep unfinished job drafts, remove completed jobs, and open their branches or learning cards directly.
- Bookmark openings, extract a selected branch as a separate repertoire, and reuse cached graph layouts. Optional stronger crossing reduction remains opt-in.
- Introduce a reversible 46-step English/German guided tour with an already analysed example game, window-local instructions and explicit interaction confirmations.
- Improve audio recovery during background work and require a unique safe destination with an engine quality check in Save a Piece.
- Fix restored tutorial engine variations and statistics previews.

Existing opening schemas and IDs remain compatible with 0.24.0. The previous release remains available as a fallback. The final beta passed 1,421 local regression checks; stable promotion changes version metadata and release documentation only.

## 0.25.3-beta — Development

- Restore cached engine variations immediately when revisiting a tutorial lesson checkpoint.
- Refresh the dimmed statistics preview after chart invalidation, including repeated committed metric and grouping changes.
- Highlight the statistics chart in step 45 and place its explanation above the chart.

## 0.25.2-beta — Development

- Blocked complete mouse-button sequences on informational tutorial steps, including double-clicks, without intercepting title-bar controls.
- Made tutorial steps reproducible checkpoints: revisiting lessons restores the exercise, revisiting selection tasks clears previous answers, and completed windows close and reopen when needed.
- Prevented duplicate learning windows when repeating the opening action.
- Require committed dropdown choices rather than hover previews. Statistics and evaluation-graph interactions unlock an explicit Next action; arrow-key navigation remains available.
- Added a 46th tutorial card for exploring statistics, revised English/German instructions and refined highlighted areas.
- Kept branch panning/zooming available without node selection during its introductory explanation.
- Refresh dimmed game-library controls alongside the board when navigating its evaluation graph.

## 0.25.1-beta — Development

- Centered tutorial close/navigation glyphs, moved navigation beside the step counter, and enabled Left/Right navigation during interactive steps.
- Suppressed tutorial arrows where the target surrounds the information card; fitted text to narrower cards.
- Require actual statistic-type and time-grouping changes during the tutorial (explicit navigation arrows can still skip steps).
- Bundled a synthetic, pre-analysed tutorial game so its evaluation graph opens without an engine wait.
- Corrected generator forecasts after depth cutoffs and excluded already-completed approach moves from the active frontier.
- Added an optional, default-off stronger crossing-reduction search with a bounded runtime and a strict globally-better-only acceptance rule.
- Shifted generator status text slightly left and enlarged its loading ring.
- Kept opening formats, stable-release behavior and private user data unchanged.

## 0.25.0-beta.1 — Development

- Added prioritized opening-generation jobs with shared engine workers, per-job approval and optional queue-wide pauses.
- Added optional work/rest intervals and a single multi-job graph preview.
- Added extraction of a selected variation, preserving its approach moves and the original repertoire.
- Moved initial branch graph preparation off the UI thread and reused legal-move lists per node.
- Added bounded layout caching and exact crossing-candidate pruning for faster repeated branch loading.
- Added home-menu bookmarks and persistent unsent generator drafts, with completed-job cleanup and direct learning/branch actions.
- Require one safe destination and a depth-10 engine quality check in Save a Piece exercises.
- Added an English/German guided first-run overview and a restart action in General settings.
- Embedded tutorial cards in their host windows, with live board updates, review navigation and keyboard-safe interaction steps.
- Removed synchronous audio-dispatch waits; defer playback until media loading completes and lower background engine process priority.
- Kept opening schemas and existing opening IDs compatible with 0.24.0.

## 0.24.0 — 2026-10-18

- Added English project, installation, development, privacy, contribution, security and license documentation.
- Prepared a curated source export and Windows package without uploading either.
- Recorded verification of Stockfish 19's binary, source and neural network.
- Separated Pixabay audio licensing from the GPL source and synthesized-sound fallback.
- Replaced the public legacy opening with two sanitized generated repertoires.
- Added a removable synthetic game for fresh public profiles.
- Moved the 400-day vision-statistics demo toggle to Chess Vision settings, off by default.
- Added the maintainer's retrospective prompt record and screenshots; removed unused documentation templates.
- Adopted the 0.24.0 development version number for the initial public source publication.
- Assessed future CI without exporting the private tests or enabling workflows.

Existing application feature architecture is unchanged.
