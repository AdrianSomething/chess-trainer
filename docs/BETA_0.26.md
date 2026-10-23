# 0.26 development status

Work is on `dev/0.26.0`; the stable release and its README download remain unchanged.

## Issue alignment (reviewed 2026-10-24)

| Issue | Implemented in the beta series | Still requires work or wider validation |
| --- | --- | --- |
| [#6: database integration](https://github.com/AdrianSomething/chess-trainer/issues/6) | Local imports/Lichess downloads, filters, transposition-aware frequency index, lazy browser, shared statistics in branches/learning/generator/analysis/Custom Game | Dataset-driven generator selection rules and prioritizing common mistakes in opponent-error training are not implemented. These are separate consumers of the index, not implied by displaying a statistics bar. |
| [#7: storage](https://github.com/AdrianSomething/chess-trainer/issues/7) | Packed moves, exact shared positions, variable counters, identical-counter reuse, smaller temporary keys, no stored destination key per edge, optional rare-continuation pruning, measured sample forecasts, live working-file size and cancellation cleanup | Full multi-million-game benchmarks are still needed. No universal 1:32 compression guarantee. See the measured bounded comparison in [DATASETS.md](DATASETS.md). |
| [#8: graph layout](https://github.com/AdrianSomething/chess-trainer/issues/8) | Bounded crossing-reduction heuristic, cached geometry and order-preserving centering; adjustments are rejected if they increase measured crossings | Crossing minimization is heuristic, not a guarantee of an optimal drawing. Retain manual checks on large and unusual graphs. |

Issues remain open: the broader 0.26 goals are not all completed by 0.26.1-beta.

## Compatibility and testing

- Old normalized datasets and format-2 frequency indexes remain readable. A rebuild creates format 3; keep an older snapshot when testing an older app version.
- No opening migration, personal data import, source-file deletion or main-branch merge is part of this beta.
- Local tests use isolated synthetic profiles; the bounded archive benchmark reads only a 1,000-record prefix of the provided file.
- Native form/lifecycle smoke tests supplement unit/integration tests. They do not replace human visual acceptance of every theme, DPI and screen size.
- GitHub Actions remains **disabled by request**. [CI_ASSESSMENT.md](CI_ASSESSMENT.md) contains the recommendation, not an active workflow.
