# 0.27.1-beta

Development branch: `dev/0.27.0`. Stable 0.26.5 remains unchanged. This beta adds keyboard board input, dataset-guided repertoire tools and an optional experimental Engine Lab.


## Changes in 0.27.1-beta

- Automatic Stockfish opponents now honor the requested depth instead of also imposing a 2.5-second move limit. Background bar/Top-3 searches keep their responsive time limits. Example measurement can run to the configured depths and can be cancelled.
- Preferred-move plans support both castling sides and both colors, move the rook, consume rights and support `O-O` keyboard entry. Real-play legality is still checked later by the engine/rules.
- Coverage is calculated in the background, displayed above the right board, and excludes the fixed starting prefix. Its result list uses one themed scrollbar with immediate scrolling.
- Engine Lab now has Setup / Training / Results, task cards, scalar/curve schedules, optional board grids, persistent engine identities, multiple runs and resumable checkpoints. A point-and-line score graph is shown during training and in results.
- Four methods and minimal templates: population evolution, Stockfish imitation, self-play actor-critic and NEAT. Three tensor model profiles include an 8-GB-card backpropagation profile verified with a bounded RTX 4060 Laptop GPU memory test. NEAT uses an independent CPU topology.
- Save a model, use it as the evaluation/opponent policy, open a separate custom game, or continue training. Stockfish remains permanent and continues validating generators and puzzles.
- See [Engine Lab documentation](../addons/engine-lab/README.md) for score definitions, memory measurements, configuration, dependencies and experimental limitations. Existing opening and dataset formats are unchanged.

## Keyboard board input

Press **Tab** while a playable board is active, type a move and press **Enter**. Ordinary text/numeric settings retain their usual focus behavior. The shared board matches available moves; existing mode handlers still decide whether a move is accepted. Read-only previews are not made playable.

- Simplified SAN ignores check/mate/capture annotations. `e4`, `Pe4`, `e2e4`, `ee24` and `2e4e` can identify the same pawn move. `Ped`, `exd`, `exd4` and `Pexd4#` work when the position disambiguates them. `Bea` restricts the bishop's source file to e and target file to a. Use `O-O`/`O-O-O` and `a8=N` for castling/underpromotion.
- The initial candidate is the visually lowest, leftmost destination, respecting board rotation. Arrow keys select a destination for the current piece; Shift switches between matching pieces. Candidate dots and a destination outline show the choice. Esc closes input.
- Invalid input does not play a move. Changing position clears keyboard candidates. Setup boards retain their unrestricted placement behavior; premoves use the existing premove candidate rules.

## Opening Coverage Score

In **Branch → Analysis**, coverage is calculated automatically from the currently built dataset index. The score is 0–100 and measures represented opponent replies. Each observed reply is weighted by its absolute game count divided by `1 + floor(halfmove distance from generation start / 2)`. Early, frequent omissions therefore cost more. Own moves and the fixed, unbranched opening prefix are excluded. Transposed positions are counted once; positions without data are reported separately, not assigned invented frequencies.

The window ranks missing continuations by their immediate score contribution. Selecting a row opens a staged branch edit with that move played; adding nodes and confirming the edit remain explicit. This is an empirical repertoire index, not an Elo measure. Newly explored positions can change the denominator, so suggested gains describe the existing evaluated graph only. Generated openings now retain optional `generationStartFen` metadata; older openings remain readable and their fixed prefix is recognized without requiring labels.

## Opening Branch Generator

New per-section settings (both default off):

- **Common replies to continue:** retain the most frequent dataset opponent replies, up to this count and never above the section's complexity limit. They can survive a complexity cutoff; the maximum depth still applies.
- **Stop below game count:** stop exploring an observed opponent position below this count, retaining terminal counter-moves as usual.

The general **Minimum dataset evidence** defaults to 10 games. Below it, normal engine-only rules apply. These settings use the built index, not an uncommitted change to active datasets. Enabling them without a readable index reports an error. Estimates use the same policy and are invalidated when the index changes.

**Preferred moves** plans learner-side moves after the real starting sequence. Ghost pieces and numbered destinations illustrate the intended order. Opposing pieces may be crossed during planning; own pieces must first vacate the path. This does not alter the legal starting position. The generator later checks actual legality and engine scores.

A preference must exceed the learner-side evaluation floor (default −0.5) and lose at most 0.1 pawns against the best move. If unavailable, later executable preferences are considered; otherwise the best engine move is used. Three consecutive deviations end preferences for that path; a matching preference resets the count. Transposition compression has priority. At a merged position, the first queued path's preference progress supplies the continuation, preserving a single answer per position. Queue jobs preserve their own plans and settings.

## Engine Lab

Open through **Settings → General**, below the guided overview. Esc returns to the normal menu. It is a separate optional add-on with a headless JSON-lines host and the same chess rules as the desktop app. No Python, CUDA or model download is required for normal chess training ; the headless position protocol is also usable without Python.

See [setup, architecture and training defaults](../addons/engine-lab/README.md) and the [protocol](../addons/engine-lab/PROTOCOL.md). The add-on offers three tensor architectures, disk-backed population members, teacher imitation, self-play actor-critic and NEAT topology evolution. Balanced matches, shared tasks, curve schedules, score graphs and optional Stockfish benchmarks are described in its README. Checkpoints and experiments stay in private application data. No trained strength or Elo claim is made.

## Compatibility and verification

Existing opening JSON files and dataset indexes remain supported. New generator options have disabled/backward-compatible defaults. The app does not migrate or publish personal games, settings or training weights. English and German strings/tooltips are included.

Validation includes the existing local rules/UI regression suite and `tools/Test.ps1 -EngineGeneration`, synthetic public checks, all four Python training methods and checkpoint continuation, CUDA population tasks and gradient smoke runs, and the live result/save/default-engine/delete workflow in an isolated profile. Full 100-round training has **not** been run; the training scaffold is experimental. CI adds deterministic 0.27 checks and dependency-free Python schedule tests, without GPU installation, live dataset downloads or long training.
