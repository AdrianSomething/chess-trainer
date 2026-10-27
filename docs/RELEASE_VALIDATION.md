# Validation records

## 0.27.1-beta — 2026-10-27

Local verification completed the existing rules/UI/dataset suite and real-engine checks through `tools/Test.ps1 -EngineGeneration -NoRestore`. Additional synthetic public regressions cover planned castling, fixed-prefix coverage and Engine Lab construction. Ten dependency-free Python checks cover schedules, curve validation, portable NEAT actions and the four minimal templates.

All four training methods completed bounded local runs and checkpoint continuation. CUDA checks exercised tournament/Stockfish/mate/teacher tasks and gradient methods. An isolated Windows UI harness verified Setup / Training / Results, a real score point, model saving, default-engine inference, deletion and Stockfish fallback; coverage calculation and the result window were also checked. These checks do not access the maintainer’s games or normal profile.

The large backpropagation profile completed two FP32 AdamW updates at batch size 16 on an 8 GB RTX 4060 Laptop GPU: peak tensor allocation 6,648,979,456 bytes; peak PyTorch reservation 6,677,331,968 bytes. This is a memory smoke test, not a 100-round training or strength benchmark. Driver/display allocations and other programs are additional. See the [Engine Lab documentation](../addons/engine-lab/README.md) for experimental boundaries.

## Historical publication baseline — 0.24.0

Validation date: 2026-10-18. These are local checks, not GitHub CI or an independent audit.

## Functional baseline

The preceding publication preparation completed the local rules/schema checks, 1,033 UI/regression PASS records and 127 real-engine PASS records. The public-source changes for 0.24.0 concern version metadata, documentation, supplied screenshots and prompt-record privacy; no chess behavior is intentionally changed.

Both bundled graphs passed the application's legality/reference validator: 714 learner cards for the English example and 383 for the Pirc example. Fresh-profile checks verified English defaults, exactly one synthetic game, persistent demo deletion, preservation of existing libraries and an off-by-default statistics demo in Chess Vision settings.

## Publication checks

Source and release exports exclude private profiles, real PGN archives, build logs and historical work folders. Credential/path pattern scanning complements an explicit file selection. The maintainer's supplied screenshots were visually reviewed; screenshot text/EXIF metadata is omitted from the public copies. PROMPTS.md preserves historical German prompts with identifying crash paths and report identifiers redacted.

Stockfish's binary, pinned source and embedded network were verified as documented in LICENSE_COMPLIANCE.md. No executable is tracked in this source repository.

## Limitations

The previous build initially could not reach NuGet vulnerability information; a subsequent package audit reached nuget.org and reported no vulnerable packages. This project has no ordinary third-party PackageReferences; that result does not audit native Stockfish, Windows or every runtime component.

Tests cannot prove every interaction, training recommendation or hardware combination. The regression harness remains outside this repository pending a separate CI/test migration. Releases are unsigned. No independent bit-identical rebuild or blanket legal guarantee is claimed.

Original editable Figma documents and audio download certificates/dates were not supplied; their provenance and licensing boundaries are documented rather than reconstructed.
