# Opening Branch Generator

Set up the initial sequence, choose the learning side and configure depth sections. The local engine explores plausible opponent replies and selects one learner response per position. Search depth is engine depth; branch depth is distance in half-moves from the generation start. These are different controls.

Section parameters govern complexity, evaluation cutoffs, opponent loss, compression tolerance and optional variation among similarly evaluated learner moves. Zero learner variation preserves the deterministic best-response policy; learning compression has priority. Repeated positions can be shared as transpositions. Complexity/maximum-depth endpoints retain opponent-reply context.

The preview is a projection of generation progress, not the authoritative saved graph. Forecasts estimate workload; branching and transpositions can make real duration differ substantially. Workers and threads consume CPU/RAM; increase them cautiously.

Generated files use FORMAT.md version 2. Review generated recommendations before serious study. The engine's evaluation and “human-unfriendly” labels are heuristics, not guarantees about human difficulty.
