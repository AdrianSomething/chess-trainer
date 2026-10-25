# 0.26 development status

Work is on `dev/0.26.0`; the stable release and its README download remain unchanged.

## Issue alignment (reviewed 2026-10-24)

| Issue | Implemented in the beta series | Still requires work or wider validation |
| --- | --- | --- |
| [#6: database integration](https://github.com/AdrianSomething/chess-trainer/issues/6) | Local imports/Lichess downloads, filters, transposition-aware frequency index, lazy browser, shared statistics in branches/learning/generator/analysis/Custom Game | Dataset-driven generator selection rules and prioritizing common mistakes in opponent-error training are not implemented. These are separate consumers of the index, not implied by displaying a statistics bar. |
| [#7: storage](https://github.com/AdrianSomething/chess-trainer/issues/7) | Packed moves, exact shared positions, variable counters, identical-counter reuse, smaller temporary keys, no stored destination key per edge, optional rare-continuation pruning, measured sample forecasts, live working-file size and cancellation cleanup | Full multi-million-game benchmarks are still needed. No universal 1:32 compression guarantee. See the measured bounded comparison in [DATASETS.md](DATASETS.md). |
| [#8: graph layout](https://github.com/AdrianSomething/chess-trainer/issues/8) | Bounded crossing-reduction heuristic, cached geometry and order-preserving centering; adjustments are rejected if they increase measured crossings | Crossing minimization is heuristic, not a guarantee of an optimal drawing. Retain manual checks on large and unusual graphs. |

Issues remain open: the broader 0.26 goals are not all completed by 0.26.5-beta.

## 0.26.5-beta corrections

- Virtualize the Lichess catalog: draw visible rows and the scrollbar together, with a single full-row selection highlight and an explicit selected-dataset count.
- Separate each download/import clock from the overall queue clock; a new item starts at zero without disturbing concurrent work or pause accounting.
- Render selected branch outlines in a final overlay pass above neighboring nodes and all other markings.
- Keep the streaming dataset index, existing files, stable downloads and main branch unchanged.

## 0.26.4-beta corrections

- Streaming compact indexing is now the sole build path. Remove the superseded SQL index builder, edge payload writer and beta checkbox; keep existing index readers. The sorting, merge fan-in, exact counts and pruning algorithm are unchanged.
- Merge progress shows pass/total passes, block group/total groups, processed input entries and a separate approximate merge ETA. Pauses do not inflate the measured duration; later passes can revise the estimate as duplicate keys coalesce.
- Pruned-size forecasts now extrapolate retained-size growth from two nested samples at the same cutoff instead of displaying a missing-frequency warning. A suitable complete index still takes precedence for exact measurement.
- Workspace estimates account for simultaneous input/output runs (2.25 times projected full-depth size, or the scaled measured sample peak if larger). They are separate from the smaller pruned output and are estimates, not reserved disk quotas.
- Minimum games per node remains 0 by default: unlike 0, a value of 1 removes outgoing continuations at singleton positions. No silent semantic change or profile migration.
- Update the supplied branch screenshot. No stable download-link change, release or main-branch merge.

## 0.26.3-beta corrections

- Settings → Datasets exposes **Streaming compact index (beta)**, off by default. It replaces temporary SQL aggregation with compressed sorted runs; rebuilding writes adaptive format 4. Old indexes stay readable and changing the switch does not overwrite an existing tree.
- Exact counters and graph reachability are compared against the stable builder, including repeated positions, transpositions, custom FENs, castling, en passant and promotion. Larger datasets still need wider performance validation; no 10-million-game latency guarantee is inferred from small tests.
- Dataset pause freezes the ring and highlights Resume; confirmation buttons follow the app language. The black-win filter is translated.
- Forced prefixes leading to a fork are now collapsible first-move/ellipsis nodes; terminal tails retain their wide preview.
- Settings → Branches now selects Sugiyama (default), Legacy (the previous normal layout), or the stronger-crossing alternative. The old independent layout checkboxes are removed from the branch sidebar; old serialized fields remain readable.
- Opening Freeplay immediately reflows history when Top 3 is toggled and exposes Game statistics on the same row.

## 0.26.2-beta corrections

- Correct shared board colors (a1/d8 dark, d1/h1 light), with off-screen rendered-pixel checks in both orientations and all bundled skins. Chess rules and stored positions are unchanged.

- Enable the bounded Windows build/synthetic-checks workflow; keep releases and large downloads manual.
- Match dataset bookmarks, rename confirmation and button colors to the opening menu; use **Datasets** consistently in English.
- Explain close/resume behavior for the current operation instead of showing one combined warning.
- Show actual completed index size and measured peak build workspace. Measure stricter cutoffs from complete retained frequencies when available; do not extrapolate a tiny rare-tail sample into an arbitrary range.
- Store temporary adjacency in its position row, eliminating repeated parent keys and the separate edge B-tree. The final format stays compatible.
- Compact only complete terminal forced lines into one wide node; preserve expand/collapse controls and fit visible geometry.
- Add whole-column centering and the separate [Sugiyama beta](BRANCH_LAYOUT.md).

## Compatibility and testing

- Old normalized datasets and format-2/3 frequency indexes remain readable. Every rebuild now creates format 4, which requires 0.26.3 or later. Earlier complete index files are not removed automatically. Opening JSON remains compatible.
- No opening migration, personal data import or main-branch merge is part of this beta. Removal of the superseded builder affects source code only, not user files.
- Local tests use isolated synthetic profiles; the bounded archive benchmark reads only a 1,000-record prefix of the provided file.
- Native form/lifecycle smoke tests supplement unit/integration tests. They do not replace human visual acceptance of every theme, DPI and screen size.
- GitHub Actions is enabled for the development branch in 0.26.2; [CI_ASSESSMENT.md](CI_ASSESSMENT.md) describes its limited scope.
